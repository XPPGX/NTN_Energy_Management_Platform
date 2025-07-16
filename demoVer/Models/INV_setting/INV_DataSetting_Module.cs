using demoVer.Services;
using demoVer.Utils;

namespace demoVer.Models
{
    public class INV_DataSetting_Module : ObservableModule
    {
        
        //引入commonData
        private readonly CommonData _commonData;
        public INV_DataSetting_Module(CommonData commonData)
        {
            _commonData = commonData;
        }

        //AC_Series
        private byte _AC_Series; //0 for 110VAC, 1 for 220VAC
        public byte AC_Series
        {
            get => _AC_Series;
            set
            {
                _AC_Series = value;
                UpdateACV_DisplayMap();
            }
        }
        

        //ACF
        private byte _ACF;
        public byte ACF
        {
            get => _ACF;
            set => _ACF = value;
        }
        public Dictionary<byte, string> ACF_DisplayMap = new()
        {
            {0, "50Hz"},
            {1, "60Hz"}
        };
        public string ACF_Display_str
        {
            get => ACF_DisplayMap.ContainsKey(_ACF) ? ACF_DisplayMap[_ACF] : "??";
            set
            {
                if(!ACF_DisplayMap.ContainsValue(value)){return;}

                //用value 找 key
                var key_value_pair = ACF_DisplayMap.FirstOrDefault(pair => pair.Value == value);
                _ACF = key_value_pair.Key;
            }
        }


        //ACV
        private byte _ACV;
        public byte ACV
        {
            get => _ACV;
            set => _ACV = value;
        }
        public Dictionary<byte, string> ACV_DisplayMap = new();
        public void UpdateACV_DisplayMap()
        {
            ACV_DisplayMap.Clear();

            if(_AC_Series == 0)
            {
                ACV_DisplayMap.Add(0, "100VAC");
                ACV_DisplayMap.Add(1, "110VAC");
                ACV_DisplayMap.Add(2, "115VAC");
                ACV_DisplayMap.Add(3, "120VAC");
            }
            else if(_AC_Series == 1)
            {
                ACV_DisplayMap.Add(0, "200VAC");
                ACV_DisplayMap.Add(1, "220VAC");
                ACV_DisplayMap.Add(2, "230VAC");
                ACV_DisplayMap.Add(3, "240VAC");
            }
        }
        public string ACV_Display_str
        {
            get => ACV_DisplayMap.ContainsKey(_ACV) ? ACV_DisplayMap[_ACV] : "??";
            set
            {
                if(!ACV_DisplayMap.ContainsValue(value)){return;}

                var key_value_pair = ACV_DisplayMap.FirstOrDefault(pair => pair.Value == value);
                _ACV = key_value_pair.Key;
            }
        }
        
        //touchable
        private uint _Button_touchable_status;
        public uint Button_touchable_status
        {
            get => _Button_touchable_status;
            set => _Button_touchable_status = value;
        }
        public bool AC_mode_charging_touchable
        {
            get => ((_Button_touchable_status & 0x00000040) == 0x00000040) ? true : false; 
        }
        public bool Grid_tied_power_feeding_touchable
        {
            get => ((_Button_touchable_status & 0x00000080) == 0x00000080) ? true : false;
        }
        public bool Output_priority_touchable
        {
            get => ((_Button_touchable_status & 0x00000010) == 0x00000010) ? true : false;
        }
        public bool Charge_priority_touchable
        {
            get => ((_Button_touchable_status & 0x00000020) == 0x00000020) ? true : false;
        }

        //AC_mode_charging_enable
        private bool _AC_mode_charging_enable;
        public bool AC_mode_charging_enable
        {
            get => _AC_mode_charging_enable;
            set => _AC_mode_charging_enable = value;
        }

        //Grid_tied_power_feeding
        private bool _Grid_tied_power_feeding_enable;
        public bool Grid_tied_power_feeding_enable
        {
            get => _Grid_tied_power_feeding_enable;
            set => _Grid_tied_power_feeding_enable = value;
        }

        //Output priority
        private byte _Output_priority; //0 : utility, 1: battery, 2: solar
        public byte Output_priority
        {
            get => _Output_priority;
            set => _Output_priority = value;
        }

        //Charge priority
        private byte _Charging_priority; //0: utility, 1: solar
        public byte Charging_priority
        {
            get => _Charging_priority;
            set => _Charging_priority = value;
        }

        //BAT_Alarm
        private uint _BAT_Alarm;
        public uint BAT_Alarm => _BAT_Alarm;
        public double BAT_Alarm_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_Alarm, _commonData.VDC_Factor * 10);
            set => _BAT_Alarm = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }

        private uint _BAT_Alarm_Max;
        public uint BAT_Alarm_Max => _BAT_Alarm_Max;
        public double BAT_Alarm_Max_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_Alarm_Max, _commonData.VDC_Factor * 10);
            set => _BAT_Alarm_Max = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }

        private uint _BAT_Alarm_Min;
        public uint BAT_Alarm_Min => _BAT_Alarm_Min;
        public double BAT_Alarm_Min_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_Alarm_Min, _commonData.VDC_Factor * 10);
            set => _BAT_Alarm_Min = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }


        //BAT_Shutdown
        private uint _BAT_Shutdown;
        public uint BAT_Shutdown => _BAT_Shutdown;
        public double BAT_Shutdown_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_Shutdown, _commonData.VDC_Factor * 10);
            set => _BAT_Shutdown = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }

        private uint _BAT_Shutdown_Max;
        public uint BAT_Shutdown_Max => _BAT_Shutdown_Max;
        public double BAT_Shutdown_Max_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_Shutdown_Max, _commonData.VDC_Factor * 10);
            set => _BAT_Shutdown_Max = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }

        private uint _BAT_Shutdown_Min;
        public uint BAT_Shutdown_Min => _BAT_Shutdown_Min;
        public double BAT_Shutdown_Min_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_Shutdown_Min, _commonData.VDC_Factor * 10);
            set => _BAT_Shutdown_Min = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }
        
        //BAT_Recharge range
        private uint _BAT_Recharge;
        public uint BAT_Recharge => _BAT_Recharge;
        public double BAT_Recharge_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_Recharge, _commonData.VDC_Factor * 10);
            set => _BAT_Recharge = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }

        private uint _BAT_Recharge_Min;
        public uint BAT_Recharge_Min => _BAT_Recharge_Min;
        public double BAT_Recharge_Min_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_Recharge_Min, _commonData.VDC_Factor * 10);
            set => _BAT_Recharge_Min = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }
        
        //BAT_OV_Alarm range
        private uint _BAT_OV_Alarm;
        public uint BAT_OV_Alarm => _BAT_OV_Alarm;
        public double BAT_OV_Alarm_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_OV_Alarm, _commonData.VDC_Factor * 10);
            set => _BAT_OV_Alarm = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }

        private uint _BAT_OV_Alarm_Max;
        public uint BAT_OV_Alarm_Max => _BAT_OV_Alarm_Max;
        public double BAT_OV_Alarm_Max_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_OV_Alarm_Max, _commonData.VDC_Factor * 10);
            set => _BAT_OV_Alarm_Max = (uint)ScalingComputer.DevideOperation_doubleVer((double) value, _commonData.VDC_Factor) / 10;
        }

        private uint _BAT_OV_Alarm_Min;
        public uint BAT_OV_Alarm_Min => _BAT_OV_Alarm_Min;
        public double BAT_OV_Alarm_Min_Display
        {
            get => ScalingComputer.MultOperation_doubleVer((double)_BAT_OV_Alarm_Min, _commonData.VDC_Factor * 10);
            set => _BAT_OV_Alarm_Min = (uint)ScalingComputer.DevideOperation_doubleVer((double)value, _commonData.VDC_Factor) / 10;
        }

        //BAT_Capacity range
        private uint _BAT_Capacity;
        public uint BAT_Capacity => _BAT_Capacity;
        public int BAT_Capacity_Display
        {
            get => (int)_BAT_Capacity;
            set => _BAT_Capacity = (uint)value;
        }

        private uint _BAT_Capacity_Max = 9999;
        public int BAT_Capacity_Max => (int)_BAT_Capacity_Max;

        public bool SaveSettingData(INVSetting Data)
        {
            bool changed_Flag = false;

            if( ACF_Display_str                 != Data.new_ACF_Display_str             ||
                ACV_Display_str                 != Data.new_ACV_Display_str             ||
                AC_mode_charging_enable         != Data.new_AC_mode_charging_enable     ||
                Grid_tied_power_feeding_enable  != Data.new_Grid_tied_feeding_enable    ||
                Output_priority                 != Data.new_Output_priority             ||
                Charging_priority               != Data.new_Charge_priority             ||
                BAT_Alarm_Display               != Data.new_BAT_Alarm_Display           ||
                BAT_Shutdown_Display            != Data.new_BAT_Shutdown_Display        ||
                BAT_Recharge_Display            != Data.new_BAT_Recharge_Display        ||
                BAT_OV_Alarm_Display            != Data.new_BAT_OV_Alarm_Value          ||
                BAT_Capacity_Display            != Data.new_BAT_Capacity_Value)
            {
                changed_Flag = true;

                ACF_Display_str = Data.new_ACF_Display_str;
                ACV_Display_str = Data.new_ACV_Display_str;
                AC_mode_charging_enable = Data.new_AC_mode_charging_enable;
                Grid_tied_power_feeding_enable = Data.new_Grid_tied_feeding_enable;
                Output_priority = Data.new_Output_priority;
                Charging_priority = Data.new_Charge_priority;
                BAT_Alarm_Display = Data.new_BAT_Alarm_Display;
                BAT_Shutdown_Display = Data.new_BAT_Shutdown_Display;
                BAT_Recharge_Display = Data.new_BAT_Recharge_Display;
                BAT_OV_Alarm_Display = Data.new_BAT_OV_Alarm_Value;
                BAT_Capacity_Display = Data.new_BAT_Capacity_Value;

                NotifyChanged();
            }

            Console.WriteLine($"[INV][SaveSettingData] : return {changed_Flag}");
            return changed_Flag;
        }


        //Total data update
        public void UpdateFrom(INV_InitData source)
        {
            _AC_Series = source.AC_Series;
            _ACF = source.ACF;
            _ACV = source.ACV;
            _Button_touchable_status = source.Button_touchable_status;

            _AC_mode_charging_enable = ((source.Button_touchable_status & 0x00000040) == 0x00000040) ? true : false;
            _Grid_tied_power_feeding_enable = ((source.Button_touchable_status & 0x00000080) == 0x00000080) ? true : false;

            _BAT_Alarm_Max = source.BAT_Alarm_Max;
            _BAT_Alarm_Min = source.BAT_Alarm_Min;
            _BAT_Alarm = source.BAT_Alarm_Value;
            
            _BAT_Shutdown_Max = source.BAT_Shutdown_Max;
            _BAT_Shutdown_Min = source.BAT_Shutdown_Min;
            _BAT_Shutdown = source.BAT_Shutdown_Value;

            _BAT_Recharge_Min = source.BAT_Recharge_Min;
            _BAT_Recharge = source.BAT_Recharge_Value;

            _BAT_OV_Alarm_Max = source.BAT_OV_Alarm_Max;
            _BAT_OV_Alarm_Min = source.BAT_OV_Alarm_Min;
            _BAT_OV_Alarm = source.BAT_OV_Alarm_Value;

            _BAT_Capacity = source.BAT_Capacity_Value;

            UpdateACV_DisplayMap();

            Console.WriteLine($"AC_Series = {_AC_Series}, ACF = {_ACF}, ACV = {_ACV}");
            NotifyChanged();
        }
        
        public INVSetting_To_JS ToDto()
        {
            return new INVSetting_To_JS
            {
                acf_display_str = ACF_Display_str,
                acv_display_str = ACV_Display_str,
                
                ac_mode_charging_enable = AC_mode_charging_enable,
                grid_tied_feeding_enable = Grid_tied_power_feeding_enable,
                output_priority = Output_priority,
                charge_priority = Charging_priority,

                bat_alarm_display = BAT_Alarm_Display,
                bat_shutdown_display = BAT_Shutdown_Display,
                bat_recharge_display = BAT_Recharge_Display,
                bat_ov_alarm_display = BAT_OV_Alarm_Display,
                bat_capacity_value = BAT_Capacity_Display
            };
        }
    }

    public class INVSetting
    {
        public string new_ACF_Display_str {get; set;}
        public string new_ACV_Display_str {get; set;}
        
        public bool new_AC_mode_charging_enable {get; set;}
        public bool new_Grid_tied_feeding_enable {get; set;}
        public byte new_Output_priority {get; set;}
        public byte new_Charge_priority {get; set;}
        
        public double new_BAT_Alarm_Display {get; set;}
        public double new_BAT_Shutdown_Display {get; set;}
        public double new_BAT_Recharge_Display {get; set;}
        public double new_BAT_OV_Alarm_Value {get; set;}
        public int new_BAT_Capacity_Value {get; set;}
    }

    public class INVSetting_To_JS
    {
        public string acf_display_str {get; set;}
        public string acv_display_str {get; set;}

        public bool ac_mode_charging_enable {get; set;}
        public bool grid_tied_feeding_enable {get; set;}
        public byte output_priority {get; set;}
        public byte charge_priority {get; set;}

        public double bat_alarm_display {get; set;}
        public double bat_shutdown_display {get; set;}
        public double bat_recharge_display {get; set;}
        public double bat_ov_alarm_display {get; set;}
        public int bat_capacity_value {get; set;}
    }
}