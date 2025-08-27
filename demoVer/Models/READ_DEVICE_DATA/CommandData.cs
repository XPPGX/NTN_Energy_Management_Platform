using demoVer.Services;
using demoVer.Utils;
using System.Text.Json.Serialization;
using demoVer.Interfaces;

namespace demoVer.Models
{
    public class BitMeaning
    {
        public uint? bit {get; set;}
        public uint? length {get; set;}
        public bool? loggable {get; set;}
        public Dictionary<string, string>? valueMap {get; set;}

        public BitMeaning deepClone()
        {
            return new BitMeaning
            {
                bit = this.bit,
                length = this.length,
                loggable = this.loggable,
                valueMap = new Dictionary<string, string>(this.valueMap),
            };
        }
    }

    // The element format of the received Json from "Read Memory"
    public class SingleRawCommand_JsonFormat
    {
        [JsonPropertyName("commandName")]
        public string? commandName {get; set;}

        [JsonPropertyName("data")]
        public List<byte> data {get; set;} = new();

        [JsonPropertyName("scaling")]
        public float scaling {get; set;}

        [JsonPropertyName("baseUnit")]
        public string? baseUnit {get; set;}

        [JsonPropertyName("dataFormat")]
        public string? dataFormat {get; set;}

        [JsonPropertyName("split")]
        public string? split {get; set;}

        [JsonPropertyName("signed")]
        public bool? signed {get; set;}

        [JsonPropertyName("mask")]
        public string? mask {get; set;}

        [JsonPropertyName("groupIndex")]
        public uint? groupIndex {get; set;}

        [JsonPropertyName("bitFields")]
        public List<BitMeaning> bitFields {get; set;} = new();

        [JsonPropertyName("isSNnumber")]
        public bool? isSNnumber {get; set;}
    }

    //The structure that stored in the process memory, for "Read Memory in the framework"
    public class CommandRawData
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

        public void UpdateFrom(CommandRawData other)
        {   
            Data        = other.Data?.ToList(); //複製新的List<byte>
            Scaling     = other.Scaling;
            BaseUnit    = other.BaseUnit;
            DataFormat  = other.DataFormat;
            Split       = other.Split;
            Signed      = other.Signed;
            Mask        = other.Mask;
            GroupIndex  = other.GroupIndex;
            BitFields   = other.BitFields?.Select(bit => bit.deepClone()).ToList();
            isSNnumber  = other.isSNnumber;
        }

        public void ClearAllActions() => OnChanged = null;
    }

    public class GroupChangedEventArgs : EventArgs
    {
        public required string snapShot_str {get; set;}
    }

    public class Group_CommandRawData
    {
        //[inject]
        private readonly IGroupsDataDecoder _decoder;

        //存每個groupIndex對應的資料
        //如果沒有groupIndex則視為groupIndex = 0        
        public Dictionary<uint, CommandRawData> Groups {get; set;} = new();
        
        
        // public readonly Dictionary<uint, Action> _handlers = new();
        // //Group層級事件
        // public event Action<uint>? OnChildChanged;
        // //把RawData放入Group，順便掛上OnChange的handler
        // public void AddOrUpdateGroup(uint index, CommandRawData Raw)
        // {
        //     if(_handlers.TryGetValue(index, out var oldHandler))
        //     {
        //         if(Groups.TryGetValue(index, out var oldRaw))
        //         {
        //             oldRaw.OnChanged -= oldHandler;
        //         }
                
        //         _handlers.Remove(index);
        //     }

        //     Action handler = () => ChildDataChanged();

        //     //掛handler
        //     Raw.OnChanged += handler;
            
        //     //存group的handler，解除handler時會用到(防止memory leak)
        //     _handlers[index] = handler;

        //     //把RawData放入Groups中
        //     Groups[index] = Raw;
        // }

        // //解掛Groups內的每一個Group的Handler
        // public void ClearAllHandlers()
        // {
        //     foreach (var pair in _handlers)
        //     {
        //         if(Groups.TryGetValue(pair.Key, out var Raw))
        //         {
        //             Raw.OnChanged -= pair.Value;
        //         }
        //     }
        //     _handlers.Clear();
        // }

        // private void ChildDataChanged()
        // {
        //     OnChildChanged?.Invoke();
        // }

        public IEnumerable<CommandRawData> GetOrderedGroups()
        {
            return Groups.OrderBy(g => g.Key).Select(g => g.Value);
        }
        
    }
}