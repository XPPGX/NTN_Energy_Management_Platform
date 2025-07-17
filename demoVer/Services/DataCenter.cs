using demoVer.Models;
using demoVer.Broadcast;
using Microsoft.AspNetCore.SignalR;

using System.IO;
using System.Text.Json;

namespace demoVer.Services
{
    public class ObservableModule
    {
        public event Action? OnUpdated;

        protected void NotifyChanged() => OnUpdated?.Invoke();
    }

    public class DataCenter
    {
        private readonly HeartbeatService _heartbeat;
        private readonly IHubContext<DataHub> _hubContext; //SignalR Hub
        private readonly CommonData _commonData;
        private readonly DataChangeEventManager _eventManager;

        //[DEBUG]模擬資料
        private List<string> _labels = new();
        private List<double> _values = new();
        private List<double> _values2 = new();
        //[DEBUG]外部元件可訂閱此事件來接收圖表刷新通知
        public event Func<Task>? OnChartDataUpdated;

        public Battery_DataSetting_Module Battery {get; set;}
        public INV_DataSetting_Module INV{get; set;}
        public allDevice_ModData MOD_DATA{get; set;}
        public Dictionary<string, ModCommandData> INV_MOD_READ_Data {get;} = new();

        public DataCenter(  CommonData commonData, 
                            HeartbeatService heartbeat, 
                            IHubContext<DataHub> hubContext,
                            DataChangeEventManager eventManager)
        {
            _heartbeat  = heartbeat;
            _hubContext = hubContext;
            _commonData = commonData;
            _eventManager = eventManager;
            
            MOD_DATA = new allDevice_ModData(_eventManager);

            Battery     = new Battery_DataSetting_Module(_commonData);
            Battery.UpdateFrom(new Battery_InitData()); //接收初始值
            INV         = new INV_DataSetting_Module(_commonData);
            INV.UpdateFrom(new INV_InitData()); //接收初始值



            _heartbeat.OnTick += async () => 
            {
                AddSimulatedData();
                NotifyChartSubscribers();

                Update_Read_Data();

                await RefreshAllAsync();
            };
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
        public void ApplySimulatedDataToChart(CHART_SETTING chartSetting)
        {
            chartSetting.Labels = _labels.ToArray();
            if (chartSetting.chart_single_data_lines.Count == 1)
            {
                chartSetting.chart_single_data_lines[0].Data = _values.ToArray();
            }
            else if(chartSetting.chart_single_data_lines.Count == 2)
            {
                chartSetting.chart_single_data_lines[0].Data = _values.ToArray();
                chartSetting.chart_single_data_lines[1].Data = _values2.ToArray();
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
                Console.WriteLine(rawJsonData_String);
                return rawJsonData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON read fail: {ex.Message}");
                return new List<ModSingleRawCommandFormat>();
            }
        }

        public void LoadSingleDeviceINVData(List<ModSingleRawCommandFormat> rawList)
        {
            INV_MOD_READ_Data.Clear();

            foreach (var cmd in rawList)
            {
                if (string.IsNullOrEmpty(cmd.commandName)) continue;

                var data = new ModCommandData()
                {
                    Data = cmd.data,
                    Scaling = cmd.scaling,
                    BaseUnit = cmd.baseUnit,
                    DataFormat = cmd.dataFormat,
                    Split = cmd.split
                };

                INV_MOD_READ_Data[cmd.commandName] = data;
            }
        }

        

        private void Update_Read_Data()
        {
            const uint targetAddr = 0;
            List<string> cmds = new List<string>();
            cmds.Add("READ_VIN");
            cmds.Add("READ_IIN");
            
            foreach(var cmd_str in cmds)
            {
                var cmd = MOD_DATA.GetCommandData(targetAddr, cmd_str);
                if (cmd != null)
                {
                    // 模擬數值，例如電壓 200~300V 間浮動
                    float simulatedValue = 200f + (float)(new Random().NextDouble() * 100);

                    // 將 float → ushort → byte[]
                    ushort scaled = (ushort)(simulatedValue / cmd.Scaling);
                    var bytes = BitConverter.GetBytes(scaled);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    Console.WriteLine($"模擬[{cmd_str}]變更為: {simulatedValue} → Bytes: [{bytes[0]}, {bytes[1]}]");

                    // 寫入並觸發 OnChanged（如果不同的話）
                    cmd.Data = new List<byte> { bytes[0], bytes[1] };
                }
                else
                {
                    Console.WriteLine($"找不到 addr = {targetAddr} 的 {cmd_str}");
                }
            }
        }
        #endregion API_Simulate
    }
}