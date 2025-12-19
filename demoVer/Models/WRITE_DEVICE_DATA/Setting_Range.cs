using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using demoVer.Utils;
namespace demoVer.Models
{
    public class Setting_Range
    {
        [JsonPropertyName("port")]
        public string port { get; set; } = string.Empty;
        [JsonPropertyName("protocol")]
        public string protocol { get; set; } = string.Empty;
        [JsonPropertyName("epoch")]
        public string epoch {get; set;} = string.Empty;
        [JsonPropertyName("checkOK")]
        public bool checkOK { get; set; } = false;
        [JsonPropertyName("modelError")]
        public bool modelError { get; set; } = false;
        [JsonPropertyName("rangeOK")]
        public bool rangeOK { get; set; } = false;
        [JsonPropertyName("modelName")]
        public string? modelName { get; set; } = string.Empty;
        [JsonPropertyName("addr")]
        public List<uint> addr { get; set; } = new();
        [JsonPropertyName("ranges")]
        public ConcurrentDictionary<string, SingleCmdRange> ranges { get; set; } = new();
        [JsonPropertyName("rangeIssues")]
        public RangeIssues? RangeIssues {get; set;} = new RangeIssues();

        //初始化
        // public Setting_Range()
        // {
        //     //初始化 有range的 HexCmd
        //     ranges.TryAdd("00B0", new SingleCmdRange()); //CURVE_CC
        //     ranges.TryAdd("00B1", new SingleCmdRange()); //CURVE_CV
        //     ranges.TryAdd("00B2", new SingleCmdRange()); //CURVE_FV
        //     ranges.TryAdd("00B3", new SingleCmdRange()); //CURVE_TC
        //     ranges.TryAdd("00B9", new SingleCmdRange()); //BAT_ALM_VOLT
        //     ranges.TryAdd("00BA", new SingleCmdRange()); //BAT_SHDN_VOLT
        //     ranges.TryAdd("00BB", new SingleCmdRange()); //BAT_RCHG_VOLT
        //     ranges.TryAdd("00BC", new SingleCmdRange()); //BAT_OV_ALM_VOLT
        // }
    }
    public class SingleCmdRange
    {
        [JsonPropertyName("commandName")] 
        public string? cmdName { get; set; } = string.Empty;
        [JsonPropertyName("commandType")]
        public string commandType {get; set;} = string.Empty;
        [JsonPropertyName("rangeSource")]
        public string rangeSource {get; set;} = string.Empty;
        [JsonPropertyName("min")]
        public double? min { get; set; } = 0.0;
        [JsonPropertyName("max")]
        public double? max { get; set; } = 0.0;
        [JsonPropertyName("default")]
        public double? defaultVal {get; set;} = 0.0;   
    }

    public class RangeIssues
    {
        [JsonPropertyName("reason")]
        public string? reason {get; set;} = string.Empty;
        [JsonPropertyName("missingCommands")]
        public List<string>? missingCommands {get; set;} = new List<string>();
    }
}