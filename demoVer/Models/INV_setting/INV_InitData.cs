namespace demoVer.Models
{
    public class INV_InitData
    {
        public byte AC_Series = 0;
        public byte ACF = 0;
        public byte ACV = 0;
        public uint Button_touchable_status = 0x0000015F; //bit0:BAT Alarm bit1:BAT Shutdown bit2:BAT Recharge bit3:BAT Capacity bit4:Output Priority bit5:Charging Priority bit6:AC_mode_charging_enable bit7:Grid_tied_power_feeding_enable bit8:BAT OV Alarm
        public byte Output_priority = (byte)0;
        public byte Charge_priority = (byte)0;

        public uint BAT_Alarm_Max       = 500;
        public uint BAT_Alarm_Min       = 376;
        public uint BAT_Alarm_Value     = 440;
        public uint BAT_Shutdown_Max    = 480;
        public uint BAT_Shutdown_Min    = 368;
        public uint BAT_Shutdown_Value  = 380;
        public uint BAT_Recharge_Min    = 368;
        public uint BAT_Recharge_Value  = 368;
        public uint BAT_OV_Alarm_Max    = 660;
        public uint BAT_OV_Alarm_Min    = 600;
        public uint BAT_OV_Alarm_Value  = 620;
        
        public uint BAT_Capacity_Value  = 0;
    }
}