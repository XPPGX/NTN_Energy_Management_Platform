using demoVer.Services;
using demoVer.Utils;
namespace demoVer.Models
{
    // public class BitMeaning
    // {
    //     public uint? bit {get; set;}
    //     public uint? length {get; set;}
    //     public bool? loggable {get; set;}
    //     public Dictionary<string, string>? valueMap {get; set;}

    //     public BitMeaning deepClone()
    //     {
    //         return new BitMeaning
    //         {
    //             bit = this.bit,
    //             length = this.length,
    //             loggable = this.loggable,
    //             valueMap = new Dictionary<string, string>(this.valueMap),
    //         };
    //     }
    // }

    public class ModSingleRawCommandFormat
    {
        public string? commandName {get; set;}
        public List<byte> data {get; set;} = new();
        public float scaling {get; set;}
        public string? baseUnit {get; set;}
        public string? dataFormat {get; set;}
        public string? split {get; set;}
        public bool? signed {get; set;}
        public string? mask {get; set;}
        public uint? groupIndex {get; set;}
        public List<BitMeaning> bitFields {get; set;} = new();
        public bool? isSNnumber {get; set;}
    }

    public class ModCommandData
    {
        private List<byte> _Data = new();
        public List<byte> Data
        {
            get => _Data;
            set
            {
                if(!_Data.SequenceEqual(value))
                {
                    _Data = value.ToList();
                    OnChanged?.Invoke();
                }
            }
        }
        
        public float Scaling { get; set; }
        public string? BaseUnit { get; set; }
        public string? DataFormat { get; set; }
        public string? Split { get; set; }
        public bool? Signed {get; set;}
        public string? Mask {get; set;}
        public uint? GroupIndex {get; set;}
        public List<BitMeaning> BitFields {get; set;}
        public bool? isSNnumber {get; set;}

        public event Action? OnChanged;

        /// 給 UI 顯示用的屬性，根據 Data, Scaling, DataFormat 等轉換後的字串
        public object? DisplayValue => ParseByteData();

        private object? ParseByteData()
        {
            try
            {
                // if(Data == null || Data.Count == 0)
                // {
                //     return null;
                // }

                // if(Data.BitFields != null && BitFields.Any())
                // {
                //     int rawValue = BytesToInt(Data, Signed); //還可以加shift但目前沒用到

                //     var activeFields = new List<string>();
                //     foreach(var bf in BitFields)
                //     {
                //         if(bf.loggable != true) continue;
                //         int mask = ((1 << bf.length) - 1) bf.Bit;
                //         int extracted = (rawValue & mask) >> bf.Bit;
                //         if(bf.valueMap != null && bf.valueMap.TryGetValue(extracted, out var mapped))
                //         {
                //             if(!string.IsNullOrEmpty(mapped))
                //             {
                //                 activeFields.Add(mapped);
                //             }
                //         }
                //     }
                //     return activeFields.Count > 0 ? string.join(",", activeFields);
                    
                // }

                // else if((Mask != null) && Mask.Any())
                // {
                //     return "[ParseByteData Error] : Mask not implemented";
                // }

                // else if(!string.IsNullOrEmpty(Split))
                // {
                //     try
                //     {
                //         int size = int.Parse(Split[0].ToString());
                //         string separator = block.Split.Substring(1);

                //         var results = new List<string>();

                //         for(int i = 0 ; i < data.Count ; i += size)
                //         {
                //             var slice = data.Skip(i).Take(size).ToList();
                //             if(slice.Count < size) break;

                //             var decoded = ApplyFormat(slice, block);

                //             if(decoded is double d)
                //             {
                //                 results.Add(d.ToString("G"));
                //             }
                //             else
                //             {
                //                 results.Add(decoded?.ToString() ?? "");
                //             }
                //         }
                //     }
                // }
                if (DataFormat == "Numeric")
                {   

                    if(Scaling < 1)
                    {
                        if(Data.Count == 2)
                        {   //不解碼，留到js去解碼，因為還有組合命令 : 例如 => (READ_OP_VA_HI << 16 | READ_OP_VA_LO)
                            uint value = (uint)((Data[0] << 8) | Data[1]);
                            return value;
                        }

                        if(Data.Count == 6)
                        {
                            ulong value = (uint)((Data[0] << 5 | Data[1] << 4 | Data[2] << 3 | Data[3] << 2 | Data[4] << 1 | Data[5]));
                            return value;
                        }
                    }

                    if(Scaling == 1)
                    {
                        if(Data.Count == 2)
                        {
                            uint value = (uint)((Data[0] << 8) | Data[1]); //Big Endian
                            return value;
                        }
                    }
                    
                }
                else if (DataFormat == "ASCII")
                {
                    return System.Text.Encoding.ASCII.GetString(Data.ToArray());
                }
            }
            catch
            {
                return "解析錯誤";
            }

            return "無資料";
        }

        public void UpdateFrom(ModCommandData other)
        {   
            Data = other.Data;
            Scaling = other.Scaling;
            BaseUnit = other.BaseUnit;
            DataFormat = other.DataFormat;
            Split = other.Split;
            Signed = other.Signed;
            Mask = other.Mask;
            GroupIndex = other.GroupIndex;
            BitFields = other.BitFields;
            isSNnumber = other.isSNnumber;
        }

        public void toDTO_Json()
        {
            
        }
        private int BytesToInt(List<byte> data, bool signed, int shiftBytes = 0)
        {
            if(data.Count > 4)
            {
                throw new NotSupportedException($"BytesToInt 不支援超過 4 bytes (len={data.Count})");
            }

            var effectiveData = ((shiftBytes > 0) && (shiftBytes < data.Count))?
                data.Skip(shiftBytes).ToList() : data;
            
            int value = 0;
            for(int i = 0 ; (i < effectiveData.Count) && (i < 4) ; i ++)
            {
                value |= effectiveData[i] << (8 * (effectiveData.Count - 1 - i));
            }

            if(signed)
            {
                int bits = effectiveData.Count * 8;
                int signBit = 1 << (bits - 1);
                if((value & signBit) != 0)
                {
                    value -= (1 << bits);
                }
            }
            return value;
        }
    }
}