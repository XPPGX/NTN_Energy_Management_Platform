using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace demoVer.Models
{
    public class Battery_InitData
    {
        [JsonPropertyName("cc")]        
        public int CC { get; set; }
        [JsonPropertyName("cc_max")]
        public int CC_Max { get; set; }
        [JsonPropertyName("cc_min")]
        public int CC_Min { get; set; }

        [JsonPropertyName("tc")]
        public int TC { get; set; } = 600;
        [JsonPropertyName("tc_max")]
        public int TC_Max { get; set; } = 1800;
        [JsonPropertyName("tc_min")]
        public int TC_Min { get; set; } = 120;

        [JsonPropertyName("cv")]
        public int CV { get; set; } = 576;
        [JsonPropertyName("cv_max")]
        public int CV_Max { get; set; } = 600;
        [JsonPropertyName("cv_min")]
        public int CV_Min { get; set; } = 400;

        [JsonPropertyName("fv")]
        public int FV { get; set; } = 552;
        [JsonPropertyName("fv_min")]
        public int FV_Min { get; set; } = 400;

        [JsonPropertyName("cc_timeout_minute")]
        public int CC_TimeOut_Minute {get; set;} = 600;
        
        [JsonPropertyName("cv_timeout_minute")]
        public int CV_TimeOut_Minute {get; set;} = 600;

        [JsonPropertyName("fv_timeout_minute")]
        public int FV_TimeOut_Minute {get; set;} = 600;

        [JsonPropertyName("cct_enable")]
        public bool CCT_Enable {get; set;} = false;

        [JsonPropertyName("cvt_enable")]
        public bool CVT_Enable {get; set;} = false;

        [JsonPropertyName("fvt_enable")]
        public bool FVT_Enable {get; set;} = false;

        [JsonPropertyName("curve_stage")]
        public bool CurveStage {get; set;} = false;
    
        public static Battery_InitData LoadFromJsonFile()
        {
            string AppDataDirectory = Path.GetFullPath("App_Data");
            string BAT_SettingFilePath = Path.Combine(AppDataDirectory, "BAT_Setting_LastTime.json");

            if(!File.Exists(BAT_SettingFilePath))
            {
                Console.WriteLine($"{BAT_SettingFilePath} 檔案不存在");
                return new Battery_InitData();
            }

            string json = File.ReadAllText(BAT_SettingFilePath);
            var options = new JsonSerializerOptions{PropertyNameCaseInsensitive = true};
            
            return JsonSerializer.Deserialize<Battery_InitData>(json, options) ?? new Battery_InitData();
        }
    }
}