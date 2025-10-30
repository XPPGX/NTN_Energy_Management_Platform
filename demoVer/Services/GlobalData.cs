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
        //[Debug]
        public bool Debug_Flag { get; set; } = true;
        //[inject]
        private readonly IGroupsDataDecoder _decoder;
        private readonly HeartbeatService _heartbeat;
        private readonly LinkAddrManager _linkAddrManager;
        private readonly SubSystemManager _subSystemManager;
        //For log
        private string _category;
        private bool _disposed;

        //Const
        public const uint STATUS_INV = 0x00000001 << 0;  //STATUS : 比對bit
        public const uint STATUS_BYP = 0x00000001 << 1;  //STATUS : 比對bit
        public const uint STATUS_UTI_OK = 0x00000001 << 2;  //STATUS : 比對bit
        public const uint STATUS_CHG = 0x00000001 << 3;  //STATUS : 比對bit
        public const uint STATUS_SOLAR_EN = 0x00000001 << 4;  //STATUS : 比對bit
        public const uint STATUS_SAVING = 0x00000001 << 5;  //STATUS : 比對bit
        public const uint STATUS_BAT_LOW_ALM = 0x00000001 << 6;  //STATUS : 比對bit

        public const byte STATUS_INV_DISCON = 0;
        public const byte STATUS_INV_ERROR = 3;
        public const byte STATUS_INV_INVERTER = 4;
        public const byte STATUS_INV_SAVING = 5;
        public const byte STATUS_INV_BY_PASS = 6;
        public const byte STATUS_INV_CHARGING = 7;
        public const byte STATUS_INV_STANDBY = 8;
        public const byte STATUS_INV_BATTERY_FIRST = 9;

        public const byte INV_MODE_INVERTER = 0;
        public const byte INV_MODE_SAVING = 1;
        public const byte INV_MODE_BY_PASS = 2;
        public const byte INV_MODE_CHARGING = 3;
        public const byte INV_MODE_STANDBY = 4;
        public const byte INV_MODE_SHUTDOWN = 5;
        public const byte INV_MODE_BATTERY_FIRST = 6;
        public const byte INV_MODE_NONE = 0xFF;

        public const byte INV_PHASE_0 = 0;
        public const byte INV_PHASE_180 = 1;
        public const byte INV_PHASE_120 = 2;
        public const byte INV_PHASE_M120 = 3;
        public const byte INV_SINGLE_PHASE = 0x01;
        public const byte INV_TWO_PHASE = 0x03;
        public const byte INV_THREE_PHASE = 0x07;


        //Variable
        public bool InitSysOK { get; set; } = false;
        public InitStage nowInitStage = InitStage.FromJson;

        private int _invConnectNum;
        public int INV_ConnectNum
        {
            get => Volatile.Read(ref _invConnectNum);
            private set => Volatile.Write(ref _invConnectNum, value);
        }

        //System variables(Heartbeat.OnTick)
        //如果計算速度很慢，可以把所有算式都放在某個foreach INVs_Phase裡面
        public byte Sys_PhaseStatus = 0;
        public string? Sys_ModelName = "";
        public event Func<Task>? Sys_ModelName_OnChanged;
        public bool Sys_modelError = false;

        public ConcurrentDictionary<uint, byte?> INVs_Phase { get; set; } = new ConcurrentDictionary<uint, byte?>(); //紀錄連線中的所有INV的Phase(以數值紀錄，非字串)
        public double INV_IP_V_PHASE1 = 0; //Input     V      : max value in all INVs
        public double INV_IP_F_PHASE1 = 0; //Input     F      : sum value in all INVs
        public double INV_OP_V_PHASE1 = 0; //Output    V      : max value in all INVs
        public double INV_OP_F_PHASE1 = 0; //Output    F      : sum value in all INVs
        public double INV_OP_A_PHASE1 = 0; //Output    A      : sum value in all INVs
        public double INV_OP_Load_PHASE1 = 0; //Output    Load   : sum value in all INVs

        public double[] Sys_IP_V_Phases = new double[3];        //records the max IP_V in each phase(phase1, phase2, phase3)
        public double[] SyS_IP_F_Phases = new double[3];        //records the max IP_F in each phase(phase1, phase2, phase3)
        public double[] Sys_OP_V_Phases = new double[3];        //records the max OP_V in each phase(phase1, phase2, phase3)
        public double BAT_V = 0; //BAT_V            : max value in all INVs
        private string? INV_ModelName_tmp = "";
        public Real_allDeviceData Real_Devices_ReadData { get; set; } = new Real_allDeviceData();
        public allDevice_Data Device_ReadData { get; set; } = new allDevice_Data();                  //Polling讀取各Device資料
        public LinkedDeviceStore LinkedDevices { get; } = new LinkedDeviceStore();                      //以concurrent字典記錄目前有連線的devices
        public WriteAPI_Datas Device_WriteData { get; set; } = new WriteAPI_Datas();
        /// <summary>
        /// 外層string => port,
        /// 內層string => protocol
        /// </summary>
        

        public List<SubAppSystem> SubSystems { get; set; } = new List<SubAppSystem>();
        public int? ActiveSubAppSystemID { get; set; }

        public GlobalVar(IGroupsDataDecoder decoder,
                            HeartbeatService heartbeats,
                            LinkAddrManager linkAddrManager,
                            SubSystemManager subSystemManager)
        {
            //For log
            _category = GetType().FullName!;

            //[inject]
            _decoder = decoder;
            _heartbeat = heartbeats;
            _linkAddrManager = linkAddrManager;
            _subSystemManager = subSystemManager;
            //Init
            // initSubAppSystem();

            //Events(Actions)
            LinkedDevices.linkChanged += Get_INV_ConnectNum;
            _heartbeat.OnTick += TickTask;
        }



        public void initSubAppSystem()
        {
            // SubSystems.Add(new SubAppSystem { subSystemID = 0, port = "CAN1", protocolFileName = "NTN-5K_CAN.json", startAddr = 0, length = ConstDefinition.Max_PortDeviceNum });
            // Device_WriteData.SubAppSystem_WriteMemorys.Add(new SubAppSystem_WriteMemory(0));

            // SubSystems.Add(new SubAppSystem { subSystemID = 1, port = "CAN2", protocolFileName = "NTN-5K_CAN.json", startAddr = 0, length = ConstDefinition.Max_PortDeviceNum });
            // Device_WriteData.SubAppSystem_WriteMemorys.Add(new SubAppSystem_WriteMemory(1));

            // SubSystems.Add(new SubAppSystem { subSystemID = 2, port = "MOD1", protocolFileName = "NTN-5K_MOD.json", startAddr = 0, length = ConstDefinition.Max_PortDeviceNum });
            // Device_WriteData.SubAppSystem_WriteMemorys.Add(new SubAppSystem_WriteMemory(2));

            // SubSystems.Add(new SubAppSystem { subSystemID = 3, port = "MOD2", protocolFileName = "NTN-5K_MOD.json", startAddr = 0, length = ConstDefinition.Max_PortDeviceNum });
            // Device_WriteData.SubAppSystem_WriteMemorys.Add(new SubAppSystem_WriteMemory(3));
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



        public void updateSystemVar()
        {
            // Compute_SYS_Phase();
            // if((Sys_PhaseStatus & 0x1) != 0)
            // {

            // }
            // if((Sys_PhaseStatus & 0x2) != 0)
            // {

            // }
            // if((Sys_PhaseStatus & 0x4) != 0)
            // {
            //     INV_IP_V = Compute_INV_IP_V(2);
            //     INV_IP_F = Compute_INV_IP_F(2);
            //     INV_OP_V = Compute_INV_OP_V(2);
            //     INV_OP_F = Compute_INV_OP_F(2);
            //     INV_OP_A = Compute_INV_OP_A(2);
            // } 
            // BAT_V = Compute_BAT_V()
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

        public string? Get_ModelName_ByAddr(uint addr)
        {
            try
            {

                var oneDeviceData_Copy = Real_Devices_ReadData.Get_oneDevice_DataSnapshot(addr);
                if (oneDeviceData_Copy == null) return null;

                var tmp_modelName = oneDeviceData_Copy.parseCmdData("MFR_MODEL");
                if (tmp_modelName is null) return null;

                // object tmp_modelName = _decoder.Decode(groups_copy, "MFR_MODEL", addr);
                // if(tmp_modelName == null) return null;

                AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_ModelName_ByAddr] tmp_modelName : {(string)tmp_modelName}", AppLogLevel.Trace);
                return (string)tmp_modelName;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_ModelName_ByAddr] Error : {e}", AppLogLevel.Error);
                return null;
            }
        }

        public List<(uint, string)>? Get_addr_and_ModelNames_List()
        {
            try
            {
                var addr_ModelName_pairlist = new List<(uint, string)>();
                for (uint addr = 0; addr < 256; addr++)
                {
                    var tmp_modelName = Get_ModelName_ByAddr(addr);

                    if (tmp_modelName == null) continue;

                    var pair = (addr, tmp_modelName);
                    addr_ModelName_pairlist.Add(pair);
                }

                if (addr_ModelName_pairlist.Count > 0)
                {
                    AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_addr_and_ModelNames_List] addr_ModelName_pairlist.Count : {addr_ModelName_pairlist.Count}", AppLogLevel.Trace);
                    return addr_ModelName_pairlist;
                }
                return null;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_addr_and_ModelNames_List] Error : {e}", AppLogLevel.Error);
                return null;
            }
        }

        public Dictionary<string, string>? Get_oneDevice_Cmd_unit_Dict(uint addr)
        {
            try
            {
                var oneDeviceData_Copy = Real_Devices_ReadData.Get_oneDevice_DataSnapshot(addr);
                if (oneDeviceData_Copy == null) return null;

                var oneDeviceData_cmd_unit_Dict = oneDeviceData_Copy.Get_Cmd_Unit_Dict();
                if (oneDeviceData_cmd_unit_Dict is null || oneDeviceData_cmd_unit_Dict.Count == 0) return null;

                return oneDeviceData_cmd_unit_Dict;

                // var oneDeivceData_ref = Device_ReadData.Get_oneDeviceData(addr);
                // if(oneDeivceData_ref == null) return null;

                // var oneDeviceData_cmd_unit_Dict = oneDeivceData_ref.Get_Cmd_unit_Dict();
                // if(oneDeviceData_cmd_unit_Dict == null || oneDeviceData_cmd_unit_Dict.Count == 0) return null;

                // return oneDeviceData_cmd_unit_Dict;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_oneDevice_Cmd_unit_Dict] Error : {e}", AppLogLevel.Error);
            }

            return null;
        }

        public string? Get_INV_Fault(uint addr)
        {
            // var cmd = "INV_FAULT";
            // var targetGroups = Device_ReadData.GetCommandGroups(addr, cmd);
            // if(targetGroups is null) return null;

            // var decoded = _decoder.Decode(targetGroups, cmd, addr);
            // AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_INV_Fault] decoded = {(string)decoded}", AppLogLevel.Trace);

            // if(string.IsNullOrEmpty((string)decoded)) return null;

            return null;
        }


        public byte? Get_INV_Phase(uint addr)
        {
            string cmd = "INV_STATUS";
            var targetGroups = Device_ReadData.GetCommandGroups(addr, cmd);
            if (targetGroups is null) return null;

            var data = (targetGroups.Groups[0].Data[1]) & 0x03;  //判斷相位的bit在LSB 2位
            byte? return_val = 0;
            switch (data)
            {
                case INV_PHASE_0:
                    return_val = INV_PHASE_0;
                    break;
                case INV_PHASE_180:
                    return_val = INV_PHASE_180;
                    break;
                case INV_PHASE_120:
                    return_val = INV_PHASE_120;
                    break;
                case INV_PHASE_M120:
                    return_val = INV_PHASE_M120;
                    break;
                default:
                    return_val = null;
                    break;
            }


            //如果decode很慢的話，要換成data直接位元運算，寫switch-case取string
            // var decoded = _decoder.Decode(targetGroups, cmd);
            // if(decoded is null) return null;


            AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_INV_Phase] addr = {addr}, return_val = {return_val}", AppLogLevel.Trace);
            // return (string)decoded;
            return return_val;
        }

        private int? findActiveSubAppSys(uint firstAddr)
        {
            int? activeSubSysID = null;

            foreach (var subSys in SubSystems)
            {
                uint portStartAddr = (uint)getPortStartAddr(subSys.port);
                uint trueStartAddr = (uint)portStartAddr + (uint)subSys.startAddr;
                uint trueEndAddr = (uint)trueStartAddr + (uint)subSys.length - 1;

                if (firstAddr < trueStartAddr) continue;

                if (firstAddr > trueEndAddr) continue;

                activeSubSysID = subSys.subSystemID;

                break;
            }

            return activeSubSysID;
        }

        /// <summary>
        /// 計算各子系統的系統變數
        /// </summary>
        private void ComputeOverallValues()
        {

            // //1. 取得快照
            var allSubSystems = _subSystemManager.GetAllSubSystems_Ref_In_List(); //各子系統快照
            var LinkingAddrs = _linkAddrManager.SnapshotAsHashSet(); //連線中的addr快照

            // //2. 在每個子系統裡面計算系統變數
            foreach (var subSys in allSubSystems)
            {
                
                AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] SubSystem Port = {subSys.Port}, Protocol = {subSys.Protocol}, AddrSet Count = {subSys.AddrSet.Count}", AppLogLevel.Trace);
                //3. 使用各子系統的AddrSet中的addr搭配Link，取得目前在線上的device data進行計算
                List<Real_SingleDeviceData_JsonFormat> onlineDevices_DeviceDatas = new();
                List<uint> debug_Addrs = new();
                foreach (var read_addr in subSys.AddrSet)
                {
                    uint nowStoringAddr = Custom.getAddrOffsetByPort(subSys.Port) + read_addr;

                    //如果 在子系統中的Addr，目前沒有連線，則直接跳過計算
                    if (!LinkingAddrs.Contains(nowStoringAddr)) continue;
                    
                    debug_Addrs.Add(nowStoringAddr);


                    var deviceData = Real_Devices_ReadData.Get_oneDevice_DataSnapshot(nowStoringAddr);
                    if (deviceData != null)
                    {
                        onlineDevices_DeviceDatas.Add(deviceData);
                    }
                }

                // Console.WriteLine($"[GlobalData][ComputeOverallValues] Port = {subSys.Port}, protocol = {subSys.Protocol}, onlineDevices count = {onlineDevices_DeviceDatas.Count}, addrs: {string.Join(",", debug_Addrs)}");
                //4. 使用onlineDevices_DeviceDatas進行系統變數計算
                subSys.ComputeOverallValues_in_SubSystem(onlineDevices_DeviceDatas);
                // Console.WriteLine($"[GlobalData][ComputeOverallValues] Port = {subSys.Port}, protocol = {subSys.Protocol}, nowMode = {subSys.nowMode}");
            }

            #region Old Code

            // uint[] linkedAddr_array = LinkedDevices.Snapshot();
            // //For computing MODE
            // uint INV = 0, Saving = 0, ByPass = 0;
            // uint Charging = 0, Standby = 0;
            // //For computing IP/OP V
            // double[] tmpMaxs_INV_IP_V = new double[3]; //[0]:phase1 ; [1]:phase2; [2]:phase3
            // double[] tmpMaxs_INV_OP_V = new double[3];
            // //For computing IP/OP F
            // double[] tmp_accumulate_IP_F = new double[3];
            // double[] tmp_accumulate_OP_F = new double[3];

            // try
            // {
            //     //Debug
            //     // Debug_Print_mdlName(0);
            //     // Debug_Print_mdlName(64);
            //     // Debug_Print_mdlName(128);
            //     // Debug_Print_mdlName(192);
            //     //initialize temp variables
            //     INV_isCHG_Enable = false;
            //     INV_isAC_Standby = false;
            //     INV_ModelName_tmp = "";

            //     //clear SysModelName
            //     if (linkedAddr_array.Length == 0)
            //     {
            //         Sys_ModelName = "";
            //     }
            //     else
            //     {
            //         uint firstAddr = linkedAddr_array[0];

            //         //取得 temp ModelName 準備與舊ModelName進行比對
            //         INV_ModelName_tmp = Get_ModelName_ByAddr(firstAddr);
            //         // INV_ModelName_tmp = "NTN-5K-248  "; //測試時，ModelName都是[0,0,0,0,0,0]時使用
            //         //Assign 當前 Active的subAppSystem (後續應可動態調整，當前先以第一個linkedAddr 所在的範圍作為 Active subAppSystem
            //         ActiveSubAppSystemID = findActiveSubAppSys(firstAddr);

            //         AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] firstAddr = {firstAddr}, INV_ModelName_tmp = {INV_ModelName_tmp}, ActiveSubAppSystemID = {ActiveSubAppSystemID}", AppLogLevel.Trace);
            //     }

            //     for (int index = 0; index < linkedAddr_array.Length; index++)
            //     {
            //         //for loop init local var
            //         string? fault_str = "";
            //         byte? phase_byte = 0;
            //         byte? status_byte = 0;

            //         //get the addr we are processing now                    
            //         uint addr = linkedAddr_array[index];

            //         //process ModelError
            //         // if(Sys_modelError is false)
            //         // {
            //         //     string? Iter_ModelName = Get_ModelName_ByAddr(addr);
            //         //     if(!string.IsNullOrEmpty(Iter_ModelName))
            //         //     {
            //         //         if(!string.Equals(Iter_ModelName, INV_ModelName_tmp, StringComparison.Ordinal))
            //         //         {
            //         //             INV_ModelName_tmp = "Model_ERROR";
            //         //             Sys_modelError = true;
            //         //         }
            //         //     }
            //         // }

            //         //Process INV_FAULT
            //         // Parse_INV_FAULT(addr, out fault_str);
            //         //Process INV_Phase and Status
            //         byte singleDevice_mode = Parse_INV_Mode(addr);
            //         // Console.WriteLine($"[ComputeOverallValues] addr={addr}, status_byte={singleDevice_mode}");
            //         //!!!這裡的case要用ConstDefinition的Enum來寫
            //         switch (singleDevice_mode)
            //         {
            //             case (byte)ConstDefinition.SYS_Mode_Options.DISCON: break;
            //             case (byte)ConstDefinition.SYS_Mode_Options.ERROR: break;
            //             case (byte)ConstDefinition.SYS_Mode_Options.INVERTER: INV++; break;
            //             case (byte)ConstDefinition.SYS_Mode_Options.SAVING: Saving++; break;
            //             case (byte)ConstDefinition.SYS_Mode_Options.BY_PASS: ByPass++; break;
            //             case (byte)ConstDefinition.SYS_Mode_Options.CHARGER: Charging++; break;
            //             case (byte)ConstDefinition.SYS_Mode_Options.STANDBY: Standby++; break;
            //             default: break;
            //         }

            //         //Process tmpMaxValue of each phase corresponding the addr
            //         Compute_INV_IP_V(tmpMaxs_INV_IP_V, addr);
            //         Compute_INV_OP_V(tmpMaxs_INV_OP_V, addr);
            //         //process IP/OP F of each phase corresponding the addr

            //     }

            //     Sys_Charger_Enable = INV_isCHG_Enable;
            //     Sys_isAC_Standby = INV_isAC_Standby;
            //     Sys_ModelName_Assign();
            //     AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] Sys_ModelName = {Sys_ModelName}", AppLogLevel.Trace);
            //     Sys_INV_Mode_Assign(INV, Saving, ByPass, Charging, Standby);
            //     AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] Sys_INV_Mode : {Sys_INV_Mode}", AppLogLevel.Trace);
            //     // AssignMaxs_INV_IP_V(tmpMaxs_INV_IP_V);
            //     AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] Sys_IP_V_Phases : {string.Join(", ", Sys_IP_V_Phases)}", AppLogLevel.Debug);
            //     // AssignMaxs_INV_OP_V(tmpMaxs_INV_OP_V);
            //     AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] Sys_OP_V_Phases : {string.Join(", ", Sys_OP_V_Phases)}", AppLogLevel.Debug);
            // }
            // catch (Exception e)
            // {
            //     AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] Error : {e}", AppLogLevel.Error);
            // }
            #endregion Old Code
        }

        
        private async Task Sys_ModelName_Assign()
        {
            if (!string.Equals("Model_ERROR", INV_ModelName_tmp, StringComparison.Ordinal))
            {
                Sys_modelError = false;
            }
            if (!string.Equals(Sys_ModelName, INV_ModelName_tmp, StringComparison.Ordinal))
            {//modelname changed

                Sys_ModelName = INV_ModelName_tmp;
                Console.WriteLine($"[Sys_ModelName_Assign] mdlName Change");
                //inform hooked Events

                if (Sys_ModelName_OnChanged is not null)
                {
                    foreach (Func<Task> handler in Sys_ModelName_OnChanged.GetInvocationList())
                    {
                        try
                        {
                            await handler();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Sys_ModelName_Assign] Handler failed: {ex.Message}");
                        }
                    }

                    AppLogger.Log_To_File_log(_category, $"[GlobalData][Sys_ModelName_Assign] have Hook", AppLogLevel.Trace);
                }
                else
                {
                    AppLogger.Log_To_File_log(_category, $"[GlobalData][Sys_ModelName_Assign] No Hook", AppLogLevel.Trace);
                }
            }
        }

        private void Parse_INV_FAULT(uint addr, out string? fault)
        {
            // string fault_cmd = "INV_FAULT";
            // try
            // {
            //     var targetGroups = Device_ReadData.GetCommandGroups(addr, fault_cmd);
            //     if(targetGroups == null)
            //     {
            //         fault = null;
            //         return;
            //     }

            //     string? tmp = (string?)_decoder.Decode(targetGroups, fault_cmd, addr);
            //     fault = tmp;
            // }
            // catch(Exception e)
            // {
            //     AppLogger.Log_To_File_log(_category, $"[GlobalData][Parse_INV_FAULT] Error : {e}", AppLogLevel.Error);
            //     fault = null;
            // }
            fault = null;
        }

        private byte Parse_INV_Mode(uint addr)
        {
            try
            {
                byte status = 0;
                string status_Cmd = "INV_STATUS";
                var INV_STATUS_DecodeList = (List<decodeContent>?)(Real_Devices_ReadData.Get_oneDevice_DataSnapshot(addr)?.parseCmdData(status_Cmd));
                if (INV_STATUS_DecodeList is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[GlobalData][Parse_INV_Mode] addr:{addr}, INV_STATUS_DecodeList is null", AppLogLevel.Trace);
                    status = (byte)ConstDefinition.SYS_Mode_Options.DISCON;
                    return status;
                }

                string? INV_STATUS_str = BitFieldParser.FitParserFunction(status_Cmd, INV_STATUS_DecodeList);
                switch (INV_STATUS_str)
                {
                    case ConstDefinition.INVERTER_MODE_str:
                        status = (byte)ConstDefinition.SYS_Mode_Options.INVERTER;
                        break;
                    case ConstDefinition.SAVING_MODE_str:
                        status = (byte)ConstDefinition.SYS_Mode_Options.SAVING;
                        break;
                    case ConstDefinition.BYPASS_MODE_str:
                        status = (byte)ConstDefinition.SYS_Mode_Options.BY_PASS;
                        break;
                    case ConstDefinition.CHARGING_MODE_str:
                        status = (byte)ConstDefinition.SYS_Mode_Options.CHARGER;
                        break;
                    case ConstDefinition.STANDBY_MODE_str:
                        status = (byte)ConstDefinition.SYS_Mode_Options.STANDBY;
                        break;
                    default:
                        status = (byte)ConstDefinition.SYS_Mode_Options.DISCON;
                        break;
                }
                // Console.WriteLine($"[Parse_INV_Mode] addr:{addr}, status : {status}");
                return status;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Parse_INV_Mode] Error : {e}", AppLogLevel.Error);
                return (byte)ConstDefinition.SYS_Mode_Options.DISCON;
            }
        }

        private void GetMaxs_INV_V(byte SysPhase_Index, byte? addrPhase, double[] tmpMaxs, double decodedValue)
        {
            if (addrPhase is null) return;

            byte judgedPhase = judgeNowPhase(SysPhase_Index);
            if (addrPhase == judgedPhase)
            {
                switch (judgedPhase)
                {
                    case INV_PHASE_0:
                    case INV_PHASE_180:
                    case INV_PHASE_120:
                    case INV_PHASE_M120:
                        if (decodedValue > tmpMaxs[SysPhase_Index])
                        {
                            tmpMaxs[SysPhase_Index] = decodedValue;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void Compute_INV_IP_V(double[] tmpMaxs, uint addr)
        {
            // string Cmd = "READ_VIN";
            // try
            // {
            //     //解碼
            //     var targetGroups = Device_ReadData.GetCommandGroups(addr, Cmd);
            //     if(targetGroups == null) return;
            //     var value = _decoder.Decode(targetGroups, Cmd, addr);


            //     //取出addr對應的phase
            //     if(INVs_Phase.TryGetValue(addr, out byte? addrPhase))
            //     {//累加各phase的tmpMax

            //         //For phase 1
            //         if((Sys_PhaseStatus & 0x1) != 0)
            //         {   
            //             GetMaxs_INV_V(0, addrPhase, tmpMaxs, (double)value);
            //             AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_IP_V] Sys_PhaseStatus = {Sys_PhaseStatus}, addr = {addr}, decode_VIN = {value} Sys_IP_V_Phases : {string.Join(", ", Sys_IP_V_Phases)}", AppLogLevel.Debug);
            //         } 
            //         //For phase 2
            //         if((Sys_PhaseStatus & 0x2) != 0)
            //         {
            //             GetMaxs_INV_V(1, addrPhase, tmpMaxs, (double)value);
            //             AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_IP_V] Sys_PhaseStatus = {Sys_PhaseStatus}, addr = {addr}, decode_VIN = {value} Sys_IP_V_Phases : {string.Join(", ", Sys_IP_V_Phases)}", AppLogLevel.Debug);
            //         }
            //         //For phase 3
            //         if((Sys_PhaseStatus & 0x4) != 0)
            //         {
            //             GetMaxs_INV_V(2, addrPhase, tmpMaxs, (double)value);
            //             AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_IP_V] Sys_PhaseStatus = {Sys_PhaseStatus}, addr = {addr}, decode_VIN = {value} Sys_IP_V_Phases : {string.Join(", ", Sys_IP_V_Phases)}", AppLogLevel.Debug);
            //         }    
            //     }
            // }
            // catch(Exception e)
            // {
            //     AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_IP_V] Error : {e}", AppLogLevel.Error);
            // }
        }

        private void AssignMaxs_INV_IP_V(double[] tmpMaxs)
        {
            Array.Copy(tmpMaxs, Sys_IP_V_Phases, tmpMaxs.Length);
        }

        private void Compute_INV_OP_V(double[] tmpMaxs, uint addr)
        {
            // string Cmd = "READ_AC_VOUT";
            // try
            // {
            //     //解碼
            //     var targetGroups = Device_ReadData.GetCommandGroups(addr, Cmd);
            //     if(targetGroups == null) return;
            //     var value = _decoder.Decode(targetGroups, Cmd, addr);


            //     //取出addr對應的phase
            //     if(INVs_Phase.TryGetValue(addr, out byte? addrPhase))
            //     {//累加各phase的tmpMax

            //         //For phase 1
            //         if((Sys_PhaseStatus & 0x1) != 0)
            //         {   
            //             GetMaxs_INV_V(0, addrPhase, tmpMaxs, (double)value);
            //             AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_V] Sys_PhaseStatus = {Sys_PhaseStatus}, addr = {addr}, decode_VOUT = {value} Sys_OP_V_Phases : {string.Join(", ", Sys_OP_V_Phases)}", AppLogLevel.Debug);
            //         } 
            //         //For phase 2
            //         if((Sys_PhaseStatus & 0x2) != 0)
            //         {
            //             GetMaxs_INV_V(1, addrPhase, tmpMaxs, (double)value);
            //             AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_V] Sys_PhaseStatus = {Sys_PhaseStatus}, addr = {addr}, decode_VOUT = {value} Sys_OP_V_Phases : {string.Join(", ", Sys_OP_V_Phases)}", AppLogLevel.Debug);
            //         }
            //         //For phase 3
            //         if((Sys_PhaseStatus & 0x4) != 0)
            //         {
            //             GetMaxs_INV_V(2, addrPhase, tmpMaxs, (double)value);
            //             AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_V] Sys_PhaseStatus = {Sys_PhaseStatus}, addr = {addr}, decode_VOUT = {value} Sys_OP_V_Phases : {string.Join(", ", Sys_OP_V_Phases)}", AppLogLevel.Debug);
            //         }    
            //     }
            // }
            // catch(Exception e)
            // {
            //     AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_V] Error : {e}", AppLogLevel.Error);
            // }
        }

        private void AssignMaxs_INV_OP_V(double[] tmpMaxs)
        {
            Array.Copy(tmpMaxs, Sys_OP_V_Phases, tmpMaxs.Length);
        }

        private void Compute_SYS_Phase()
        {
            //只取當前有連線的設備
            uint[] linkedAddr_array = LinkedDevices.Snapshot();

            AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] linkedAddr_array.Length = {linkedAddr_array.Length}", AppLogLevel.Trace);

            byte Phase_status_temp = 0x01; //暫存系統Status

            for (int index = 0; index < linkedAddr_array.Length; index++)
            {
                uint addr = linkedAddr_array[index];
                byte? addr_phase = Get_INV_Phase(addr);

                //紀錄(addr, addr_phase)到INVs_Phase
                INVs_Phase[addr] = addr_phase;

                //INV status is out of cases
                if (addr_phase == null) continue; //which may be the device from other Company


                switch (addr_phase)
                {
                    case INV_PHASE_0:
                        if (Phase_status_temp <= INV_SINGLE_PHASE)
                        {
                            Phase_status_temp = INV_SINGLE_PHASE;
                        }
                        AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] addr = {addr}, addr_phase = {Phase_status_temp}", AppLogLevel.Trace);
                        break;
                    case INV_PHASE_120:
                    case INV_PHASE_M120:
                        if (Phase_status_temp <= INV_THREE_PHASE)
                        {
                            Phase_status_temp = INV_THREE_PHASE;
                        }
                        AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] addr = {addr}, addr_phase = {Phase_status_temp}", AppLogLevel.Trace);
                        break;
                    case INV_PHASE_180:
                        if (Phase_status_temp <= INV_TWO_PHASE)
                        {
                            Phase_status_temp = INV_TWO_PHASE;
                        }
                        AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] addr = {addr}, addr_phase = {Phase_status_temp}", AppLogLevel.Trace);
                        break;

                    default:
                        AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] addr = {addr}, default : addr_phase = {addr_phase}", AppLogLevel.Trace);
                        break;
                }
            }

            Sys_PhaseStatus = Phase_status_temp;
        }

        private byte judgeNowPhase(uint phase)
        {
            byte Phase_temp = 0;
            //判斷當前相位
            if (Sys_PhaseStatus == INV_SINGLE_PHASE)
            {
                Phase_temp = INV_PHASE_0;
            }
            else if (Sys_PhaseStatus == INV_TWO_PHASE)
            {
                if (phase == 0)
                {
                    Phase_temp = INV_PHASE_0;
                }
                else if (phase == 1)
                {
                    Phase_temp = INV_PHASE_180;
                }
            }
            else if (Sys_PhaseStatus == INV_THREE_PHASE)
            {
                if (phase == 0)
                {
                    Phase_temp = INV_PHASE_0;
                }
                else if (phase == 1)
                {
                    Phase_temp = INV_PHASE_120;

                }
                else if (phase == 2)
                {
                    Phase_temp = INV_PHASE_M120;
                }
            }
            return Phase_temp;
        }

        private void Debug_Print_mdlName(uint addr)
        {
            var oneDeviceData = Real_Devices_ReadData.Get_oneDevice_DataSnapshot(addr);
            if (oneDeviceData is null)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Debug_Print_mdlName] addr = {addr}, oneDeviceData is null", AppLogLevel.Trace);
                Console.WriteLine($"[Debug_Print_mdlName] addr = {addr}, oneDeviceData is null");
                return;
            }
            string? mdlName = (string?)oneDeviceData.parseCmdData("MFR_MODEL");

            AppLogger.Log_To_File_log(_category, $"[GlobalData][Debug_Print_mdlName] addr = {addr}, mdlName = {mdlName}", AppLogLevel.Trace);
            Console.WriteLine($"[Debug_Print_mdlName] addr = {addr}, mdlName = {mdlName}");
        }

        
    }
    
    
}