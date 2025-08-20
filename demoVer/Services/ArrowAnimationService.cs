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
        private readonly GlobalVar _globalVar;
        public triangle_group triGroup1 { get; } = new();
        public triangle_group triGroup2 { get; } = new();
        public triangle_group triGroup3 { get; } = new();

        public event Action? OnTick;
        
        

        public ArrowAnimationService(GlobalVar globalVar)
        {
            _category = GetType().FullName!;
            _globalVar = globalVar;
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            _ = RunLoop(_cts.Token);
        }

        private async Task RunLoop(CancellationToken token)
        {
            ArrowDirection G2M = ArrowDirection.Hidden; //triGroup1
            ArrowDirection M2L = ArrowDirection.Hidden; //triGroup2
            ArrowDirection M2B = ArrowDirection.Hidden; //triGroup3
            try
            {
                while (await _timer.WaitForNextTickAsync(token))
                {
                    //取得各箭頭方向
                    _globalVar.GetArrowDirections(out G2M, out M2L, out M2B);
                    AppLogger.Log_To_File_log(_category, $"[ArrowAnimationService][RunLoop] G2M = {G2M}, M2L = {M2L}, M2B = {M2B}", AppLogLevel.Trace);
                    
                    triGroup1.SetDirection(G2M);
                    triGroup2.SetDirection(M2L);
                    triGroup3.SetDirection(M2B);

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

                    OnTick?.Invoke(); // 通知 UI 更新
                }
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[ArrowAnimationService][RunLoop] Error : {e} ", AppLogLevel.Error);
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
            _cts.Dispose();
        }
    }
}
