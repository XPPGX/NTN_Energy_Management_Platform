using demoVer.Models;
using demoVer.Utils;

namespace demoVer.Services
{
    public class PollingOptions
    {
        public int PerRequestDelayMs {get; set;} = 5;
        public int RequestTimeoutMs {get; set;} = 100;
    }

    public class PollingWave
    {   //記錄這次polling的port(CAN1, ...)，起始Addr(startAddr), 有多少連續的device(length)
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

    public class PollingRead : BackgroundService
    {
        private readonly GlobalVar _globalVar;
        private readonly ApiManager _apiManager;
    
        private List<PollingWave> _pollingWave = new List<PollingWave>();
        private PollingOptions _opt = new PollingOptions();
        private const uint portMaxDeviceNum = 64;

        private int waveIndex           = 0;
        private string nowPollingPort   = "";
        private uint nowPollingAddr     = 0;
        private int nowPollingCount     = 0;
        private uint nowStoringAddr     = 0;
        public PollingRead(GlobalVar globalVar, ApiManager apiManager)
        {
            _globalVar  = globalVar;
            _apiManager = apiManager;
            initPollingWave();
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while(!ct.IsCancellationRequested)
            {
                using var reqCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                reqCts.CancelAfter(_opt.RequestTimeoutMs);        

                try
                {
                    nowPollingPort  = _pollingWave[waveIndex].port;
                    // nowPollingAddr  = (uint)(_globalVar.getPortStartAddr(nowPollingPort) + _pollingWave[waveIndex].startAddr + nowPollingCount);
                    nowPollingAddr = (uint)(_pollingWave[waveIndex].startAddr + nowPollingCount);
                    nowStoringAddr = (uint)waveIndex * portMaxDeviceNum + nowPollingAddr;
                    AppLogger.Log_To_File_log($"[PollingRead][ExecuteAsync] nowPollingPort = {nowPollingPort}, nowPollingAddr = {nowPollingAddr}");

                    //用READ_API取得 單台INV的資料 （建議 ApiManager 方法支援 CancellationToken）
                    var res = await _apiManager.apiRead_OneDeviceData(nowPollingPort, nowPollingAddr);
                    if(res == null)
                    {
                        AppLogger.Log_To_File_log($"[PollingRead][ExecuteAsync] : {nowPollingPort}@{nowPollingAddr} ReadAPI return null");
                    

                        _globalVar.LinkedDevices.Unlink(nowStoringAddr);
                        
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
                    AppLogger.Log_To_File_log($"[PollingRead][ExecuteAsync] : Timeout, {e}");
                }
                catch (HttpRequestException ex)
                {
                    AppLogger.Log_To_File_log($"[PollingRead][ExecuteAsync] : Read {nowPollingAddr} HTTP failed, {ex}");
                }
                catch (System.Exception ex)
                {
                    AppLogger.Log_To_File_log($"[PollingRead][ExecuteAsync] : Read {nowPollingAddr} SYS failed, {ex}");
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
        }



        
        
        public void initPollingWave()
        {
            //暫定PollingWave為以下，之後會更新「初始化方式」
            waveIndex       = 0;
            nowPollingPort  = "";
            nowPollingAddr  = 0;
            nowPollingCount = 0;

            _pollingWave.Add(new PollingWave{port="CAN1", startAddr=0, length=64});
            _pollingWave.Add(new PollingWave{port="CAN2", startAddr=0, length=64});
            _pollingWave.Add(new PollingWave{port="MOD1", startAddr=0, length=64});
            _pollingWave.Add(new PollingWave{port="MOD2", startAddr=0, length=64});
        }
    }
}