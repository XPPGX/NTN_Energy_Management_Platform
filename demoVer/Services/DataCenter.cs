using demoVer.Models;
using demoVer.Broadcast;
using Microsoft.AspNetCore.SignalR;

using System.IO;
using System.Text.Json;
using demoVer.Interfaces;

namespace demoVer.Services
{
    public class ObservableModule
    {
        public event Action? OnUpdated;

        protected void NotifyChanged() => OnUpdated?.Invoke();
    }

    public class DataCenter
    {
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
        public allDevice_ModData MOD_DATA{get; set;}
        // public allDevice_Data Device_ReadData {get; set;} //Get from framework via READ_API


        //[DEBUG]模擬資料
        private List<string> _labels = new();
        private List<double> _values = new();
        private List<double> _values2 = new();
        //[DEBUG]外部元件可訂閱此事件來接收圖表刷新通知
        public event Func<Task>? OnChartDataUpdated;

        

        // public bool ApiSendFlag = false;
        
        public DataCenter(  CommonData commonData, 
                            HeartbeatService heartbeat, 
                            IHubContext<DataHub> hubContext,
                            DataChangeEventManager eventManager,
                            ApiManager apiManager,
                            GlobalVar globalVar,
                            IGroupsDataDecoder decoder)
        {
            _heartbeat  = heartbeat;
            _hubContext = hubContext;
            _commonData = commonData;
            _eventManager = eventManager;
            _apiManager = apiManager;
            _globalVar = globalVar;
            _decoder = decoder;

            MOD_DATA = new allDevice_ModData(_eventManager);
            // Device_ReadData = new allDevice_Data();
            Battery     = new Battery_DataSetting_Module(_commonData);
            Battery.UpdateFrom(new Battery_InitData()); //接收初始值
            INV         = new INV_DataSetting_Module(_commonData);
            INV.UpdateFrom(new INV_InitData()); //接收初始值
            uint pollingCounter = 0;
            bool ApiReadFlag = false;
            uint counter = 0;
            bool counterEnable = true;
            _heartbeat.OnTick += async () => 
            {
                // AddSimulatedData();
                NotifyChartSubscribers();

                if(ApiReadFlag == false)
                {
                    // var result = await _apiManager.ReadSingleINV("CAN1", 0);
                    // string parsedJson = JsonSerializer.Serialize(result, new JsonSerializerOptions {WriteIndented = true});
                    // Console.WriteLine("[Parsed Result] = " + parsedJson);
                    // MOD_DATA.Read_oneDevice_ModData(0, result);

                    // READ_API_TEST();
                    
                    
                    ApiReadFlag = true;
                }

                if(counter == 3)
                {
                    counter = 0;
                    // LocalDataChange_Test();
                }
                counter ++;
                
                // if(pollingCounter == 10)
                // {
                //     _globalVar.Debug_Flag = false;
                //     pollingCounter = 0;
                // }
                
                // pollingCounter ++;

                // Update_Read_Data();
                await RefreshAllAsync();
            };
        }

        public async Task READ_API_TEST()
        {
            var result = await _apiManager.apiRead_OneDeviceData("CAN1", 0);
            string parsedJson = JsonSerializer.Serialize(result, new JsonSerializerOptions {WriteIndented = true});
            _globalVar.Device_ReadData.Read_oneDevice_Data(0, result);
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
                Console.WriteLine($"[DataCenter][ApplyRealDataToChart] isMobile = {isMobile}, hash = {this.GetHashCode}");
                int chartMaxDataCount = (isMobile == true) ? 10 : 30;
                // 確保有線條
                if (chartSetting.chart_single_data_lines == null || chartSetting.chart_single_data_lines.Count == 0)
                    return;

                //同步兩條線資料長度
                int nowDataLength = 0;
                nowDataLength = chartSetting.chart_single_data_lines[0].Data?.Length ?? 0;
                Console.WriteLine($"[DataCenter][ApplyRealDataToChart] nowDataLength = {nowDataLength}");
                

                //更新每條線的資料
                foreach (var line in chartSetting.chart_single_data_lines)
                {
                    if (string.IsNullOrEmpty(line.Cmd)) continue;

                    // 從 Device_ReadData 抓這個 addr + Cmd 的資料
                    var groups = _globalVar.Device_ReadData.GetCommandGroups(line.addr, line.Cmd);
                    if (groups != null)
                    {
                        var decoded = _decoder.Decode(groups, line.Cmd);
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
                    // var cmd = MOD_DATA.GetCommandData(targetAddr, cmd_str);
                    // if (cmd != null)
                    // {
                    //     // 模擬數值，例如電壓 200~300V 間浮動
                    //     float simulatedValue = 200f + (float)(new Random().NextDouble() * 100);

                    //     // 將 float → ushort → byte[]
                    //     ushort scaled = (ushort)(simulatedValue / cmd.Scaling);
                    //     var bytes = BitConverter.GetBytes(scaled);

                    //     if (!BitConverter.IsLittleEndian)
                    //         Array.Reverse(bytes);

                    //     Console.WriteLine($"模擬[{cmd_str}]變更為: {simulatedValue} → Bytes: [{bytes[0]}, {bytes[1]}]");

                    //     // 寫入並觸發 OnChanged（如果不同的話）
                    //     cmd.Data = new List<byte> { bytes[0], bytes[1] };
                    // }
                    // else
                    // {
                    //     Console.WriteLine($"找不到 addr = {targetAddr} 的 {cmd_str}");
                    // }
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