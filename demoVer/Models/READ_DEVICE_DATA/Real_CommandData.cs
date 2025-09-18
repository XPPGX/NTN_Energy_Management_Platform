using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace demoVer.Models
{
    public class SingleCommandData
    {
        [JsonPropertyName("type")]
        public string type {get; set;}

        [JsonPropertyName("value")]
        public object value {get; set;}

        [JsonPropertyName("unit")]
        public string unit {get; set;}
    }

    public class Real_SingleDeviceData_JsonFormat
    {
        [JsonPropertyName("port")]
        public string port {get; set;}

        [JsonPropertyName("addr")]
        public uint addr {get; set;}

        [JsonPropertyName("protocolName")]
        public string protocolName {get; set;}

        [JsonPropertyName("timestamp")]
        public DateTimeOffset timestamp {get; set;}

        [JsonPropertyName("values")]
        public Dictionary<string, SingleCommandData> values {get; set;} = new();
    }

}