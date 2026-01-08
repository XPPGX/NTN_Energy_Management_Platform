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
        [JsonPropertyName("CC_Timeout_Max_val")]
        public double CC_Timeout_Max_val {get; set;} = 0.0;
        [JsonPropertyName("CC_Timeout_Min_val")]
        public double CC_Timeout_Min_val {get; set;} = 0.0;

        [JsonPropertyName("CV_Timeout_val")]
        public double CV_Timeout_val { get; set; } = 0.0;
        [JsonPropertyName("CV_Timeout_Max_val")]
        public double CV_Timeout_Max_val {get; set;} = 0.0;
        [JsonPropertyName("CV_Timeout_Min_val")]
        public double CV_Timeout_Min_val {get; set;} = 0.0;
        
        [JsonPropertyName("FV_Timeout_val")]
        public double FV_Timeout_val { get; set; } = 0.0;
        [JsonPropertyName("FV_Timeout_Max_val")]
        public double FV_Timeout_Max_val {get; set;} = 0.0;
        [JsonPropertyName("FV_Timeout_Min_val")]
        public double FV_Timeout_Min_val {get; set;} = 0.0;

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
        public bool UpdateNowValue_From_SubSystem(Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat> infosForWriteCmd)
        {
            bool Changed = false;

            //Update BatterySetting
            var new_CURVE_CC_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B0");
            if (CURVE_CC_val != new_CURVE_CC_val)
            {
                CURVE_CC_val = new_CURVE_CC_val;
                Changed = true;
            }

            var new_CURVE_CV_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B1");
            if(CURVE_CV_val != new_CURVE_CV_val) 
            {
                CURVE_CV_val = new_CURVE_CV_val;
                Changed = true;
            }

            var new_CURVE_FV_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B2");
            if (CURVE_FV_val != new_CURVE_FV_val)
            {
                CURVE_FV_val = new_CURVE_FV_val;
                Changed = true;
            }
            var new_CURVE_TC_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B3");
            if (CURVE_TC_val != new_CURVE_TC_val)
            {
                CURVE_TC_val = new_CURVE_TC_val;
                Changed = true;
            }

            bool new_selectStage, new_CCT_Enable, new_CVT_Enable, new_FVT_Enable;
            (new_selectStage, new_CCT_Enable, new_CVT_Enable, new_FVT_Enable) = GetNowValueHelper.parse_CURVE_CONFIG(infosForWriteCmd, "00B4");
            if (selectStage != new_selectStage || CCT_Enable != new_CCT_Enable || CVT_Enable != new_CVT_Enable || FVT_Enable != new_FVT_Enable)
            {
                selectStage = new_selectStage;
                CCT_Enable = new_CCT_Enable;
                CVT_Enable = new_CVT_Enable;
                FVT_Enable = new_FVT_Enable;
                
                Changed = true;
            }

            double new_CC_Timeout_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B5");
            if (CC_Timeout_val != new_CC_Timeout_val)
            {
                CC_Timeout_val = new_CC_Timeout_val;
                Changed = true;
            }

            double new_CV_Timeout_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B6");
            if (CV_Timeout_val != new_CV_Timeout_val)
            {
                CV_Timeout_val = new_CV_Timeout_val;
                Changed = true;
            }
            double new_FV_Timeout_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B7");
            if (FV_Timeout_val != new_FV_Timeout_val)
            {
                FV_Timeout_val = new_FV_Timeout_val;
                Changed = true;
            }

            //Update InverterSetting
            string new_Output_ACV_Set = GetNowValueHelper.parse_Output_ACV_Set(infosForWriteCmd, "0102");
            if (Output_ACV_Set != new_Output_ACV_Set)
            {
                Output_ACV_Set = new_Output_ACV_Set;
                Changed = true;
            }

            string new_Output_ACF_Set = GetNowValueHelper.parse_Output_ACF_Set(infosForWriteCmd, "0103");
            if (Output_ACF_Set != new_Output_ACF_Set)
            {
                Output_ACF_Set = new_Output_ACF_Set;
                Changed = true;
            }

            OUTPUT_PRIORITY new_OutputPrio;
            CHARGING_PRIORITY new_ChargingPrio;
            (new_OutputPrio, new_ChargingPrio) = GetNowValueHelper.parse_INV_CONFIG(infosForWriteCmd, "0101");
            if (OutputPrio != new_OutputPrio || ChargingPrio != new_ChargingPrio)
            {
                OutputPrio = new_OutputPrio;
                ChargingPrio = new_ChargingPrio;
                Changed = true;
            }

            bool new_CHG_Enable, new_GRID_Enable;
            (new_CHG_Enable, new_GRID_Enable) = GetNowValueHelper.parse_INV_OPERATION(infosForWriteCmd, "0100");
            if (CHG_Enable != new_CHG_Enable || GRID_Enable != new_GRID_Enable)
            {
                CHG_Enable = new_CHG_Enable;
                GRID_Enable = new_GRID_Enable;
                Changed = true;
            }

            double new_BAT_ALM_VOLT_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00B9");
            if (BAT_ALM_VOLT_val != new_BAT_ALM_VOLT_val)
            {
                BAT_ALM_VOLT_val = new_BAT_ALM_VOLT_val;
                Changed = true;
            }

            double new_BAT_SHDN_VOLT_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00BA");
            if (BAT_SHDN_VOLT_val != new_BAT_SHDN_VOLT_val)
            {
                BAT_SHDN_VOLT_val = new_BAT_SHDN_VOLT_val;
                Changed = true;
            }

            double new_BAT_RCHG_VOLT_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00BB");
            if (BAT_RCHG_VOLT_val != new_BAT_RCHG_VOLT_val)
            {
                BAT_RCHG_VOLT_val = new_BAT_RCHG_VOLT_val;
                Changed = true;
            }

            double new_BAT_OV_ALM_VOLT_val = GetNowValueHelper.parse_Numeric_Val(infosForWriteCmd, "00BC");
            if (BAT_OV_ALM_VOLT_val != new_BAT_OV_ALM_VOLT_val)
            {
                BAT_OV_ALM_VOLT_val = new_BAT_OV_ALM_VOLT_val;
                Changed = true;
            }

            return Changed;
        }
        public bool UpdateBoundaries_From_SubSystem(ConcurrentDictionary<string, SingleCmdRange> RangesDict)
        {
            bool Changed = false;

            //Update BatterySetting
            if (RangesDict.TryGetValue("00B0", out var CC_Range))
            {
                var max = CC_Range.max ?? 0.0;
                var min = CC_Range.min ?? 0.0;
                if(CURVE_CC_Max_val != max || CURVE_CC_Min_val != min) { Changed = true; }
                CURVE_CC_Max_val = max;
                CURVE_CC_Min_val = min;
            }
            if (RangesDict.TryGetValue("00B1", out var CV_Range))
            {
                var max = CV_Range.max ?? 0.0;
                var min = CV_Range.min ?? 0.0;
                if(CURVE_CV_Max_val != max || CURVE_CV_Min_val != min){ Changed = true; }
                CURVE_CV_Max_val = max;
                CURVE_CV_Min_val = min;
            }
            if (RangesDict.TryGetValue("00B2", out var FV_Range))
            {
                var max = FV_Range.max ?? 0.0;
                var min = FV_Range.min ?? 0.0;
                if(CURVE_FV_Max_val != max || CURVE_FV_Min_val != min){ Changed = true; }
                CURVE_FV_Max_val = max;
                CURVE_FV_Min_val = min;
            }
            if (RangesDict.TryGetValue("00B3", out var TC_Range))
            {
                var max = TC_Range.max ?? 0.0;
                var min = TC_Range.min ?? 0.0;
                if(CURVE_TC_Max_val != max || CURVE_TC_Min_val != min){ Changed = true; }
                CURVE_TC_Max_val = max;
                CURVE_TC_Min_val = min;
            }

            //CC_Timeout
            if (RangesDict.TryGetValue("00B5", out var CC_Timeout_Range))
            {
                var max = CC_Timeout_Range.max ?? 0.0;
                var min = CC_Timeout_Range.min ?? 0.0;
                if(CC_Timeout_Max_val != max || CC_Timeout_Min_val != min){ Changed = true; }
                CC_Timeout_Max_val = max;
                CC_Timeout_Min_val = min;
            }

            //CV_Timeout
            if (RangesDict.TryGetValue("00B6", out var CV_Timeout_Range))
            {
                var max = CV_Timeout_Range.max ?? 0.0;
                var min = CV_Timeout_Range.min ?? 0.0;
                if(CV_Timeout_Max_val != max || CV_Timeout_Min_val != min){ Changed = true; }
                CV_Timeout_Max_val = max;
                CV_Timeout_Min_val = min;
            }

            //FV_Timeout
            if (RangesDict.TryGetValue("00B7", out var FV_Timeout_Range))
            {
                var max = FV_Timeout_Range.max ?? 0.0;
                var min = FV_Timeout_Range.min ?? 0.0;
                if(FV_Timeout_Max_val != max || FV_Timeout_Min_val != min){ Changed = true; }
                FV_Timeout_Max_val = max;
                FV_Timeout_Min_val = min;
            }


            //Update InverterSetting
            if (RangesDict.TryGetValue("00B9", out var BAT_ALM_VOLT_Range))
            {
                var max = BAT_ALM_VOLT_Range.max ?? 0.0;
                var min = BAT_ALM_VOLT_Range.min ?? 0.0;
                if(BAT_ALM_VOLT_Max_val != max || BAT_ALM_VOLT_Min_val != min){ Changed = true; }
                BAT_ALM_VOLT_Max_val = max;
                BAT_ALM_VOLT_Min_val = min;
            }
            if (RangesDict.TryGetValue("00BA", out var BAT_SHDN_VOLT_Range))
            {
                var max = BAT_SHDN_VOLT_Range.max ?? 0.0;
                var min = BAT_SHDN_VOLT_Range.min ?? 0.0;
                if(BAT_SHDN_VOLT_Max_val != max || BAT_SHDN_VOLT_Min_val != min){ Changed = true; }
                BAT_SHDN_VOLT_Max_val = max;
                BAT_SHDN_VOLT_Min_val = min;
            }
            if (RangesDict.TryGetValue("00BB", out var BAT_RCHG_VOLT_Range))
            {
                var max = BAT_RCHG_VOLT_Range.max ?? 0.0;
                var min = BAT_RCHG_VOLT_Range.min ?? 0.0;
                if(BAT_RCHG_VOLT_Max_val != max || BAT_RCHG_VOLT_Min_val != min){ Changed = true; }
                BAT_RCHG_VOLT_Max_val = max;
                BAT_RCHG_VOLT_Min_val = min;
            }
            if (RangesDict.TryGetValue("00BC", out var BAT_OV_ALM_VOLT_Range))
            {
                var max = BAT_OV_ALM_VOLT_Range.max ?? 0.0;
                var min = BAT_OV_ALM_VOLT_Range.min ?? 0.0;
                if (BAT_OV_ALM_VOLT_Max_val != max || BAT_OV_ALM_VOLT_Min_val != min) { Changed = true; }
                BAT_OV_ALM_VOLT_Max_val = max;
                BAT_OV_ALM_VOLT_Min_val = min;
            }
            
            return Changed;
        }
        
    }
}
