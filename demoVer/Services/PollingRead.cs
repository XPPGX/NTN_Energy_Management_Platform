using demoVer.Models;
using demoVer.Utils;
using System.Threading;
using System.Threading.Tasks;
using demoVer.Interfaces;
using System;
using System.Collections.Generic;

//1. 在開始polling前先取得link-status，
//2. 每次polling單台時比對是否有在link-status，有才發送api
//2. 等polling完port.length，就再取一次link-status看是否有新的

namespace demoVer.Services
{   
    public class PollingOptions
    {
        public int PerRequestDelayMs {get; set;} = 100;
        public int RequestTimeoutMs {get; set;} = 1000;
    }

    //紀錄polling的Port跟Addr範圍
    public class PollingWave
    {
        //e.g.
        /*
            e.g. port = "CAN1"
            startAddr = 0
            length = 2

            這代表，Polling單台的service會polling CAN1的Addr 0, 1 兩台設備
        */
        public string port; //CAN1, CAN2, MOD1, MOD2
        public int startAddr;
        public int length;
    }
    
    public class PollingRead : BackgroundService, PausableWorker
    {
        //injections
        private readonly GlobalVar _globalVar;
        private readonly ApiManager _apiManager;
        private readonly LinkAddrManager _linkAddrManager;

        //控制PollingRead的啟動時機    
        private volatile bool _enabled;
        private readonly SemaphoreSlim _startGate = new(0, 1);

        //Variables
        private string _category;
        private List<PollingWave> _pollingWave = new List<PollingWave>();
        private PollingOptions _opt = new PollingOptions();
        private const uint portMaxDeviceNum = 64;

        private int waveIndex           = 0;    //紀錄 當前是哪個wave
        private string nowPollingPort   = "";   //紀錄 當前wave的port
        private uint nowPollingAddr     = 0;    //紀錄 發送API發送的addr(每個port只有0~63)
        private int nowPollingCount     = 0;    //紀錄 當前wave的polling到第幾addr
        private uint nowStoringAddr     = 0;    //紀錄 從API收到的資料要存進 Device_ReadData 對應 位置
        private HashSet<uint> linkingAddr = new();
        //儲存addr(模組端) 與 API發送addr(framework端) 差異如下：
        // Port         |模組            |framework
        // =======================================
        // CAN1         |0~63            |0~63 
        // CAN2         |64~127          |0~63
        // MOD1         |128~191         |0~63
        // MOD2         |191~255         |0~63
        private LinkStatus_JsonFormat rcv_linkStatus = new();
        private int counter = 0;

        public PollingRead( GlobalVar globalVar,
                            ApiManager apiManager,
                            LinkAddrManager linkAddrManager)
        {
            _globalVar  = globalVar;
            _apiManager = apiManager;
            _linkAddrManager = linkAddrManager;
            _category = GetType().FullName!;
            
            // _pollingWave = _globalVar.SubSystems;
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
                        counter ++;
                        if(counter == 50)
                        {
                            await PollNowLinkAddr(ct);
                            counter = 0;
                        }
                    }
                    
                }
                catch(OperationCanceledException)
                {

                }
                
            }
        }


        private async Task PollOneStepAsync(CancellationToken ct)
        {
            //用於控制是否要等100ms再發下一次polling
            bool IsAPI_sent = true;

            try
            {   
                //release
                using var reqCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                reqCts.CancelAfter(_opt.RequestTimeoutMs);        
                
                nowPollingPort  = _pollingWave[waveIndex].port;
                nowPollingAddr = (uint)(_pollingWave[waveIndex].startAddr + nowPollingCount);
                nowStoringAddr = (uint)waveIndex * portMaxDeviceNum + nowPollingAddr;

                //判斷當前連線中是否有 nowPollingAddr
                var tmp_linkingAddr = new HashSet<uint>(linkingAddr);
                
                if(!tmp_linkingAddr.Contains(nowStoringAddr))
                {// 此次polling的addr不在link-status中，視為斷線，跳過此次polling

                    AppLogger.Log_To_File_log(_category, $"[PollingRead][PollOneStepAsync] polling {nowStoringAddr} is not linked, pass it.", AppLogLevel.Trace);
                
                    //======預計廢除========
                    //移除 link
                    _globalVar.LinkedDevices.Unlink(nowStoringAddr);
                    //移除 Phase字典中的addr
                    // _globalVar.INVs_Phase.TryRemove(nowPollingAddr, out var removed);
                    //刪除Device_ReadData裡面，nowStoringAddr的資料
                    _globalVar.Device_ReadData.Remove_oneDevice_Data(nowStoringAddr);
                    //======================

                    //預計保留
                    _linkAddrManager.Unlink(nowStoringAddr);
                    _globalVar.Real_Devices_ReadData.Remove_oneDevice_Data(nowPollingAddr);

                    AppLogger.Log_To_File_log(_category, $"[PollingRead][PollOneStepAsync] : {nowPollingPort}@{nowPollingAddr} removing done...", AppLogLevel.Trace);
                    
                    IsAPI_sent = false;
                }
                else
                {// 此次polling的addr在link-status中，視為連線，發送API取得資料

                    //用READ_API取得 單台INV的資料 （建議 ApiManager 方法支援 CancellationToken）
                    AppLogger.Log_To_File_log(_category, $"[PollingRead][PollOneStepAsync] nowPollingPort = {nowPollingPort}, nowPollingAddr = {nowPollingAddr} Begin...", AppLogLevel.Trace);
                    var res = await _apiManager.apiReadReal_OneDeviceData(nowPollingPort, nowPollingAddr);
                    AppLogger.Log_To_File_log(_category, $"[PollingRead][PollOneStepAsync] nowPollingPort = {nowPollingPort}, nowPollingAddr = {nowPollingAddr} Done...", AppLogLevel.Trace);

                    //檢查 ReadRealAPI資料有效性
                    bool res_is_valid = checkApiResponseValidity(res);

                    if(res_is_valid is false)
                    {//此次資料無效，模組視該addr為斷線，移除該addr資料(若存在)

                        AppLogger.Log_To_File_log(_category, $"[PollingRead][PollOneStepAsync] : response is {res_is_valid}, {nowPollingPort}@{nowPollingAddr} removing begin...", AppLogLevel.Trace);
                        //======預計廢除========
                        //移除 link
                        _globalVar.LinkedDevices.Unlink(nowStoringAddr);
                        //移除 Phase字典中的addr
                        _globalVar.INVs_Phase.TryRemove(nowPollingAddr, out var removed);
                        //刪除Device_ReadData裡面，nowStoringAddr的資料
                        _globalVar.Device_ReadData.Remove_oneDevice_Data(nowStoringAddr);
                        //======================


                        //預計保留
                        _linkAddrManager.Unlink(nowStoringAddr);
                        _globalVar.Real_Devices_ReadData.Remove_oneDevice_Data(nowPollingAddr);
                        AppLogger.Log_To_File_log(_category, $"[PollingRead][PollOneStepAsync] : {nowPollingPort}@{nowPollingAddr} removing done...", AppLogLevel.Trace);
                        
                    }
                    else
                    {//此次資料有效，儲存該addr資料
                        if(waveIndex >= 0)
                        {
                            AppLogger.Log_To_File_log(_category, $"[PollingRead][PollOneStepAsync] : response is {res_is_valid}, {nowPollingPort}@{nowPollingAddr} saving start...", AppLogLevel.Trace);
                            
                            //======預計廢除========
                            _globalVar.LinkedDevices.Link(nowStoringAddr);
                            //======================

                            //預計保留
                            _linkAddrManager.Link(nowStoringAddr);
                            _globalVar.Real_Devices_ReadData.SaveReal_oneDevice_Data(nowStoringAddr, res);
                            AppLogger.Log_To_File_log(_category, $"[PollingRead][PollOneStepAsync] : {nowPollingPort}@{nowPollingAddr} saving done...", AppLogLevel.Trace);
                        }
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                AppLogger.Log_To_File_log(_category, $"[PollingRead][ExecuteAsync] : Timeout, {e}", AppLogLevel.Warning);
            }
            catch (HttpRequestException ex)
            {
                AppLogger.Log_To_File_log(_category, $"[PollingRead][ExecuteAsync] : Read {nowPollingAddr} HTTP failed, {ex}", AppLogLevel.Debug);
            }
            catch (System.Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[PollingRead][ExecuteAsync] : Read {nowPollingAddr} SYS failed, {ex}", AppLogLevel.Error);
            }
            finally
            {
                //遞增Index
                if (nowPollingCount >= (_pollingWave[waveIndex].length - 1))
                {
                    waveIndex = (waveIndex + 1) % _pollingWave.Count;
                    nowPollingCount = 0;
                }
                else
                {
                    nowPollingCount++;
                }
                
                AppLogger.Log_To_File_log(_category, $"[PollingRead][PollOneStepAsync] IsAPI_sent = {IsAPI_sent}", AppLogLevel.Trace);
                if (_opt.PerRequestDelayMs > 0 && IsAPI_sent is true)
                {
                    await Task.Delay(_opt.PerRequestDelayMs, ct);
                }
                // //delay 20ms
                // await Task.Delay(1000);
            }
        }
        
        private async Task PollNowLinkAddr(CancellationToken ct)
        {
            bool PollingNowLink_isSucc = true;
            try
            {
                //release
                using var reqCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                reqCts.CancelAfter(_opt.RequestTimeoutMs);

                rcv_linkStatus = await _apiManager.apiRead_LinkStatus();
                if(rcv_linkStatus is null)
                {
                    PollingNowLink_isSucc = false;
                    
                    linkingAddr.Clear();
                    AppLogger.Log_To_File_log(_category, $"[PollingRead][PollNowLinkAddr] rcv_linkStatus is Null", AppLogLevel.Trace);
                }
                else
                {
                    PollingNowLink_isSucc = true;

                    var INV_products = rcv_linkStatus.Products["All"];

                    for(uint i = 0 ; i < 64 ; i ++)
                    {
                        uint CAN1_index = (uint)((INV_products.CAN1_LINK >> (int)i) & 1);
                        uint CAN2_index = (uint)((INV_products.CAN2_LINK >> (int)i) & 1);
                        uint MOD1_index = (uint)((INV_products.MOD1_LINK >> (int)i) & 1);
                        uint MOD2_index = (uint)((INV_products.MOD2_LINK >> (int)i) & 1);

                        // Console.WriteLine($"CAN1_index = {CAN1_index}, CAN2_index = {CAN2_index}, MOD1_index = {MOD1_index}, MOD2_index = {MOD2_index}");
                        
                        if(CAN1_index == 1)
                        {
                            linkingAddr.Add(i);
                        }
                        else
                        {
                            linkingAddr.Remove(i);
                        }

                        if(CAN2_index == 1)
                        {
                            linkingAddr.Add(i + 64);
                        }
                        else
                        {
                            linkingAddr.Remove(i + 64);
                        }

                        if(MOD1_index == 1)
                        {
                            linkingAddr.Add(i + 128);
                        }
                        else
                        {
                            linkingAddr.Remove(i + 128);
                        }

                        if(MOD2_index == 1)
                        {
                            linkingAddr.Add(i + 192);
                        }
                        else
                        {
                            linkingAddr.Remove(i + 192);
                        }
                    }
                    AppLogger.Log_To_File_log(_category, $"[PollingRead][PollNowLinkAddr] {string.Join(", ", linkingAddr)}", AppLogLevel.Trace);
                }
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[PollingRead][PollNowLinkAddr] Error : {e}", AppLogLevel.Error);
                return;
            }
            finally
            {
                AppLogger.Log_To_File_log(_category, $"[PollingRead][PollNowLinkAddr] PollingNowLink_isSucc = {PollingNowLink_isSucc}", AppLogLevel.Trace);

                if(PollingNowLink_isSucc is false)
                {//LinkStatus API 回傳NULL，則等待5秒，再發下一次
                    await Task.Delay(5000, ct);
                }
                else
                {//正常取得LinkStatus 則等待一次成功polling的時間，再發下一次
                    if(_opt.PerRequestDelayMs > 0)
                    {
                        await Task.Delay(_opt.PerRequestDelayMs, ct);
                    }
                }
            }
        }
        public void initPollingWave()
        {
            //暫定PollingWave為以下，之後會更新「初始化方式」
            waveIndex = 0;
            nowPollingPort = "";
            nowPollingAddr = 0;
            nowPollingCount = 0;

            //當前開放CAN1, CAN2, MOD1, MOD2的addr全部polling
            _pollingWave.Add(new PollingWave { port = "CAN1", startAddr = 0, length = 64 });

            _pollingWave.Add(new PollingWave { port = "CAN2", startAddr = 0, length = 64 });

            _pollingWave.Add(new PollingWave { port = "MOD1", startAddr = 0, length = 64 });

            _pollingWave.Add(new PollingWave { port = "MOD2", startAddr = 0, length = 64 });

            //發送API的addr會根據linkingAddr來決定
        }

        //檢查 ReadRealAPI資料有效性
        private bool checkApiResponseValidity(Real_SingleDeviceData_JsonFormat res)
        {
            //檢查 res 是否為 null
            if (res is null)
            {
                AppLogger.Log_To_File_log(_category, $"[PollingRead][checkApiResponseValidity] res is invalid !! (res is null)", AppLogLevel.Trace);
                return false;
            }

            /*
                目前針對 明緯設備 與 其他廠牌設備 做不同的有效性檢查
                1. 明緯的設備皆有 MFR_MODEL 命令，所以檢查 MFR_MODEL 是否存在即可判斷資料有效性
                2. 對其他廠牌設備，不一定有MFR_MODEL命令，所以暫時不做其他檢查
            */

            //檢查MFR_MODEL是否為空
            if (res.values.TryGetValue("MFR_MODEL", out var tmpData))
            {
                if (string.Equals(tmpData.type, "ASCII", StringComparison.OrdinalIgnoreCase))
                {
                    string tmp_modelName = tmpData.value.ToString() ?? "";
                    if (string.IsNullOrEmpty(tmp_modelName))
                    {
                        AppLogger.Log_To_File_log(_category, $"[PollingRead][checkApiResponseValidity] res is invalid !! (MFR_MODEL is empty)", AppLogLevel.Trace);
                        return false;
                    }
                }
                else
                {
                    AppLogger.Log_To_File_log(_category, $"[PollingRead][checkApiResponseValidity] res is invalid !! (MFR_MODEL type is not ASCII)", AppLogLevel.Trace);
                    return false;
                }
            }
            
            return true;
        }
    }
}