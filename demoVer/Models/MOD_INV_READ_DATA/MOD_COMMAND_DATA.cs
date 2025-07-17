using demoVer.Services;
using demoVer.Utils;
namespace demoVer.Models
{
    public class ModSingleRawCommandFormat
    {
        public string? commandName {get; set;}
        public List<byte> data {get; set;} = new();
        public float scaling {get; set;}
        public string? baseUnit {get; set;}
        public string? dataFormat {get; set;}
        public string? split {get; set;}
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


        public event Action? OnChanged;

        /// 給 UI 顯示用的屬性，根據 Data, Scaling, DataFormat 等轉換後的字串
        public object? DisplayValue => ParseByteData();

        private object? ParseByteData()
        {
            try
            {
                if (DataFormat == "Numeric" && Data.Count >= 2)
                {
                    float value = (float)((Data[0] << 8) | Data[1]); // Big Endian
                    return ScalingComputer.MultOperation(value, Scaling);
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
        }
    }
}