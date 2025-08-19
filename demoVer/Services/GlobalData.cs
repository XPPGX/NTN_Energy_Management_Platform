using demoVer.Models;
using demoVer.Utils;
using System.Threading;
using demoVer.Services;
using demoVer.Interfaces;

namespace demoVer.Services
{
    public class GlobalVar
    {
        //[inject]
        private readonly IGroupsDataDecoder _decoder;
        
        //For log
        private string _category;
        private bool _disposed;

        //Const
        public const int CAN1_addr              = 0;
        public const int CAN2_addr              = 64;
        public const int MOD1_addr              = 128;
        public const int MOD2_addr              = 192;
        public const uint STATUS_INV            = 0x00000001 << 0;
        public const uint STATUS_BYP            = 0x00000001 << 1;
        public const uint STATUS_UTI_OK         = 0x00000001 << 2;
        public const uint STATUS_CHG            = 0x00000001 << 3;
        public const uint STATUS_SOLAR_EN       = 0x00000001 << 4;
        public const uint STATUS_SAVING         = 0x00000001 << 5;
        public const uint STATUS_BAT_LOW_ALM    = 0x00000001 << 6; 
        
        private bool IsSteady {get; set;} = false;

        private int _invConnectNum;
        public int INV_ConnectNum
        {
            get => Volatile.Read(ref _invConnectNum);
            private set => Volatile.Write(ref _invConnectNum, value);
        }

        //READ API Data Structure
        public allDevice_Data Device_ReadData {get; set;}       //Polling讀取各Device資料
        public LinkedDeviceStore LinkedDevices {get;}           //以concurrent字典記錄目前有連線的devices
        public List<SettingData> Setting_CAN_Check {get; set;}  //為檢查是否與Write後，APP與Framework的data一致，所以要讀回來
        public List<SettingData> Setting_MOD_Check {get; set;}  //為檢查是否與Write後，APP與Framework的data一致，所以要讀回來

        //Write API Data Structure
        public List<SettingData> Setting_CAN_Write {get; set;}
        public List<SettingData> Setting_MOD_Write {get; set;}
        
        public GlobalVar(IGroupsDataDecoder decoder)
        {
            //For log
            _category                   = GetType().FullName!;
            
            //[inject]
            _decoder                    = decoder;

            //[Variables]
            Device_ReadData             = new allDevice_Data();
            LinkedDevices               = new LinkedDeviceStore();
            Setting_CAN_Check           = new List<SettingData>();
            Setting_CAN_Write           = new List<SettingData>();
            Setting_MOD_Check           = new List<SettingData>();
            Setting_MOD_Write           = new List<SettingData>();

            
            //Events(Actions)
            LinkedDevices.linkChanged   += Get_INV_ConnectNum;
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
        

        //只看是否有INV_FAULT
        public uint Get_INV_Fault(uint INV_FAULT_Val)
        {
            return INV_FAULT_Val & 0xFEFF;
        }

        
        public string Get_INV_Status(uint INV_FAULT_Val, uint INV_STATUS_Val)
        {
            if(Get_INV_Fault(INV_FAULT_Val) != 0){return "Error";}

            if(((INV_STATUS_Val & STATUS_INV) != 0) && ((INV_STATUS_Val & STATUS_SAVING) != 0)){return "Saving";}

            if((INV_STATUS_Val & STATUS_INV) != 0){return "Inverter";}
            
            if((INV_STATUS_Val & STATUS_BYP) != 0){return "Bypass";}

            if((INV_STATUS_Val & STATUS_CHG) != 0){return "Charger";}

            return "Standby";
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

        

        // public string? Get_INV_IP_V(uint phase)
        // {
            
        // }
    }
}