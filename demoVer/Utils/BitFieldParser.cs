using demoVer.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;
using MudBlazor;
using demoVer.Services;
namespace demoVer.Utils
{
    public static class BitFieldParser
    {

        /// <summary>
        /// 用於顯示解析後的內容
        /// </summary>
        /// <param name="cmdName">命令名稱</param>
        /// <param name="decodeList">解碼內容列表</param>
        /// <returns>解析後的字串</returns>
        public static string FitParserFunction(string cmdName, List<decodeContent> decodeList)
        {
            switch (cmdName)
            {
                case "INV_FAULT":
                    return Parse_INV_FAULT(decodeList); //這也要改參數
                case "INV_STATUS":
                    return Parse_INV_STATUS(decodeList); //這也要改參數
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 解析INV_FAULT的BitField，回傳所有True的fault名稱，以逗號分隔
        /// </summary>
        /// <param name="decodeList"></param>
        /// <returns></returns>
        public static string Parse_INV_FAULT(List<decodeContent> decodeList)
        {
            if (decodeList == null)
            {
                return "";
            }

            List<string> faultList = new List<string>();

            foreach (var item in decodeList)
            {

                if (string.Equals(item.value, "True", StringComparison.OrdinalIgnoreCase))
                {

                    if (!string.IsNullOrEmpty(item.name))
                    {
                        faultList.Add(item.name);
                    }
                }
            }

            if (faultList.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(",", faultList);
        }

       

        /// <summary>
        /// 解析INV_STATUS的BitField，回傳目前的 Mode 字串
        /// </summary>
        /// <param name="decodeList"></param>
        /// <returns> Mode_str </returns>
        public static string Parse_INV_STATUS(List<decodeContent> decodeList)
        {
            Dictionary<string, string> statusDict = new Dictionary<string, string>();
            foreach (var item in decodeList)
            {
                if (!string.IsNullOrEmpty(item.name))
                {
                    statusDict[item.name] = item.value;
                }
            }

            bool INV_MODE = strToBool(statusDict.GetValueOrDefault("INV_MODE", "False"));
            bool BYPASS_MODE = strToBool(statusDict.GetValueOrDefault("BYPASS_MODE", "False"));
            bool AC_OK = strToBool(statusDict.GetValueOrDefault("AC_OK", "False")); //UTI
            bool CHG_ON = strToBool(statusDict.GetValueOrDefault("CHG_ON", "False"));
            bool SAVING_MODE = strToBool(statusDict.GetValueOrDefault("SAVING_MODE", "False"));

            if (INV_MODE && SAVING_MODE) { return ConstDefinition.SAVING_MODE_str; }
            else if (INV_MODE) { return ConstDefinition.INVERTER_MODE_str; }
            else if (BYPASS_MODE) { return ConstDefinition.BYPASS_MODE_str; }
            else if (CHG_ON) { return ConstDefinition.CHARGING_MODE_str; }
            else { return ConstDefinition.STANDBY_MODE_str; }
        }

        public static (bool AC_StandBy, bool AC_Charger_Enable, ConstDefinition.SYS_Mode_Options modeEnum) Parse_INV_STATUS_GetEnum(List<decodeContent> decodeList)
        {
            bool tmp_AC_StandBy = false;
            bool tmp_AC_Charger_Enable = false;
            ConstDefinition.SYS_Mode_Options modeEnum = ConstDefinition.SYS_Mode_Options.DISCON;

            Dictionary<string, string> statusDict = new Dictionary<string, string>();
            foreach (var item in decodeList)
            {
                if (!string.IsNullOrEmpty(item.name))
                {
                    statusDict[item.name] = item.value;
                    // Console.WriteLine($"[BitFieldParser][Parse_INV_STATUS_GetEnum] name:{item.name}, value:{item.value}");
                }
            }
            bool INV_MODE = strToBool(statusDict.GetValueOrDefault("INV_MODE", "False"));
            bool BYPASS_MODE = strToBool(statusDict.GetValueOrDefault("BYPASS_MODE", "False"));
            bool AC_OK = strToBool(statusDict.GetValueOrDefault("AC_OK", "False")); //UTI
            bool CHG_ON = strToBool(statusDict.GetValueOrDefault("CHG_ON", "False"));
            bool SAVING_MODE = strToBool(statusDict.GetValueOrDefault("SAVING_MODE", "False"));

            if (INV_MODE && SAVING_MODE) { modeEnum = ConstDefinition.SYS_Mode_Options.SAVING; }
            else if (INV_MODE) { modeEnum = ConstDefinition.SYS_Mode_Options.INVERTER; }
            else if (BYPASS_MODE) { modeEnum = ConstDefinition.SYS_Mode_Options.BY_PASS; }
            else if (CHG_ON) { modeEnum = ConstDefinition.SYS_Mode_Options.CHARGER; }
            else { modeEnum = ConstDefinition.SYS_Mode_Options.STANDBY; }

            if (AC_OK is true) { tmp_AC_StandBy = true; }
            if (CHG_ON is true) { tmp_AC_Charger_Enable = true; }

            return (tmp_AC_StandBy, tmp_AC_Charger_Enable, modeEnum);
        }

        public static ConstDefinition.INV_Phase_Options Parse_INV_STATUS_Phase_Enum(List<decodeContent> decodeList)
        {
            var phaseObj = decodeList.Find(item => item.name == "PHASE");
            if (phaseObj is not null)
            {
                string phaseStr = phaseObj.value ?? "";
                switch (phaseStr)
                {
                    case ConstDefinition.PHASE_0_str: return ConstDefinition.INV_Phase_Options.PHASE_0;
                    case ConstDefinition.PHASE_180_str: return ConstDefinition.INV_Phase_Options.PHASE_180;
                    case ConstDefinition.PHASE_120_str: return ConstDefinition.INV_Phase_Options.PHASE_120;
                    case ConstDefinition.PHASE_240_str: return ConstDefinition.INV_Phase_Options.PHASE_240;
                    default:
                        Console.WriteLine($"[BitFieldParser][Parse_INV_STATUS_Phase] 非法的 PHASE 字串: {phaseStr}");
                        return ConstDefinition.INV_Phase_Options.PHASE_0;
                }
            }
            return ConstDefinition.INV_Phase_Options.PHASE_0;
        }

        public static bool strToBool(string booleanStr)
        {
            bool result;
            if (bool.TryParse(booleanStr, out result))
            {
                return result;
            }
            else
            {
                Console.WriteLine("不是合法的布林值字串");
            }
            return false;
        }

        #region INV_STATUS_Method
        public static string parse_INV_STATUS_By_rawValue(object rawValue)
        {
            if(rawValue == null) return ConstDefinition.STANDBY_MODE_str;

            uint rawValue_int = Custom.ConvertToUint32(rawValue);
            var decodeDict = DecodeBits("INV_STATUS", rawValue_int); //用 FixedCmdName "INV_STATUS" 去查解碼規則
            
            bool INV_MODE = Custom.strToBool(decodeDict.GetValueOrDefault("INV", "False"));
            bool BYPASS_MODE = Custom.strToBool(decodeDict.GetValueOrDefault("BYP", "False"));
            bool CHG_ON = Custom.strToBool(decodeDict.GetValueOrDefault("CHG_ON", "False"));
            bool SAVING_MODE = Custom.strToBool(decodeDict.GetValueOrDefault("SAVING", "False"));

            if (INV_MODE && SAVING_MODE) { return ConstDefinition.SAVING_MODE_str; }
            else if (INV_MODE) { return ConstDefinition.INVERTER_MODE_str; }
            else if (BYPASS_MODE) { return ConstDefinition.BYPASS_MODE_str; }
            else if (CHG_ON) { return ConstDefinition.CHARGING_MODE_str; }
            else { return ConstDefinition.STANDBY_MODE_str; }
        }

        public static (bool AC_StandBy, bool AC_Charger_Enable, ConstDefinition.SYS_Mode_Options modeEnum) parse_INV_STATUS_GetEnum_By_rawValue(object rawValue)
        {
            bool tmp_AC_StandBy = false;
            bool tmp_AC_Charger_Enable = false;
            ConstDefinition.SYS_Mode_Options modeEnum = ConstDefinition.SYS_Mode_Options.DISCON;
            
            Dictionary<string, string> statusDict = DecodeBits("INV_STATUS", Custom.ConvertToUint32(rawValue));
            
            bool INV_MODE = strToBool(statusDict.GetValueOrDefault("INV", "False"));
            bool BYPASS_MODE = strToBool(statusDict.GetValueOrDefault("BYP", "False"));
            bool AC_OK = strToBool(statusDict.GetValueOrDefault("UTI_OK", "False")); //UTI
            bool CHG_ON = strToBool(statusDict.GetValueOrDefault("CHG_ON", "False"));
            bool SAVING_MODE = strToBool(statusDict.GetValueOrDefault("SAVING", "False"));

            if (INV_MODE && SAVING_MODE) { modeEnum = ConstDefinition.SYS_Mode_Options.SAVING; }
            else if (INV_MODE) { modeEnum = ConstDefinition.SYS_Mode_Options.INVERTER; }
            else if (BYPASS_MODE) { modeEnum = ConstDefinition.SYS_Mode_Options.BY_PASS; }
            else if (CHG_ON) { modeEnum = ConstDefinition.SYS_Mode_Options.CHARGER; }
            else { modeEnum = ConstDefinition.SYS_Mode_Options.STANDBY; }

            if (AC_OK is true) { tmp_AC_StandBy = true; }
            if (CHG_ON is true) { tmp_AC_Charger_Enable = true; }

            return (tmp_AC_StandBy, tmp_AC_Charger_Enable, modeEnum);
        }

        public static ConstDefinition.INV_Phase_Options Parse_INV_STATUS_Phase_Enum_By_rawValue(object rawValue)
        {

            var decodeDict = DecodeBits("INV_STATUS", Custom.ConvertToUint32(rawValue));
            string phaseStr = decodeDict.GetValueOrDefault("INV_PHASE", "");
            switch (phaseStr)
            {
                case ConstDefinition.PHASE_0_str: return ConstDefinition.INV_Phase_Options.PHASE_0;
                case ConstDefinition.PHASE_180_str: return ConstDefinition.INV_Phase_Options.PHASE_180;
                case ConstDefinition.PHASE_120_str: return ConstDefinition.INV_Phase_Options.PHASE_120;
                case ConstDefinition.PHASE_240_str: return ConstDefinition.INV_Phase_Options.PHASE_240;
                default:
                    Console.WriteLine($"[BitFieldParser][Parse_INV_STATUS_Phase] 非法的 PHASE 字串: {phaseStr}");
                    return ConstDefinition.INV_Phase_Options.PHASE_0;
            }
        }
        #endregion INV_STATUS_Method
        

        #region INV_FAULT_Method
        public static bool Is_Any_INV_FAULT_By_rawValue(object rawValue)
        {
            if(rawValue == null)
            {
                return true;
            }

            uint value = Custom.ConvertToUint32(rawValue);
            return (value != 0) ? true : false;
        }
        #endregion INV_FAULT_Method


        public static Dictionary<string, string> DecodeBits(string cmdName, uint rawValue_int)
        {
            var result = new Dictionary<string, string>();

            if (!GlobalVar.HardCodedDecodeLogic.BitDefinitions.TryGetValue(cmdName, out var definition))
            {
                return result;
            }

            if (definition.BitIndexLookup.Count == 0)
            {
                definition.BuildLookup();
            }

            foreach (var field in definition.Fields)
            {
                var mask = (1 << field.Length) - 1;
                var fieldValue = (rawValue_int >> field.StartBit) & mask;

                if (field.Values == null)
                {//解碼 True/False 值
                    result[field.Name] = fieldValue == 1 ? "True" : "False";
                    continue;
                }

                if (field.Values.TryGetValue((int)fieldValue, out var meaning))
                {
                    result[field.Name] = meaning;
                    continue;
                }

                result[field.Name] = fieldValue.ToString();
            }

            return result;
        }
    }
}