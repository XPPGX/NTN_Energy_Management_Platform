using demoVer.Services;
using demoVer.Utils;
using System.Text.Json.Serialization;
namespace demoVer.Models
{
    public class ADDR_VALUES
    {
        public uint addr {get; set;}
        public List<byte> value {get; set;}

        public ADDR_VALUES DeepClone()
        {
            return new ADDR_VALUES
            {
                addr = this.addr,
                value = new List<byte>(this.value)
            };
        }
    }

    // The element format of the received Json from "Write Memory"
    public class SingleRawSettingCommand_JsonFormat
    {
        [JsonPropertyName("commandName")]
        public string commandName {get; set;} = string.Empty;

        [JsonPropertyName("isPerAddr")]
        public bool isPerAddr {get; set;}

        //If there is targetValue in the received Json data,
        //then there is no addrValues. Vice versa.
        
        [JsonPropertyName("targetValue")]
        public List<byte>? targetValue {get; set;}

        [JsonPropertyName("addrValues")]
        public List<ADDR_VALUES>? addrValues {get; set;}
    }

    //The structure that stored in the process memory, for "Write Memory in the framework"
    public class settingCommandRawData
    {
        public bool IsPerAddr {get; set;}
        public List<byte>? TargetValue {get; set;}
        public List<ADDR_VALUES>? AddrValues {get; set;}

        public void UpdateWith(settingCommandRawData newData)
        {
            IsPerAddr = newData.IsPerAddr;
            TargetValue = (IsPerAddr) ? null : newData.TargetValue?.ToList();
            AddrValues = (IsPerAddr) ? newData.AddrValues?.Select(addrVal => addrVal.DeepClone()).ToList() : null;
        }
    
        public settingCommandRawData DeepClone()
        {
            return new settingCommandRawData
            {
                IsPerAddr = this.IsPerAddr,
                TargetValue = (this.IsPerAddr) ? null : this.TargetValue?.ToList(),
                AddrValues = (this.IsPerAddr) ? this.AddrValues?.Select(addrVal => addrVal.DeepClone()).ToList() : null
            };
        }

    }

}