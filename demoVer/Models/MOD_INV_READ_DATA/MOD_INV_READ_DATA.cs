using demoVer.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public class CommandData : ObservableModule
    {
        private readonly CommonData _commonData;
        public CommandData(CommonData commonData)
        {
            _commonData = commonData;
        }

        private List<byte> _data = new();
        public List<byte> Data
        {
            get => _data;
            set
            {
                if (!_data.SequenceEqual(value))
                {
                    _data = value;
                    NotifyChanged();
                }
            }
        }

        private float _scaling;
        public float Scaling
        {
            get => _scaling;
            set
            {
                if (_scaling != value)
                {
                    _scaling = value;
                    NotifyChanged();
                }
            }
        }

        private string? _baseUnit;
        public string? BaseUnit
        {
            get => _baseUnit;
            set
            {
                if (_baseUnit != value)
                {
                    _baseUnit = value;
                    NotifyChanged();
                }
            }
        }

        private string? _dataFormat;
        public string? DataFormat
        {
            get => _dataFormat;
            set
            {
                if (_dataFormat != value)
                {
                    _dataFormat = value;
                    NotifyChanged();
                }
            }
        }

        private string? _split;
        public string? Split
        {
            get => _split;
            set
            {
                if (_split != value)
                {
                    _split = value;
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 給 UI 顯示用的屬性，根據 Data/Scaling/DataFormat 等轉換後的字串
        /// </summary>
        public object? DisplayValue => ParseByteData();

        private object? ParseByteData()
        {
            // 🔧 這裡是你要實作的轉換邏輯，下面是範例（你可以替換掉）
            try
            {
                if(DataFormat == "Numeric" && Data.Count > 0)
                {
                    Console.WriteLine("Numeric Data ~");
                    if(Split == null)
                    {
                        
                        float value = (float)((Data[0] << 8) | Data[1]); //Big Endian合成
                        return ScalingComputer.MultOperation(value, Scaling);
                    }
                }
                else if(DataFormat == "ASCII")
                {
                    Console.WriteLine("ASCII Data ~");
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
            Data = new List<byte>(other.Data);  // 深複製
            Scaling = other.Scaling;
            BaseUnit = other.BaseUnit;
            DataFormat = other.DataFormat;
            Split = other.Split;
            NotifyChanged();
        }

    }
}