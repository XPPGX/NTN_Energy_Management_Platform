using System.Text.Json.Serialization;
namespace demoVer.Models
{
    public class SettingData
    {
        [JsonPropertyName("commandName")]
        public string commandName {get; set;} = "";

        [JsonPropertyName("targetValue")]
        public List<byte> targetValue {get; set;} = new();
    }
}