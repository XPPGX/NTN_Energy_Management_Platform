using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json.Serialization;

namespace demoVer.Models
{
    public class GET_RealSingleRawSettingCMD_JsonFormat
    {
        [JsonPropertyName("commandName")]
        public string CommandName { get; set; }

        [JsonPropertyName("cmdCode")]
        public string cmdCode { get; set; }

        [JsonPropertyName("isPerAddr")]
        public bool IsPerAddr { get; set; }

        [JsonPropertyName("dataFormat")]
        public string DataFormat { get; set; }

        [JsonPropertyName("byteLength")]
        public int ByteLength { get; set; }

        [JsonPropertyName("scaling")]
        public double? Scaling { get; set; }

        [JsonPropertyName("baseUnit")]
        public string? BaseUnit { get; set; }

        [JsonPropertyName("signed")]
        public bool? Signed { get; set; }

        [JsonPropertyName("shift")]
        public int? Shift { get; set; }

        [JsonPropertyName("bitcontrol")]
        public List<BitControl>? BitControl { get; set; } = new();

        [JsonPropertyName("target")]
        public WriteVal? Target { get; set; } = new();

        [JsonPropertyName("targetDirty")]
        public bool TargetDirty { get; set; }

        [JsonPropertyName("addrValues")]
        public List<AddrValue>? AddrValues { get; set; } = new();
    }

    public class BitControl
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("bit")]
        public int Bit { get; set; }

        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("valueMap")]
        public Dictionary<string, string> ValueMap { get; set; } = new();
    }
    
    public class WriteVal
    {
        [JsonPropertyName("number")]
        public double? Number { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("bits")]
        public Dictionary<string, int>? Bits { get; set; } = new();
    }

    public class AddrValue
    {
        [JsonPropertyName("addr")]
        public int Addr { get; set; }

        [JsonPropertyName("value")]
        public WriteVal Value { get; set; } = new();

        [JsonPropertyName("dirty")]
        public bool Dirty { get; set; }

        public AddrValue deepClone()
        {
            return new AddrValue
            {
                Addr = this.Addr,
                Value = new WriteVal
                {
                    Number = this.Value.Number,
                    Text = this.Value.Text,
                    Bits = new Dictionary<string, int>(this.Value.Bits ?? new())
                },
                Dirty = this.Dirty
            };
        }
    }

    public class Post_RealSingleRawSettingCMD_JsonFormat
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } = "";
        
        [JsonPropertyName("Protocol")]
        public string Protocol { get; set; } = "";
        
        [JsonPropertyName("CommandName")]
        public string CommandName { get; set; } = "";
        
        [JsonPropertyName("Target")]
        public WriteVal? Target { get; set; }
        
        [JsonPropertyName("addrValues")]
        public List<AddrValue>? AddrValues { get; set; }
    }
}