using demoVer.Models;
using demoVer.Utils;
using System.Threading;
using System.Threading.Tasks;
using demoVer.Interfaces;

namespace demoVer.Services
{   
    public class PollingOptions
    {
        public int PerRequestDelayMs {get; set;} = 10;
        public int RequestTimeoutMs {get; set;} = 100;
    }

    public class PollingWave
    {   //記錄這段連續的addr，其polling的port(CAN1, ...)，起始Addr(startAddr), 有多少連續的device(length)
        //e.g.
        /*
            e.g. port = "CAN1"
            startAddr = 0
            length = 2

            這代表，在CAN1 port上從Addr 0 開始兩格，有device連接，也就是Addr 0, Addr 1有device連接
        */
        
        public string port; //CAN1, CAN2, MOD1, MOD2
        public int startAddr;
        public int length;
    }
    
    // 目前是先全部addr polling
    // 之後要改成針對不同wave polling
    public class PollingRead : BackgroundService, PausableWorker
    {
        //injections
        private readonly GlobalVar _globalVar;
        private readonly ApiManager _apiManager;

        //控制PollingRead的啟動時機    
        private volatile bool _enabled;
        private readonly SemaphoreSlim _startGate = new(0, 1);

        //Variables
        private string _category;
        private List<PollingWave> _pollingWave = new List<PollingWave>();
        private PollingOptions _opt = new PollingOptions();
        private const uint portMaxDeviceNum = 64;

        private int waveIndex           = 0;    //紀錄 當前是那個wave
        private string nowPollingPort   = "";   //紀錄 當前wave的port
        private uint nowPollingAddr     = 0;    //紀錄 發送API發送的addr(每個port只有0~63)
        private int nowPollingCount     = 0;    //紀錄 當前wave的polling到第幾addr
        private uint nowStoringAddr     = 0;    //紀錄 從API收到的資料要存進 Device_ReadData 對應 位置
        //儲存addr(模組端) 與 API發送addr(framework端) 差異如下：
        // Port         |模組            |framework
        // =======================================
        // CAN1         |0~63            |0~63 
        // CAN2         |64~127          |0~63
        // MOD1         |128~191         |0~63
        // MOD2         |191~255         |0~63


        public PollingRead( GlobalVar globalVar,
                            ApiManager apiManager)
        {
            _globalVar  = globalVar;
            _apiManager = apiManager;
            _category = GetType().FullName!;
            initPollingWave();
        }

        public Task EnableAsync()
        {
            _enabled = true;
            if(_startGate.CurrentCount == 0) _startGate.Release();
            return Task.CompletedTask;
        }

        public async Task DisableAsync(TimeSpan? delay = null)
        {
            if(delay.HasValue) await Task.Delay(delay.Value);
            _enabled = false;
        }

        //Background service 的 進入點
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while(!ct.IsCancellationRequested)
            {
                try
                {
                    while(!_enabled && !ct.IsCancellationRequested)
                    {
                        await _startGate.WaitAsync(TimeSpan.FromSeconds(10), ct);
                    }

                    while(_enabled && !ct.IsCancellationRequested)
                    {
                       await PollOneStepAsync(ct);
                    }
                    
                }
                catch(OperationCanceledException)
                {

                }
                
            }
        }


        private async Task PollOneStepAsync(CancellationToken ct)
        {
            try
            {
                //debug
                // if(!_globalVar.Debug_Flag)
                // {
                //     await Task.Delay(50, ct);
                //     return;
                // }

                //release
                using var reqCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                reqCts.CancelAfter(_opt.RequestTimeoutMs);        
                
                nowPollingPort  = _pollingWave[waveIndex].port;
                // nowPollingAddr  = (uint)(_globalVar.getPortStartAddr(nowPollingPort) + _pollingWave[waveIndex].startAddr + nowPollingCount);
                nowPollingAddr = (uint)(_pollingWave[waveIndex].startAddr + nowPollingCount);
                nowStoringAddr = (uint)waveIndex * portMaxDeviceNum + nowPollingAddr;
                
                // AppLogger.Log_To_File_log(_category, $"[PollingRead][ExecuteAsync] nowPollingPort = {nowPollingPort}, nowPollingAddr = {nowPollingAddr}", AppLogLevel.Trace);

                //用READ_API取得 單台INV的資料 （建議 ApiManager 方法支援 CancellationToken）
                var res = await _apiManager.apiRead_OneDeviceData(nowPollingPort, nowPollingAddr);
                if(res == null)
                {
                    // AppLogger.Log_To_File_log(_category, $"[PollingRead][ExecuteAsync] : {nowPollingPort}@{nowPollingAddr} ReadAPI return null", AppLogLevel.Trace);
                
                    //移除 link
                    _globalVar.LinkedDevices.Unlink(nowStoringAddr);
                    //移除 Phase字典中的addr
                    _globalVar.INVs_Phase.TryRemove(nowPollingAddr, out var removed);

                    //刪除Device_ReadData裡面，nowStoringAddr的資料
                    _globalVar.Device_ReadData.Remove_oneDevice_Data(nowStoringAddr);
                }
                else
                {
                    //儲存 單台INV的資料到記憶體中 
                    if(waveIndex >= 0)
                    {
                        _globalVar.LinkedDevices.Link(nowStoringAddr);

                        _globalVar.Device_ReadData.Read_oneDevice_Data(nowStoringAddr, res);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                // AppLogger.Log_To_File_log(_category, $"[PollingRead][ExecuteAsync] : Timeout, {e}", AppLogLevel.Warning);
            }
            catch (HttpRequestException ex)
            {
                // AppLogger.Log_To_File_log(_category, $"[PollingRead][ExecuteAsync] : Read {nowPollingAddr} HTTP failed, {ex}", AppLogLevel.Debug);
            }
            catch (System.Exception ex)
            {
                // AppLogger.Log_To_File_log(_category, $"[PollingRead][ExecuteAsync] : Read {nowPollingAddr} SYS failed, {ex}", AppLogLevel.Error);
            }

            //遞增Index
            if(nowPollingCount >= (_pollingWave[waveIndex].length - 1))
            {
                waveIndex = (waveIndex + 1) % _pollingWave.Count;
                nowPollingCount = 0;
            }
            else
            {
                nowPollingCount ++;
            }

            if(_opt.PerRequestDelayMs > 0)
            {
                await Task.Delay(_opt.PerRequestDelayMs, ct);
            }
        }
        
        
        public void initPollingWave()
        {
            //暫定PollingWave為以下，之後會更新「初始化方式」
            waveIndex       = 0;
            nowPollingPort  = "";
            nowPollingAddr  = 0;
            nowPollingCount = 0;

            _pollingWave.Add(new PollingWave{port="CAN1", startAddr=0, length=1});
            // _pollingWave.Add(new PollingWave{port="CAN2", startAddr=0, length=64});
            // _pollingWave.Add(new PollingWave{port="MOD1", startAddr=0, length=64});
            // _pollingWave.Add(new PollingWave{port="MOD2", startAddr=0, length=64});
        }
    }
}