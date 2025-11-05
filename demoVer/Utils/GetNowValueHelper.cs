using System.Diagnostics;
using demoVer.Models;
using MudBlazor;
namespace demoVer.Utils
{
    public static class GetNowValueHelper
    {
        private static readonly string _category = typeof(GetNowValueHelper).FullName!;
        public static string getClearCmdCode(string rawCmdCode)
        {
            if (rawCmdCode.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return rawCmdCode.Substring(2);
            }
            else
            {
                return rawCmdCode;
            }
        }
        /// <summary>
        /// 取得目前曲線設定的階段及Timeout Enable狀態
        /// </summary>
        /// <param name="nowValuesInfo">整個子系統的InfosForWriteCmd</param>
        /// <param name="ClearCmdCode">目標命令的純命令碼(無0x)</param>
        /// <returns> (目前曲線設定的階段, CCT_Enable狀態, CVT_Enable狀態, FVT_Enable狀態)</returns>
        public static (bool selectstage, bool CCT_Enable, bool CVT_Enable, bool FVT_Enable) parse_CURVE_CONFIG(Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat> nowValuesInfo, string ClearCmdCode)
        {

            bool selectStage = false;
            bool CCT_Enable = false;
            bool CVT_Enable = false;
            bool FVT_Enable = false;

            try
            {
                var cmdSettingData = nowValuesInfo.ContainsKey(ClearCmdCode) ? nowValuesInfo[ClearCmdCode] : null;
                if (cmdSettingData is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_CURVE_CONFIG] cmdSettingData is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return (false, false, false, false);
                }

                string RawCmdCode_tmp = "0x" + ClearCmdCode;
                if (!string.Equals(RawCmdCode_tmp, cmdSettingData.cmdCode, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_CURVE_CONFIG] cmdCode mismatch: expected {RawCmdCode_tmp}, got {cmdSettingData.cmdCode}", AppLogLevel.Debug);
                    return (false, false, false, false);
                }

                if (cmdSettingData.Target is null || cmdSettingData.Target.Bits is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_CURVE_CONFIG] one of the (Target, or Target.Bits) is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return (false, false, false, false);
                }

                //取Bit6(CurveStage)
                if (cmdSettingData.Target.Bits.TryGetValue("BIT_6", out int val))
                {
                    if (val == 1) { selectStage = true; }
                    else { selectStage = false; }
                }

                //取Bit8 (CCT_Enable)
                if (cmdSettingData.Target.Bits.TryGetValue("BIT_8", out int val2))
                {
                    if (val2 == 1) { CCT_Enable = true; }
                    else { CCT_Enable = false; }
                }

                //取Bit9 (CVT_Enable)
                if (cmdSettingData.Target.Bits.TryGetValue("BIT_9", out int val3))
                {
                    if (val3 == 1) { CVT_Enable = true; }
                    else { CVT_Enable = false; }
                }
                //取Bit10 (FVT_Enable)
                if (cmdSettingData.Target.Bits.TryGetValue("BIT_10", out int val4))
                {
                    if (val4 == 1) { FVT_Enable = true; }
                    else { FVT_Enable = false; }
                }

                AppLogger.Log_To_File_log(_category, $"[parse_CURVE_CONFIG] CmdCode: {ClearCmdCode}, Result: selectStage = {selectStage}, CCT_Enable = {CCT_Enable}, CVT_Enable = {CVT_Enable}, FVT_Enable = {FVT_Enable}", AppLogLevel.Debug);
                return (selectStage, CCT_Enable, CVT_Enable, FVT_Enable);
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[parse_CURVE_CONFIG] CmdCode: {ClearCmdCode}, Exception: {e.Message}", AppLogLevel.Error);
                return (false, false, false, false);
            }
        }

        /// <summary>
        /// 取得目前 Numeric型態 數值
        /// </summary>
        /// <param name="nowValuesInfo">整個子系統的InfosForWriteCmd</param>
        /// <param name="ClearCmdCode">目標命令的純命令碼(無0x)</param>
        /// <returns>double型態的目標</returns>
        public static double parse_Numeric_Val(Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat> nowValuesInfo, string ClearCmdCode)
        {

            try
            {
                var cmdSettingData = nowValuesInfo.ContainsKey(ClearCmdCode) ? nowValuesInfo[ClearCmdCode] : null;
                if (cmdSettingData is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_Numeric_Val] cmdSettingData is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return 0.0;
                }

                string RawCmdCode_tmp = "0x" + ClearCmdCode;
                if (!string.Equals(RawCmdCode_tmp, cmdSettingData.cmdCode, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_Numeric_Val] cmdCode mismatch: expected {RawCmdCode_tmp}, got {cmdSettingData.cmdCode}", AppLogLevel.Debug);
                    return 0.0;
                }

                if (cmdSettingData.Target is null || cmdSettingData.Target.Number is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_Numeric_Val] one of the (Target, or Target.Number) is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return 0.0;
                }


                AppLogger.Log_To_File_log(_category, $"[parse_Numeric_Val] CmdCode: {ClearCmdCode}, Numeric Value: {cmdSettingData.Target.Number.Value}", AppLogLevel.Debug);

                return cmdSettingData.Target.Number ?? 0.0;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[parse_Numeric_Val] CmdCode: {ClearCmdCode}, Exception: {e.Message}", AppLogLevel.Error);
                return 0.0;
            }
        }

        /// <summary>
        /// 取得目前 輸出優先 跟 充電優先
        /// </summary>
        /// <param name="nowValuesInfo">整個子系統的InfosForWriteCmd</param>
        /// <param name="ClearCmdCode">目標命令的純命令碼(無0x)</param>
        /// <returns>outputPrio, ChargingPrio</returns>
        public static (OUTPUT_PRIORITY selectOutputPriority, CHARGING_PRIORITY selectChargingPriority) parse_INV_CONFIG(Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat> nowValuesInfo, string ClearCmdCode)
        {
            OUTPUT_PRIORITY outputPrio = OUTPUT_PRIORITY.Utility;
            CHARGING_PRIORITY chargingPrio = CHARGING_PRIORITY.Utility;

            try
            {
                var cmdSettingData = nowValuesInfo.ContainsKey(ClearCmdCode) ? nowValuesInfo[ClearCmdCode] : null;
                if (cmdSettingData is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_INV_CONFIG] cmdSettingData is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return (OUTPUT_PRIORITY.Utility, CHARGING_PRIORITY.Utility);
                }

                string RawCmdCode_tmp = "0x" + ClearCmdCode;
                if (!string.Equals(RawCmdCode_tmp, cmdSettingData.cmdCode, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_INV_CONFIG] cmdCode mismatch: expected {RawCmdCode_tmp}, got {cmdSettingData.cmdCode}", AppLogLevel.Debug);
                    return (OUTPUT_PRIORITY.Utility, CHARGING_PRIORITY.Utility);
                }

                if (cmdSettingData.Target is null || cmdSettingData.Target.Bits is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_INV_CONFIG] one of the (Target, or Target.Bits) is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return (OUTPUT_PRIORITY.Utility, CHARGING_PRIORITY.Utility);
                }

                //取OutputPriority
                if (cmdSettingData.Target.Bits.TryGetValue("BIT_0", out int val1))
                {
                    outputPrio = val1 switch
                    {
                        0 => OUTPUT_PRIORITY.Utility,
                        1 => OUTPUT_PRIORITY.Battery,
                        2 => OUTPUT_PRIORITY.Solar,
                        _ => OUTPUT_PRIORITY.Utility,
                    };
                }
                //取ChargingPriority
                if (cmdSettingData.Target.Bits.TryGetValue("BIT_2", out int val2))
                {
                    chargingPrio = val2 switch
                    {
                        0 => CHARGING_PRIORITY.Utility,
                        1 => CHARGING_PRIORITY.Solar,
                        _ => CHARGING_PRIORITY.Utility,
                    };
                }
                AppLogger.Log_To_File_log(_category, $"[parse_INV_CONFIG] CmdCode: {ClearCmdCode}, OutputPriority: {outputPrio}, ChargingPriority: {chargingPrio}", AppLogLevel.Debug);
                return (outputPrio, chargingPrio);
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[parse_INV_CONFIG] CmdCode: {ClearCmdCode}, Exception: {e.Message}", AppLogLevel.Error);
                return (OUTPUT_PRIORITY.Utility, CHARGING_PRIORITY.Utility);
            }
        }

        /// <summary>
        /// 取得目前 INV_OPERATION 的 CHG_EN 跟 GRID_EN 狀態
        /// </summary>
        /// <param name="nowValuesInfo">整個子系統的InfosForWriteCmd</param>
        /// <param name="ClearCmdCode">目標命令的純命令碼(無0x)</param>
        /// <returns>CHG_EN, GRID_EN</returns>
        public static (bool CHG_EN, bool GRID_EN) parse_INV_OPERATION(Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat> nowValuesInfo, string ClearCmdCode)
        {
            bool CHG_EN = false;
            bool GRID_EN = false;

            try
            {
                var cmdSettingData = nowValuesInfo.ContainsKey(ClearCmdCode) ? nowValuesInfo[ClearCmdCode] : null;
                if (cmdSettingData is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_INV_OPERATION] cmdSettingData is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return (false, false);
                }

                string RawCmdCode_tmp = "0x" + ClearCmdCode;
                if (!string.Equals(RawCmdCode_tmp, cmdSettingData.cmdCode, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_INV_OPERATION] cmdCode mismatch: expected {RawCmdCode_tmp}, got {cmdSettingData.cmdCode}", AppLogLevel.Debug);
                    return (false, false);
                }

                if (cmdSettingData.AddrValues is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_INV_OPERATION] AddrValues is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return (false, false);
                }

                //因整個子系統INV_Operation的BIT_2一樣，故取任一個addr的Value.Bits
                var firstAddrValue = cmdSettingData.AddrValues[0];
                if (firstAddrValue.Value.Bits is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_INV_OPERATION] Value.Bits is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return (false, false);
                }
                var firstBitsDict = firstAddrValue.Value.Bits;
                //取CHG_EN
                if (firstBitsDict.TryGetValue("BIT_2", out int val1))
                {
                    CHG_EN = val1 == 1;
                }
                //取GRID_EN
                if(firstBitsDict.TryGetValue("BIT_3", out int val2))
                {
                    GRID_EN = val2 == 1;
                }
                AppLogger.Log_To_File_log(_category, $"[parse_INV_OPERATION] CmdCode: {ClearCmdCode}, CHG_EN: {CHG_EN}, GRID_EN: {GRID_EN}", AppLogLevel.Debug);
                return (CHG_EN, GRID_EN);
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[parse_INV_OPERATION] CmdCode: {ClearCmdCode}, Exception: {e.Message}", AppLogLevel.Error);
                return (false, false);
            }
        }

        /// <summary>
        /// 取得目前 ACF_Set 設定
        /// </summary>
        /// <param name="nowValuesInfo">整個子系統的InfosForWriteCmd</param>
        /// <param name="ClearCmdCode">目標命令的純命令碼(無0x)</param>
        /// <returns>ACF_Set 的字串</returns>
        public static string parse_Output_ACF_Set(Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat> nowValuesInfo, string ClearCmdCode)
        {
            string ACF_Set_str = "50Hz";
            try
            {
                var cmdSettingData = nowValuesInfo.ContainsKey(ClearCmdCode) ? nowValuesInfo[ClearCmdCode] : null;
                if (cmdSettingData is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_ACF_Set] cmdSettingData is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return ACF_Set_str;
                }

                string RawCmdCode_tmp = "0x" + ClearCmdCode;
                if (!string.Equals(RawCmdCode_tmp, cmdSettingData.cmdCode, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_ACF_Set] cmdCode mismatch: expected {RawCmdCode_tmp}, got {cmdSettingData.cmdCode}", AppLogLevel.Debug);
                    return ACF_Set_str;
                }

                if (cmdSettingData.Target is null || cmdSettingData.Target.Bits is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_ACF_Set] one of the (Target, or Target.Bits) is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return ACF_Set_str;
                }

                //取Bit0 (ACF_Enable)
                if (cmdSettingData.Target.Bits.TryGetValue("BIT_0", out int val))
                {
                    switch (val)
                    {
                        case 1:
                            ACF_Set_str = "50Hz";
                            break;
                        case 2:
                            ACF_Set_str = "60Hz";
                            break;
                        default:
                            ACF_Set_str = "50Hz";
                            break;
                    }
                }
                AppLogger.Log_To_File_log(_category, $"[parse_ACF_Set] CmdCode: {ClearCmdCode}, ACF_Set_str: {ACF_Set_str}", AppLogLevel.Debug);
                return ACF_Set_str;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[parse_ACF_Set] CmdCode: {ClearCmdCode}, Exception: {e.Message}", AppLogLevel.Error);
                return ACF_Set_str;
            }
        }

        /// <summary>
        /// 取得目前 ACV_Set 設定
        /// </summary>
        /// <param name="nowValuesInfo">整個子系統的InfosForWriteCmd</param>
        /// <param name="ClearCmdCode">目標命令的純命令碼(無0x)</param>
        /// <returns>ACV_Set 的字串</returns>
        public static string parse_Output_ACV_Set(Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat> nowValuesInfo, string ClearCmdCode)
        {
            string ACV_Set_str = "100/200";
            try
            {
                var cmdSettingData = nowValuesInfo.ContainsKey(ClearCmdCode) ? nowValuesInfo[ClearCmdCode] : null;
                if (cmdSettingData is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_ACV_Set] cmdSettingData is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return ACV_Set_str;
                }

                string RawCmdCode_tmp = "0x" + ClearCmdCode;
                if (!string.Equals(RawCmdCode_tmp, cmdSettingData.cmdCode, StringComparison.OrdinalIgnoreCase))
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_ACV_Set] cmdCode mismatch: expected {RawCmdCode_tmp}, got {cmdSettingData.cmdCode}", AppLogLevel.Debug);
                    return ACV_Set_str;
                }

                if (cmdSettingData.Target is null || cmdSettingData.Target.Bits is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[parse_ACV_Set] one of the (Target, or Target.Bits) is null for CmdCode: {ClearCmdCode}", AppLogLevel.Debug);
                    return ACV_Set_str;
                }
                
                //取Bit0 (ACV_Set)
                if (cmdSettingData.Target.Bits.TryGetValue("BIT_0", out int val))
                {
                    switch (val)
                    {
                        case 1:
                            ACV_Set_str = "100/200";
                            break;
                        case 2:
                            ACV_Set_str = "110/220";
                            break;
                        case 3:
                            ACV_Set_str = "115/230";
                            break;
                        case 4:
                            ACV_Set_str = "120/240";
                            break;
                        default:
                            ACV_Set_str = "100/200";
                            break;
                    }
                }
                AppLogger.Log_To_File_log(_category, $"[parse_ACV_Set] CmdCode: {ClearCmdCode}, ACV_Set_str: {ACV_Set_str}", AppLogLevel.Debug);
                return ACV_Set_str;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[parse_ACV_Set] CmdCode: {ClearCmdCode}, Exception: {e.Message}", AppLogLevel.Error);
                return ACV_Set_str;
            }
        }
    }
}