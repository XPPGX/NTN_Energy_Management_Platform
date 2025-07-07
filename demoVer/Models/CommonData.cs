namespace demoVer.Models
{	
    //目前的初始值是 [測試用]
    public class CommonData
    {
        public string ModelName {get; set;} = "NTN-5K-224";
        
        public ScalingFactorGroup ScalingFactors {get; set;} = new();
    }

    public class ScalingFactorGroup
    {
        public float Freq_Factor            {get; set;} = 0.01f;
        public float Watt_Factor            {get; set;} = 0.1f;
        public float CURVE_TIMEOUT_Factor   {get; set;} = 1f;
        public float TEMPERATURE_Factor     {get; set;} = 0.1f;
        public float FAN_SPEED_Factor       {get; set;} = 1f;
        public float IAC_Factor             {get; set;} = 0.1f;
        public float VAC_Factor             {get; set;} = 0.1f;
        public float IDC_Factor             {get; set;} = 0.01f;
        public float VDC_Factor             {get; set;} = 0.01f;
    
        public float DevideOperation(float value, float factor)
        {
            switch(factor)
            {
                case 0.001f: return value * 1000;
                case 0.01f:  return value * 100;
                case 0.1f:   return value * 10;
                case 1.0f:   return value * 1;
                // case 10f:    return value / 10;
                // case 100f:   return value / 100;
                default:    return -1;
            }
        }

        public float MultOperation(float value, float factor)
        {
            return AutoSnapToDecimal(value * factor, 5);
        }

        public float AutoSnapToDecimal(float value, int decimals = 2)
        {
            float rounded = (float)Math.Round(value, decimals);
            float tolerance = MathF.Pow(10, -decimals) * 5;
            return Math.Abs(value - rounded) < tolerance ? rounded : value;
        }        


        public float Scale_Freq(float value)            => DevideOperation(value, Freq_Factor);
        public float Scale_Watt(float value)            => DevideOperation(value, Watt_Factor);
        public float Scale_CURVE_TIMEOUT(float value)   => DevideOperation(value, CURVE_TIMEOUT_Factor);
        public float Scale_TEMPERATURE(float value)     => DevideOperation(value, TEMPERATURE_Factor);
        public float Scale_FAN_SPEED(float value)       => DevideOperation(value, FAN_SPEED_Factor);
        public float Scale_IAC(float value)             => DevideOperation(value, IAC_Factor);
        public float Scale_VAC(float value)             => DevideOperation(value, VAC_Factor);
        public float Scale_IDC(float value)             => DevideOperation(value, IDC_Factor);
        public float Scale_VDC(float value)             => DevideOperation(value, VDC_Factor);

    }
}