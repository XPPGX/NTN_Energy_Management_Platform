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

        //[DEBUG]模擬資料
        private List<string> _labels = new();
        private List<double> _values = new();
        private List<double> _values2 = new();
        //[DEBUG]外部元件可訂閱此事件來接收圖表刷新通知
        public event Func<Task>? OnChartDataUpdated;

        public Battery_DataSetting_Module Battery {get;}
        public Dictionary<string, CommandData> INV_MOD_READ_Data {get;} = new();
        // public List<Dictionary<string, CommandData>> INV_MOD_READ_AllData {get;} = new();


        public DataCenter(CommonData commonData, HeartbeatService heartbeat, IHubContext<DataHub> hubContext)
        {
            _heartbeat  = heartbeat;
            _hubContext = hubContext;
            _commonData = commonData;

            Battery     = new Battery_DataSetting_Module(_commonData);

            _heartbeat.OnTick += async () => 
            {
                AddSimulatedData();
                NotifyChartSubscribers();

                Update_ReadVIN_Data();

                await RefreshAllAsync();
            };
        }

        public async Task BroadcastBatteryChangeAsync()
        {
            var dto = Battery.ToDto();
            await _hubContext.Clients.All.SendAsync("BatteryUpdated", dto);
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
        public async Task <List<SingleRawCommandFormat>> LoadMockJsonAsync(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                List<SingleRawCommandFormat> rawJsonData = new List<SingleRawCommandFormat>();

                rawJsonData = JsonSerializer.Deserialize<List<SingleRawCommandFormat>>(json, options) ?? new();
                
                var rawJsonData_String = JsonSerializer.Serialize(rawJsonData, new JsonSerializerOptions{
                    WriteIndented = true
                });
                Console.WriteLine(rawJsonData_String);
                return rawJsonData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON read fail: {ex.Message}");
                return new List<SingleRawCommandFormat>();
            }
        }

        public void LoadSingleDeviceINVData(List<SingleRawCommandFormat> rawList)
        {
            INV_MOD_READ_Data.Clear();

            foreach (var cmd in rawList)
            {
                if (string.IsNullOrEmpty(cmd.commandName)) continue;

                var data = new CommandData(_commonData)
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

        private void Update_ReadVIN_Data()
        {
            if(INV_MOD_READ_Data.TryGetValue("READ_VIN", out var cmd))
            {
                // 模擬數值，例如電壓 200~300V 間浮動
                float simulatedValue = 200f + (float)(new Random().NextDouble() * 100);

                // 依據 scaling factor 將 float 轉成 byte[]
                var scaled = (ushort)(simulatedValue / cmd.Scaling);
                var bytes = BitConverter.GetBytes(scaled); // 預設為 LittleEndian

                if (BitConverter.IsLittleEndian == false)
                    Array.Reverse(bytes);

                Console.WriteLine($"byte[0] = {bytes[0]}, byte[1] = {bytes[1]}");
                // 使用 ushort 佔 2 bytes (CommandData.Data 為 List<byte>)
                cmd.Data = new List<byte> { bytes[0], bytes[1] };
            }
        }
        #endregion API_Simulate
    }
}