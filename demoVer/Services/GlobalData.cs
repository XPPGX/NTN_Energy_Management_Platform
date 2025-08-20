using demoVer.Models;
using demoVer.Utils;
using System.Threading;
using demoVer.Services;
using demoVer.Interfaces;
using System.Collections.Concurrent;

namespace demoVer.Services
{
    public class GlobalVar
    {
        //[Debug]
        public bool Debug_Flag {get; set;} = true;
        //[inject]
        private readonly IGroupsDataDecoder _decoder;
        private readonly HeartbeatService _heartbeat;
        //For log
        private string _category;
        private bool _disposed;

        //Const
        public const int CAN1_addr              = 0;
        public const int CAN2_addr              = 64;
        public const int MOD1_addr              = 128;
        public const int MOD2_addr              = 192;

        public const uint STATUS_INV            = 0x00000001 << 0;  //STATUS : 比對bit
        public const uint STATUS_BYP            = 0x00000001 << 1;  //STATUS : 比對bit
        public const uint STATUS_UTI_OK         = 0x00000001 << 2;  //STATUS : 比對bit
        public const uint STATUS_CHG            = 0x00000001 << 3;  //STATUS : 比對bit
        public const uint STATUS_SOLAR_EN       = 0x00000001 << 4;  //STATUS : 比對bit
        public const uint STATUS_SAVING         = 0x00000001 << 5;  //STATUS : 比對bit
        public const uint STATUS_BAT_LOW_ALM    = 0x00000001 << 6;  //STATUS : 比對bit

        public const byte STATUS_INV_DISCON         = 0;
        public const byte STATUS_INV_ERROR          = 3;
        public const byte STATUS_INV_INVERTER       = 4;
        public const byte STATUS_INV_SAVING         = 5;
        public const byte STATUS_INV_BY_PASS        = 6;
        public const byte STATUS_INV_CHARGING       = 7;
        public const byte STATUS_INV_STANDBY        = 8;
        public const byte STATUS_INV_BATTERY_FIRST  = 9;
        
        public const byte INV_MODE_INVERTER         = 0;
        public const byte INV_MODE_SAVING           = 1;
        public const byte INV_MODE_BY_PASS          = 2;
        public const byte INV_MODE_CHARGING         = 3;
        public const byte INV_MODE_STANDBY          = 4;
        public const byte INV_MODE_SHUTDOWN         = 5;
        public const byte INV_MODE_BATTERY_FIRST    = 6;
        public const byte INV_MODE_NONE             = 0xFF;

        public const byte INV_PHASE_0           = 0;
        public const byte INV_PHASE_180         = 1;
        public const byte INV_PHASE_120         = 2;
        public const byte INV_PHASE_M120        = 3;
        public const byte INV_SINGLE_PHASE      = 0x01;
        public const byte INV_TWO_PHASE         = 0x03;
        public const byte INV_THREE_PHASE       = 0x07;

        
        //Variable
        private bool IsSteady {get; set;} = false;

        private int _invConnectNum;
        public int INV_ConnectNum
        {
            get => Volatile.Read(ref _invConnectNum);
            private set => Volatile.Write(ref _invConnectNum, value);
        }

        //System variables(Heartbeat.OnTick)
        //如果計算速度很慢，可以把所有算式都放在某個foreach INVs_Phase裡面
        public byte Sys_PhaseStatus     = 0;
        public byte Sys_INV_Mode        = 0;
        public bool Sys_Charger_Enable  = false;
        public bool Sys_isAC_Standby    = false;
        public string? ModelName        = "";
        public ConcurrentDictionary<uint, byte?> INVs_Phase {get; set;} //紀錄連線中的所有INV的Phase(以數值紀錄，非字串)
        public double INV_IP_V_PHASE1               = 0; //Input     V      : max value in all INVs
        public double INV_IP_F_PHASE1               = 0; //Input     F      : sum value in all INVs
        public double INV_OP_V_PHASE1               = 0; //Output    V      : max value in all INVs
        public double INV_OP_F_PHASE1               = 0; //Output    F      : sum value in all INVs
        public double INV_OP_A_PHASE1               = 0; //Output    A      : sum value in all INVs
        public double INV_OP_Load_PHASE1            = 0; //Output    Load   : sum value in all INVs
        public double BAT_V                         = 0; //BAT_V            : max value in all INVs
        private bool INV_isCHG_Enable               = false;
        private bool INV_isAC_Standby               = false;


        //READ API Data Structure
        public allDevice_Data Device_ReadData {get; set;}       //Polling讀取各Device資料
        public LinkedDeviceStore LinkedDevices {get;}           //以concurrent字典記錄目前有連線的devices
        public List<SettingData> Setting_CAN_Check {get; set;}  //為檢查是否與Write後，APP與Framework的data一致，所以要讀回來
        public List<SettingData> Setting_MOD_Check {get; set;}  //為檢查是否與Write後，APP與Framework的data一致，所以要讀回來

        //Write API Data Structure
        public List<SettingData> Setting_CAN_Write {get; set;}
        public List<SettingData> Setting_MOD_Write {get; set;}
        
        public GlobalVar(   IGroupsDataDecoder decoder,
                            HeartbeatService heartbeats)
        {
            //For log
            _category                   = GetType().FullName!;
            
            //[inject]
            _decoder                    = decoder;
            _heartbeat                  = heartbeats;

            //[System Variable]
            INVs_Phase                  = new ConcurrentDictionary<uint, byte?>();

            //[data structure instances]
            Device_ReadData             = new allDevice_Data();
            LinkedDevices               = new LinkedDeviceStore();
            Setting_CAN_Check           = new List<SettingData>();
            Setting_CAN_Write           = new List<SettingData>();
            Setting_MOD_Check           = new List<SettingData>();
            Setting_MOD_Write           = new List<SettingData>();

            
            //Events(Actions)
            LinkedDevices.linkChanged   += Get_INV_ConnectNum;
            _heartbeat.OnTick           += TickTask;
        }

        public async Task TickTask()
        {
            // updateSystemVar();
            ComputeOverallValues();
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
            switch(port)
            {
                case "CAN1" : return CAN1_addr;
                case "CAN2" : return CAN2_addr;
                case "MOD1" : return MOD1_addr;
                case "MOD2" : return MOD2_addr;
                default : return 0;
            }
        }

        public void Get_INV_ConnectNum()
        {
            INV_ConnectNum = LinkedDevices.Snapshot().Length;
            AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_INV_Connection] INV_ConnectNum = {INV_ConnectNum}", AppLogLevel.Trace);
        }

        public void Dispose()
        {
            if(_disposed) return;

            LinkedDevices.linkChanged -= Get_INV_ConnectNum;

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public string? Get_ModelName_ByAddr(uint addr)
        {
            try
            {
                var groups_copy = Device_ReadData.GetCommandGroups(addr, "MFR_MODEL");
                if(groups_copy == null) return null;
                
                
                object tmp_modelName = _decoder.Decode(groups_copy, "MFR_MODEL");
                if(tmp_modelName == null) return null;
                
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_ModelName_ByAddr] tmp_modelName : {(string)tmp_modelName}", AppLogLevel.Trace);
                return (string)tmp_modelName;
            }
            catch(Exception e)
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
                for(uint addr = 0 ; addr < 256 ; addr ++)
                {
                    string tmp_modelName = Get_ModelName_ByAddr(addr);

                    if(tmp_modelName == null) continue;

                    var pair = (addr, tmp_modelName);
                    addr_ModelName_pairlist.Add(pair);
                }

                if(addr_ModelName_pairlist.Count > 0)
                {
                    AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_addr_and_ModelNames_List] addr_ModelName_pairlist.Count : {addr_ModelName_pairlist.Count}", AppLogLevel.Trace);
                    return addr_ModelName_pairlist;
                }
                return null;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_addr_and_ModelNames_List] Error : {e}", AppLogLevel.Error);
                return null;
            }
        }

        public Dictionary<string, string>? Get_oneDevice_Cmd_unit_Dict(uint addr)
        {
            try
            {
                var tmp_cmd_unit_dict = new Dictionary<string ,string>();
                
                var oneDeivceData_ref = Device_ReadData.Get_oneDeviceData(addr);
                if(oneDeivceData_ref == null) return null;

                var oneDeviceData_cmd_unit_Dict = oneDeivceData_ref.Get_Cmd_unit_Dict();
                if(oneDeviceData_cmd_unit_Dict == null || oneDeviceData_cmd_unit_Dict.Count == 0) return null;
                
                return oneDeviceData_cmd_unit_Dict;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_oneDevice_Cmd_unit_Dict] Error : {e}", AppLogLevel.Error);
            }

            return null;
        }

        
        //只看是否有INV_FAULT
        // public uint Get_INV_Fault(uint INV_FAULT_Val)
        // {
        //     return INV_FAULT_Val & 0xFEFF;
        // }

        
        // public object Get_INV_Status(uint INV_FAULT_Val, uint INV_STATUS_Val, bool returnStr_Flag)
        // {
        //     if(Get_INV_Fault(INV_FAULT_Val) != 0){return returnStr_Flag ? "Error" : STATUS_INV_ERROR;}

        //     if(((INV_STATUS_Val & STATUS_INV) != 0) && ((INV_STATUS_Val & STATUS_SAVING) != 0)){return returnStr_Flag ? "Saving" : STATUS_INV_SAVING;}

        //     if((INV_STATUS_Val & STATUS_INV) != 0){return returnStr_Flag ? "Inverter" : STATUS_INV_INVERTER;}
            
        //     if((INV_STATUS_Val & STATUS_BYP) != 0){return returnStr_Flag ? "Bypass" : STATUS_INV_BY_PASS;}

        //     if((INV_STATUS_Val & STATUS_CHG) != 0){return returnStr_Flag ? "Charger" : STATUS_INV_CHARGING;}

        //     return returnStr_Flag ? "Standby" : STATUS_INV_STANDBY;
        // }


        public string? Get_INV_Fault(uint addr)
        {
            var cmd = "INV_FAULT";
            var targetGroups = Device_ReadData.GetCommandGroups(addr, cmd);
            if(targetGroups is null) return null;

            var decoded = _decoder.Decode(targetGroups, cmd);
            AppLogger.Log_To_File_log(_category, $"[GlobalData][Get_INV_Fault] decoded = {(string)decoded}", AppLogLevel.Trace);
            
            if(string.IsNullOrEmpty((string)decoded)) return null;
            
            return (string)decoded;
        }

        
        public object Get_INV_Status(string INV_FAULT_Val, uint INV_STATUS_Val, bool returnStr_Flag)
        {
            if(!string.IsNullOrEmpty(INV_FAULT_Val)){return returnStr_Flag ? INV_FAULT_Val : STATUS_INV_ERROR;}
            
            // if(Get_INV_Fault(INV_FAULT_Val) != 0){return returnStr_Flag ? "Error" : STATUS_INV_ERROR;}
            
            //chargerEnable判斷
            if(((INV_STATUS_Val) & STATUS_CHG) != 0)
            {
                INV_isCHG_Enable = true;
            }

            if((INV_STATUS_Val & STATUS_UTI_OK) != 0)
            {
                INV_isAC_Standby = true;
            }

            if(((INV_STATUS_Val & STATUS_INV) != 0) && ((INV_STATUS_Val & STATUS_SAVING) != 0)){return returnStr_Flag ? "Saving" : STATUS_INV_SAVING;}

            if((INV_STATUS_Val & STATUS_INV) != 0){return returnStr_Flag ? "Inverter" : STATUS_INV_INVERTER;}
            
            if((INV_STATUS_Val & STATUS_BYP) != 0){return returnStr_Flag ? "Bypass" : STATUS_INV_BY_PASS;}

            if((INV_STATUS_Val & STATUS_CHG) != 0){return returnStr_Flag ? "Charger" : STATUS_INV_CHARGING;}

            return returnStr_Flag ? "Standby" : STATUS_INV_STANDBY;
        }

       
        public byte? Get_INV_Phase(uint addr)
        {
            string cmd = "INV_STATUS";
            var targetGroups = Device_ReadData.GetCommandGroups(addr, cmd);
            if(targetGroups is null) return null;

            var data = (targetGroups.Groups[0].Data[1]) & 0x03;  //判斷相位的bit在LSB 2位
            byte? return_val = 0;
            switch(data)
            {
                case INV_PHASE_0 : 
                    return_val = INV_PHASE_0;
                    break;
                case INV_PHASE_180 : 
                    return_val = INV_PHASE_180;
                    break;
                case INV_PHASE_120 : 
                    return_val = INV_PHASE_120;
                    break;
                case INV_PHASE_M120 : 
                    return_val = INV_PHASE_M120;
                    break;
                default : 
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

        private void ComputeOverallValues()
        {
            uint[] linkedAddr_array = LinkedDevices.Snapshot();
            uint INV = 0, Saving = 0, ByPass = 0;
            uint Charging = 0, Standby = 0;
            try
            {
                INV_isCHG_Enable = false;
                INV_isAC_Standby = false;
                for(int index = 0 ; index < linkedAddr_array.Length ; index ++)
                {
                    string? fault_str = "";
                    byte? phase_byte = 0;
                    byte? status_byte = 0;
                    
                    uint addr = linkedAddr_array[index];
                    
                    Parse_INV_FAULT(addr, out fault_str);
                    Parse_INV_Phase_Status(addr, fault_str, out phase_byte, out status_byte);
                    
                    switch(status_byte)
                    {
                        case STATUS_INV_DISCON:
                        case STATUS_INV_ERROR:
                            break;
                        case STATUS_INV_INVERTER:
                            INV++;
                            break;
                        case STATUS_INV_SAVING:
                            Saving++;
                            break;
                        case STATUS_INV_BY_PASS:
                            ByPass++;
                            break;
                        case STATUS_INV_CHARGING:
                            Charging++;
                            break;
                        case STATUS_INV_STANDBY:
                            Standby++;
                            break;
                    }
                }
                Sys_Charger_Enable  = INV_isCHG_Enable;
                Sys_isAC_Standby    = INV_isAC_Standby;

                if(INV > 0){Sys_INV_Mode = INV_MODE_INVERTER;}
                else if(Saving > 0){Sys_INV_Mode = INV_MODE_SAVING;}
                else if(ByPass > 0){Sys_INV_Mode = INV_MODE_BY_PASS;}
                else if(Charging > 0){Sys_INV_Mode = INV_MODE_CHARGING;}
                else if(Standby > 0){Sys_INV_Mode = INV_MODE_STANDBY;}
                else {Sys_INV_Mode = INV_MODE_NONE;}
                
                AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] Sys_INV_Mode : {Sys_INV_Mode}", AppLogLevel.Trace);
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][ComputeOverallValues] Error : {e}", AppLogLevel.Error);
            }
        }

        private void Parse_INV_FAULT(uint addr, out string? fault)
        {
            string fault_cmd = "INV_FAULT";
            try
            {
                var targetGroups = Device_ReadData.GetCommandGroups(addr, fault_cmd);
                if(targetGroups == null)
                {
                    fault = null;
                    return;
                }

                string? tmp = (string?)_decoder.Decode(targetGroups, fault_cmd);
                fault = tmp;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Parse_INV_FAULT] Error : {e}", AppLogLevel.Error);
                fault = null;
            }
        }

        private void Parse_INV_Phase_Status(uint addr, string? fault_str, out byte? phase, out byte? status)
        {
            string status_Cmd = "INV_STATUS";
            try
            {
                var targetGroups = Device_ReadData.GetCommandGroups(addr, status_Cmd);
                if(targetGroups is null){phase=null; status=null; return;}
                
                byte INV_STATUS_Phase_data = (byte)((targetGroups.Groups[0].Data[1]) & 0x3);
                uint INV_STATUS_Whole_data = (((uint)targetGroups.Groups[0].Data[1]) << 8) | ((uint)targetGroups.Groups[0].Data[0]);
                
                //parse phase
                phase = INV_STATUS_Phase_data;
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Parse_INV_Phase_Status] phase : {phase}", AppLogLevel.Trace);

                //parse status
                status = (byte?)Get_INV_Status(fault_str, INV_STATUS_Whole_data, false);
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Parse_INV_Phase_Status] status : {status}", AppLogLevel.Trace);
            }
            catch(Exception e)
            {
                phase = null;
                status = null;
                AppLogger.Log_To_File_log(_category, $"[GlobalData][Parse_INV_Status] Error : {e}", AppLogLevel.Error);
            }
        }

        public void GetArrowDirections(out ArrowDirection G2M, out ArrowDirection M2L, out ArrowDirection M2B)
        {   
            //G : 市電
            //M : Machine(NTN)
            //L : Load
            //B : Battery

            //以下都是箭頭方向
            
            switch(Sys_INV_Mode)
            {
                case INV_MODE_INVERTER:
                    G2M = ArrowDirection.Hidden;
                    M2L = ArrowDirection.Right;
                    M2B = ArrowDirection.Up;
                    break;
                case INV_MODE_SAVING:
                    G2M = ArrowDirection.Hidden;
                    M2L = ArrowDirection.Right;
                    M2B = ArrowDirection.Up;
                    break;
                case INV_MODE_BY_PASS:
                    if(Sys_Charger_Enable)
                    {
                        G2M = ArrowDirection.Right;
                        M2L = ArrowDirection.Right;
                        M2B = ArrowDirection.Down;
                    }
                    else
                    {
                        G2M = ArrowDirection.Right;
                        M2L = ArrowDirection.Right;
                        M2B = ArrowDirection.Hidden;
                    }
                    break;
                case INV_MODE_CHARGING:
                    G2M = ArrowDirection.Right;
                    M2L = ArrowDirection.Hidden;
                    M2B = ArrowDirection.Down;
                    break;
                case INV_MODE_STANDBY:
                    if(Sys_isAC_Standby)
                    {
                        G2M = ArrowDirection.Right;
                        M2L = ArrowDirection.Hidden;
                        M2B = ArrowDirection.Hidden;
                    }
                    else
                    {
                        G2M = ArrowDirection.Hidden;
                        M2L = ArrowDirection.Hidden;
                        M2B = ArrowDirection.Up;
                    }
                    break;
                case INV_MODE_SHUTDOWN:
                    G2M = ArrowDirection.Hidden;
                    M2L = ArrowDirection.Hidden;
                    M2B = ArrowDirection.Up;
                    break;
                case INV_MODE_BATTERY_FIRST:
                    G2M = ArrowDirection.Left;
                    M2L = ArrowDirection.Right;
                    M2B = ArrowDirection.Up;
                    break;
                default:
                    G2M = ArrowDirection.Hidden;
                    M2L = ArrowDirection.Hidden;
                    M2B = ArrowDirection.Hidden;
                    break;
            }
        }
        // private void Compute_SYS_Phase()
        // {
        //     //只取當前有連線的設備
        //     uint[] linkedAddr_array = LinkedDevices.Snapshot();
            
        //     AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] linkedAddr_array.Length = {linkedAddr_array.Length}", AppLogLevel.Trace);
            
        //     byte Phase_status_temp = 0x01; //暫存系統Status
            
        //     for(int index = 0 ; index < linkedAddr_array.Length ; index ++)
        //     {
        //         uint addr = linkedAddr_array[index];
        //         byte? addr_phase = Get_INV_Phase(addr);

        //         //紀錄(addr, addr_phase)到INVs_Phase
        //         INVs_Phase[addr] = addr_phase;

        //         //INV status is out of cases
        //         if(addr_phase == null) continue; //which may be the device from other Company
                

        //         switch(addr_phase)
        //         {
        //             case INV_PHASE_0:
        //                 if(Phase_status_temp <= INV_SINGLE_PHASE)
        //                 {
        //                     Phase_status_temp = INV_SINGLE_PHASE;
        //                 }
        //                 AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] addr = {addr}, addr_phase = {Phase_status_temp}", AppLogLevel.Trace);
        //                 break;
        //             case INV_PHASE_120: 
        //             case INV_PHASE_M120:   
        //                 if(Phase_status_temp <= INV_THREE_PHASE)
        //                 {
        //                     Phase_status_temp = INV_THREE_PHASE;
        //                 }
        //                 AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] addr = {addr}, addr_phase = {Phase_status_temp}", AppLogLevel.Trace);
        //                 break;
        //             case INV_PHASE_180:
        //                 if(Phase_status_temp <= INV_TWO_PHASE)
        //                 {
        //                     Phase_status_temp = INV_TWO_PHASE;
        //                 }
        //                 AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] addr = {addr}, addr_phase = {Phase_status_temp}", AppLogLevel.Trace);
        //                 break;
                    
        //             default :
        //                 AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_SYS_Phase] addr = {addr}, default : addr_phase = {addr_phase}", AppLogLevel.Trace);
        //                 break;
        //         }
        //     }

        //     Sys_PhaseStatus = Phase_status_temp;
        // }

        // private byte judgeNowPhase(uint phase)
        // {
        //     byte Phase_temp = 0;
        //     //判斷當前相位
        //     if(Sys_PhaseStatus == INV_SINGLE_PHASE)
        //     {
        //         Phase_temp = INV_PHASE_0;
        //     }
        //     else if(Sys_PhaseStatus == INV_TWO_PHASE)
        //     {
        //         if(phase == 0)
        //         {
        //             Phase_temp = INV_PHASE_0;
        //         }
        //         else if(phase == 1)
        //         {
        //             Phase_temp = INV_PHASE_180;
        //         }
        //     }
        //     else if(Sys_PhaseStatus == INV_THREE_PHASE)
        //     {
        //         if(phase == 0)
        //         {
        //             Phase_temp = INV_PHASE_0;
        //         }
        //         else if(phase == 1)
        //         {
        //             Phase_temp = INV_PHASE_120;
    
        //         }
        //         else if(phase == 2)
        //         {
        //             Phase_temp = INV_PHASE_M120;
        //         }
        //     }
        //     return Phase_temp;
        // }

        // private double Compute_INV_IP_V(uint phase)
        // {
        //     double max_val = 0;
        //     byte judgedPhase = 0;

        //     string Cmd = "READ_VIN";
        //     judgedPhase = judgeNowPhase(phase);
        //     try
        //     {
        //         //直接從Dict拿已經解碼好的Phase
        //         foreach(var (addr, addrPhase) in INVs_Phase)
        //         {
        //             if(addrPhase == judgedPhase)
        //             {
        //                 var targetGroups = Device_ReadData.GetCommandGroups(addr, Cmd);
        //                 var value = _decoder.Decode(targetGroups, Cmd);
        //                 switch(judgedPhase)
        //                 {
        //                     case INV_PHASE_0:
        //                     case INV_PHASE_180:
        //                     case INV_PHASE_120:
        //                     case INV_PHASE_M120:
        //                         if((double)value > max_val)
        //                         {
        //                             max_val = (double)value;
        //                         }
        //                         break;
        //                     default:
        //                         break;
        //                 }
        //             }
        //         }
        //         return max_val;

        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_IP_V] max_val = {max_val}", AppLogLevel.Trace);

        //     }
        //     catch(Exception e)
        //     {
        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_IP_V] Error : {e}", AppLogLevel.Error);
        //     }
        // }

        // //數值計算要再檢查
        // private double Compute_INV_IP_F(uint phase)
        // {
        //     float count_R = 0, count_S = 0, count_T = 0;
        //     double tmp = 0;
        //     byte judgedPhase = 0;
            
        //     string Cmd = "READ_FREQ";
        //     judgedPhase = judgeNowPhase(phase);
        //     try
        //     {
        //         foreach(var (addr, addrPhase) in INVs_Phase)
        //         {

        //             if(addrPhase == judgedPhase)
        //             {
        //                 var targetGroups = Device_ReadData.GetCommandGroups(addr, Cmd);
        //                 var value = _decoder.Decode(targetGroups, Cmd);
        //                 switch(judgedPhase)
        //                 {
        //                     case INV_PHASE_0:
        //                         if((double)value != 0)
        //                         {
        //                             tmp = tmp + (double)value;
        //                             count_R ++;
        //                         }
        //                         break;
        //                     case INV_PHASE_180:
        //                         if((double)value != 0)
        //                         {
        //                             tmp = tmp + (double)value;
        //                             count_S ++;
        //                         }
        //                         break;
        //                     case INV_PHASE_120:
        //                         if((double)value != 0)
        //                         {
        //                             tmp = tmp + (double)value;
        //                             count_T ++;
        //                         }
        //                         break;
        //                     case INV_PHASE_M120:
        //                         if((double)value != 0)
        //                         {
        //                             tmp = tmp + (double)value;
        //                             count_T ++;
        //                         }
        //                         break;

        //                     default:
        //                         break;
        //                 }
        //             }
        //         }

                
        //         switch(judgedPhase)
        //         {
        //             case INV_PHASE_0:
        //                 if(count_R > 0)
        //                 {
        //                     double tmp1 = (ScalingComputer.AutoSnapToDecimal_doubleVer(tmp) / count_R);
        //                     tmp = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp1);
        //                 }
        //                 break;
        //             case INV_PHASE_180:
        //                 if(count_S > 0)
        //                 {
        //                     double tmp1 = (ScalingComputer.AutoSnapToDecimal_doubleVer(tmp) / count_S);
        //                     tmp = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp1);
        //                 }
        //                 break;
        //             case INV_PHASE_120:
        //                 if(count_T > 0)
        //                 {
        //                     double tmp1 = (ScalingComputer.AutoSnapToDecimal_doubleVer(tmp) / count_T);
        //                     tmp = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp1);
        //                 }
        //                 break;
        //             case INV_PHASE_M120:
        //                 if(count_T > 0)
        //                 {
        //                     double tmp1 = (ScalingComputer.AutoSnapToDecimal_doubleVer(tmp) / count_T);
        //                     tmp = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp1);
        //                 }
        //                 break;

        //             default:
        //                 break;
        //         }

        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_IP_V] tmp : {tmp}", AppLogLevel.Trace);

        //         return tmp;
        //     }
        //     catch(Exception e)
        //     {
        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_IP_F] Error : {e}", AppLogLevel.Error);
        //     }
            
        // }

        // private double Compute_INV_OP_V(uint phase)
        // {
        //     double max_val = 0;
        //     byte judgedPhase = 0;

        //     string Cmd = "READ_AC_VOUT";
        //     judgedPhase = judgeNowPhase(phase);

        //     try
        //     {
        //         foreach(var (addr, addrPhase) in INVs_Phase)
        //         {
        //             if(addrPhase == judgedPhase)
        //             {   
        //                 var targetGroups = Device_ReadData.GetCommandGroups(addr, Cmd);
        //                 var value = _decoder.Decode(targetGroups, Cmd);
        //                 switch(judgedPhase)
        //                 {
        //                     case INV_PHASE_0:
        //                     case INV_PHASE_180:
        //                     case INV_PHASE_120:
        //                     case INV_PHASE_M120:
        //                         if((double)value > max_val)
        //                         {
        //                             max_val = (double)value;
        //                         }
        //                         break;
        //                     default:
        //                         break;
        //                 }
        //             }
        //         }
        //         return max_val;

        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_V] max_val : {max_val}", AppLogLevel.Trace);
        //     }
        //     catch(Exception e)
        //     {
        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_V] Error : {e}", AppLogLevel.Error);
        //     }
        // }

        // //待處理
        // private double Compute_INV_OP_F(uint phase)
        // {
        //     float count_R = 0, count_S = 0, count_T = 0;
        //     double tmp = 0;
        //     byte judgedPhase = 0;
            
        //     string Cmd = "READ_AC_FOUT";
        //     judgedPhase = judgeNowPhase(phase);
        //     try
        //     {
        //         foreach(var (addr, addrPhase) in INVs_Phase)
        //         {

        //             if(addrPhase == judgedPhase)
        //             {
        //                 var targetGroups = Device_ReadData.GetCommandGroups(addr, Cmd);
        //                 var value = _decoder.Decode(targetGroups, Cmd);
        //                 switch(judgedPhase)
        //                 {
        //                     case INV_PHASE_0:
        //                         if((double)value != 0)
        //                         {
        //                             tmp = tmp + (double)value;
        //                             count_R ++;
        //                         }
        //                         break;
        //                     case INV_PHASE_180:
        //                         if((double)value != 0)
        //                         {
        //                             tmp = tmp + (double)value;
        //                             count_S ++;
        //                         }
        //                         break;
        //                     case INV_PHASE_120:
        //                         if((double)value != 0)
        //                         {
        //                             tmp = tmp + (double)value;
        //                             count_T ++;
        //                         }
        //                         break;
        //                     case INV_PHASE_M120:
        //                         if((double)value != 0)
        //                         {
        //                             tmp = tmp + (double)value;
        //                             count_T ++;
        //                         }
        //                         break;

        //                     default:
        //                         break;
        //                 }
        //             }
        //         }

                
        //         switch(judgedPhase)
        //         {
        //             case INV_PHASE_0:
        //                 if(count_R > 0)
        //                 {
        //                     double tmp1 = (ScalingComputer.AutoSnapToDecimal_doubleVer(tmp) / count_R);
        //                     tmp = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp1);
        //                 }
        //                 break;
        //             case INV_PHASE_180:
        //                 if(count_S > 0)
        //                 {
        //                     double tmp1 = (ScalingComputer.AutoSnapToDecimal_doubleVer(tmp) / count_S);
        //                     tmp = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp1);
        //                 }
        //                 break;
        //             case INV_PHASE_120:
        //                 if(count_T > 0)
        //                 {
        //                     double tmp1 = (ScalingComputer.AutoSnapToDecimal_doubleVer(tmp) / count_T);
        //                     tmp = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp1);
        //                 }
        //                 break;
        //             case INV_PHASE_M120:
        //                 if(count_T > 0)
        //                 {
        //                     double tmp1 = (ScalingComputer.AutoSnapToDecimal_doubleVer(tmp) / count_T);
        //                     tmp = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp1);
        //                 }
        //                 break;

        //             default:
        //                 break;
        //         }

        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_V] tmp : {tmp}", AppLogLevel.Trace);

        //         return tmp;
        //     }
        //     catch(Exception e)
        //     {
        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_F] Error : {e}", AppLogLevel.Error);
        //     }
        // }

        
        // private double Compute_INV_Current_Data(uint addr)
        // {
        //     double Aout_Decode = 0;

        //     string VA_Cmd = "READ_OP_VA";
        //     string VOUT_Cmd = "READ_AC_VOUT";

        //     try
        //     {
        //         if(INVs_Phase.TryGetValue(addr, out var targetGroups))
        //         {
        //             AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_Current_Data] addr : {addr} is not exist", AppLogLevel.Trace);

        //             var AC_Vout_val = _decoder.Decode(targetGroups, VOUT_Cmd);
        //             if(AC_Vout_val == null || AC_Vout_val == 0)
        //             {
        //                 AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_Current_Data] AC_Vout_val = {AC_Vout_val} is invalid ", AppLogLevel.Trace);
        //                 return 0;
        //             }
        //             var VA_val = _decoder.Decode(targetGroups, VA_Cmd);
        //             Aout_Decode = ScalingComputer.AutoSnapToDecimal_doubleVer((double)VA_val / (double)AC_Vout_val);
        //             AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_Current_Data] VA_val={VA_val}, AC_Vout={AC_Vout}, Aout_Decode = {Aout_Decode}", AppLogLevel.Trace);
                    
        //             if(Aout_Decode < 0.5f)
        //             {
        //                 Aout_Decode = 0;
        //             }
        //             return Aout_Decode;
        //         }
        //         else
        //         {
        //             AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_Current_Data] addr : {addr} is not exist", AppLogLevel.Trace);
        //         }
        //     }
        //     catch
        //     {
        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_Current_Data] Error : {e}", AppLogLevel.Error);
        //     }
        // }

        // private double Compute_INV_OP_A(uint phase)
        // {
        //     double tmp          = 0;
        //     byte judgedPhase    = 0;
            
        //     judgedPhase         = judgeNowPhase(phase);

        //     try
        //     {
        //         foreach(var (addr, addrPhase) in INVs_Phase)
        //         {
        //             if(addrPhase == judgedPhase)
        //             {
        //                 switch(judgedPhase)
        //                 {
        //                     case INV_PHASE_0:
        //                     case INV_PHASE_180:
        //                     case INV_PHASE_120:
        //                     case INV_PHASE_M120:
        //                         tmp = tmp + Compute_INV_Current_Data(addr);
        //                         break;

        //                     default:
        //                         tmp = 0;
        //                         break;
        //                 }
        //             }
        //         }
        //         return tmp;
        //     }
        //     catch(Exception e)
        //     {
        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_A] Error : {e}", AppLogLevel.Error);
        //     }
        // }

        // private double Compute_INV_OP_Load(uint phase)
        // {
        //     // double tmp = 0, count_R = 0;
        //     // try
        //     // {
        //     //     foreach(var (addr, addrPhase) in INVs_Phase)
        //     //     {
        //     //         string Cmd = "READ_OP_LD_PCNT";
        //     //         var targetGroups = Device_ReadData.GetCommandGroups(addr, Cmd);
        //     //         var value = _decoder.Decode(targetGroups, Cmd);

        //     //         tmp = tmp + double(value);
        //     //         count_R ++;
        //     //     }

        //     //     if(count_R == 0)
        //     //     {
        //     //         count_R = 1;
        //     //     }

        //     //     double tmp1 = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp);
        //     //     tmp = ScalingComputer.AutoSnapToDecimal_doubleVer(tmp1 / count_R);
        //     //     return tmp;
        //     // }
        //     // catch(Exception e)
        //     // {
        //     //     AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_INV_OP_Load] Error : {e}", AppLogLevel.Error);
        //     // }
        // }

        // private double Compute_BAT_V()
        // {
        //     double tmp = 0;
        //     string Cmd = "READ_VBAT";
        //     try
        //     {
        //         foreach(var (addr, addrPhase) in INVs_Phase)
        //         {
        //             var targetGroups = Device_ReadData.GetCommandGroups(addr, Cmd);
        //             var value = _decoder.Decode(targetGroups, Cmd);

        //             if((double)value > tmp)
        //             {
        //                 tmp = (double)value;
        //             }
        //         }

        //         return tmp;
        //     }
        //     catch(Exception e)
        //     {
        //         AppLogger.Log_To_File_log(_category, $"[GlobalData][Compute_BAT_V] Error : {e}", AppLogLevel.Error);
        //     }
        // }

        
    }
}