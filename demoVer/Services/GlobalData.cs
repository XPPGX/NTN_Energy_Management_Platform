using demoVer.Models;
namespace demoVer.Services
{
    public class GlobalVar
    {
        public const int CAN1_addr = 0;
        public const int CAN2_addr = 64;
        public const int MOD1_addr = 128;
        public const int MOD2_addr = 192;

        

        //READ API Data Structure
        public allDevice_Data Device_ReadData {get; set;}       //Polling讀取各Device資料
        public LinkedDeviceStore LinkedDevices {get;}      //以concurrent字典記錄目前有連線的devices
        public List<SettingData> Setting_CAN_Check {get; set;}  //為檢查是否與Write後，APP與Framework的data一致，所以要讀回來
        public List<SettingData> Setting_MOD_Check {get; set;}  //為檢查是否與Write後，APP與Framework的data一致，所以要讀回來

        //Write API Data Structure
        public List<SettingData> Setting_CAN_Write {get; set;}
        public List<SettingData> Setting_MOD_Write {get; set;}

        public GlobalVar()
        {
            Device_ReadData     = new allDevice_Data();
            LinkedDevices       = new LinkedDeviceStore();
            Setting_CAN_Check   = new List<SettingData>();
            Setting_CAN_Write   = new List<SettingData>();
            Setting_MOD_Check   = new List<SettingData>();
            Setting_MOD_Write   = new List<SettingData>();
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
    }
}