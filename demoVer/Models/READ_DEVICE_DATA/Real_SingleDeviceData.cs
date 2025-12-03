using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using demoVer.Utils;
using Newtonsoft.Json;
namespace demoVer.Models
{
    public class Real_SingleDeviceData_JsonFormat
    {
        
        [JsonPropertyName("port")]
        public string port { get; set; }

        [JsonPropertyName("addr")]
        public uint addr { get; set; }

        [JsonPropertyName("protocolName")]
        public string protocolName { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTimeOffset timestamp { get; set; }

        [JsonPropertyName("values")]
        public Dictionary<string, SingleCommandData> values { get; set; } = new();

        private string _category = "";
        public Real_SingleDeviceData_JsonFormat()
        {
            _category = GetType().FullName!;
        }

        public void UpdateSelf_From(Real_SingleDeviceData_JsonFormat source)
        {
            this.port = source.port;
            this.addr = source.addr;
            this.protocolName = source.protocolName;
            this.timestamp = source.timestamp;

            HashSet<string> nowCmds = new HashSet<string>(this.values.Keys);

            foreach (var (cmdName, sourceCmdData) in source.values)
            {
                if (this.values.ContainsKey(cmdName))
                {// 更新現有的cmd資料
                    this.values[cmdName].type = sourceCmdData.type;
                    this.values[cmdName].value = sourceCmdData.value;
                    this.values[cmdName].unit = sourceCmdData.unit;

                    this.values[cmdName].decode = (sourceCmdData.decode != null)
                        ? new List<decodeContent>(sourceCmdData.decode.ConvertAll(x =>
                            new decodeContent
                            {
                                name = x.name,
                                value = x.value
                            }))
                        : null;
                    
                    this.values[cmdName].commandCode = sourceCmdData.commandCode;
                    this.values[cmdName].rule = (sourceCmdData.rule != null)
                        ? new List<ruleContent>(sourceCmdData.rule.ConvertAll(x =>
                            new ruleContent
                            {
                                name = x.name,
                                bit = x.bit,
                                length = x.length,
                                valueMap = x.valueMap != null ? new Dictionary<string, string>(x.valueMap) : null
                            }))
                        : null;
                }
                else
                {// 新增新的cmd資料
                    this.values.Add(cmdName, sourceCmdData);
                }

                // 從nowCmds移除已更新的cmd
                nowCmds.Remove(cmdName);
            }

            // 移除本次取得資料時已不在新資料中的cmd與其對應cmdData
            foreach (var cmdName in nowCmds)
            {
                this.values.Remove(cmdName);
            }

        }

        public void ReleaseMemory()
        {
            this.values.Clear();
        }

        public Real_SingleDeviceData_JsonFormat DeepClone()
        {
            var clone = new Real_SingleDeviceData_JsonFormat();
            clone.UpdateSelf_From(this);
            return clone;
        }

        public string GetCmdType(string cmd)
        {
            if (!this.values.TryGetValue(cmd, out var cmdData))
            {
                return string.Empty;
            }
            else
            {
                return cmdData.type;
            }
        }

        public object? parseCmdData(string cmd)
        {
            if (!this.values.TryGetValue(cmd, out var cmdData)) return null;

            var rawValue = cmdData.value;

            switch (cmdData.type)
            {
                case "Numeric":
                    //處理JsonElement (System.Text.Json反序列化後的型態)
                    if (rawValue is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.Number && je.TryGetDouble(out var num)) return num;
                        else { return null; }
                    }
                    if (rawValue is double d) return d;
                    if (rawValue is float f) return (double)f;
                    if (rawValue is int i) return (double)i;
                    if (rawValue is long l) return (double)l;
                    return null;
                case "ASCII":
                    if (rawValue is JsonElement asciiJe)
                    {
                        return asciiJe.GetString();
                    }
                    else
                    {
                        return rawValue?.ToString();
                    }

                case "BitField":
                    // return (List<decodeContent>?)cmdData.DeepClone_decode();
                    return rawValue;
                default:
                    return null;
            }
        }
        public Dictionary<string, Dictionary<string, string>>? GetCmdRule(string cmd)
        {
            if(!this.values.TryGetValue(cmd, out var cmdData)) return null;

            Dictionary<string, Dictionary<string, string>> ruleDict = new();
            return ruleDict; //[Pending]
        }
        public Dictionary<string, string>? Get_Cmd_Unit_Dict()
        {
            try
            {
                var cmdUnitDict = new Dictionary<string, string>();

                foreach (var (cmdName, cmdData) in this.values)
                {
                    cmdUnitDict[cmdName] = cmdData.unit ?? string.Empty;
                    AppLogger.Log_To_File_log(_category, $"[Real_SingleDeviceData_JsonFormat][Get_Cmd_Unit_Dict] cmd : {cmdName}, unit_str = {cmdData.unit} ");
                }
                return cmdUnitDict;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[Real_SingleDeviceData_JsonFormat][Get_Cmd_Unit_Dict] Error : {e.Message}", AppLogLevel.Error);
                return null;
            }
        }
    }

    public class SingleCommandData
    {
        [JsonPropertyName("type")]
        public string type { get; set; }

        private object _value;
        [JsonPropertyName("value")]
        public object value // 結構內value，經過json反序列化後存進來會變成JsonElement，要用時必須轉型
        {
            get => _value;
            set
            {
                if (!AreValuesEqual(_value, value, type)) //這裡的 "value" 是新的value，非結構內value。 
                {
                    _value = value;

                    OnChanged?.Invoke();
                }
            }
        }

        [JsonPropertyName("unit")]
        public string? unit { get; set; }

        [JsonPropertyName("commandCode")]
        public string? commandCode {get; set;} = string.Empty;

        [JsonPropertyName("rule")]
        public List<ruleContent>? rule {get; set;} = new();

        [JsonPropertyName("decode")]
        public List<decodeContent>? decode { get; set; }

        public event Action? OnChanged;

        public void clearAllActions() => OnChanged = null;

        public bool AreValuesEqual(object? oldValue, object? newValue, string type)
        {
            //null檢查
            if (oldValue == null && newValue == null) return true;
            if (oldValue == null || newValue == null) return false;

            try
            {
                switch (type)
                {
                    case "Numeric":
                        double oldNum_double = Custom.ConvertToDouble(oldValue);
                        double newNum_double = Custom.ConvertToDouble(newValue);
                        return oldNum_double == newNum_double;
                    case "ASCII":
                        string oldStr = oldValue.ToString() ?? string.Empty;
                        string newStr = newValue.ToString() ?? string.Empty;
                        return string.Equals(oldStr, newStr, StringComparison.Ordinal);
                    case "BitField":
                        // BitField類型的值，value轉成uint32後比較數值，如果不一樣則視為不相等，再由訂閱者呼叫BitFieldParser內function去解析
                        uint oldVal_uint32 = Custom.ConvertToUint32(oldValue);
                        uint newVal_uint32 = Custom.ConvertToUint32(newValue);
                        return oldVal_uint32 == newVal_uint32;
                    default:
                        return false;
                }
            }
            catch
            {
                return false; // 轉換失敗視為不相等
            }
        }

        public List<decodeContent>? DeepClone_decode()
        {
            if (this.decode == null) return null;

            var cloneList = new List<decodeContent>(this.decode.ConvertAll(x =>
                new decodeContent
                {
                    name = x.name,
                    value = x.value
                }));

            return cloneList;
        }

        public SingleCommandData DeepClone()
        {
            var clone = new SingleCommandData
            {
                type = this.type,
                unit = this.unit,
                value = this.value
            };

            if (this.decode != null)
            {
                clone.decode = this.DeepClone_decode();
            }

            return clone;
        }
    }

    public class decodeContent
    { 
        [JsonPropertyName("name")]
        public string? name { get; set; } = null;
        [JsonPropertyName("value")]
        public string? value { get; set; } = null;
    }

    public class ruleContent
    {
        [JsonPropertyName("name")]
        public string? name {get; set;} = string.Empty;
        [JsonPropertyName("bit")]
        public uint bit {get; set;} = 0;
        [JsonPropertyName("length")]
        public uint length {get; set;} = 0;
        [JsonPropertyName("valueMap")]
        public Dictionary<string, string>? valueMap {get; set;} = new();
    }
}