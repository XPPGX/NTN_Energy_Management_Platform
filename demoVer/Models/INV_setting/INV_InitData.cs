using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace demoVer.Models
{
    public class INV_InitData
    {

        

        [JsonPropertyName("ac_series")]
        public byte AC_Series {get; set;} = 0;

        [JsonPropertyName("acf")]
        public byte ACF {get; set;} = 0;

        [JsonPropertyName("acv")]
        public byte ACV {get; set;} = 0;

        [JsonPropertyName("btn_status")]
        //In json file, 0x0000015F == 351
        public uint Button_touchable_status {get; set;} = 0x0000015F; //bit0:BAT Alarm bit1:BAT Shutdown bit2:BAT Recharge bit3:BAT Capacity bit4:Output Priority bit5:Charging Priority bit6:AC_mode_charging_enable bit7:Grid_tied_power_feeding_enable bit8:BAT OV Alarm
        
        [JsonPropertyName("out_prio")]
        public byte Output_priority {get; set;} = (byte)0;

        [JsonPropertyName("chg_prio")]
        public byte Charge_priority {get; set;} = (byte)0;

        [JsonPropertyName("bat_alarm_max")]
        public uint BAT_Alarm_Max       {get; set;} = 500;

        [JsonPropertyName("bat_alarm_min")]
        public uint BAT_Alarm_Min       {get; set;} = 376;

        [JsonPropertyName("bat_alarm_val")]
        public uint BAT_Alarm_Value     {get; set;} = 440;

        [JsonPropertyName("bat_shutdown_max")]
        public uint BAT_Shutdown_Max    {get; set;} = 480;

        [JsonPropertyName("bat_shutdown_min")]
        public uint BAT_Shutdown_Min    {get; set;} = 368;

        [JsonPropertyName("bat_shutdown_val")]
        public uint BAT_Shutdown_Value  {get; set;} = 380;

        [JsonPropertyName("bat_recharge_min")]
        public uint BAT_Recharge_Min    {get; set;} = 368;
        
        [JsonPropertyName("bat_recharge_val")]
        public uint BAT_Recharge_Value  {get; set;} = 368;

        [JsonPropertyName("bat_ov_alarm_max")]
        public uint BAT_OV_Alarm_Max    {get; set;} = 660;
        
        [JsonPropertyName("bat_ov_alarm_min")]
        public uint BAT_OV_Alarm_Min    {get; set;} = 600;

        [JsonPropertyName("bat_ov_alarm_val")]
        public uint BAT_OV_Alarm_Value  {get; set;} = 620;
        

        [JsonPropertyName("bat_capacity_val")]
        public uint BAT_Capacity_Value  {get; set;} = 0;
        
        public static INV_InitData LoadFromJsonFile()
        {
            string AppDataDirectory = Path.GetFullPath("App_Data");
            string INV_SettingFilePath = Path.Combine(AppDataDirectory, "INV_Setting_LastTime.json");

            if(!File.Exists(INV_SettingFilePath))
            {
                Console.WriteLine($"{INV_SettingFilePath} 檔案不存在");
                return new INV_InitData();
            }

            string json = File.ReadAllText(INV_SettingFilePath);
            var options = new JsonSerializerOptions{PropertyNameCaseInsensitive = true};

            return JsonSerializer.Deserialize<INV_InitData>(json, options) ?? new INV_InitData();
        }
    }
}