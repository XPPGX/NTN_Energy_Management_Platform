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
using MudBlazor.Charts;

namespace demoVer.Services
{
    public class ObservableModule
    {
        public event Action? OnUpdated;

        protected void NotifyChanged() => OnUpdated?.Invoke();
    }

    public class DataCenter : IDisposable
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
        private readonly SqlProcessor _sqlProcessor;
        //[Declare] variables shared in whole process
        public Battery_DataSetting_Module Battery {get; set;}
        public INV_DataSetting_Module INV{get; set;}


        //[DEBUG]模擬資料
        private List<string> _labels = new();
        private List<double> _values = new();
        private List<double> _values2 = new();
        //[DEBUG]外部元件可訂閱此事件來接收圖表刷新通知
        public event Func<Task>? OnChartDataUpdated;
        // public bool ApiSendFlag = false;
        
        
        protected string basePath = "";
        private readonly SemaphoreSlim _DataCenter_SaveData_lock = new(1, 1);
        public string oldMdlName = "";

        public DataCenter(  CommonData commonData, 
                            HeartbeatService heartbeat, 
                            IHubContext<DataHub> hubContext,
                            DataChangeEventManager eventManager,
                            ApiManager apiManager,
                            GlobalVar globalVar,
                            IGroupsDataDecoder decoder,
                            SqlProcessor sqlProcessor)
        {
            _category = GetType().FullName!;

            _heartbeat      = heartbeat;
            _hubContext     = hubContext;
            _commonData     = commonData;
            _eventManager   = eventManager;
            _apiManager     = apiManager;
            _globalVar      = globalVar;
            _decoder        = decoder;
            _sqlProcessor   = sqlProcessor;
            
            Battery     = new Battery_DataSetting_Module(_commonData);
            INV         = new INV_DataSetting_Module(_commonData);

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

            _heartbeat.OnTick += async () => 
            {
                NotifyChartSubscribers();
                await RefreshAllAsync();
            };
        }
        

        public bool initSys()
        {
            //0. Read base path
            read_basePath();
            //1. Init file paths
            AppLogger.Init_AppLogger(basePath);
            Console.WriteLine("[DataCenter] Init : AppLogger done");
            
            _sqlProcessor.Init(basePath);
            Console.WriteLine("[DataCenter] Init : SqlProcessor done");


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

            Console.WriteLine($"Application Path : ${basePath}");
        }

        public string get_basePath()
        {
            return basePath;
        }

        public async Task DataCenter_MdlNameChange_Task()
        {
            if(string.IsNullOrEmpty(_globalVar.Sys_ModelName))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][DataCenter_MdlNameChange_Task] _globalVar.Sys_ModelName is Null or Empty", AppLogLevel.Trace);
                return;
            }

            if(string.Equals(oldMdlName, _globalVar.Sys_ModelName, StringComparison.OrdinalIgnoreCase))
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][DataCenter_MdlNameChange_Task] oldMdlName == _globalVar.Sys_ModelName", AppLogLevel.Trace);
                
                _commonData.ScalingFactor_OK = true;
                
                return;
            }
            else
            {
                AppLogger.Log_To_File_log(_category, $"[DataCenter][DataCenter_MdlNameChange_Task] oldMdlName = {oldMdlName}, _globalVar.Sys_ModelName = {_globalVar.Sys_ModelName}", AppLogLevel.Trace);
            }
        }

        private async Task RefreshAllAsync()
        {
            await Task.CompletedTask;
        }

        #region DrawChartFunctions
        public void ApplyRealDataToChart(CHART_SETTING chartSetting)
        {
            try
            {
                int chartMaxDataCount = 30;
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
                    if (string.IsNullOrEmpty(line.Cmd))
                    {
                        AppLogger.Log_To_File_log(_category, $"[ApplyRealDataToChart] line.Cmd is null or empty", AppLogLevel.Debug);
                        continue;
                    }

                    bool AddEmptyData = false;

                    var oneDeviceData = _globalVar.Real_Devices_ReadData.Get_oneDevice_DataSnapshot(line.addr);
                    if (oneDeviceData == null)
                    {
                        AppLogger.Log_To_File_log(_category, $"[ApplyRealDataToChart] oneDeviceData is null for addr = {line.addr}", AppLogLevel.Debug);
                        AddEmptyData = true;
                    }

                    var cmdType = oneDeviceData?.GetCmdType(line.Cmd) ?? string.Empty;
                    if (!string.Equals(cmdType, "Numeric", StringComparison.OrdinalIgnoreCase))
                    {
                        AppLogger.Log_To_File_log(_category, $"[ApplyRealDataToChart] cmdType is not Numeric for Cmd = {line.Cmd}, cmdType = {cmdType}", AppLogLevel.Debug);
                        AddEmptyData = true;
                    }

                    if(AddEmptyData is true)
                    {
                        var newChartLineData = line.Data?.ToList() ?? new List<double?>();

                        // 限制資料長度，避免無限累積
                        if (newChartLineData.Count >= chartMaxDataCount)
                        {
                            // 移除舊資料，保持固定長度
                            int excessCount = newChartLineData.Count - chartMaxDataCount + 1;
                            if (excessCount > 0)
                            {
                                newChartLineData.RemoveRange(0, excessCount);
                            }
                        }

                        newChartLineData?.Add(null);
                        if (newChartLineData is not null)
                        {
                            line.Data = newChartLineData.ToArray();
                        }
                    }
                    else
                    {
                        var realValue = oneDeviceData?.parseCmdData(line.Cmd);
                        if (realValue is not null)
                        {
                            double? RealValue_double = (double?)realValue;
                            var newChartLineData = line.Data?.ToList() ?? new List<double?>();

                            // 限制資料長度，避免無限累積
                            if (newChartLineData.Count >= chartMaxDataCount)
                            {
                                // 移除舊資料，保持固定長度
                                int excessCount = newChartLineData.Count - chartMaxDataCount + 1;
                                if (excessCount > 0)
                                {
                                    newChartLineData.RemoveRange(0, excessCount);
                                }
                            }

                            newChartLineData?.Add(RealValue_double);
                            if (newChartLineData is not null)
                            {
                                line.Data = newChartLineData.ToArray();
                            }
                        }
                    }
                }

                // 更新 X 軸標籤（時間戳），同樣限制長度
                var labels = chartSetting.Labels?.ToList() ?? new List<string>();
                if (labels.Count >= chartMaxDataCount)
                {
                    // 同步移除舊的標籤
                    int excessCount = labels.Count - chartMaxDataCount + 1;
                    if (excessCount > 0)
                    {
                        labels.RemoveRange(0, excessCount);
                    }
                }

                labels.Add(DateTime.Now.ToString("HH:mm:ss"));
                chartSetting.Labels = labels.ToArray();
            }
            catch (Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApplyRealDataToChart] Error: {ex.Message}", AppLogLevel.Error);
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

        public void Dispose()
        {
            // 清理 HeartbeatService 的事件訂閱
            _heartbeat.OnTick -= async () => 
            {
                NotifyChartSubscribers();
                await RefreshAllAsync();
            };
        }
        #endregion //DrawChartFunctions
    }
}