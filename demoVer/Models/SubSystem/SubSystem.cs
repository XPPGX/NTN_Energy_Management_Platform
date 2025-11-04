using Microsoft.AspNetCore.SignalR;
using demoVer.Utils;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        public string epoch { get; set; } = string.Empty;

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

        #region For Writable Cmd
        /***************************************************************************
         *                         Range 相關資料結構與方法
         **************************************************************************/
        /// <summary>只有數值型態的指令會有設定範圍(上限、下限)，要用cmdCode當Key來找</summary>
        public ConcurrentDictionary<string, SingleCmdRange> SettingRanges { get; set; } = new();
        private readonly SemaphoreSlim _SettingRange_Lock = new(1, 1);
        public async Task UpdateSettingRanges_From(ConcurrentDictionary<string, SingleCmdRange> newRanges)
        {
            await _SettingRange_Lock.WaitAsync();

            try
            {
                foreach (var (HexCmd, newRange_Obj) in newRanges)
                {
                    if (this.SettingRanges.ContainsKey(HexCmd))
                    {
                        this.SettingRanges[HexCmd].min = newRange_Obj.min;
                        this.SettingRanges[HexCmd].max = newRange_Obj.max;
                        this.SettingRanges[HexCmd].cmdCode = newRange_Obj.cmdCode;
                    }
                    else
                    {
                        this.SettingRanges.TryAdd(HexCmd, newRange_Obj);
                    }
                    // Console.WriteLine($"[SubSystem][UpdateSettingRanges_From] Updated Setting Range for CmdCode {HexCmd}: Min={newRange_Obj.min}, Max={newRange_Obj.max}");
                }
                //同時也更新 nowConfigurableVars 的 Boundaries
                if (nowConfigurableVars.UpdateBoundaries_From_SubSystem(this.SettingRanges) is true)
                {
                    //觸發 OnChanged 事件
                    OnChanged?.Invoke();
                }
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystem][UpdateSettingRanges_From] Exception: {e.Message}", AppLogLevel.Error);
            }
            finally
            {
                _SettingRange_Lock.Release();
            }
        }


        /***************************************************************************
         *                     Writable Cmd 相關資料結構與方法
         **************************************************************************/
        /// <summary>存放Write Cmd的資訊，以cmdCode為Key</summary>
        public Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat> InfosForWriteCmd { get; set; } = new();
        /// <summary>設定頁面實際上會用到的值(從InfosForWriteCmd解析出來)</summary>
        public PageUsableVars nowConfigurableVars { get; set; } = new();
        public readonly SemaphoreSlim _SettingRange_Lock_WriteCmdInfo = new(1, 1); 
        public event Action? OnChanged; //這個Action在設定Apply的時候也要觸發
        public async Task UpdateInfosForWriteCmd_From(List<GET_RealSingleRawSettingCMD_JsonFormat> newInfos)
        {
            await _SettingRange_Lock_WriteCmdInfo.WaitAsync();

            try
            {
                foreach (var new_singleRawSettingCmd in newInfos)
                {
                    //取出 cmdCode 作為 key
                    string cmdCode = new_singleRawSettingCmd.cmdCode;
                    //框架給的 WriteCmdInfo_API 裡面的 cmdCode 跟 Range_API 裡面的 cmdCode 不一樣，多了一個"0x"在開頭
                    string ClearCmdCode = GetNowValueHelper.getClearCmdCode(cmdCode);
                    if (this.InfosForWriteCmd.ContainsKey(ClearCmdCode))
                    {
                        AppLogger.Log_To_File_log(_category, $"[SubSystem][UpdateInfosForWriteCmd_From] Update existing Write CMD Info for CmdCode {ClearCmdCode}", AppLogLevel.Debug);
                        this.InfosForWriteCmd[ClearCmdCode] = new_singleRawSettingCmd;
                    }
                    else
                    {
                        AppLogger.Log_To_File_log(_category, $"[SubSystem][UpdateInfosForWriteCmd_From] Add new Write CMD Info for CmdCode {ClearCmdCode}", AppLogLevel.Debug);
                        this.InfosForWriteCmd.Add(ClearCmdCode, new_singleRawSettingCmd);
                    }

                }

                //3. 更新 nowConfigurableVars
                if (nowConfigurableVars.UpdateNowValue_From_SubSystem(this.InfosForWriteCmd) is true)
                {
                    //觸發 OnChanged 事件
                    OnChanged?.Invoke();
                }
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystem][UpdateInfosForWriteCmd_From] Exception: {e.Message}", AppLogLevel.Error);
            }
            finally
            {
                _SettingRange_Lock_WriteCmdInfo.Release();
            }
        }

        /***************************************************************************
         *                  Write Process Flow 相關資料結構與方法
         **************************************************************************/
        /// <summary>給SubSystemManager使用的寫入流程鎖，確保同一時間，一個子系統只有一個寫入流程在進行</summary>
        
        public readonly SemaphoreSlim BAT_WriteProcessFlowLock = new(1, 1);
        public readonly SemaphoreSlim INV_WriteProcessFlowLock = new(1, 1);
        
        //這個或許可以用Dictionary去做
        public SemaphoreSlim? Get_WriteProcessFlowLock_By_PageSelection(int PageSelection)
        {
            switch(PageSelection)
            {
                case 0: //Battery
                    return BAT_WriteProcessFlowLock;
                case 1: //Inverter
                    return INV_WriteProcessFlowLock;
                default:
                    return null;
            }
        }
        public List<Post_RealSingleRawSettingCMD_JsonFormat> BatterySetting_WriteFailedCmds_List { get; set; } = new(); //每次送出命令後，暫存失敗的CMD，準備顯示在UI上。
        public List<Post_RealSingleRawSettingCMD_JsonFormat> InverterSetting_WriteFailedCmds_List { get; set; } = new(); //每次送出命令後，暫存失敗的CMD，準備顯示在UI上。

        //這個或許可以用Dictionary去做
        public void Update_WriteFailedCmdsList_By_PageSelection(int PageSelection, List<Post_RealSingleRawSettingCMD_JsonFormat> writeFailedCmds_List)
        {
            switch(PageSelection)
            {
                case 0: //Battery
                    BatterySetting_WriteFailedCmds_List = writeFailedCmds_List;
                    break;
                case 1: //Inverter
                    InverterSetting_WriteFailedCmds_List = writeFailedCmds_List;
                    break;
                default:
                    break;
            }
        }
        /***************************************************************************
         *                  Helper Methods for Writable Cmd
         **************************************************************************/
        /// <summary>根據 cmdCode 找到對應的 cmdName</summary>
        /// <returns>CommandName</returns>
        public string cmdCode_mappingTo_cmdName(string cmdCode) => (InfosForWriteCmd.ContainsKey(cmdCode)) ? InfosForWriteCmd[cmdCode].CommandName : string.Empty;
        public (string Format, bool isPerAddr) GetSettingCmdFormat_and_IsPerAddr_By_ClearCmdCode(string clearCmdCode)
        {
            if (InfosForWriteCmd.ContainsKey(clearCmdCode))
            {
                var cmdInfo = InfosForWriteCmd[clearCmdCode];
                return (cmdInfo.DataFormat, cmdInfo.IsPerAddr);
            }
            return (string.Empty, false);
        }
        #endregion For Writable Cmd

        #region Computed In SubSystem
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

            AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeMode] ({this.Port}, {this.Protocol}) SubSystem Mode Computed : {nowMode.ToDisplayName()} (INV:{INV_MODE_Count}, SAVING:{SAVING_MODE_Count}, BYPASS:{BYPASS_MODE_Count}, CHARGING:{CHARGING_MODE_Count}, STANDBY:{STANDBY_MODE_Count})", AppLogLevel.Trace);
        }
        #endregion Computed In SubSystem
    }
}