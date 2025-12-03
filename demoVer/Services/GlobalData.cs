using demoVer.Models;
using demoVer.Utils;
using System.Threading;
using demoVer.Services;
using demoVer.Interfaces;
using System.Collections.Concurrent;

namespace demoVer.Services
{
    public class SubAppSystem
    {
        //記錄這段連續的addr，其polling的port(CAN1, ...)，起始Addr(startAddr), 有多少連續的device(length)
        //e.g.
        /*
            e.g. port = "CAN1"
            startAddr = 0
            length = 2

            這代表，在CAN1 port上從Addr 0 開始兩格有device連接，也就是Addr 0, Addr 1有device連接
        */
        public int subSystemID;
        public string port; //CAN1, CAN2, MOD1, MOD2
        public string protocolFileName; //NTN-5K_CAN.json
        public uint startAddr;
        public uint length;
    }

    public class GlobalVar
    {
        //[inject]
        private readonly HeartbeatService _heartbeat;
        private readonly LinkAddrManager _linkAddrManager;
        private readonly SubSystemManager _subSystemManager;
        //For log
        private string _category;
        private bool _disposed;

        //Variable
        public bool InitSysOK { get; set; } = false;
        public InitStage nowInitStage = InitStage.FromJson;

        private int _invConnectNum;
        public int INV_ConnectNum
        {
            get => Volatile.Read(ref _invConnectNum);
            private set => Volatile.Write(ref _invConnectNum, value);
        }

        public string? Sys_ModelName = "";
    
        /// <summary>
        /// FixedCmdName => FixedCmdCode 對照表
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> HardCodes { get; set; } = new Dictionary<string, Dictionary<string, string>>(); 
        /// <summary>
        /// FixedCmdCode => FixedCmdName 對照表
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> HardCodesReverse { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        /// <summary>
        /// FixedCmdName => DecodeLogic 對照表
        /// </summary>
        public static CommandBitFieldSpec HardCodedDecodeLogic { get; set; } = new CommandBitFieldSpec();
    
        public Real_allDeviceData Real_Devices_ReadData { get; set; } = new Real_allDeviceData();
        public allDevice_Data Device_ReadData { get; set; } = new allDevice_Data();                  //Polling讀取各Device資料
        public LinkedDeviceStore LinkedDevices { get; } = new LinkedDeviceStore();                      //以concurrent字典記錄目前有連線的devices
        public WriteAPI_Datas Device_WriteData { get; set; } = new WriteAPI_Datas();
        

        public List<SubAppSystem> SubSystems { get; set; } = new List<SubAppSystem>();
        public int? ActiveSubAppSystemID { get; set; }

        public GlobalVar(IGroupsDataDecoder decoder,
                            HeartbeatService heartbeats,
                            LinkAddrManager linkAddrManager,
                            SubSystemManager subSystemManager)
        {
            //For log
            _category = GetType().FullName!;

            _heartbeat = heartbeats;
            _linkAddrManager = linkAddrManager;
            _subSystemManager = subSystemManager;


            //Events(Actions)
            LinkedDevices.linkChanged += Get_INV_ConnectNum;
            _heartbeat.OnTick += TickTask;
        }

        public string getActiveSubSysPort()
        {
            try
            {
                if (ActiveSubAppSystemID is int index && index >= 0 && index < SubSystems.Count)
                {
                    return SubSystems[index].port;
                }
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][getActiveSubSysPort] Error : {e}", AppLogLevel.Error);
            }

            return string.Empty;
        }

        public async Task TickTask()
        {
            // updateSystemVar();
            switch (nowInitStage)
            {
                case InitStage.FromJson:
                    break;
                case InitStage.FromDevice:
                case InitStage.Done:
                    //FromDevice 跟 Done，都一樣要從 Polling READ_API 那邊讀值
                    // Compute_SYS_Phase(); //for loop length = 256
                    ComputeOverallValues(); // for loop length = 256
                    break;
                default:
                    break;
            }
        }

        public int getPortStartAddr(string port)
        {
            switch (port)
            {
                case "CAN1": return ConstDefinition.CAN1_addr;
                case "CAN2": return ConstDefinition.CAN2_addr;
                case "MOD1": return ConstDefinition.MOD1_addr;
                case "MOD2": return ConstDefinition.MOD2_addr;
                default: return 0;
            }
        }

        public (string, uint) Get_Port_RealAddr_ByAddr(uint addr)
        {
            if (addr >= ConstDefinition.MOD2_addr) { return ("MOD2", addr - ConstDefinition.MOD2_addr); }
            if (addr >= ConstDefinition.MOD1_addr) { return ("MOD1", addr - ConstDefinition.MOD1_addr); }
            if (addr >= ConstDefinition.CAN2_addr) { return ("CAN2", addr - ConstDefinition.CAN2_addr); }
            if (addr >= ConstDefinition.CAN1_addr) { return ("CAN1", addr - ConstDefinition.CAN1_addr); }
            return (string.Empty, addr);
        }

        public void Get_INV_ConnectNum()
        {
            INV_ConnectNum = LinkedDevices.Snapshot().Length;
            AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_INV_Connection] INV_ConnectNum = {INV_ConnectNum}", AppLogLevel.Trace);
        }

        public IReadOnlyList<SubSystem> GetSubSystemsSnapshot()
        {
            // Return a snapshot list so consumers can iterate without touching the manager internals.
            return _subSystemManager.GetAllSubSystems_Ref_In_List();
        }

        public void Dispose()
        {
            if (_disposed) return;

            LinkedDevices.linkChanged -= Get_INV_ConnectNum;

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 取得所有子系統的 (port, protocol, realAddr, storingAddr) 清單
        /// </summary>        
        public List<(string, string, uint, uint)> GetAllSubSys_PortAddrPair_InList_FromSubSysManager()
        {
            return _subSystemManager.GetAllSubSystem_PortAddr_pairInList();
        }
        public Dictionary<string, string> GetCmdUnit_Dict_FromSubsysManager(string port, string protocol)
        {
            return _subSystemManager.GetCmd_Unit_Dict_InOneSubSystem(port, protocol);
        }

        public string? Get_Subsystem_Summary_Value(string port, string protocol, string cmdName)
        {
            try
            {
                var subsys = _subSystemManager.GetOneSubSystem_Ref(port, protocol);
                if (subsys is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_Subsystem_Summary_Value] SubSystem not found for (port, protocol) = ({port}, {protocol})", AppLogLevel.Trace);
                    return null;
                }

                return subsys.GetSummaryValue(cmdName);
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_Subsystem_Summary_Value] Error : {e}", AppLogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// 計算各子系統的系統變數
        /// </summary>
        private void ComputeOverallValues()
        {

            // 1. 取得快照
            var allSubSystems = _subSystemManager.GetAllSubSystems_Ref_In_List(); //各子系統快照
            var LinkingAddrs = _linkAddrManager.SnapshotAsHashSet(); //連線中的addr快照
            
            // 2. 在每個子系統裡面計算系統變數
            foreach (var subSys in allSubSystems)
            {
                try
                {
                    //3. 使用各子系統的AddrSet中的addr搭配Link，取得目前在線上的device data進行計算
                    List<Real_SingleDeviceData_JsonFormat> onlineDevices_DeviceDatas = new();
                    foreach (var read_addr in subSys.AddrSet)
                    {
                        uint nowStoringAddr = Custom.getAddrOffsetByPort(subSys.Port) + read_addr;

                        //如果 在子系統中的Addr，目前沒有連線，則直接跳過計算
                        if (!LinkingAddrs.Contains(nowStoringAddr)) continue;
                        
                        var deviceData = Real_Devices_ReadData.Get_oneDevice_DataSnapshot(nowStoringAddr);
                        if (deviceData != null)
                        {
                            onlineDevices_DeviceDatas.Add(deviceData);
                        }
                    }

                    //4. 使用onlineDevices_DeviceDatas進行系統變數計算
                    subSys.ComputeOverallValues_in_SubSystem(onlineDevices_DeviceDatas);
                }
                catch(Exception e)
                {
                    AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] SubSystem Port = {subSys.Port}, Protocol = {subSys.Protocol} Error : {e}", AppLogLevel.Error);
                    continue;
                }   
            }
        }
    }
    
    
}