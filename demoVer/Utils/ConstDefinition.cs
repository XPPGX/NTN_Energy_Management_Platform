namespace demoVer.Utils
{
    public static class ConstDefinition
    {
        #region Common
        public const int Max_PortDeviceNum = 64;
        public const string Str_50Hz = "50Hz";
        public const string Str_60Hz = "60Hz";
        public const string Str_AC_InputType_100VAC = "100VAC";
        public const int Timeout_Max = 6000; //分鐘
        #endregion 

        #region AddressOffset
        public const int CAN1_addr = 0;
        public const int CAN2_addr = 64;
        public const int MOD1_addr = 128;
        public const int MOD2_addr = 192;
        #endregion AddressOffset

        #region INV_Mode_Status
        public const string INVERTER_MODE_str = "Inverter";
        public const string SAVING_MODE_str = "Saving";
        public const string BYPASS_MODE_str = "Bypass";
        public const string CHARGING_MODE_str = "Charger";
        public const string STANDBY_MODE_str = "Standby";
        public const string SHUTDOWN_MODE_str = "Shutdown";
        public const string ERROR_MODE_str = "Error";
        public const string BAT_first_MODE_str = "Battery First";
        public const string AC_OK_str = "AC OK";
        public const string DEFAULT_MODE_str = "";
        public enum SYS_Mode_Options
        {
            DISCON = 0,
            ERROR = 3,
            INVERTER = 4,
            SAVING = 5,
            BY_PASS = 6,
            CHARGER = 7,
            STANDBY = 8,
            BATTERY_FIRST = 9,
            BY_PASS_AC_CHARGER = 10,
            AC_OK = 11,
            SHUTDOWN = 12,
        }
        #endregion INV_Mode_Status

        #region INV_Phase_Status
        public const string PHASE_0_str = "Phase 0°";
        public const string PHASE_180_str = "Phase 180°";
        public const string PHASE_120_str = "Phase 120°";
        public const string PHASE_240_str = "Phase 240°";
        public enum INV_Phase_Options
        {
            PHASE_0 = 0,
            PHASE_180 = 1,
            PHASE_120 = 2,
            PHASE_240 = 3,
        }
        public enum INV_Phase_SubSys
        {
            INV_SINGLE_PHASE = 1,
            INV_TWO_PHASE = 3,
            INV_THREE_PHASE = 7,
        }
        #endregion INV_Phase_Status
    }
}