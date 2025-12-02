using Microsoft.AspNetCore.SignalR;
using demoVer.Utils;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Runtime.CompilerServices;
using System.Threading;
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

        public List<uint> GetAddrList() => AddrSet.ToList();
        #endregion From Status API

        #region For Writable Cmd
        /***************************************************************************
         *                   SubSystem物件的 FireAndForget Lock
         **************************************************************************/
        /// <summary>給 SubSystemManager 使用的 FireAndForget 鎖，確保同一時間，一個子系統只有一個 FireAndForget 流程在進行</summary>
        public readonly SemaphoreSlim SubSystem_FireAndForget_Lock = new(1, 1);

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
                AppLogger.Log_To_File_log(_category, $"[SubSystem][UpdateSettingRanges_From] Start for (port, protocol) = ({this.Port}, {this.Protocol})", AppLogLevel.Debug);
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
                AppLogger.Log_To_File_log(_category, $"[SubSystem][UpdateSettingRanges_From] done for (port, protocol) = ({this.Port}, {this.Protocol})", AppLogLevel.Debug);
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
                AppLogger.Log_To_File_log(_category, $"[SubSystem][UpdateInfosForWriteCmd_From] Start for (port, protocol) = ({this.Port}, {this.Protocol})", AppLogLevel.Debug);
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
                AppLogger.Log_To_File_log(_category, $"[SubSystem][UpdateInfosForWriteCmd_From] done for (port, protocol) = ({this.Port}, {this.Protocol})", AppLogLevel.Debug);
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
            switch (PageSelection)
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
            switch (PageSelection)
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
        public string? GetBitKey_FromCmdInfo(string clearCmdCode, int bitPosition)
        {
            try
            {
                if (!InfosForWriteCmd.ContainsKey(clearCmdCode))
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][GetBitKey_FromCmdInfo] clearCmdCode {clearCmdCode} not found in InfosForWriteCmd", AppLogLevel.Warning);
                    return null;
                }
                var cmdInfo = InfosForWriteCmd[clearCmdCode];

                var correctKey = cmdInfo.GetBitKey(bitPosition);

                AppLogger.Log_To_File_log(_category, $"[SubSystem][GetBitKey_FromCmdInfo] clearCmdCode {clearCmdCode}, bitPosition {bitPosition} => BitKey: {correctKey}", AppLogLevel.Debug);
                return correctKey;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystem][GetBitKey_FromCmdInfo] Exception: {e.Message}", AppLogLevel.Error);
                return null;
            }

        }
        #endregion For Writable Cmd

        #region For Read Cmd Format
        private int _isMappingCmdCodeToNameInit = 0;
        public ConcurrentDictionary<string, string> Mapping_Cmd_CodeToName {get; set;} = new(); //Key: cmdCode, Value: UserDefined CmdName
        public ConcurrentDictionary<string, string> ReadCmd_Unit_ConDict { get; set; } = new(); //Key: cmdName, Value: unit string
        private readonly SemaphoreSlim _ReadCmd_Format_Lock = new(1, 1);

        public async Task Update_ReadCmdFormat(ProtocolFormat? newReadCmd_with_Format)
        {
            await _ReadCmd_Format_Lock.WaitAsync();
            try
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystem][Update_ReadCmdFormat] Start for (port, protocol) = ({this.Port}, {this.Protocol})", AppLogLevel.Debug);
                ReadCmd_Unit_ConDict.Clear();
                if (newReadCmd_with_Format is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Update_ReadCmdFormat] newReadCmd_with_Format is null, skip updating", AppLogLevel.Warning);
                    return;
                }
                var newValues = newReadCmd_with_Format.values;
                foreach (var (cmdName, FormatObj) in newValues)
                {
                    if (FormatObj.unit is not null)
                    {
                        ReadCmd_Unit_ConDict[cmdName] = FormatObj.unit;
                    }
                    
                    if (FormatObj.commandCode is not null)
                    {
                       Mapping_Cmd_CodeToName[FormatObj.commandCode] = cmdName;
                    }
                }

                //[Debug]印出更新結果
                foreach(var kvp in newValues)
                {
                    string cmdName = kvp.Key;
                    string unitStr = kvp.Value.unit ?? "null";
                    string cmdCodeStr = kvp.Value.commandCode ?? "null";
                    bool unitStoreSucc = ReadCmd_Unit_ConDict.TryGetValue(cmdName, out string? storedUnit);
                    bool cmdCodeStoreSucc = Mapping_Cmd_CodeToName.TryGetValue(cmdCodeStr, out string? storedCmdName);
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Update_ReadCmdFormat] ReadCmd Format Updated: CmdName={cmdName}, Unit={unitStr}, StoredUnit={storedUnit}, CmdCode={cmdCodeStr}, StoredCmdName={storedCmdName}", AppLogLevel.Debug);
                }
                AppLogger.Log_To_File_log(_category, $"[SubSystem][Update_ReadCmdFormat] done for (port, protocol) = ({this.Port}, {this.Protocol})", AppLogLevel.Debug);
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystem][Update_ReadCmdFormat] Exception: {e.Message}", AppLogLevel.Error);
            }
            finally
            {
                _ReadCmd_Format_Lock.Release();
            }
        }

        public Dictionary<string, string> GetReadCmd_Unit_Dict()
        {
            return ReadCmd_Unit_ConDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        
        public string? Get_UserDefined_CmdName_By_CmdCode(string cmdCode)
        {
            if (Mapping_Cmd_CodeToName.TryGetValue(cmdCode, out string? cmdName))
            {
                return cmdName;
            }
            return null;
        }

        
        public string Get_PortWithoutNum() => Custom.getPort_ExceptFor_Num(this.Port);

        #endregion For Read Cmd Format

        #region Computed In SubSystem
        public bool SubSystemSettingExist { get; set; } = false;
        public bool isAnyDeviceOnline { get; set; } = false;
        public ulong newSettingAddrBitMap { get; set; } = 0x0000000000000000UL;

        public ComputedValues_In_SubSystem ComputedVals = new ComputedValues_In_SubSystem();

        public bool AC_StandBy { get; set; } = false;
        public bool AC_Charger_Enable { get; set; } = false;
        public ArrowDirections ArrowDirections { get; private set; } = ArrowDirections.Hidden;
        public string DisplayModeName => ComputedVals.nowMode.ToDisplayName();

        //目前這裡計算用的命令都是用Protocol的原始命令名稱去取值，如果之後要支援不同命令名稱，可能要用Mapping cmdCode的方式去取值
        public void ComputeOverallValues_in_SubSystem(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            //是否有在線設備
            isAnyDeviceOnline = onlineDevicesDatas.Count > 0;

            //取得連線數
            ComputeOnline_INV_Num(onlineDevicesDatas);

            //取得 Mode
            ComputeMode(onlineDevicesDatas);

            //取得 Phase
            ComputePhase(onlineDevicesDatas);
            // AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeOverallValues_in_SubSystem] ({this.Port}, {this.Protocol}) Phase computed : {ComputedVals.nowPhase.ToString()}", AppLogLevel.Debug);

            //更新 INV_IP_V 所有數值
            Compute_INV_IP_V(onlineDevicesDatas);
            // AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeOverallValues_in_SubSystem] ({this.Port}, {this.Protocol}) INV_IP_V, Phase_0 : {ComputedVals.INV_IP_V.Phase_0}, Phase_180 : {ComputedVals.INV_IP_V.Phase_180}, Phase_120 : {ComputedVals.INV_IP_V.Phase_120}, Phase_240 : {ComputedVals.INV_IP_V.Phase_240}", AppLogLevel.Debug);

            //更新 INV_IP_F 所有數值
            Compute_INV_IP_F(onlineDevicesDatas);
            // AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeOverallValues_in_SubSystem] ({this.Port}, {this.Protocol}) INV_IP_F, Phase_0 : {ComputedVals.INV_IP_F.Phase_0}, Phase_180 : {ComputedVals.INV_IP_F.Phase_180}, Phase_120 : {ComputedVals.INV_IP_F.Phase_120}, Phase_240 : {ComputedVals.INV_IP_F.Phase_240}", AppLogLevel.Debug);

            //更新 INV_OP_V 所有數值
            Compute_INV_OP_V(onlineDevicesDatas);
            // AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeOverallValues_in_SubSystem] ({this.Port}, {this.Protocol}) INV_OP_V, Phase_0 : {ComputedVals.INV_OP_V.Phase_0}, Phase_180 : {ComputedVals.INV_OP_V.Phase_180}, Phase_120 : {ComputedVals.INV_OP_V.Phase_120}, Phase_240 : {ComputedVals.INV_OP_V.Phase_240}", AppLogLevel.Debug);

            //更新 INV_OP_F 所有數值
            Compute_INV_OP_F(onlineDevicesDatas);
            // AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeOverallValues_in_SubSystem] ({this.Port}, {this.Protocol}) INV_OP_F, Phase_0 : {ComputedVals.INV_OP_F.Phase_0}, Phase_180 : {ComputedVals.INV_OP_F.Phase_180}, Phase_120 : {ComputedVals.INV_OP_F.Phase_120}, Phase_240 : {ComputedVals.INV_OP_F.Phase_240}", AppLogLevel.Debug);

            //更新 INV_OP_A 所有數值
            Compute_INV_OP_A(onlineDevicesDatas);
            // AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeOverallValues_in_SubSystem] ({this.Port}, {this.Protocol}) INV_OP_A, Phase_0 : {ComputedVals.INV_OP_A.Phase_0}, Phase_180 : {ComputedVals.INV_OP_A.Phase_180}, Phase_120 : {ComputedVals.INV_OP_A.Phase_120}, Phase_240 : {ComputedVals.INV_OP_A.Phase_240}", AppLogLevel.Debug);

            //更新 INV_OP_Load
            Compute_INV_OP_Load(onlineDevicesDatas);
            // AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeOverallValues_in_SubSystem] ({this.Port}, {this.Protocol}) INV_OP_Load : {ComputedVals.INV_OP_Load}", AppLogLevel.Debug);

            //更新 INV_OP_VA
            Compute_INV_OP_VA(onlineDevicesDatas);
            // AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeOverallValues_in_SubSystem] ({this.Port}, {this.Protocol}) INV_OP_VA : {ComputedVals.INV_OP_VA}", AppLogLevel.Debug);

            //更新 VBAT
            Compute_INV_VBAT(onlineDevicesDatas);
            // AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeOverallValues_in_SubSystem] ({this.Port}, {this.Protocol}) INV_VBAT : {ComputedVals.INV_VBAT}", AppLogLevel.Debug);
            
            //取得 ArrowDirections
            ArrowDirections = ArrowDirectionHelper.Resolve(ComputedVals.nowMode, AC_Charger_Enable, AC_StandBy);


            //其他系統變數計算，未來擴充

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
            uint INV_MODE_Count = 0;
            uint SAVING_MODE_Count = 0;
            uint BYPASS_MODE_Count = 0;
            uint CHARGING_MODE_Count = 0;
            uint STANDBY_MODE_Count = 0;

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
                    ComputedVals.setMode(ConstDefinition.SYS_Mode_Options.ERROR);
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeMode] Device Addr {oneDevice_Data.addr} has INV_FAULT : {INV_FAULT_str}, set SubSystem mode to Error", AppLogLevel.Debug);
                }

                //2. 取得 INV_STATUS 命令中會影響 Mode 的 資料
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
            if (INV_MODE_Count > 0) { ComputedVals.setMode(ConstDefinition.SYS_Mode_Options.INVERTER); }
            else if (SAVING_MODE_Count > 0) { ComputedVals.setMode(ConstDefinition.SYS_Mode_Options.SAVING); }
            else if (BYPASS_MODE_Count > 0) { ComputedVals.setMode(ConstDefinition.SYS_Mode_Options.BY_PASS); }
            else if (CHARGING_MODE_Count > 0) { ComputedVals.setMode(ConstDefinition.SYS_Mode_Options.CHARGER); }
            else if (STANDBY_MODE_Count > 0) { ComputedVals.setMode(ConstDefinition.SYS_Mode_Options.STANDBY); }
            else { ComputedVals.setMode(ConstDefinition.SYS_Mode_Options.DISCON); }

            AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputeMode] ({this.Port}, {this.Protocol}) SubSystem Mode Computed : {ComputedVals.nowMode.ToDisplayName()} (INV:{INV_MODE_Count}, SAVING:{SAVING_MODE_Count}, BYPASS:{BYPASS_MODE_Count}, CHARGING:{CHARGING_MODE_Count}, STANDBY:{STANDBY_MODE_Count})", AppLogLevel.Trace);
        }

        /// <summary>
        /// 根據子系統內所有在線設備的資料，計算子系統的在線Inverter數量
        /// </summary>
        /// <param name="onlineDevicesDatas"></param>
        public void ComputeOnline_INV_Num(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            //計算在線的 Inverter 數量
            ComputedVals.ComputeOnline_INV_Num = (uint)onlineDevicesDatas.Count;
        }

        /// <summary>
        /// 根據子系統內所有在線設備的資料，計算子系統的相位模式(nowPhase)
        /// </summary>
        /// <param name="onlineDevicesDatas"></param>
        public void ComputePhase(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            ConstDefinition.INV_Phase_SubSys tmpPhase = ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE;

            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取得 INV_STATUS 命令中會影響 Phase 的 資料
                var INV_STATUS_DecodeList = (List<decodeContent>?)oneDevice_Data.parseCmdData("INV_STATUS");
                if (INV_STATUS_DecodeList is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][ComputePhase] INV_STATUS DecodeList is null, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }
                var oneDevice_Phase = BitFieldParser.Parse_INV_STATUS_Phase_Enum(INV_STATUS_DecodeList);

                switch (oneDevice_Phase)
                {
                    case ConstDefinition.INV_Phase_Options.PHASE_0:
                        if (tmpPhase <= ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE)
                        {
                            tmpPhase = ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE;
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_180:
                        if (tmpPhase <= ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE)
                        {
                            tmpPhase = ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE;
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_120:
                    case ConstDefinition.INV_Phase_Options.PHASE_240:
                        if (tmpPhase <= ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmpPhase = ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE;
                        }
                        break;
                    default:
                        break;
                }
            }

            // Console.WriteLine($"[SubSystem][ComputePhase] ({this.Port}, {this.Protocol}) Phase computed : {tmpPhase.ToString()}");
            ComputedVals.setPhase(tmpPhase);
        }

        public void Compute_INV_IP_V(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            double tmp_phase_0 = 0.0;
            double tmp_phase_180 = 0.0;
            double tmp_phase_120 = 0.0;
            double tmp_phase_240 = 0.0;


            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取單台的Phase
                var decodeList = (List<decodeContent>?)oneDevice_Data.parseCmdData("INV_STATUS");
                if (decodeList is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_V] INV_STATUS DecodeList is null, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }
                var oneDevice_Phase = BitFieldParser.Parse_INV_STATUS_Phase_Enum(decodeList);
                //2. 取單台的 READ_VIN
                var oneDevice_READ_VIN_val = ((double?)oneDevice_Data.parseCmdData("READ_VIN")) ?? 0.0;

                switch (oneDevice_Phase)
                {
                    case ConstDefinition.INV_Phase_Options.PHASE_0:
                        tmp_phase_0 = (oneDevice_READ_VIN_val > tmp_phase_0) ? oneDevice_READ_VIN_val : tmp_phase_0;
                        // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_V] Device Addr {oneDevice_Data.addr} Phase 0 READ_VIN : {oneDevice_READ_VIN_val}, tmp_phase_0 updated to {tmp_phase_0}", AppLogLevel.Debug);
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_180:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE)
                        {
                            tmp_phase_180 = (oneDevice_READ_VIN_val > tmp_phase_180) ? oneDevice_READ_VIN_val : tmp_phase_180;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_V] Device Addr {oneDevice_Data.addr} Phase 180 READ_VIN : {oneDevice_READ_VIN_val}, tmp_phase_180 updated to {tmp_phase_180}", AppLogLevel.Debug);
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_120:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_120 = (oneDevice_READ_VIN_val > tmp_phase_120) ? oneDevice_READ_VIN_val : tmp_phase_120;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_V] Device Addr {oneDevice_Data.addr} Phase 120 READ_VIN : {oneDevice_READ_VIN_val}, tmp_phase_120 updated to {tmp_phase_120}", AppLogLevel.Debug);
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_240:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_240 = (oneDevice_READ_VIN_val > tmp_phase_240) ? oneDevice_READ_VIN_val : tmp_phase_240;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_V] Device Addr {oneDevice_Data.addr} Phase 240 READ_VIN : {oneDevice_READ_VIN_val}, tmp_phase_240 updated to {tmp_phase_240}", AppLogLevel.Debug);
                        }
                        break;
                    default:
                        break;
                }
            }

            switch (ComputedVals.nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    //只計算 Phase 0，其他相位設為0
                    ComputedVals.INV_IP_V.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_IP_V.Phase_180 = 0.0;
                    ComputedVals.INV_IP_V.Phase_120 = 0.0;
                    ComputedVals.INV_IP_V.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    //計算 Phase 0 和 Phase 180，其他相位設為0
                    ComputedVals.INV_IP_V.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_IP_V.Phase_180 = tmp_phase_180;
                    ComputedVals.INV_IP_V.Phase_120 = 0.0;
                    ComputedVals.INV_IP_V.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    //計算 Phase 0 和 Phase 120 和 Phase 240，Phase 180設為0
                    ComputedVals.INV_IP_V.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_IP_V.Phase_180 = 0.0;
                    ComputedVals.INV_IP_V.Phase_120 = tmp_phase_120;
                    ComputedVals.INV_IP_V.Phase_240 = tmp_phase_240;
                    break;
                default:
                    break;
            }
        }

        public void Compute_INV_IP_F(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            double tmp_phase_0 = 0.0;
            double tmp_phase_180 = 0.0;
            double tmp_phase_120 = 0.0;
            double tmp_phase_240 = 0.0;
            double count_R = 0.0;
            double count_S = 0.0;
            double count_T_120 = 0.0;
            double count_T_240 = 0.0;

            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取單台的Phase
                var decodeList = (List<decodeContent>?)oneDevice_Data.parseCmdData("INV_STATUS");
                if (decodeList is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_F] INV_STATUS DecodeList is null, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }
                var oneDevice_Phase = BitFieldParser.Parse_INV_STATUS_Phase_Enum(decodeList);

                //2. 取單台的 READ_FREQ
                var oneDevice_READ_FREQ_val = ((double?)oneDevice_Data.parseCmdData("READ_FREQ")) ?? 0.0;
                //排除 READ_FREQ 為 0.0 的設備
                if (oneDevice_READ_FREQ_val == 0.0)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_F] Device Addr {oneDevice_Data.addr} READ_FREQ is 0.0, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }

                switch (oneDevice_Phase)
                {
                    case ConstDefinition.INV_Phase_Options.PHASE_0:
                        tmp_phase_0 += oneDevice_READ_FREQ_val;
                        count_R += 1.0;
                        // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_F] Device Addr {oneDevice_Data.addr} Phase 0 READ_FREQ : {oneDevice_READ_FREQ_val}, tmp_phase_0 accumulated to {tmp_phase_0}", AppLogLevel.Debug);
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_180:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE)
                        {
                            tmp_phase_180 += oneDevice_READ_FREQ_val;
                            count_S += 1.0;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_F] Device Addr {oneDevice_Data.addr} Phase 180 READ_FREQ : {oneDevice_READ_FREQ_val}, tmp_phase_180 accumulated to {tmp_phase_180}", AppLogLevel.Debug);
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_120:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_120 += oneDevice_READ_FREQ_val;
                            count_T_120 += 1.0;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_F] Device Addr {oneDevice_Data.addr} Phase 120 READ_FREQ : {oneDevice_READ_FREQ_val}, tmp_phase_120 accumulated to {tmp_phase_120}", AppLogLevel.Debug);
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_240:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_240 += oneDevice_READ_FREQ_val;
                            count_T_240 += 1.0;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_IP_F] Device Addr {oneDevice_Data.addr} Phase 240 READ_FREQ : {oneDevice_READ_FREQ_val}, tmp_phase_240 accumulated to {tmp_phase_240}", AppLogLevel.Debug);
                        }
                        break;
                    default:
                        break;
                }
            }

            switch (ComputedVals.nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    //只計算 Phase 0，其他相位設為0
                    ComputedVals.INV_IP_F.Phase_0 = tmp_phase_0 / ((count_R == 0.0) ? 1.0 : count_R);
                    ComputedVals.INV_IP_F.Phase_180 = 0.0;
                    ComputedVals.INV_IP_F.Phase_120 = 0.0;
                    ComputedVals.INV_IP_F.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    //計算 Phase 0 和 Phase 180，其他相位設為0
                    ComputedVals.INV_IP_F.Phase_0 = tmp_phase_0 / ((count_R == 0.0) ? 1.0 : count_R);
                    ComputedVals.INV_IP_F.Phase_180 = tmp_phase_180 / ((count_S == 0.0) ? 1.0 : count_S);
                    ComputedVals.INV_IP_F.Phase_120 = 0.0;
                    ComputedVals.INV_IP_F.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    //計算 Phase 0 和 Phase 120 和 Phase 240，Phase 180設為0
                    ComputedVals.INV_IP_F.Phase_0 = tmp_phase_0 / ((count_R == 0.0) ? 1.0 : count_R);
                    ComputedVals.INV_IP_F.Phase_180 = 0.0;
                    ComputedVals.INV_IP_F.Phase_120 = tmp_phase_120 / ((count_T_120 == 0.0) ? 1.0 : count_T_120);
                    ComputedVals.INV_IP_F.Phase_240 = tmp_phase_240 / ((count_T_240 == 0.0) ? 1.0 : count_T_240);
                    break;
                default:
                    break;
            }
        }

        public void Compute_INV_OP_V(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            double tmp_phase_0 = 0.0;
            double tmp_phase_180 = 0.0;
            double tmp_phase_120 = 0.0;
            double tmp_phase_240 = 0.0;


            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取單台的Phase
                var decodeList = (List<decodeContent>?)oneDevice_Data.parseCmdData("INV_STATUS");
                if (decodeList is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_V] INV_STATUS DecodeList is null, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }
                var oneDevice_Phase = BitFieldParser.Parse_INV_STATUS_Phase_Enum(decodeList);
                //2. 取單台的 READ_AC_VOUT
                var oneDevice_READ_AC_VOUT_val = ((double?)oneDevice_Data.parseCmdData("READ_AC_VOUT")) ?? 0.0;

                switch (oneDevice_Phase)
                {
                    case ConstDefinition.INV_Phase_Options.PHASE_0:
                        tmp_phase_0 = (oneDevice_READ_AC_VOUT_val > tmp_phase_0) ? oneDevice_READ_AC_VOUT_val : tmp_phase_0;
                        // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_V] Device Addr {oneDevice_Data.addr} Phase 0 READ_AC_VOUT : {oneDevice_READ_AC_VOUT_val}, tmp_phase_0 updated to {tmp_phase_0}", AppLogLevel.Debug);
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_180:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE)
                        {
                            tmp_phase_180 = (oneDevice_READ_AC_VOUT_val > tmp_phase_180) ? oneDevice_READ_AC_VOUT_val : tmp_phase_180;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_V] Device Addr {oneDevice_Data.addr} Phase 180 READ_AC_VOUT : {oneDevice_READ_AC_VOUT_val}, tmp_phase_180 updated to {tmp_phase_180}", AppLogLevel.Debug);
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_120:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_120 = (oneDevice_READ_AC_VOUT_val > tmp_phase_120) ? oneDevice_READ_AC_VOUT_val : tmp_phase_120;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_V] Device Addr {oneDevice_Data.addr} Phase 120 READ_AC_VOUT : {oneDevice_READ_AC_VOUT_val}, tmp_phase_120 updated to {tmp_phase_120}", AppLogLevel.Debug);
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_240:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_240 = (oneDevice_READ_AC_VOUT_val > tmp_phase_240) ? oneDevice_READ_AC_VOUT_val : tmp_phase_240;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_V] Device Addr {oneDevice_Data.addr} Phase 240 READ_AC_VOUT : {oneDevice_READ_AC_VOUT_val}, tmp_phase_240 updated to {tmp_phase_240}", AppLogLevel.Debug);
                        }
                        break;
                    default:
                        break;
                }
            }

            switch (ComputedVals.nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    //只計算 Phase 0，其他相位設為0
                    ComputedVals.INV_OP_V.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_OP_V.Phase_180 = 0.0;
                    ComputedVals.INV_OP_V.Phase_120 = 0.0;
                    ComputedVals.INV_OP_V.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    //計算 Phase 0 和 Phase 180，其他相位設為0
                    ComputedVals.INV_OP_V.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_OP_V.Phase_180 = tmp_phase_180;
                    ComputedVals.INV_OP_V.Phase_120 = 0.0;
                    ComputedVals.INV_OP_V.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    //計算 Phase 0 和 Phase 120 和 Phase 240，Phase 180設為0
                    ComputedVals.INV_OP_V.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_OP_V.Phase_180 = 0.0;
                    ComputedVals.INV_OP_V.Phase_120 = tmp_phase_120;
                    ComputedVals.INV_OP_V.Phase_240 = tmp_phase_240;
                    break;
                default:
                    break;
            }
        }

        public void Compute_INV_OP_F(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            double tmp_phase_0 = 0.0;
            double tmp_phase_180 = 0.0;
            double tmp_phase_120 = 0.0;
            double tmp_phase_240 = 0.0;
            double count_R = 0.0;
            double count_S = 0.0;
            double count_T_120 = 0.0;
            double count_T_240 = 0.0;

            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取單台的Phase
                var decodeList = (List<decodeContent>?)oneDevice_Data.parseCmdData("INV_STATUS");
                if (decodeList is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_F] INV_STATUS DecodeList is null, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }
                var oneDevice_Phase = BitFieldParser.Parse_INV_STATUS_Phase_Enum(decodeList);

                //2. 取單台的 READ_AC_FOUT
                var oneDevice_READ_AC_FOUT_val = ((double?)oneDevice_Data.parseCmdData("READ_AC_FOUT")) ?? 0.0;
                //排除 READ_AC_FOUT 為 0.0 的設備
                if (oneDevice_READ_AC_FOUT_val == 0.0)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_F] Device Addr {oneDevice_Data.addr} READ_AC_FOUT is 0.0, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }

                switch (oneDevice_Phase)
                {
                    case ConstDefinition.INV_Phase_Options.PHASE_0:
                        tmp_phase_0 += oneDevice_READ_AC_FOUT_val;
                        count_R += 1.0;
                        // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_F] Device Addr {oneDevice_Data.addr} Phase 0 READ_AC_FOUT : {oneDevice_READ_AC_FOUT_val}, tmp_phase_0 accumulated to {tmp_phase_0}", AppLogLevel.Debug);
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_180:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE)
                        {
                            tmp_phase_180 += oneDevice_READ_AC_FOUT_val;
                            count_S += 1.0;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_F] Device Addr {oneDevice_Data.addr} Phase 180 READ_AC_FOUT : {oneDevice_READ_AC_FOUT_val}, tmp_phase_180 accumulated to {tmp_phase_180}", AppLogLevel.Debug);
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_120:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_120 += oneDevice_READ_AC_FOUT_val;
                            count_T_120 += 1.0;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_F] Device Addr {oneDevice_Data.addr} Phase 120 READ_AC_FOUT : {oneDevice_READ_AC_FOUT_val}, tmp_phase_120 accumulated to {tmp_phase_120}", AppLogLevel.Debug);
                        }
                        break;

                    case ConstDefinition.INV_Phase_Options.PHASE_240:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_240 += oneDevice_READ_AC_FOUT_val;
                            count_T_240 += 1.0;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_F] Device Addr {oneDevice_Data.addr} Phase 240 READ_AC_FOUT : {oneDevice_READ_AC_FOUT_val}, tmp_phase_240 accumulated to {tmp_phase_240}", AppLogLevel.Debug);
                        }
                        break;
                    default:
                        break;
                }
            }

            switch (ComputedVals.nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    //只計算 Phase 0，其他相位設為0
                    ComputedVals.INV_OP_F.Phase_0 = tmp_phase_0 / ((count_R == 0.0) ? 1.0 : count_R);
                    ComputedVals.INV_OP_F.Phase_180 = 0.0;
                    ComputedVals.INV_OP_F.Phase_120 = 0.0;
                    ComputedVals.INV_OP_F.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    //計算 Phase 0 和 Phase 180，其他相位設為0
                    ComputedVals.INV_OP_F.Phase_0 = tmp_phase_0 / ((count_R == 0.0) ? 1.0 : count_R);
                    ComputedVals.INV_OP_F.Phase_180 = tmp_phase_180 / ((count_S == 0.0) ? 1.0 : count_S);
                    ComputedVals.INV_OP_F.Phase_120 = 0.0;
                    ComputedVals.INV_OP_F.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    //計算 Phase 0 和 Phase 120 和 Phase 240，Phase 180設為0
                    ComputedVals.INV_OP_F.Phase_0 = tmp_phase_0 / ((count_R == 0.0) ? 1.0 : count_R);
                    ComputedVals.INV_OP_F.Phase_180 = 0.0;
                    ComputedVals.INV_OP_F.Phase_120 = tmp_phase_120 / ((count_T_120 == 0.0) ? 1.0 : count_T_120);
                    ComputedVals.INV_OP_F.Phase_240 = tmp_phase_240 / ((count_T_240 == 0.0) ? 1.0 : count_T_240);
                    break;
                default:
                    break;
            }
        }

        public void Compute_INV_OP_A(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            double tmp_phase_0 = 0.0;
            double tmp_phase_180 = 0.0;
            double tmp_phase_120 = 0.0;
            double tmp_phase_240 = 0.0;

            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取單台的Phase
                var decodeList = (List<decodeContent>?)oneDevice_Data.parseCmdData("INV_STATUS");
                if (decodeList is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_A] INV_STATUS DecodeList is null, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }
                var oneDevice_Phase = BitFieldParser.Parse_INV_STATUS_Phase_Enum(decodeList);

                //2. 取單台的 READ_AC_VOUT
                var oneDevice_READ_AC_VOUT_val = ((double?)oneDevice_Data.parseCmdData("READ_AC_VOUT")) ?? 0.0;
                //排除 READ_AC_VOUT 為 0.0 的設備
                if (oneDevice_READ_AC_VOUT_val == 0.0)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_A] Device Addr {oneDevice_Data.addr} READ_AC_VOUT is 0.0, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }

                //3. 取單台的 READ_OP_VA
                var oneDevice_READ_OP_VA_val = ((double?)oneDevice_Data.parseCmdData("READ_OP_VA")) ?? 0.0;

                //4. 計算單台的 INV_Current
                double oneDevice_INV_Current_val = oneDevice_READ_OP_VA_val / ((oneDevice_READ_AC_VOUT_val == 0.0) ? 1.0 : oneDevice_READ_AC_VOUT_val);
                oneDevice_INV_Current_val = (oneDevice_INV_Current_val < 0.5) ? 0.0 : oneDevice_INV_Current_val; //過濾過小的電流值

                switch (oneDevice_Phase)
                {
                    case ConstDefinition.INV_Phase_Options.PHASE_0:
                        tmp_phase_0 += oneDevice_INV_Current_val;
                        // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_A] Device Addr {oneDevice_Data.addr} Phase 0 INV_AC_VOUT : {oneDevice_READ_AC_VOUT_val}, READ_OP_VA : {oneDevice_READ_OP_VA_val}, tmp_phase_0 accumulated to {tmp_phase_0}", AppLogLevel.Debug);
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_180:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE)
                        {
                            tmp_phase_180 += oneDevice_INV_Current_val;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_A] Device Addr {oneDevice_Data.addr} Phase 180 INV_AC_VOUT : {oneDevice_READ_AC_VOUT_val}, READ_OP_VA : {oneDevice_READ_OP_VA_val}, tmp_phase_180 accumulated to {tmp_phase_180}", AppLogLevel.Debug);
                        }
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_120:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_120 += oneDevice_INV_Current_val;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_A] Device Addr {oneDevice_Data.addr} Phase 120 INV_AC_VOUT : {oneDevice_READ_AC_VOUT_val}, READ_OP_VA : {oneDevice_READ_OP_VA_val}, tmp_phase_120 accumulated to {tmp_phase_120}", AppLogLevel.Debug);
                        }
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_240:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_240 += oneDevice_INV_Current_val;
                            // AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_A] Device Addr {oneDevice_Data.addr} Phase 240 INV_AC_VOUT : {oneDevice_READ_AC_VOUT_val}, READ_OP_VA : {oneDevice_READ_OP_VA_val}, tmp_phase_240 accumulated to {tmp_phase_240}", AppLogLevel.Debug);
                        }
                        break;
                    default:
                        break;
                }
            }

            switch (ComputedVals.nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    //只計算 Phase 0，其他相位設為0
                    ComputedVals.INV_OP_A.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_OP_A.Phase_180 = 0.0;
                    ComputedVals.INV_OP_A.Phase_120 = 0.0;
                    ComputedVals.INV_OP_A.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    //計算 Phase 0 和 Phase 180，其他相位設為0
                    ComputedVals.INV_OP_A.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_OP_A.Phase_180 = tmp_phase_180;
                    ComputedVals.INV_OP_A.Phase_120 = 0.0;
                    ComputedVals.INV_OP_A.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    //計算 Phase 0 和 Phase 120 和 Phase 240，Phase 180設為0
                    ComputedVals.INV_OP_A.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_OP_A.Phase_180 = 0.0;
                    ComputedVals.INV_OP_A.Phase_120 = tmp_phase_120;
                    ComputedVals.INV_OP_A.Phase_240 = tmp_phase_240;
                    break;
                default:
                    break;
            }
        }

        public void Compute_INV_OP_Load(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            double total_LOAD = 0.0;
            double counter = 0.0;
            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取單台的 OP_LD_PCNT
                var oneDevice_OP_LOAD_val = ((double?)oneDevice_Data.parseCmdData("READ_OP_LD_PCNT")) ?? 0.0;
                total_LOAD += oneDevice_OP_LOAD_val;
                counter += 1.0;
            }

            counter = (counter == 0.0) ? 1.0 : counter;
            total_LOAD = total_LOAD / counter;

            ComputedVals.INV_OP_Load = total_LOAD;
        }

        public void Compute_INV_OP_VA(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            double tmp_phase_0 = 0.0;
            double tmp_phase_180 = 0.0;
            double tmp_phase_120 = 0.0;
            double tmp_phase_240 = 0.0;

            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取單台的Phase
                var decodeList = (List<decodeContent>?)oneDevice_Data.parseCmdData("INV_STATUS");
                if (decodeList is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_VA] INV_STATUS DecodeList is null, exclude device in computing", AppLogLevel.Debug);
                    continue;
                }
                var oneDevice_Phase = BitFieldParser.Parse_INV_STATUS_Phase_Enum(decodeList);

                //2. 取單台的 READ_OP_VA
                var oneDevice_OP_VA_val = ((double?)oneDevice_Data.parseCmdData("READ_OP_VA")) ?? 0.0;

                switch (oneDevice_Phase)
                {
                    case ConstDefinition.INV_Phase_Options.PHASE_0:
                        tmp_phase_0 += oneDevice_OP_VA_val;
                        AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_VA] Device Addr {oneDevice_Data.addr} Phase 0 READ_OP_VA : {oneDevice_OP_VA_val}, tmp_phase_0 accumulated to {tmp_phase_0}", AppLogLevel.Trace);
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_180:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE)
                        {
                            tmp_phase_180 += oneDevice_OP_VA_val;
                            AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_VA] Device Addr {oneDevice_Data.addr} Phase 180 READ_OP_VA : {oneDevice_OP_VA_val}, tmp_phase_180 accumulated to {tmp_phase_180}", AppLogLevel.Trace);
                        }
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_120:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_120 += oneDevice_OP_VA_val;
                            AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_VA] Device Addr {oneDevice_Data.addr} Phase 120 READ_OP_VA : {oneDevice_OP_VA_val}, tmp_phase_120 accumulated to {tmp_phase_120}", AppLogLevel.Trace);
                        }
                        break;
                    case ConstDefinition.INV_Phase_Options.PHASE_240:
                        if (ComputedVals.nowPhase == ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE)
                        {
                            tmp_phase_240 += oneDevice_OP_VA_val;
                            AppLogger.Log_To_File_log(_category, $"[SubSystem][Compute_INV_OP_VA] Device Addr {oneDevice_Data.addr} Phase 240 READ_OP_VA : {oneDevice_OP_VA_val}, tmp_phase_240 accumulated to {tmp_phase_240}", AppLogLevel.Trace);
                        }
                        break;
                    default:
                        break;
                }
            }

            tmp_phase_0 = (tmp_phase_0 < 500.0) ? 0.0 : tmp_phase_0;
            tmp_phase_180 = (tmp_phase_180 < 500.0) ? 0.0 : tmp_phase_180;
            tmp_phase_120 = (tmp_phase_120 < 500.0) ? 0.0 : tmp_phase_120;
            tmp_phase_240 = (tmp_phase_240 < 500.0) ? 0.0 : tmp_phase_240;

            switch (ComputedVals.nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    //只計算 Phase 0，其他相位設為0
                    ComputedVals.INV_OP_VA.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_OP_VA.Phase_180 = 0.0;
                    ComputedVals.INV_OP_VA.Phase_120 = 0.0;
                    ComputedVals.INV_OP_VA.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    //計算 Phase 0 和 Phase 180，其他相位設為0
                    ComputedVals.INV_OP_VA.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_OP_VA.Phase_180 = tmp_phase_180;
                    ComputedVals.INV_OP_VA.Phase_120 = 0.0;
                    ComputedVals.INV_OP_VA.Phase_240 = 0.0;
                    break;
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    //計算 Phase 0 和 Phase 120 和 Phase 240，Phase 180設為0
                    ComputedVals.INV_OP_VA.Phase_0 = tmp_phase_0;
                    ComputedVals.INV_OP_VA.Phase_180 = 0.0;
                    ComputedVals.INV_OP_VA.Phase_120 = tmp_phase_120;
                    ComputedVals.INV_OP_VA.Phase_240 = tmp_phase_240;
                    break;
                default:
                    break;
            }
        }

        public void Compute_INV_VBAT(List<Real_SingleDeviceData_JsonFormat> onlineDevicesDatas)
        {
            double tmp_VBAT = 0.0;
            foreach (var oneDevice_Data in onlineDevicesDatas)
            {
                //1. 取單台的 READ_VBAT
                var oneDevice_READ_VBAT_val = ((double?)oneDevice_Data.parseCmdData("READ_VBAT")) ?? 0.0;
                tmp_VBAT = (oneDevice_READ_VBAT_val > tmp_VBAT) ? oneDevice_READ_VBAT_val : tmp_VBAT;
            }

            ComputedVals.V_BAT = tmp_VBAT;
        }
        #endregion Computed In SubSystem

        public string? GetSummaryValue(string cmdName)
        {
            return cmdName switch
            {
                "Input_V" => ComputedVals.getINV_IP_V_str(),
                "Input_F" => ComputedVals.getINV_IP_F_str(),
                "Output_V" => ComputedVals.getINV_OP_V_str(),
                "Output_F" => ComputedVals.getINV_OP_F_str(),
                "Output_I" => ComputedVals.getINV_OP_I_str(),
                "Output_Load" => ComputedVals.getINV_OP_Load_str(),
                "Output_VA" => ComputedVals.getINV_OP_VA_str(),
                "Battery_V" => ComputedVals.getINV_VBAT_str(),
                _ => null
            };
        }
    }

    public class ComputedValues_In_SubSystem
    {
        public ConstDefinition.SYS_Mode_Options nowMode { get; set; } = ConstDefinition.SYS_Mode_Options.DISCON;
        public uint ComputeOnline_INV_Num { get; set; } = 0;
        public ConstDefinition.INV_Phase_SubSys nowPhase { get; set; } = ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE;

        public PhasesVar INV_IP_V { get; set; } = new PhasesVar();
        public PhasesVar INV_IP_F { get; set; } = new PhasesVar();
        public PhasesVar INV_OP_V { get; set; } = new PhasesVar();
        public PhasesVar INV_OP_F { get; set; } = new PhasesVar();
        public PhasesVar INV_OP_A { get; set; } = new PhasesVar();
        public PhasesVar INV_OP_VA { get; set; } = new PhasesVar();
        public double INV_OP_Load { get; set; } = 0.0;
        public double V_BAT { get; set; } = 0.0;

        public void setMode(ConstDefinition.SYS_Mode_Options mode) => this.nowMode = mode;
        public void setPhase(ConstDefinition.INV_Phase_SubSys phase) => this.nowPhase = phase;

        public string getINV_IP_V_str()
        {
            switch (nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    return $"R: {INV_IP_V.Phase_0:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    return $"R: {INV_IP_V.Phase_0:F2}, S: {INV_IP_V.Phase_180:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    return $"R: {INV_IP_V.Phase_0:F2}, T1: {INV_IP_V.Phase_120:F2}, T2: {INV_IP_V.Phase_240:F2}";
                default:
                    return $"N/A";
            }
        }

        public string getINV_IP_F_str()
        {
            switch (nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    return $"R: {INV_IP_F.Phase_0:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    return $"R: {INV_IP_F.Phase_0:F2}, S: {INV_IP_F.Phase_180:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    return $"R: {INV_IP_F.Phase_0:F2}, T1: {INV_IP_F.Phase_120:F2}, T2: {INV_IP_F.Phase_240:F2}";
                default:
                    return $"N/A";
            }
        }
        public string getINV_OP_V_str()
        {
            switch (nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    return $"R: {INV_OP_V.Phase_0:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    return $"R: {INV_OP_V.Phase_0:F2}, S: {INV_OP_V.Phase_180:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    return $"R: {INV_OP_V.Phase_0:F2}, T1: {INV_OP_V.Phase_120:F2}, T2: {INV_OP_V.Phase_240:F2}";
                default:
                    return $"N/A";
            }
        }
        public string getINV_OP_F_str()
        {
            switch (nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    return $"R: {INV_OP_F.Phase_0:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    return $"R: {INV_OP_F.Phase_0:F2}, S: {INV_OP_F.Phase_180:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    return $"R: {INV_OP_F.Phase_0:F2}, T1: {INV_OP_F.Phase_120:F2}, T2: {INV_OP_F.Phase_240:F2}";
                default:
                    return $"N/A";
            }
        }
        public string getINV_OP_I_str()
        {
            switch (nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    return $"R: {INV_OP_A.Phase_0:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    return $"R: {INV_OP_A.Phase_0:F2}, S: {INV_OP_A.Phase_180:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    return $"R: {INV_OP_A.Phase_0:F2}, T1: {INV_OP_A.Phase_120:F2}, T2: {INV_OP_A.Phase_240:F2}";
                default:
                    return $"N/A";
            }
        }
        public string getINV_OP_Load_str()
        {
            return $"{INV_OP_Load:F2}";
        }
        public string getINV_OP_VA_str()
        {
            switch (nowPhase)
            {
                case ConstDefinition.INV_Phase_SubSys.INV_SINGLE_PHASE:
                    return $"R: {INV_OP_VA.Phase_0:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_TWO_PHASE:
                    return $"R: {INV_OP_VA.Phase_0:F2}, S: {INV_OP_VA.Phase_180:F2}";
                case ConstDefinition.INV_Phase_SubSys.INV_THREE_PHASE:
                    return $"R: {INV_OP_VA.Phase_0:F2}, T1: {INV_OP_VA.Phase_120:F2}, T2: {INV_OP_VA.Phase_240:F2}";
                default:
                    return $"N/A";
            }
        }
        public string getINV_VBAT_str()
        {
            return $"{V_BAT:F2}";
        }
    }

    public class PhasesVar
    {
        public double Phase_0 { get; set; } = 0.0;
        public double Phase_180 { get; set; } = 0.0;
        public double Phase_120 { get; set; } = 0.0;
        public double Phase_240 { get; set; } = 0.0;
    }
}