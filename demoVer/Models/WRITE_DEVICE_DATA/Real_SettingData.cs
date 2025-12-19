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


        /// <summary>
        /// 快速查詢用: BitPosition => BitControl,
        /// Key: BitPosition_int, 
        /// Value: BitControl_class
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, BitControl> Lookup_BitControl = new(); 

        /// <summary>
        /// 透過 BitPosition 取得對應的 BitControl 名稱
        /// (已考慮User可能會用不同的 BitName 的情況)
        /// </summary>
        /// <param name="bitPostion"></param>
        /// <returns></returns>
        public string? GetBitKey(int bitPostion)
        {
            var BitControl = this.BitControl;
            if(BitControl is null)
            {
                Console.WriteLine($"GetBitKey: BitControl is null");
                return null;
            }

            if(Lookup_BitControl.TryGetValue(bitPostion, out var targetBitControl))
            {
                return targetBitControl.Name;
            }
            else
            {
                Console.WriteLine($"GetBitKey: bitPostion {bitPostion} not found in Lookup_BitControl");
                return null;
            }
        }

        public void Build_Lookup_BitControl()
        {
            if(BitControl is null)
            {
                return;                
            }

            Lookup_BitControl.Clear();
            foreach (var bitControl in BitControl)
            {
                Lookup_BitControl[bitControl.Bit] = bitControl;
            }
        }
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