using System.Text.Json.Serialization;

namespace demoVer.Models
{

    public class BAT_nowData
    {
        [JsonPropertyName("port")]
        public string port { get; set; } = string.Empty;

        [JsonPropertyName("protocol")]
        public string protocol { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public SOC data {get; set; } = new SOC();
    }
    public class SOC
    {
        [JsonPropertyName("BAT_level")]
        public float BAT_level { get; set; } = 0;

        [JsonPropertyName("BAT_Remain_Time")]
        public float BAT_Remain_Time { get; set; } = 0;

        [JsonPropertyName("BAT_Capacity")]
        public float BAT_Capacity { get; set; } = 0;
    }
}