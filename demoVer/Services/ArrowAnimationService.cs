using demoVer.Models;
using demoVer.Utils;
namespace demoVer.Services
{
    public class ArrowAnimationService : IAsyncDisposable
    {
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _cts = new();
        public triangle_group triGroup1 { get; } = new();
        public triangle_group triGroup2 { get; } = new();
        public triangle_group triGroup3 { get; } = new();

        public event Action? OnTick;

        public ArrowAnimationService()
        {
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            _ = RunLoop(_cts.Token);
        }

        private async Task RunLoop(CancellationToken token)
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(token))
                {
                    if (triGroup1.get_showArrowFlag())
                    {
                        triGroup1.activeIndex = (triGroup1.activeIndex + 1) % 3;
                        AppLogger.Log("group1.index : " + triGroup1.activeIndex);
                    }
                    if (triGroup2.get_showArrowFlag())
                    {
                        triGroup2.activeIndex = (triGroup2.activeIndex + 1) % 3;
                        AppLogger.Log("group2.index : " + triGroup2.activeIndex);
                    }

                    if (triGroup3.get_showArrowFlag())
                    {
                        triGroup3.activeIndex = (triGroup3.activeIndex + 1) % 3;
                        AppLogger.Log("group3.index : " + triGroup3.activeIndex);
                    }

                    OnTick?.Invoke(); // 通知 UI 更新
                }
            }
            catch (OperationCanceledException) { }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
