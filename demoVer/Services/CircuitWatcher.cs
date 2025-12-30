using Microsoft.AspNetCore.Components.Server.Circuits;
using demoVer.Utils;
using demoVer.Interfaces;
namespace demoVer.Services
{
    public class CircuitsWatcher : CircuitHandler
    {
        private readonly PausableWorker _worker;
        private int _onlineCount;
        private CancellationTokenSource? _disableCts;

        public CircuitsWatcher(PausableWorker worker)
        {
            _worker = worker;
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            var n = Interlocked.Increment(ref _onlineCount);

            _disableCts?.Cancel();

            if(n == 1)
            {
                // _ = _worker.EnableAsync();
            }

            return Task.CompletedTask;
        }

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            var n = Interlocked.Decrement(ref _onlineCount);

            if(n == 0)
            {
                _disableCts?.Cancel();
                _disableCts = new CancellationTokenSource();

                _ = Task.Run(async() =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), _disableCts.Token);
                    }
                    catch(OperationCanceledException)
                    {
                        return;
                    }
                    await _worker.DisableAsync();
                });
                
            }
            return Task.CompletedTask;
        }
    }
}