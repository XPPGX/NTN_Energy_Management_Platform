using demoVer.Models;
using demoVer.Utils;
namespace demoVer.Services
{
    public class CodeTranslator
    {
        private readonly SubSystemManager _subSystemManager;
        private readonly string _category = "";
        public CodeTranslator(SubSystemManager subSystemManager)
        {
            _category = GetType().FullName!;
            _subSystemManager = subSystemManager;
        }

        /// <summary>
        /// 將 FixedCmdName 轉換為 UserDefined CmdName
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="FixedCmdName"></param>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public string? Translate_FixedCmdName_To_UserDefinedName(string FixedCmdName, uint addr=0)
        {
            string port = "";
            string protocol = "";
            if(!_subSystemManager.Mapping_Addr_To_SubSys.TryGetValue(addr.ToString(), out var portProtocol_Tuple)) return null; // 找不到對應的 Port 和 Protocol//取 subsys 參考
            (port, protocol) = portProtocol_Tuple;
            
            Console.WriteLine("[CodeTranslator][Translate_FixedCmdName_To_UserDefinedName] port: " + port + ", protocol: " + protocol);

            var subsys = _subSystemManager.GetOneSubSystem_Ref(port, protocol);
            if(subsys is null) return null; // 找不到對應的 SubSystem
            AppLogger.Log_To_File_log(_category, "[CodeTranslator][Translate_FixedCmdName_To_UserDefinedName] got subsys", AppLogLevel.Trace);

            //取 ClearPort
            string clearPort = Custom.getPort_ExceptFor_Num(port); //得到 "CAN" or "MOD"
            if(string.IsNullOrEmpty(clearPort)) return null; // 無效的 Port
            Console.WriteLine("[CodeTranslator][Translate_FixedCmdName_To_UserDefinedName] clearPort: " + clearPort);

            //用 ClearPort 搭配 FixedCmdName 查 FixedCmdCode
            if(!GlobalVar.HardCodes.TryGetValue(clearPort, out var cmdDict)) return null; // 取cmdDict ; 若取不到對應的 CmdCode 字典則 return null
            if(!cmdDict.TryGetValue(FixedCmdName, out var FixedCmdCode)) return null; // 取cmdDict ; 若取不到對應的 CmdCode 則 return null
            Console.WriteLine("[CodeTranslator][Translate_FixedCmdName_To_UserDefinedName] FixedCmdCode: " + FixedCmdCode);

            //用FixedCmdCode 查 UserDefinedName
            var userDefinedCmdName = subsys.Get_UserDefined_CmdName_By_CmdCode(FixedCmdCode);
            Console.WriteLine("[CodeTranslator][Translate_FixedCmdName_To_UserDefinedName] userDefinedCmdName: " + userDefinedCmdName);

            return userDefinedCmdName;
        }

        // FixedCmdName 轉 FixedCmdCode
        public string? TranslateNameToCode(string port, string cmdName)
        {
            if (GlobalVar.HardCodes.TryGetValue(port, out var cmdDict))
            {
                if (cmdDict.TryGetValue(cmdName, out var cmdCode))
                {
                    return cmdCode;
                }
            }
            return null; // 找不到對應的 CmdCode
        }

        // FixedCmdCode 轉 FixedCmdName
        public string? TranslateCodeToName(string port, string cmdCode)
        {
            if (GlobalVar.HardCodesReverse.TryGetValue(port, out var cmdDict))
            {
                if (cmdDict.TryGetValue(cmdCode, out var cmdName))
                {
                    return cmdName;
                }
            }
            return null; // 找不到對應的 CmdName
        }
    }
}