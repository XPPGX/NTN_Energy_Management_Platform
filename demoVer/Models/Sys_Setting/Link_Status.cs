using System.Text.Json;
using System.Text.Json.Serialization;

namespace demoVer.Models
{
    public class HexToUlongConverter : JsonConverter<ulong>
    {
        public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (str != null && str.StartsWith("0x"))
            {
                return Convert.ToUInt64(str, 16);
            }
            return 0;
        }

        public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("0x" + value.ToString("X16"));
        }
    }

    public class LinkStatus_JsonFormat
    {
        [JsonPropertyName("updatedUtc")]
        public DateTime UpdatedUtc {get; set;}

        [JsonPropertyName("products")]
        public Dictionary<string, portLinkDetail> Products {get; set;} = new();
    }

    public class portLinkDetail
    {
        [JsonPropertyName("CAN1")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong CAN1_LINK {get; set;}

        [JsonPropertyName("CAN2")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong CAN2_LINK {get; set;}

        [JsonPropertyName("MOD1")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong MOD1_LINK {get; set;}

        [JsonPropertyName("MOD2")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong MOD2_LINK {get; set;}

        [JsonPropertyName("PM1")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong PM1_LINK {get; set;}
    }
}