using demoVer.Models;
namespace demoVer.Services
{
    public class GlobalVar
    {
        public const int CAN1_addr = 0;
        public const int CAN2_addr = 64;
        public const int MOD1_addr = 128;
        public const int MOD2_addr = 192;

        public allDevice_Data Device_ReadData {get; set;}
        
        public GlobalVar()
        {
            Device_ReadData = new allDevice_Data();

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