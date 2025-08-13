/**
針對需要將某些變數計算後得到的值，
例如：Load_Current : 是由 (OP_VA, AC_VOUT_uint)計算得到的
*/
namespace demoVer.Utils
{
    public static class VarTranslator
    {   
        public const uint STATUS_INV           = 0x00000001 << 0;
        public const uint STATUS_BYP           = 0x00000001 << 1;
        public const uint STATUS_UTI_OK        = 0x00000001 << 2;
        public const uint STATUS_CHG           = 0x00000001 << 3;
        public const uint STATUS_SOLAR_EN      = 0x00000001 << 4;
        public const uint STATUS_SAVING        = 0x00000001 << 5;
        public const uint STATUS_BAT_LOW_ALM   = 0x00000001 << 6; 

        //只看是否有INV_FAULT
        public static uint Get_INV_Fault(uint INV_FAULT_Val)
        {
            return INV_FAULT_Val & 0xFEFF;
        }

        
        public static string Get_INV_Status(uint INV_FAULT_Val, uint INV_STATUS_Val)
        {
            if(Get_INV_Fault(INV_FAULT_Val) != 0){return "Error";}

            if(((INV_STATUS_Val & STATUS_INV) != 0) && ((INV_STATUS_Val & STATUS_SAVING) != 0)){return "Saving";}

            if((INV_STATUS_Val & STATUS_INV) != 0){return "Inverter";}
            
            if((INV_STATUS_Val & STATUS_BYP) != 0){return "Bypass";}

            if((INV_STATUS_Val & STATUS_CHG) != 0){return "Charger";}

            return "Standby";
        }

        
    }
}