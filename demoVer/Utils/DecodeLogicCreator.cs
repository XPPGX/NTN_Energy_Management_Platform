using demoVer.Models;
namespace demoVer.Utils
{

    public static class HardCoded_DecodeLogicCreator
    {
        public static CommandBitFieldSpec CreateBasic_DecodeLogic()
        {
            var spec = new CommandBitFieldSpec();

            //這裡的Name都不可改動，因為會對應到BitFieldParser裡的解析函式
            //只可改動 StartBit
            spec.BitDefinitions["INV_STATUS"] = new CommandBitDefinition
            {
                Fields = new List<BitFieldDefinition>
                {
                    new BitFieldDefinition
                    {
                        Name = "INV", // TODO: set name (e.g., "Bat_OVP")
                        StartBit = 0, // TODO: set start bit
                        Length = 1, // TODO: adjust bit-length
                        Values = null // TODO: provide value map when needed
                    },
                    new BitFieldDefinition
                    {
                        Name = "BYP", // TODO
                        StartBit = 1, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "UTI_OK", // TODO
                        StartBit = 2, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "CHG_ON", // TODO
                        StartBit = 3, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "SOLAR_EN", // TODO
                        StartBit = 4, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "SAVING", // TODO
                        StartBit = 5, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "Bat_Low_ALM",
                        StartBit = 6,
                        Length = 1,
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "INV_PHASE", // TODO
                        StartBit = 8, // TODO
                        Length = 2, // TODO
                        Values = new Dictionary<int, string>()
                        {
                            {0, ConstDefinition.PHASE_0_str},
                            {1, ConstDefinition.PHASE_180_str},
                            {2, ConstDefinition.PHASE_120_str},
                            {3, ConstDefinition.PHASE_240_str}
                        }
                    }
                }
            };

            spec.BitDefinitions["INV_FAULT"] = new CommandBitDefinition
            {
                Fields = new List<BitFieldDefinition>
                {
                    new BitFieldDefinition
                    {
                        Name = "OLP_100", // TODO: set name (e.g., "Bat_OVP")
                        StartBit = 0, // TODO: set start bit
                        Length = 1, // TODO: adjust bit-length
                        Values = null // TODO: provide value map when needed
                    },
                    new BitFieldDefinition
                    {
                        Name = "OLP_110", // TODO
                        StartBit = 1, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "OLP_150", // TODO
                        StartBit = 2, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "OTP", // TODO
                        StartBit = 3, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "INV_UVP", // TODO
                        StartBit = 4, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "INV_OVP", // TODO
                        StartBit = 5, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "SCP", // TODO
                        StartBit = 6, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "EEP_Err", // TODO
                        StartBit = 7, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "SHDN", // TODO
                        StartBit = 8, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "FAN_FAIL", // TODO
                        StartBit = 9, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "Bat_UVP", // TODO
                        StartBit = 10, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "Bat_OVP", // TODO
                        StartBit = 11, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    new BitFieldDefinition
                    {
                        Name = "INV_Fault", // TODO
                        StartBit = 12, // TODO
                        Length = 1, // TODO
                        Values = null // TODO
                    },
                    // Add more fields as needed
                }
            };
        
            spec.BuildLookups();
            return spec;
        }
    }
}