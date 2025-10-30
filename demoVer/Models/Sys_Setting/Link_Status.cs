using System.Text.Json;
using System.Text.Json.Serialization;

namespace demoVer.Models
{
    public class LinkStatus_JsonFormat
    {
        [JsonPropertyName("updatedUtc")]
        public DateTime UpdatedUtc { get; set; }

        [JsonPropertyName("products")]
        public Dictionary<string, portLinkDetail> Products { get; set; } = new();

        [JsonPropertyName("partitions")]
        public List<SinglePartition> Partitions { get; set; } = new();
    }

    public class portLinkDetail
    {
        [JsonPropertyName("CAN1")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong CAN1_LINK { get; set; }

        [JsonPropertyName("CAN2")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong CAN2_LINK { get; set; }

        [JsonPropertyName("MOD1")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong MOD1_LINK { get; set; }

        [JsonPropertyName("MOD2")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong MOD2_LINK { get; set; }

        [JsonPropertyName("PM1")]
        [JsonConverter(typeof(HexToUlongConverter))]
        public ulong PM1_LINK { get; set; }
    }

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

    public class SinglePartition
    {
        [JsonPropertyName("port")]
        public string Port { get; set; } = string.Empty;

        [JsonPropertyName("protocol")]
        public string Protocol { get; set; } = string.Empty;

        [JsonPropertyName("addr")]
        public List<uint> Addr { get; set; } = new();

        [JsonPropertyName("checkOK")]
        public bool CheckOK { get; set; } = false;

        [JsonPropertyName("modelError")]
        public bool ModelError { get; set; } = false;

        [JsonPropertyName("rangeOK")]
        public bool rangeOK { get; set; } = false;

        [JsonPropertyName("modelName")]
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// 暫定的名稱，之後要改，高機率只需要改JsonPropertyName("epoch")
        /// 如果API回應中沒有這個欄位，預設值會是0。
        /// SubSystem的epoch預設是string.Empty，這樣第一次更新時只要API server端有任何Hash回應就會更新。
        /// </summary>
        [JsonPropertyName("epoch")]
        public string epoch { get; set; } = string.Empty;
    }
}