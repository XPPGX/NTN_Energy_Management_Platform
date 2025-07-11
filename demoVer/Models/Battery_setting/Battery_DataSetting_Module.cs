using demoVer.Services;
using demoVer.Utils;

namespace demoVer.Models
{
    public class Battery_DataSetting_Module : ObservableModule
    {
        //引入commonData
        private readonly CommonData _commonData;
        public Battery_DataSetting_Module(CommonData commonData)
        {
            _commonData = commonData;
        }

        //CC
        private int _CC_Raw;            //原始值(會佔用記憶體)
        public int CC_Raw => _CC_Raw;   //傳輸資料用(屬性,不消耗記憶體)
        public float CC_Display         //UI顯示與設定(屬性,不消耗記憶體)
        {
            get => ScalingComputer.MultOperation((float)_CC_Raw,  _commonData.IDC_Factor);
            set => _CC_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.IDC_Factor);
        }

        private int _CC_Max_Raw;
        public int CC_Max_Raw => _CC_Max_Raw;
        public float CC_Max_Display
        {
            get => ScalingComputer.MultOperation((float)_CC_Max_Raw, _commonData.IDC_Factor);
            set => _CC_Max_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.IDC_Factor);
        }

        private int _CC_Min_Raw;
        public int CC_Min_Raw => _CC_Min_Raw;
        public float CC_Min_Display
        {
            get => ScalingComputer.MultOperation((float)_CC_Min_Raw, _commonData.IDC_Factor);
            set => _CC_Min_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.IDC_Factor);
        }

        //TC
        private int _TC_Raw;
        public int TC_Raw => _TC_Raw;
        public float TC_Display
        {
            get => ScalingComputer.MultOperation((float)_TC_Raw, _commonData.IDC_Factor);
            set => _TC_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.IDC_Factor);
        }

        private int _TC_Max_Raw;
        public int TC_Max_Raw => _TC_Max_Raw;
        public float TC_Max_Display
        {
            get => ScalingComputer.MultOperation((float)_TC_Max_Raw, _commonData.IDC_Factor);
            set => _TC_Max_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.IDC_Factor);
        }

        private int _TC_Min_Raw;
        public int TC_Min_Raw => _TC_Min_Raw;
        public float TC_Min_Display
        {
            get => ScalingComputer.MultOperation((float)_TC_Min_Raw, _commonData.IDC_Factor);
            set => _TC_Min_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.IDC_Factor);
        }

        //CV
        private int _CV_Raw;
        public int CV_Raw => _CV_Raw;
        public float CV_Display
        {
            get => ScalingComputer.MultOperation((float)_CV_Raw, _commonData.VDC_Factor * 10);
            set => _CV_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.VDC_Factor) / 10;
        }

        private int _CV_Max_Raw;
        public int CV_Max_Raw => _CV_Max_Raw;
        public float CV_Max_Display
        {
            get => ScalingComputer.MultOperation((float)_CV_Max_Raw, _commonData.VDC_Factor * 10);
            set => _CV_Max_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.VDC_Factor);
        }

        private int _CV_Min_Raw;
        public int CV_Min_Raw => _CV_Min_Raw;
        public float CV_Min_Display
        {
            get => ScalingComputer.MultOperation((float)_CV_Min_Raw, _commonData.VDC_Factor * 10);
            set => _CV_Min_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.VDC_Factor);
        }

        //FV
        private int _FV_Raw;
        public int FV_Raw => _FV_Raw;
        public float FV_Display
        {
            get => ScalingComputer.MultOperation((float)_FV_Raw, _commonData.VDC_Factor * 10);
            set => _FV_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.VDC_Factor) / 10;
        }

        private int _FV_Min_Raw;
        public int FV_Min_Raw => _FV_Min_Raw;
        public float FV_Min_Display
        {
            get => ScalingComputer.MultOperation((float)_FV_Min_Raw, _commonData.VDC_Factor * 10);
            set => _FV_Min_Raw = (int)ScalingComputer.DevideOperation(value, _commonData.VDC_Factor);
        }

        public float FV_Max_Display => CV_Display;

        //CC_TimeOut(Minute)
        private int _CC_TimeOut_Raw;
        public int CC_TimeOut_Raw => _CC_TimeOut_Raw;
        public int CC_TimeOut_Display
        {
            get => _CC_TimeOut_Raw;
            set => _CC_TimeOut_Raw = value;
        }

        private bool _CCT_Enable;
        public bool CCT_Enable{get => _CCT_Enable; set => _CCT_Enable = value;}

        //CV_TimeOut(Minute)
        private int _CV_TimeOut_Raw;
        public int CV_TimeOut_Raw => _CV_TimeOut_Raw;
        public int CV_TimeOut_Display
        {
            get => _CV_TimeOut_Raw;
            set => _CV_TimeOut_Raw = value;
        }

        private bool _CVT_Enable;
        public bool CVT_Enable{get => _CVT_Enable; set => _CVT_Enable = value;}

        //FV_TimeOut(Minute)
        private int _FV_TimeOut_Raw;
        public int FV_TimeOut_Raw => _FV_TimeOut_Raw;
        public int FV_TimeOut_Display
        {
            get => _FV_TimeOut_Raw;
            set => _FV_TimeOut_Raw = value;
        }

        private bool _FVT_Enable;
        public bool FVT_Enable{get => _FVT_Enable; set => _FVT_Enable = value;}

        //CuverStage
        private bool _CurveStage;
        public bool CurveStage{get => _CurveStage; set => _CurveStage = value;}


        public bool SaveSettingData(BatterySetting Data)
        {
            bool changed_Flag = false;

            if( CC_Display != Data.CC_Display ||
                TC_Display != Data.TC_Display ||
                CV_Display != Data.CV_Display ||
                FV_Display != Data.FV_Display ||
                CurveStage != Data.CurveStage ||
                CCT_Enable != Data.CCT_Enable ||
                CC_TimeOut_Display != Data.CC_TimeOut_Display ||
                CVT_Enable != Data.CVT_Enable ||
                CV_TimeOut_Display != Data.CV_TimeOut_Display ||
                FVT_Enable != Data.FVT_Enable ||
                FV_TimeOut_Display != Data.FV_TimeOut_Display)
            {
                changed_Flag = true;

                CC_Display = Data.CC_Display;
                TC_Display = Data.TC_Display;
                CV_Display = Data.CV_Display;
                FV_Display = Data.FV_Display;
                CurveStage = Data.CurveStage;
                CCT_Enable = Data.CCT_Enable;
                CC_TimeOut_Display = Data.CC_TimeOut_Display;
                CVT_Enable = Data.CVT_Enable;
                CV_TimeOut_Display = Data.CV_TimeOut_Display;
                FVT_Enable = Data.FVT_Enable;
                FV_TimeOut_Display = Data.FV_TimeOut_Display;

                NotifyChanged();
                Console.WriteLine($"CC_Display = {CC_Display}");
            }

            return changed_Flag;
        }

        public BatterySetting_To_JS ToDto()
        {
            return new BatterySetting_To_JS
            {
                cc_Display = CC_Display,
                tc_Display = TC_Display,
                cv_Display = CV_Display,
                fv_Display = FV_Display,
                curveStage = CurveStage,
                cct_Enable = CCT_Enable,
                cc_TimeOut_Display = CC_TimeOut_Display,
                cvt_Enable = CVT_Enable,
                cv_TimeOut_Display = CV_TimeOut_Display,
                fvt_Enable = FVT_Enable,
                fv_TimeOut_Display = FV_TimeOut_Display,
            };
        }

        public void UpdateFrom(BatteryInitData source)
        {
            _CC_Raw = source.CC;
            _CC_Max_Raw = source.CC_Max;
            _CC_Min_Raw = source.CC_Min;

            _TC_Raw = source.TC;
            _TC_Max_Raw = source.TC_Max;
            _TC_Min_Raw = source.TC_Min;

            _CV_Raw = source.CV;
            _CV_Max_Raw = source.CV_Max;
            _CV_Min_Raw = source.CV_Min;

            _FV_Raw = source.FV;
            _FV_Min_Raw = source.FV_Min;
        
            Console.WriteLine($"CC(Raw, DMax, DMin) = {_CC_Raw}, {CC_Max_Display}, {CC_Min_Display}");
            Console.WriteLine($"TC(Raw, DMax, DMin) = {_TC_Raw}, {TC_Max_Display}, {TC_Min_Display}");
            Console.WriteLine($"CV(Raw, DMax, DMin) = {_CV_Raw}, {CV_Max_Display}, {CV_Min_Display}");
            Console.WriteLine($"FV(Raw, DMax, DMin) = {_FV_Raw}, {FV_Max_Display}, {FV_Min_Display}");
            
            NotifyChanged();
        }
    }
    

    public class BatterySetting
    {
        public float CC_Display {get; set;}
        public float TC_Display {get; set;}
        public float CV_Display {get; set;}
        public float FV_Display {get; set;}
        public bool CurveStage {get; set;}
        public bool CCT_Enable {get; set;}
        public int CC_TimeOut_Display {get; set;}
        public bool CVT_Enable {get; set;}
        public int CV_TimeOut_Display {get; set;}
        public bool FVT_Enable {get; set;}
        public int FV_TimeOut_Display {get; set;}
    }

    public class BatterySetting_To_JS
    {
        public float cc_Display {get; set;}
        public float tc_Display {get; set;}
        public float cv_Display {get; set;}
        public float fv_Display {get; set;}
        public bool curveStage {get; set;}
        public bool cct_Enable {get; set;}
        public int cc_TimeOut_Display {get; set;}
        public bool cvt_Enable {get; set;}
        public int cv_TimeOut_Display {get; set;}
        public bool fvt_Enable {get; set;}
        public int fv_TimeOut_Display {get; set;}
    }
    
}