using demoVer.Utils;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace demoVer.Models
{
    public class PageUsableVars
    {
        //這邊的Numeric類型數值，在設定頁面中使用時，需在使用小數精度調整(捨棄多餘小數位)

        //For BatterySetting
        [JsonPropertyName("CURVE_CC_val")]
        public double CURVE_CC_val { get; set; } = 0.0;
        [JsonPropertyName("CURVE_CC_Max_val")]
        public double CURVE_CC_Max_val { get; set; } = 0.0;
        [JsonPropertyName("CURVE_CC_Min_val")]
        public double CURVE_CC_Min_val { get; set; } = 0.0; //
        [JsonPropertyName("CURVE_CV_val")]
        public double CURVE_CV_val { get; set; } = 0.0;
        [JsonPropertyName("CURVE_CV_Max_val")]
        public double CURVE_CV_Max_val { get; set; } = 0.0;
        [JsonPropertyName("CURVE_CV_Min_val")]
        public double CURVE_CV_Min_val { get; set; } = 0.0; //
        [JsonPropertyName("CURVE_FV_val")]
        public double CURVE_FV_val { get; set; } = 0.0;
        [JsonPropertyName("CURVE_FV_Max_val")]
        public double CURVE_FV_Max_val { get; set; } = 0.0;
        [JsonPropertyName("CURVE_FV_Min_val")]
        public double CURVE_FV_Min_val { get; set; } = 0.0; //
        [JsonPropertyName("CURVE_TC_val")]
        public double CURVE_TC_val { get; set; } = 0.0;
        [JsonPropertyName("CURVE_TC_Max_val")]
        public double CURVE_TC_Max_val { get; set; } = 0.0;
        [JsonPropertyName("CURVE_TC_Min_val")]
        public double CURVE_TC_Min_val { get; set; } = 0.0; //
        [JsonPropertyName("selectStage")]
        public bool selectStage { get; set; } = false;
        [JsonPropertyName("CCT_Enable")]
        public bool CCT_Enable { get; set; } = false;
        [JsonPropertyName("CVT_Enable")]
        public bool CVT_Enable { get; set; } = false;
        [JsonPropertyName("FVT_Enable")]
        public bool FVT_Enable { get; set; } = false;
        [JsonPropertyName("CC_Timeout_val")]
        public double CC_Timeout_val { get; set; } = 0.0;
        [JsonPropertyName("CV_Timeout_val")]
        public double CV_Timeout_val { get; set; } = 0.0;
        [JsonPropertyName("FV_Timeout_val")]
        public double FV_Timeout_val { get; set; } = 0.0;

        //For InverterSetting
        [JsonPropertyName("Output_ACV_Set")]
        public string Output_ACV_Set { get; set; } = "100/200";
        [JsonPropertyName("Output_ACF_Set")]
        public string Output_ACF_Set { get; set; } = "50Hz";
        [JsonPropertyName("OutputPrio")]
        public OUTPUT_PRIORITY OutputPrio { get; set; } = OUTPUT_PRIORITY.Utility;
        [JsonPropertyName("ChargingPrio")]
        public CHARGING_PRIORITY ChargingPrio { get; set; } = CHARGING_PRIORITY.Utility;
        [JsonPropertyName("CHG_Enable")]
        public bool CHG_Enable { get; set; } = false;
        [JsonPropertyName("GRID_Enable")]
        public bool GRID_Enable { get; set; } = false;
        [JsonPropertyName("BAT_ALM_VOLT_val")]
        public double BAT_ALM_VOLT_val { get; set; } = 0.0;
        [JsonPropertyName("BAT_ALM_VOLT_Max_val")]
        public double BAT_ALM_VOLT_Max_val { get; set; } = 0.0;
        [JsonPropertyName("BAT_ALM_VOLT_Min_val")]
        public double BAT_ALM_VOLT_Min_val { get; set; } = 0.0; //
        [JsonPropertyName("BAT_SHDN_VOLT_val")]
        public double BAT_SHDN_VOLT_val { get; set; } = 0.0;
        [JsonPropertyName("BAT_SHDN_VOLT_Max_val")]
        public double BAT_SHDN_VOLT_Max_val { get; set; } = 0.0;
        [JsonPropertyName("BAT_SHDN_VOLT_Min_val")]
        public double BAT_SHDN_VOLT_Min_val { get; set; } = 0.0; //
        [JsonPropertyName("BAT_RCHG_VOLT_val")]
        public double BAT_RCHG_VOLT_val { get; set; } = 0.0;
        [JsonPropertyName("BAT_RCHG_VOLT_Max_val")]
        public double BAT_RCHG_VOLT_Max_val { get; set; } = 0.0;
        [JsonPropertyName("BAT_RCHG_VOLT_Min_val")]
        public double BAT_RCHG_VOLT_Min_val { get; set; } = 0.0; //
        [JsonPropertyName("BAT_OV_ALM_VOLT_val")]
        public double BAT_OV_ALM_VOLT_val { get; set; } = 0.0;
        [JsonPropertyName("BAT_OV_ALM_VOLT_Max_val")]
        public double BAT_OV_ALM_VOLT_Max_val { get; set; } = 0.0;
        [JsonPropertyName("BAT_OV_ALM_VOLT_Min_val")]
        public double BAT_OV_ALM_VOLT_Min_val { get; set; } = 0.0;
        public void UpdateNowValue_From_SubSystem(Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat> infosForWriteCmd)
        {
            //Update BatterySetting
            CURVE_CC_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B0");
            CURVE_CV_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B1");
            CURVE_FV_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B2");
            CURVE_TC_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B3");
            (selectStage, CCT_Enable, CVT_Enable, FVT_Enable) = GetNowValueHelper.parse_CURVE_CONFIG(infosForWriteCmd, "00B4");
            CC_Timeout_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B5");
            CV_Timeout_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B6");
            FV_Timeout_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B7");

            //Update InverterSetting
            Output_ACV_Set = GetNowValueHelper.parse_Output_ACV_Set(infosForWriteCmd, "0102");
            Output_ACF_Set = GetNowValueHelper.parse_Output_ACF_Set(infosForWriteCmd, "0103");
            (OutputPrio, ChargingPrio) = GetNowValueHelper.parse_INV_CONFIG(infosForWriteCmd, "0101");
            (CHG_Enable, GRID_Enable) = GetNowValueHelper.parse_INV_OPERATION(infosForWriteCmd, "0100");
            BAT_ALM_VOLT_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B9");
            BAT_SHDN_VOLT_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00BA");
            BAT_RCHG_VOLT_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00BB");
            BAT_OV_ALM_VOLT_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00BC");
        }
        public void UpdateBoundaries_From_SubSystem(ConcurrentDictionary<string, SingleCmdRange> RangesDict)
        {
            //Update BatterySetting
            if (RangesDict.TryGetValue("00B0", out var CC_Range))
            {
                CURVE_CC_Max_val = CC_Range.max;
                CURVE_CC_Min_val = CC_Range.min;
            }
            if (RangesDict.TryGetValue("00B1", out var CV_Range))
            {
                CURVE_CV_Max_val = CV_Range.max;
                CURVE_CV_Min_val = CV_Range.min;
            }
            if (RangesDict.TryGetValue("00B2", out var FV_Range))
            {
                CURVE_FV_Max_val = FV_Range.max;
                CURVE_FV_Min_val = FV_Range.min;
            }
            if (RangesDict.TryGetValue("00B3", out var TC_Range))
            {
                CURVE_TC_Max_val = TC_Range.max;
                CURVE_TC_Min_val = TC_Range.min;
            }

            //Update InverterSetting
            if (RangesDict.TryGetValue("00B9", out var BAT_ALM_VOLT_Range))
            {
                BAT_ALM_VOLT_Max_val = BAT_ALM_VOLT_Range.max;
                BAT_ALM_VOLT_Min_val = BAT_ALM_VOLT_Range.min;
            }
            if (RangesDict.TryGetValue("00BA", out var BAT_SHDN_VOLT_Range))
            {
                BAT_SHDN_VOLT_Max_val = BAT_SHDN_VOLT_Range.max;
                BAT_SHDN_VOLT_Min_val = BAT_SHDN_VOLT_Range.min;
            }
            if (RangesDict.TryGetValue("00BB", out var BAT_RCHG_VOLT_Range))
            {
                BAT_RCHG_VOLT_Max_val = BAT_RCHG_VOLT_Range.max;
                BAT_RCHG_VOLT_Min_val = BAT_RCHG_VOLT_Range.min;
            }
            if (RangesDict.TryGetValue("00BC", out var BAT_OV_ALM_VOLT_Range))
            {
                BAT_OV_ALM_VOLT_Max_val = BAT_OV_ALM_VOLT_Range.max;
                BAT_OV_ALM_VOLT_Min_val = BAT_OV_ALM_VOLT_Range.min;
            }
        }
        
    }
}
