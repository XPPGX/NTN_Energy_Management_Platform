using demoVer.Services;
using demoVer.Utils;
namespace demoVer.Models
{
    public class SingleRawCommandFormat
    {
        public string? commandName {get; set;}
        public List<byte> data {get; set;} = new();
        public float scaling {get; set;}
        public string? baseUnit {get; set;}
        public string? dataFormat {get; set;}
        public string? split {get; set;}
    }

    public class CommandData
    {
        private readonly CommonData _commonData;
        public CommandData(CommonData commonData)
        {
            _commonData = commonData;
        }

        public List<byte> Data { get; set; } = new();
        public float Scaling { get; set; }
        public string? BaseUnit { get; set; }
        public string? DataFormat { get; set; }
        public string? Split { get; set; }

        /// <summary>
        /// 給 UI 顯示用的屬性，根據 Data/Scaling/DataFormat 等轉換後的字串
        /// </summary>
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

        public void UpdateFrom(CommandData other)
        {
            Data = new List<byte>(other.Data);
            Scaling = other.Scaling;
            BaseUnit = other.BaseUnit;
            DataFormat = other.DataFormat;
            Split = other.Split;
        }
    }
}