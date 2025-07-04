namespace demoVer.Models
{
    public class BatteryInitData
    {
        public int CC { get; set; } = 6000;
        public int CC_Max { get; set; } = 6000;
        public int CC_Min { get; set; } = 1200;

        public int TC { get; set; } = 600;
        public int TC_Max { get; set; } = 1800;
        public int TC_Min { get; set; } = 120;

        public int CV { get; set; } = 576;
        public int CV_Max { get; set; } = 600;
        public int CV_Min { get; set; } = 400;

        public int FV { get; set; } = 552;
        public int FV_Min { get; set; } = 400;
    }
}