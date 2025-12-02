using demoVer.Services;
using demoVer.Utils;
using System.Collections.Concurrent;
using demoVer.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace demoVer.Services
{	
    //目前的初始值是 [測試用]
    public class CommonData : IDisposable
    {
        private string _category = "";
        
        //inject
        private readonly GlobalVar _globalVar;
        private readonly WriteProcess _writeProcess;
        
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

        public bool ScalingFactor_OK        {get; set;} = false;
        public ConcurrentDictionary<string, double> _ScalingFactors {get; set;} = new();
        
        public event Func<Task>? ScalingFactor_StateChanged;


        public CommonData(  GlobalVar globalVar,
                            WriteProcess writeProcess)
        {
            //Log
            _category = GetType().FullName!;

            //Inject
            _globalVar = globalVar;
            _writeProcess = writeProcess;

        }

        public void Dispose()
        {
            
        }
    }
}