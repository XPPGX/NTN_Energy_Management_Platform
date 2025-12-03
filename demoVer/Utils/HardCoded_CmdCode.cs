namespace demoVer.Utils
{
    public static class HardCodedCmdCodeCreator
    {

        public static Dictionary<string, Dictionary<string, string>> BuildReverse(Dictionary<string, Dictionary<string, string>> source)
        {
            var reverse = new Dictionary<string, Dictionary<string, string>>();

            foreach (var (port, commandMap) in source)
            {
                var reverseMap = new Dictionary<string, string>();

                foreach (var (commandName, commandCode) in commandMap)
                {
                    if (string.IsNullOrWhiteSpace(commandCode))
                    {
                        continue;
                    }

                    reverseMap[commandCode] = commandName;
                }

                reverse[port] = reverseMap;
            }

            return reverse;
        }

        public static (Dictionary<string, Dictionary<string, string>>, Dictionary<string, Dictionary<string, string>>) CreateBasic_HardCoded_CmdCode()
        {
            var HardCoded_CmdCode = new Dictionary<string, Dictionary<string, string>>()
            {// 只用於此App模組，由於User會自行定義CmdName，故需要一個固定的
            // FixedCmdName => FixedCmdCode => UserDefinedCmdName
            // 這裡主要負責前段(FixedCmdName => FixedCmdCode)
                {
                    "CAN", new Dictionary<string, string>()
                    {
                        {"MFR_MODEL", "0x0082"},
                        {"INV_STATUS", "0x011D"},
                        {"INV_FAULT", "0x011E"},
                        {"MFR_REVISION_B0B5", "0x0084"},
                        {"READ_FAN_SPEED_1", "0x0070"},
                        {"READ_FAN_SPEED_2", "0x0071"},
                        {"READ_AC_VOUT", "0x0108"},
                        {"READ_OP_VA", "0x0114"},
                        {"READ_CHG_CURR", "0x011B"},
                        {"READ_VBAT", "0x011A"},
                        {"READ_TEMPERATURE_1", "0x0062"},
                        {"READ_VIN", "0x0050"},
                        {"READ_FREQ", "0x0056"},
                        {"READ_AC_FOUT", "0x0105"},
                        {"READ_OP_LD_PCNT", "0x010B"},
                    }
                },
                {
                    "MOD", new Dictionary<string, string>()
                    {
                        {"MFR_MODEL", "0x0086"},
                        {"INV_STATUS", "0x011D"},
                        {"INV_FAULT", "0x011E"},
                        {"MFR_REVISION_B0B5", "0x008C"},
                        {"READ_FAN_SPEED_1", "0x0070"},
                        {"READ_FAN_SPEED_2", "0x0071"},
                        {"READ_AC_VOUT", "0x0108"},
                        {"READ_OP_VA", "0x0114"},
                        {"READ_CHG_CURR", "0x011B"},
                        {"READ_VBAT", "0x011A"},
                        {"READ_TEMPERATURE_1", "0x0062"},
                        {"READ_VIN", "0x0050"},
                        {"READ_FREQ", "0x0056"},
                        {"READ_AC_FOUT", "0x0105"},
                        {"READ_OP_LD_PCNT", "0x010B"},
                    }
                }
            };

            var HardCoded_CmdCode_Reverse = BuildReverse(HardCoded_CmdCode);
            return(HardCoded_CmdCode, HardCoded_CmdCode_Reverse);
        }
    }
}