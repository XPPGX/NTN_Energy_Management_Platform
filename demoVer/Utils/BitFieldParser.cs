using demoVer.Models;
namespace demoVer.Utils
{
    public static class BitFieldParser
    {

        /// <summary>
        /// 根據cmdName選擇適合的解析函式來解析decodeList
        /// </summary>
        /// <param name="cmdName"></param>
        /// <param name="decodeList"></param>
        /// <returns></returns>
        public static string FitParserFunction(string cmdName, List<decodeContent> decodeList)
        {
            switch (cmdName)
            {
                case "INV_FAULT":
                    return Parse_INV_FAULT(decodeList);
                case "INV_STATUS":
                    return Parse_INV_STATUS(decodeList);
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

            if (INV_MODE && SAVING_MODE) { return "Saving"; }
            if (INV_MODE) { return "Inverter"; }
            if (BYPASS_MODE) { return "Bypass"; }
            if (CHG_ON) { return "Charger"; }
            if (AC_OK) { return "Standby"; }

            return "Error";
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

    }
}