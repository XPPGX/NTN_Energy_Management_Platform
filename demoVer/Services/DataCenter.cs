using demoVer.Models;
using demoVer.Broadcast;
using demoVer.Utils;
using Microsoft.AspNetCore.SignalR;

using System;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using demoVer.Interfaces;
using System.Runtime.InteropServices;

namespace demoVer.Services
{
    public class ObservableModule
    {
        public event Action? OnUpdated;

        protected void NotifyChanged() => OnUpdated?.Invoke();
    }

    public class DataCenter
    {
        //For log
        private string _category = "";
        //[Injection] services instances

        private readonly HeartbeatService _heartbeat;
        private readonly IHubContext<DataHub> _hubContext; //SignalR Hub
        private readonly CommonData _commonData;
        private readonly DataChangeEventManager _eventManager;
        private readonly ApiManager _apiManager;
        private readonly GlobalVar _globalVar;
        private readonly IGroupsDataDecoder _decoder;
        //[Declare] variables shared in whole process
        public Battery_DataSetting_Module Battery {get; set;}
        public INV_DataSetting_Module INV{get; set;}
        // public allDevice_Data Device_ReadData {get; set;} //Get from framework via READ_API


        //[DEBUG]模擬資料
        private List<string> _labels = new();
        private List<double> _values = new();
        private List<double> _values2 = new();
        //[DEBUG]外部元件可訂閱此事件來接收圖表刷新通知
        public event Func<Task>? OnChartDataUpdated;
        // public bool ApiSendFlag = false;
        
        
        private string basePath = "";
        private readonly SemaphoreSlim _DataCenter_SaveData_lock = new(1, 1);
        public string oldMdlName = "";

        public DataCenter(  CommonData commonData, 
                            HeartbeatService heartbeat, 
                            IHubContext<DataHub> hubContext,
                            DataChangeEventManager eventManager,
                            ApiManager apiManager,
                            GlobalVar globalVar,
                            IGroupsDataDecoder decoder)
        {
            _category = GetType().FullName!;

            _heartbeat  = heartbeat;
            _hubContext = hubContext;
            _commonData = commonData;
            _eventManager = eventManager;
            _apiManager = apiManager;
            _globalVar = globalVar;
            _decoder = decoder;

            // Device_ReadData = new allDevice_Data();
            
            Battery     = new Battery_DataSetting_Module(_commonData);
            INV         = new INV_DataSetting_Module(_commonData);

            uint pollingCounter = 0;
            bool ApiReadFlag = false;
            uint counter = 0;
            bool counterEnable = true;

            bool initSysFromJson = initSys();
            if(initSysFromJson is true)
            {
                _globalVar.nowInitStage = InitStage.Done;
            }         
            else
            {
                _globalVar.nowInitStage = InitStage.FromDevice;
            }
            // _globalVar.InitSysOK = initSys();

            _heartbeat.OnTick += async () => 
            {
                // AddSimulatedData();
                NotifyChartSubscribers();

                if(ApiReadFlag == false)
                {
                    // var result = await _apiManager.ReadSingleINV("CAN1", 0);
                    // string parsedJson = JsonSerializer.Serialize(result, new JsonSerializerOptions {WriteIndented = true});
                    // Console.WriteLine("[Parsed Result] = " + parsedJson);

                    // READ_API_TEST();
                    ApiReadFlag = true;
                }

                if(counter == 3)
                {
                    counter = 0;
                    // LocalDataChange_Test();
                }
                counter ++;

                await RefreshAllAsync();
            };

            //MdlName不一樣的時候會觸發這個Event
            _globalVar.Sys_ModelName_OnChanged += DataCenter_MdlNameChange_Task;
        }

        public bool initSys()
        {
            AppLogger.Log_To_File_log(_category, $"[DataCenter][initSys] Start...", AppLogLevel.Trace);

            //0. Read base path
            read_basePath();
            
            //1. Read ModelName
            bool read_mdlName_succ = read_OldMdlName_FromJson();
            if(read_mdlName_succ is false)
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][initSys] read_mdlName FAIL", AppLogLevel.Trace);
                return false;
            }

            //2. Read Scaling Factor 
            bool read_Scaling_succ = read_OldScaling_FromJson();
            if(read_Scaling_succ is false)
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][initSys] read Scaling FAIL", AppLogLevel.Trace);
                return false;
            }
            
            //3. Read INV setting
            bool read_INV_Setting_succ = read_OldINVSetting_FromJson();
            if(read_INV_Setting_succ is false)
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][initSys] read INV_Setting FAIL", AppLogLevel.Trace);
                return false;
            }
            //4. Read BAT setting
            bool read_BAT_Setting_succ = read_OldBATSetting_FromJson();
            if(read_BAT_Setting_succ is false)
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][initSys] read BAT_Setting FAIL", AppLogLevel.Trace);
                return false;
            }
            // Battery.UpdateFrom(new Battery_InitData());

            AppLogger.Log_To_File_log(_category, $"[DataCenter][initSys] Done...", AppLogLevel.Trace);
            return true;
        }

        public void read_basePath()
        {
            //1. 確認當前OS類別，賦值給basePath
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = "";
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                basePath = "/userdata/CMU3/UserSetting/5040";
                Directory.CreateDirectory(basePath);
            }
            else
            {
                basePath = "";
            }
        }


        public bool read_OldMdlName_FromJson()
        {   
            string AppDataDirectory = Path.Combine(basePath, "App_Data");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldMdlName_FromJson] After Combine : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            AppDataDirectory = Path.GetFullPath(AppDataDirectory);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldMdlName_FromJson] After GetFullPath : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            string AppData_SysInfo = Path.Combine(AppDataDirectory, "LastTime_SysInfo.json");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldMdlName_FromJson] After GetFullPath : AppData_SysInfo = {AppData_SysInfo}", AppLogLevel.Trace);
            
            if(!File.Exists(AppData_SysInfo))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldMdlName_FromJson]{AppData_SysInfo} 檔案不存在", AppLogLevel.Trace);
                return false;
            }

            string json = File.ReadAllText(AppData_SysInfo);
            var options = new JsonSerializerOptions{PropertyNameCaseInsensitive = true};

            var sysInitData = JsonSerializer.Deserialize<Sys_InitData>(json, options) ?? new Sys_InitData();
            if(string.IsNullOrEmpty(sysInitData.mdlName))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldMdlName_FromJson]{AppData_SysInfo} 內容不存在", AppLogLevel.Trace);
                return false;
            }

            oldMdlName = sysInitData.mdlName;
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldMdlName_FromJson] oldMdlName = {oldMdlName}", AppLogLevel.Trace);
            return true;
        }

        public bool read_OldScaling_FromJson()
        {
            string AppDataDirectory = Path.Combine(basePath, "App_Data");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldScaling_FromJson] After Combine : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            AppDataDirectory = Path.GetFullPath(AppDataDirectory);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldScaling_FromJson] After GetFullPath : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);
            
            string AppData_Scaling = Path.Combine(AppDataDirectory, "LastTime_ScalingFactors.json");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldScaling_FromJson] After GetFullPath : AppData_Scaling = {AppData_Scaling}", AppLogLevel.Trace);
            
            if(!File.Exists(AppData_Scaling))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldScaling_FromJson] {AppData_Scaling} 檔案不存在", AppLogLevel.Trace);
                return false;
            }

            string json = File.ReadAllText(AppData_Scaling);
            
            var dict = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
            if(dict is null)
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldScaling_FromJson] {AppData_Scaling} 內容不存在", AppLogLevel.Trace);
                return false;    
            }

            var tmpConcurrentDict = new ConcurrentDictionary<string, double>(dict);
            _commonData.UpdateScalingFactors(tmpConcurrentDict);
            
            return true;
        }

        public bool read_OldINVSetting_FromJson()
        {
            string AppDataDirectory = Path.Combine(basePath, "App_Data");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldINVSetting_FromJson] After Combine : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            AppDataDirectory = Path.GetFullPath(AppDataDirectory);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldINVSetting_FromJson] After GetFullPath : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            string AppData_INVSetting = Path.Combine(AppDataDirectory, "INV_Setting_LastTime.json");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldINVSetting_FromJson] After GetFullPath : AppData_INVSetting = {AppData_INVSetting}", AppLogLevel.Trace);

            if(!File.Exists(AppData_INVSetting))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldINVSetting_FromJson] {AppData_INVSetting} 檔案不存在", AppLogLevel.Trace);
                return false;
            }

            INV.UpdateFrom(INV_InitData.LoadFromJsonFile(AppData_INVSetting));
            return true;
        }

        public bool read_OldBATSetting_FromJson()
        {
            string AppDataDirectory = Path.Combine(basePath, "App_Data");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldBATSetting_FromJson] After Combine : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            AppDataDirectory = Path.GetFullPath(AppDataDirectory);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldBATSetting_FromJson] After GetFullPath : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            string AppData_BATSetting = Path.Combine(AppDataDirectory, "BAT_Setting_LastTime.json");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldINVSetting_FromJson] After GetFullPath : AppData_BATSetting = {AppData_BATSetting}", AppLogLevel.Trace);

            if(!File.Exists(AppData_BATSetting))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][read_OldBATSetting_FromJson] {AppData_BATSetting} 檔案不存在", AppLogLevel.Trace);
                return false;
            }

            Battery.UpdateFrom(Battery_InitData.LoadFromJsonFile(AppData_BATSetting));
            return true;
        }

        public async Task DataCenter_MdlNameChange_Task()
        {
            if(string.Equals(oldMdlName, _globalVar.Sys_ModelName, StringComparison.OrdinalIgnoreCase))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][DataCenter_MdlNameChange_Task] oldMdlName == _globalVar.Sys_ModelName", AppLogLevel.Trace);
                
                _commonData.ScalingFactor_OK = true;
                
                return;
            }
            AppLogger.Log_To_File_log(_category, $"[DataCenter][DataCenter_MdlNameChange_Task] oldMdlName = {oldMdlName}, _globalVar.Sys_ModelName = {_globalVar.Sys_ModelName}", AppLogLevel.Trace);
            
            //重新 Loading
            if(!string.IsNullOrEmpty(_globalVar.Sys_ModelName))
            {
                //1. SysInfo
                saveSysInfo_To_JsonFile();
                //2. ScalingFactor
                await _commonData.modelNameChange_Task(); //讀新的 Scaling 到記憶體
                saveScaling_To_JsonFile(); //把記憶體內新的 Scaling 存到Json file
                //3. INV setting
                //待辦(等上下限API開通)
                //4. BAT setting
                //待辦(等上下限API開通)
            }
            
        }

        public void saveSysInfo_To_JsonFile()
        {
            string AppDataDirectory = Path.Combine(basePath, "App_Data");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][saveSysInfo_To_JsonFile] After Combine : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);
            
            AppDataDirectory = Path.GetFullPath(AppDataDirectory);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][saveSysInfo_To_JsonFile] After GetFullPath : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            string sysInfo_FilePath = Path.Combine(AppDataDirectory, "LastTime_SysInfo.json");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][saveSysInfo_To_JsonFile] After GetFullPath : sysInfo_FilePath = {sysInfo_FilePath}", AppLogLevel.Trace);

            oldMdlName = _globalVar.Sys_ModelName;

            var sysInfo = new Sys_InitData();
            sysInfo.mdlName = oldMdlName;

            if(!Directory.Exists(AppDataDirectory))
            {
                Directory.CreateDirectory(AppDataDirectory);
            }

            var options = new JsonSerializerOptions{WriteIndented = true};
            string json = JsonSerializer.Serialize(sysInfo, options);
            
            File.WriteAllText(sysInfo_FilePath, json);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][saveSysInfo_To_JsonFile] save sysInfo to JsonFile, _globalVar.Sys_ModelName = {_globalVar.Sys_ModelName}", AppLogLevel.Trace);
        }

        public void saveScaling_To_JsonFile()
        {
            string AppDataDirectory = Path.Combine(basePath, "App_Data");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][saveScaling_To_JsonFile] After Combine : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);
            
            AppDataDirectory = Path.GetFullPath(AppDataDirectory);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][saveScaling_To_JsonFile] After GetFullPath : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            string scaling_FilePath = Path.Combine(AppDataDirectory, "LastTime_ScalingFactors.json");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][saveScaling_To_JsonFile] After GetFullPath : scaling_FilePath = {scaling_FilePath}", AppLogLevel.Trace);

            var scaling_factors = _commonData._ScalingFactors;
            
            if(!Directory.Exists(AppDataDirectory))
            {
                Directory.CreateDirectory(AppDataDirectory);
            }

            var options = new JsonSerializerOptions{WriteIndented = true};
            string json = JsonSerializer.Serialize(scaling_factors, options);

            File.WriteAllText(scaling_FilePath, json);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][saveScaling_To_JsonFile] save Scaling to JsonFile", AppLogLevel.Trace);
        }


        public async Task<bool> save_INVSetting(INVSetting Data)
        {
            if(!_DataCenter_SaveData_lock.Wait(0))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting] There is someone using writing_lock , Skip", AppLogLevel.Trace);
                return false;
            }

            try
            {
                //存到記憶體
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting] Saving to memory...", AppLogLevel.Trace);
                bool memory_changed = INV.SaveSettingData(Data);
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting] Saving to memory...done", AppLogLevel.Trace);
                if(memory_changed is true)
                {
                    //存到JsonFile
                    AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting] Saving to Json...", AppLogLevel.Trace);
                    save_INVSetting_To_JsonFile();
                    AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting] Saving to Json...done", AppLogLevel.Trace);
                    return true;
                }
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting] Saving Failed, memory_changed = {memory_changed}", AppLogLevel.Trace);
                return false;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting] Error : {e}", AppLogLevel.Warning);
                return false;
            }
            finally
            {
                //釋放鎖
                _DataCenter_SaveData_lock.Release();
            }
        }

        public void save_INVSetting_To_JsonFile()
        {
            string AppDataDirectory = Path.Combine(basePath, "App_Data");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting_To_JsonFile] After Combine : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);
            
            AppDataDirectory = Path.GetFullPath(AppDataDirectory);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting_To_JsonFile] After GetFullPath : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            string INV_SettingFilePath = Path.Combine(AppDataDirectory, "INV_Setting_LastTime.json");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][save_INVSetting_To_JsonFile] After GetFullPath : INV_SettingFilePath = {INV_SettingFilePath}", AppLogLevel.Trace);

            var dataToJson = INV.ToINV_InitData();
            
            if(!Directory.Exists(AppDataDirectory))
            {
                Directory.CreateDirectory(AppDataDirectory);
            }

            var options = new JsonSerializerOptions{WriteIndented = true};
            string json = JsonSerializer.Serialize(dataToJson, options);

            File.WriteAllText(INV_SettingFilePath, json);   
        }

        public async Task<bool> save_BATSetting(BatterySetting Data)
        {
            if(!_DataCenter_SaveData_lock.Wait(0))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting] There is someone using writing_lock , Skip", AppLogLevel.Trace);
                return false;
            }

            try
            {
                //存到記憶體
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting] Saving to memory...", AppLogLevel.Trace);
                bool memory_changed = Battery.SaveSettingData(Data);
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting] Saving to memory...done", AppLogLevel.Trace);
                if(memory_changed is true)
                {
                    //存到JsonFile
                    AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting] Saving to Json...", AppLogLevel.Trace);
                    save_BATSetting_To_JsonFile();
                    AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting] Saving to Json...done", AppLogLevel.Trace);
                    return true;
                }
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting] Saving Failed, memory_changed = {memory_changed}", AppLogLevel.Trace);
                return false;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting] Error : {e}", AppLogLevel.Trace);
                return false;
            }
            finally
            {
                //釋放鎖
                _DataCenter_SaveData_lock.Release();
            }
        }

        public void save_BATSetting_To_JsonFile()
        {
            string AppDataDirectory = Path.Combine(basePath, "App_Data");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting_To_JsonFile] After Combine : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);
            
            AppDataDirectory = Path.GetFullPath(AppDataDirectory);
            AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting_To_JsonFile] After GetFullPath : AppDataDirectory = {AppDataDirectory}", AppLogLevel.Trace);

            string BAT_SettingFilePath = Path.Combine(AppDataDirectory, "BAT_Setting_LastTime.json");
            AppLogger.Log_To_File_log(_category, $"[DataCenter][save_BATSetting_To_JsonFile] After GetFullPath : BAT_SettingFilePath = {BAT_SettingFilePath}", AppLogLevel.Trace);

            var dataToJson = Battery.ToBAT_InitData();
            
            if(!Directory.Exists(AppDataDirectory))
            {
                Directory.CreateDirectory(AppDataDirectory);
            }

            var options = new JsonSerializerOptions{WriteIndented = true};
            string json = JsonSerializer.Serialize(dataToJson, options);

            File.WriteAllText(BAT_SettingFilePath, json);   
        }

        public async Task READ_API_TEST()
        {
            // var result = await _apiManager.apiRead_OneDeviceData("CAN1", 0);
            // string parsedJson = JsonSerializer.Serialize(result, new JsonSerializerOptions {WriteIndented = true});
            // _globalVar.Device_ReadData.Read_oneDevice_Data(0, result);

            // var result = await _apiManager.apiRead_SettingData("CAN1", "NTN-5K_CAN.json");
            // string parsedJson = JsonSerializer.Serialize(result, new JsonSerializerOptions {WriteIndented = true});
            
        }
        public async Task LocalDataChange_Test()
        {
            //CURVE_CV
            var tmp_groups = _globalVar.Device_ReadData.GetCommandGroups(0, "CURVE_CV");
            
            var rnd = new Random();
            var randomBytes = new List<byte>();

            for (int i = 0 ; i < 2 ; i ++)randomBytes.Add((byte)rnd.Next(0, 256));
            tmp_groups.Groups[0].Data = randomBytes;

            //MFR_MODEL
            tmp_groups = _globalVar.Device_ReadData.GetCommandGroups(0, "MFR_MODEL");
            randomBytes = new List<byte>();
            for(int i = 0 ; i < 6 ; i ++)randomBytes.Add((byte)rnd.Next(65, 90));
            tmp_groups.Groups[0].Data = randomBytes;
            randomBytes = new List<byte>();
            for(int i = 0 ; i < 6 ; i ++)randomBytes.Add((byte)rnd.Next(65, 90));
            tmp_groups.Groups[1].Data = randomBytes;

            //READ_AC_VOUT
            tmp_groups = _globalVar.Device_ReadData.GetCommandGroups(0, "READ_AC_VOUT");
            randomBytes = new List<byte>();
            for(int i = 0 ; i < 2 ; i ++)randomBytes.Add((byte)rnd.Next(1, 10));
            tmp_groups.Groups[0].Data = randomBytes;
            
            //READ_OP_VA
            tmp_groups = _globalVar.Device_ReadData.GetCommandGroups(0, "READ_OP_VA");
            randomBytes = new List<byte>();
            for(int i = 0 ; i < 2 ; i ++)randomBytes.Add((byte)rnd.Next(1, 10));
            tmp_groups.Groups[0].Data = randomBytes; 
            randomBytes = new List<byte>();
            for(int i = 0 ; i < 2 ; i ++)randomBytes.Add((byte)rnd.Next(1, 10));
            tmp_groups.Groups[1].Data = randomBytes;           
        }


        public async Task BroadcastBatteryChangeAsync()
        {
            var dto = Battery.ToDto();
            await _hubContext.Clients.All.SendAsync("BatteryUpdated", dto);
        }

        public async Task BroadcastINVChangeAsync()
        {
            var dto = INV.ToDto();
            await _hubContext.Clients.All.SendAsync("INVUpdated", dto);
        }

        private async Task RefreshAllAsync()
        {
            // Battery.NotifyChanged();
            // INV.Refresh();
            await Task.CompletedTask;
        }

        #region SimulationDataTransfer_And_DrawChart
        private void AddSimulatedData()
        {
            if (_labels.Count >= 10)
            {
                _labels.RemoveAt(0);
                _values.RemoveAt(0);
                _values2.RemoveAt(0);
            }

            _labels.Add(DateTime.Now.ToString("HH:mm:ss"));
            _values.Add(new Random().NextDouble() * 100);
            _values2.Add(new Random().NextDouble() * 100);
        }
        // public void ApplySimulatedDataToChart(CHART_SETTING chartSetting)
        // {
        //     chartSetting.Labels = _labels.ToArray();
        //     if (chartSetting.chart_single_data_lines.Count == 1)
        //     {
        //         chartSetting.chart_single_data_lines[0].Data = _values.ToArray();
        //     }
        //     else if(chartSetting.chart_single_data_lines.Count == 2)
        //     {
        //         chartSetting.chart_single_data_lines[0].Data = _values.ToArray();
        //         chartSetting.chart_single_data_lines[1].Data = _values2.ToArray();
        //     }
        // }
        public void ApplyRealDataToChart(CHART_SETTING chartSetting, bool isMobile)
        {
            try
            {
                // Console.WriteLine($"[DataCenter][ApplyRealDataToChart] isMobile = {isMobile}, hash = {this.GetHashCode}");
                int chartMaxDataCount = (isMobile == true) ? 10 : 30;
                // 確保有線條
                if (chartSetting.chart_single_data_lines == null || chartSetting.chart_single_data_lines.Count == 0)
                    return;

                //同步兩條線資料長度
                int nowDataLength = 0;
                nowDataLength = chartSetting.chart_single_data_lines[0].Data?.Length ?? 0;
                // Console.WriteLine($"[DataCenter][ApplyRealDataToChart] nowDataLength = {nowDataLength}");
                

                //更新每條線的資料
                foreach (var line in chartSetting.chart_single_data_lines)
                {
                    if (string.IsNullOrEmpty(line.Cmd)) continue;

                    // 從 Device_ReadData 抓這個 addr + Cmd 的資料
                    var groups = _globalVar.Device_ReadData.GetCommandGroups(line.addr, line.Cmd);
                    if (groups != null)
                    {
                        var decoded = _decoder.Decode(groups, line.Cmd, line.addr);
                        if (decoded != null && double.TryParse(decoded.ToString(), out var value))
                        {
                            // 把最新值 append 進 Data
                            var newData = line.Data?.ToList() ?? new List<double?>();

                            if(newData?.Count < nowDataLength)
                            {
                                for(int i = 0 ; i < nowDataLength ; i ++)
                                {
                                    newData.Add(null);
                                }
                            }

                            if (newData?.Count >= chartMaxDataCount) // 保持最多 50 筆 (可自行調整)
                                newData.RemoveAt(0);

                            

                            newData.Add(value);
                            line.Data = newData.ToArray();
                        }
                    }
                }

                // 更新 X 軸標籤（時間戳）
                var labels = chartSetting.Labels?.ToList() ?? new List<string>();
                if (labels.Count >= chartMaxDataCount)
                    labels.RemoveAt(0);

                labels.Add(DateTime.Now.ToString("HH:mm:ss"));
                chartSetting.Labels = labels.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApplyRealDataToChart] Error: {ex.Message}");
            }
        }

        // ✅ 通知 UI 重新繪製（例如透過 CardUpdateNotifier）
        private void NotifyChartSubscribers()
        {
            OnChartDataUpdated?.Invoke();
        }
        // ✅ 其他元件可以註冊來聽模擬資料更新
        public void RegisterChartListener(Func<Task> callback)
        {
            OnChartDataUpdated += callback;
        }

        public void UnregisterChartListener(Func<Task> callback)
        {
            OnChartDataUpdated -= callback;
        }
        #endregion

        #region API_Simulate
        public async Task <List<ModSingleRawCommandFormat>> LoadMockJsonAsync(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                List<ModSingleRawCommandFormat> rawJsonData = new List<ModSingleRawCommandFormat>();

                rawJsonData = JsonSerializer.Deserialize<List<ModSingleRawCommandFormat>>(json, options) ?? new();
                
                var rawJsonData_String = JsonSerializer.Serialize(rawJsonData, new JsonSerializerOptions{
                    WriteIndented = true
                });
                // Console.WriteLine(rawJsonData_String);
                return rawJsonData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON read fail: {ex.Message}");
                return new List<ModSingleRawCommandFormat>();
            }
        }

        private void Update_Read_Data()
        {
            try
            {
                const uint targetAddr = 4;
                List<string> cmds = new List<string>();
                cmds.Add("READ_TEMPERATURE_1");
                cmds.Add("READ_VIN");
                cmds.Add("READ_IIN");
                cmds.Add("READ_FREQ");
                // cmds.Add("READ_IIN");
                
                foreach(var cmd_str in cmds)
                {
                    var oneDevice_groups = _globalVar.Device_ReadData.GetCommandGroups(targetAddr, cmd_str);
                    byte val1 = (byte)(new Random().Next() * 10);
                    byte val2 = (byte)(new Random().Next() * 10);
                    oneDevice_groups.Groups[0].Data = new List<byte> {val1, val2};
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"[DataCenter][Update_Read_Data] Error : {e}");
            }
            
        }
        #endregion API_Simulate
    }
}