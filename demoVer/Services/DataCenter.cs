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

        public Battery_DataSetting_Module_W Battery {get;}

        public DataCenter(CommonData commonData, HeartbeatService heartbeat, IHubContext<DataHub> hubContext)
        {
            _heartbeat  = heartbeat;
            _hubContext = hubContext;
            _commonData = commonData;

            Battery     = new Battery_DataSetting_Module_W(_commonData);

            _heartbeat.OnTick += async () => await RefreshAllAsync();
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
    }
}