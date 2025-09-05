using demoVer.Services;
using demoVer.Utils;
using System.Text.Json.Serialization;
namespace demoVer.Models
{
    public class ADDR_VALUES : IEquatable<ADDR_VALUES>
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

        public bool Equals(ADDR_VALUES? other)
        {
            if(other == null) return false;
            if(addr != other.addr) return false;
            return value.SequenceEqual(other.value);
        }

        public override bool Equals(object? obj) => Equals(obj as ADDR_VALUES);

        public override int GetHashCode()
        {
            int hash = addr.GetHashCode();
            if(value.Count > 0)
            {
                hash ^= value[0].GetHashCode();
            }
            return hash;
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
    public class settingCommandRawData : IEquatable<settingCommandRawData>
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

        public bool Equals(settingCommandRawData? other)
        {
            if(other is null) return false;
            if(IsPerAddr != other.IsPerAddr) return false;

            if(!IsPerAddr)
            {
                // 比對 TargetValue
                if (TargetValue == null && other.TargetValue == null) return true;
                if (TargetValue == null || other.TargetValue == null) return false;
                if (!TargetValue.SequenceEqual(other.TargetValue)) return false;
            }
            else
            {
                // 比對 AddrValues
                if (AddrValues == null && other.AddrValues == null) return true;
                if (AddrValues == null || other.AddrValues == null) return false;
                if (AddrValues.Count != other.AddrValues.Count) return false;
                for (int i = 0; i < AddrValues.Count; i++)
                {
                    if (!AddrValues[i].Equals(other.AddrValues[i])) return false;
                }
            }
            
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as settingCommandRawData);

        public override int GetHashCode()
        {
            int hash = IsPerAddr.GetHashCode();
            if(!IsPerAddr && TargetValue != null)
            {
                hash ^= TargetValue.Count;
            }
            if(IsPerAddr && AddrValues != null)
            {
                hash ^= AddrValues.Count;
            }
            return hash;
        }

    }

}