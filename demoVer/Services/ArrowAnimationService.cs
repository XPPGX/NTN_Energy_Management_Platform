using demoVer.Models;
using demoVer.Utils;

namespace demoVer.Services
{
    public class ArrowAnimationService : IAsyncDisposable
    {
        private string _category;

        private bool local_debug_enable = false;
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _cts = new();
        private ArrowDirections _currentDirections = ArrowDirections.Hidden;
        private readonly object _directionLock = new();
        public triangle_group triGroup1 { get; } = new();
        public triangle_group triGroup2 { get; } = new();
        public triangle_group triGroup3 { get; } = new();

        private Task? _loopTask;
        public event Action? OnTick;
        
        

        public ArrowAnimationService()
        {
            _category = GetType().FullName!;
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            _loopTask = RunLoop(_cts.Token);
        }

        private async Task RunLoop(CancellationToken token)
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(token))
                {
                    ArrowDirections directions;
                    lock (_directionLock)
                    {
                        directions = _currentDirections;
                    }
                    triGroup1.SetDirection(directions.GridToMachine);
                    triGroup2.SetDirection(directions.MachineToLoad);
                    triGroup3.SetDirection(directions.MachineToBattery);

                    //移動發亮的箭頭
                    Step(triGroup1);
                    Step(triGroup2);
                    Step(triGroup3);
                    // if (triGroup1.get_showArrowFlag())
                    // {
                    //     triGroup1.activeIndex = (triGroup1.activeIndex + 1) % 3;
                    //     AppLogger.Log("group1.index : " + triGroup1.activeIndex, local_debug_enable);
                    // }
                    // if (triGroup2.get_showArrowFlag())
                    // {
                    //     triGroup2.activeIndex = (triGroup2.activeIndex + 1) % 3;
                    //     AppLogger.Log("group2.index : " + triGroup2.activeIndex, local_debug_enable);
                    // }

                    // if (triGroup3.get_showArrowFlag())
                    // {
                    //     triGroup3.activeIndex = (triGroup3.activeIndex + 1) % 3;
                    //     AppLogger.Log("group3.index : " + triGroup3.activeIndex, local_debug_enable);
                    // }
                    try
                    {
                        OnTick?.Invoke();
                    }
                    catch
                    {
                        // AppLogger.Log_To_File_log(_category, $"[ArrowAnimationService][RunLoop] Error : {e}", AppLogLevel.Error);
                    }
                }
            }
            catch (Exception)
            {
                // AppLogger.Log_To_File_log(_category, $"[ArrowAnimationService][RunLoop] Error : {e} ", AppLogLevel.Error);
            }
        }

        public void UpdateDirections(ArrowDirections directions)
        {
            lock (_directionLock)
            {
                _currentDirections = directions;
            }
        }

        private static void Step(triangle_group tg)
        {
            if (!tg.get_showArrowFlag()) return;
            int n = tg.Count <= 0 ? 1 : tg.Count;
            int step = (tg.Direction is ArrowDirection.Left or ArrowDirection.Up) ? -1 : 1;
            tg.activeIndex = (tg.activeIndex + step + n) % n;
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();

            if(_loopTask is not null)
            {
                try
                {
                    await _loopTask;
                }
                catch(OperationCanceledException)
                {

                }
            }
            _timer.Dispose();
            _cts.Dispose();
            OnTick = null;
        }
    }
}
