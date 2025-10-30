namespace demoVer.Utils
{
    public static class ConstDefinition
    {
        #region Common
        public const int Max_PortDeviceNum = 64;
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
    }
}