namespace demoVer.Models
{
    public enum PAGE
    {
        HOME            = 1,
        BATTERY_SETTING = 2,
        INV_SETTING     = 3,
        LINK_STATUS     = 4,
        LOG        = 5,
        USE_STRATEGY    = 6,
    }

    public enum CardType
    {
        Info = 1,
        Chart = 2,
        Status = 3
    }

    public class CardInfo
    {
        public string ID {get; set;}                                        //識別用的ID(string)
        public CardType card_Type {get; set;}                                  
        public string widthClass {get; set;}                                //定義的寬度(w25 | w50)
        public byte showSequence {get; set;}                                //存被渲染的順序 start from 0
        public string cardName {get; set;}                                  //卡片名稱
        public List<INFO_DataItem> Info_DataItems {get; set;}               //存多個INFO_DataItems
        public List<LINK_STATUS_Pair> Link_STATUS_Pairs{get; set;}          //存4個(iconPath, label) pair
        public CHART_SETTING Chart_Setting {get; set;}                      //存chart需要的資訊
    }

    public class InfoTuple
    {
        public string IconPath { get; set; }
        public string AltText { get; set; }
        public List<(string Label, string Value)> DataPairs { get; set; }
    }

    public enum INFO_SETTING_STAGE
    {
        CLOSE_ALL                       = 0,
        ADD_ROW_STAGE                   = 1,
        SELECT_ICON_STAGE               = 2,
        SETTING_VALUE_AND_CMD_STAGE     = 3,
        MACHINE_SELECTION_STAGE         = 4,
        CMD_SELECTION_STAGE             = 5,
        UNIT_SELECTION_STAGE            = 6,
        ADD_VALUE_AND_CMD_ROW_STAGE     = 7,
    }
    
    public enum STATUS_SETTING_STAGE
    {
        CLOSE_ALL                       = 0,
        OVERVIEW                        = 1,
        SELECT_ICON_STAGE               = 2,
    }

    public enum CHART_SETTING_STAGE
    {
        CLOSE_ALL                       = 0,
        OVERVIEW                        = 1,
        DISPLAY_SETTING                 = 2,
        SELECT_CMD_STAGE                = 3,
        UNIT_SELECTION_STAGE            = 4
    }

    public enum SettingStatus
    {
        YET         = 1,
        PROGRESS    = 2,
        DONE        = 3,
    }

    public class INFO_DataFormat
    {
        public string MachineName {get; set;}
        public string Cmd {get; set;}
        public string ValueName {get; set;}
        public string Unit  {get; set;} 

        public INFO_DataFormat DeepClone()
        {
            return new INFO_DataFormat
            {
                MachineName = this.MachineName,
                Cmd = this.Cmd,
                ValueName = this.ValueName,
                Unit = this.Unit
            };
        }
    }

    public class INFO_DataItem
    {
        public string iconPath { get; set; }
        public SettingStatus status {get; set; } = SettingStatus.YET;
        
        public List<INFO_DataFormat> INFO_DataFormats {get; set;} = new();

        public INFO_DataItem DeepClone()
        {
            return new INFO_DataItem
            {
                iconPath = this.iconPath,
                status = this.status,
                INFO_DataFormats = this.INFO_DataFormats.Select(data_format => data_format.DeepClone()).ToList()
            };
        }
    }

    public class LINK_STATUS_Pair
    {
        public string iconPath {get; set;}
        public string Label{get; set;}

        public LINK_STATUS_Pair DeepClone()
        {
            return new LINK_STATUS_Pair
            {
                iconPath = this.iconPath,
                Label = this.Label
            };
        }
    }

    public class CHART_SETTING
    {
        public string canvasID {get; set;}
        public string ChartTitle {get; set;}
        //X軸相關
        public string XTitle {get; set;} = "時間";
        public timeGap timeGap_selection {get; set;}
        public string[] Labels {get; set;} //X軸資料，目前只支援顯示時間
        //Y軸相關
        public string Y_left_Title {get; set;}
        public string Y_right_Title {get; set;}
        public int YMin {get; set;}
        public int YMax {get; set;}
        public List<CHART_SINGLE_DATA_LINE> chart_single_data_lines {get; set;} = new List<CHART_SINGLE_DATA_LINE>();

        public CHART_SETTING DeepClone()
        {
            return new CHART_SETTING
            {
                canvasID = this.canvasID,
                ChartTitle = this.ChartTitle,
                XTitle = this.XTitle,
                timeGap_selection = this.timeGap_selection,
                Labels = this.Labels?.ToArray(),
                Y_left_Title = this.Y_left_Title,
                Y_right_Title = this.Y_right_Title,
                YMin = this.YMin,
                YMax = this.YMax,
                chart_single_data_lines = this.chart_single_data_lines?
                    .Select(line => new CHART_SINGLE_DATA_LINE
                    {
                        status = line.status,
                        Cmd = line.Cmd,
                        Unit = line.Unit,
                        Y_axis_selection = line.Y_axis_selection,
                        color = line.color,
                        Data = line.Data?.ToArray()
                    }).ToList()
            };
        }
    }

    public enum timeGap{
        SECOND  = 1,
        MINUTE  = 2,
        HOUR    = 3,
        DAY     = 4,
        WEEK    = 5,
        MONTH   = 6,
        YEAR    = 7,
    }

    public class CHART_SINGLE_DATA_LINE
    {
        public SettingStatus status {get; set; } = SettingStatus.YET;
        public string Cmd{get; set;} = null;
        public string Unit{get; set;} = null;
        public bool? Y_axis_selection {get; set;} = null; //選擇要Y_left還是Y_right
        public string color {get; set;} = null;
        public double[] Data{get; set;} //Y軸資料
    }

    public class CHART_STAGE_CONFIG
    {
        public string canvasID{get; set;}
        public string[] Labels{get; set;}
        public string Y1Label{get; set;}
        public double[] Y1Data{get; set;}
        public string Y2Label{get; set;}
        public double[] Y2Data{get; set;}
        public string XLabel {get; set;}
    };
}