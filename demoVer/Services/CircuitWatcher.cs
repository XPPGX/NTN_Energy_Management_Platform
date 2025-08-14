using Microsoft.AspNetCore.Components.Server.Circuits;
using demoVer.Utils;

namespace demoVer.Services
{
    public class CircuitsWatcher : CircuitHandler
    {
        private int _onlineCount;
        public event Action? EnableAsync;
        public event Action? DisableAsync;
        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
        {
            var n = Interlocked.Increment(ref _onlineCount) == 1;
            AppLogger.Log_To_File_log($"[OnCircuitOpenedAsync] online = {_onlineCount}");
            if(n == true)
            {
                EnableAsync?.Invoke();
            }
            return Task.CompletedTask;    
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
        {
            var n = Interlocked.Decrement(ref _onlineCount) == 0;
            AppLogger.Log_To_File_log($"[OnCircuitClosedAsync] online = {_onlineCount}");
            if(n == true)
            {
                DisableAsync?.Invoke();
            }
            return Task.CompletedTask;
        }
    }
}