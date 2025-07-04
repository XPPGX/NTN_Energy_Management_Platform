using demoVer.Models;
namespace demoVer.Services
{
    public class ObservableModule
    {
        public event Action? OnUpdated;

        protected void NotifyChanged() => OnUpdated?.Invoke();

        protected bool SetProperty<T>(ref T storage, T value)
        {
            if(!EqualityComparer<T>.Default.Equals(storage, value))
            {
                storage = value;
                NotifyChanged();
                return true;
            }
            return false;
        }
        
    }

    public class DataCenter
    {
        private readonly HeartbeatService _heartbeat;

        private readonly CommonData _commonData;
        public Battery_DataSetting_Module_W Battery {get;}

        public DataCenter(CommonData commonData, HeartbeatService heartbeat)
        {
            _heartbeat  = heartbeat;
            
            _commonData = commonData;
            Battery     = new Battery_DataSetting_Module_W(_commonData);

            _heartbeat.OnTick += async () => await RefreshAllAsync();
        }

        private async Task RefreshAllAsync()
        {
            // Battery.Refresh();
            // INV.Refresh();
            await Task.CompletedTask;
        }
    }
}