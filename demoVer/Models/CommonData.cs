namespace demoVer.Models
{	
    //目前的初始值是 [測試用]
    public class CommonData
    {
        public string ModelName {get; set;} = "NTN-5K-224";
        
        public float Freq_Factor            {get; set;} = 0.01f;
        public float Watt_Factor            {get; set;} = 0.1f;
        public float CURVE_TIMEOUT_Factor   {get; set;} = 1f;
        public float TEMPERATURE_Factor     {get; set;} = 0.1f;
        public float FAN_SPEED_Factor       {get; set;} = 1f;
        public float IAC_Factor             {get; set;} = 0.1f;
        public float VAC_Factor             {get; set;} = 0.1f;
        public float IDC_Factor             {get; set;} = 0.01f;
        public float VDC_Factor             {get; set;} = 0.01f;
    }
}