using Microsoft.AspNetCore.SignalR;
using demoVer.Utils;
namespace demoVer.Models
{
    /// <summary>
    /// 表示一個子系統，包含其相關的屬性和方法。
    /// 同一個Port，使用同一份Protocol的Addr，就視為同一個子系統
    /// 每個子系統會計算自己的系統資訊，例如 INV_MODE。
    /// </summary>
    public class SubSystem
    {
        private string _category = "";

        public SubSystem()
        {
            _category = GetType().FullName!;
        }

        #region From Status API
        public HashSet<uint> AddrSet { get; set; } = new();
        public bool CheckOK { get; set; } = false;
        public bool ModelError { get; set; } = false;
        public string? ModelName { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public bool RangeOK { get; set; } = false;

        /// <summary>
        /// 使用SinglePartition物件更新子系統的基本資訊(不包含AddrSet, 計算屬性)
        /// </summary>
        /// <param name="source"></param>
        public void UpdateFrom(SinglePartition source)
        {
            this.Port = source.Port;
            this.Protocol = source.Protocol;
            this.CheckOK = source.CheckOK;
            this.ModelError = source.ModelError;
            this.RangeOK = source.rangeOK;
            this.ModelName = source.ModelName;
            // this.AddrSet.UnionWith(source.Addr);
        }
        #endregion From Status API

        #region Computed In SubSys
        public bool SubSystemSettingExist { get; set; } = false;
        public bool isAnyDeviceOnline { get; set; } = false;
        public ulong newSettingAddrBitMap { get; set; } = 0x0000000000000000UL;
        public ConstDefinition.SYS_Mode_Options nowMode { get; set; } = ConstDefinition.SYS_Mode_Options.DISCON;

        public bool AC_StandBy { get; set; } = false;
        public bool AC_Charger_Enable { get; set; } = false;
        public uint INV_MODE_Count { get; set; } = 0;
        public uint SAVING_MODE_Count { get; set; } = 0;
        public uint BYPASS_MODE_Count { get; set; } = 0;
        public uint CHARGING_MODE_Count { get; set; } = 0;
        public uint STANDBY_MODE_Count { get; set; } = 0;

        private void setMode(ConstDefinition.SYS_Mode_Options mode) => nowMode = mode;
        public ArrowDirections ArrowDirections { get; private set; } = ArrowDirections.Hidden;
        public string DisplayModeName => nowMode.ToDisplayName();

        public void ComputeOverallValues_in_SubSystem(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            isAnyDeviceOnline = onlineDevicesDatas.Count > 0;
            //取得 Mode
            ComputeMode(onlineDevicesDatas);

            //其他系統變數計算，未來擴充
            ArrowDirections = ArrowDirectionHelper.Resolve(nowMode, AC_Charger_Enable, AC_StandBy);
        }
        
        /// <summary>
        /// 根據子系統內所有在線設備的資料，計算子系統的系統模式(nowMode)
        /// </summary>
        /// <param name="onlineDevicesDatas">在線設備的資料列表</param> <summary>
        /// 
        /// </summary>
        /// <param name="onlineDevicesDatas"></param>
        public void ComputeMode(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            //0. 初始化
            AC_StandBy = false;
            AC_Charger_Enable = false;
            INV_MODE_Count = SAVING_MODE_Count = BYPASS_MODE_Count = CHARGING_MODE_Count = STANDBY_MODE_Count = 0;

            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取得 INV_FAULT 的 decodeList並判斷是否有Error
                var INV_FAULT_DecodeList = (List<decodeContent>?)oneDevice_Data.parseCmdData("INV_FAULT");
                if (INV_FAULT_DecodeList == null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeMode] INV_FAULT DecodeList is null, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }
                string? INV_FAULT_str = BitFieldParser.Parse_INV_FAULT(INV_FAULT_DecodeList);
                if (!string.IsNullOrEmpty(INV_FAULT_str))
                {
                    setMode(ConstDefinition.SYS_Mode_Options.ERROR);
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeMode] Device Addr {oneDevice_Data.addr} has INV_FAULT : {INV_FAULT_str}, set SubSystem mode to Error", AppLogLevel.Debug);
                }

                //2. 取得 INV_STATUS 命令中，會影響 Mode 的 資料
                var INV_STATUS_DecodeList = (List<decodeContent>?)oneDevice_Data.parseCmdData("INV_STATUS");
                if (INV_STATUS_DecodeList == null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeMode] INV_STATUS DecodeList is null, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }
                ConstDefinition.SYS_Mode_Options oneDevice_Mode = ConstDefinition.SYS_Mode_Options.DISCON;
                bool oneDevice_AC_standBy = false;
                bool oneDevice_AC_Charger_Enable = false;
                (oneDevice_AC_standBy, oneDevice_AC_Charger_Enable, oneDevice_Mode) = BitFieldParser.Parse_INV_STATUS_GetEnum(INV_STATUS_DecodeList);

                //2.1 根據 INV_STATUS_DecodeList 判斷 AC_StandBy
                if (oneDevice_AC_standBy is true) { AC_StandBy = true; }
                //2.2 根據 INV_STATUS_DecodeList 判斷 AC_Charger_Enable
                if (oneDevice_AC_Charger_Enable is true) { AC_Charger_Enable = true; }
                //2.3 統計各 Mode 數量
                switch (oneDevice_Mode)
                {
                    case ConstDefinition.SYS_Mode_Options.DISCON:
                        break;
                    case ConstDefinition.SYS_Mode_Options.INVERTER:
                        INV_MODE_Count++;
                        break;
                    case ConstDefinition.SYS_Mode_Options.SAVING:
                        SAVING_MODE_Count++;
                        break;
                    case ConstDefinition.SYS_Mode_Options.BY_PASS:
                        BYPASS_MODE_Count++;
                        break;
                    case ConstDefinition.SYS_Mode_Options.CHARGER:
                        CHARGING_MODE_Count++;
                        break;
                    case ConstDefinition.SYS_Mode_Options.STANDBY:
                        STANDBY_MODE_Count++;
                        break;
                    default:
                        break;
                }

            }

            //3. 根據統計結果，設定SubSystem的nowMode
            if (INV_MODE_Count > 0) { setMode(ConstDefinition.SYS_Mode_Options.INVERTER); }
            else if (SAVING_MODE_Count > 0) { setMode(ConstDefinition.SYS_Mode_Options.SAVING); }
            else if (BYPASS_MODE_Count > 0) { setMode(ConstDefinition.SYS_Mode_Options.BY_PASS); }
            else if (CHARGING_MODE_Count > 0) { setMode(ConstDefinition.SYS_Mode_Options.CHARGER); }
            else if (STANDBY_MODE_Count > 0) { setMode(ConstDefinition.SYS_Mode_Options.STANDBY); }
            else { setMode(ConstDefinition.SYS_Mode_Options.DISCON); }

            AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeMode] SubSystem Mode Computed : {nowMode.ToDisplayName()} (INV:{INV_MODE_Count}, SAVING:{SAVING_MODE_Count}, BYPASS:{BYPASS_MODE_Count}, CHARGING:{CHARGING_MODE_Count}, STANDBY:{STANDBY_MODE_Count})", AppLogLevel.Debug);
        }


        #endregion Computed In SubSys
    }
}