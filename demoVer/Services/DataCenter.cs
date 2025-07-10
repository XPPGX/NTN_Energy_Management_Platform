using demoVer.Models;
using demoVer.Broadcast;
using Microsoft.AspNetCore.SignalR;

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

        public Battery_DataSetting_Module_W Battery {get;}

        public DataCenter(CommonData commonData, HeartbeatService heartbeat, IHubContext<DataHub> hubContext)
        {
            _heartbeat  = heartbeat;
            _hubContext = hubContext;
            _commonData = commonData;

            Battery     = new Battery_DataSetting_Module_W(_commonData);

            _heartbeat.OnTick += async () => 
            {
                AddSimulatedData();
                NotifyChartSubscribers();
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
    }
}