using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
namespace demoVer.Models
{
    public class ProtocolFormat
    {
        [JsonPropertyName("port")]
        public string port { get; set; }

        [JsonPropertyName("addr")]
        public uint addr { get; set; } //不重要

        [JsonPropertyName("protocolName")]
        public string protocolName { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime timestamp { get; set; } //不重要

        [JsonPropertyName("values")]
        public Dictionary<string, Format> values { get; set; } = new(); 
    }

    public class Format
    {
        [JsonPropertyName("unit")]
        public string? unit { get; set; } = string.Empty;
        
        [JsonPropertyName("commandCode")]
        public string? commandCode {get; set; } = string.Empty;
    }
}