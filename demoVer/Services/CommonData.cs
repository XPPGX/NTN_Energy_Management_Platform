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

            //Event Hook
            // _globalVar.Sys_ModelName_OnChanged += modelNameChange_Task;
        }

        public void Dispose()
        {
            // _globalVar.Sys_ModelName_OnChanged -= modelNameChange_Task;
        }

        

        public async Task modelNameChange_Task()
        {
            // //這裡是寫死的
            // var subSysID = (int)(_globalVar.ActiveSubAppSystemID);
        
            // var subSysInfo = _globalVar.SubSystems[subSysID];
            // var targetSubSystem_WriteMem = _globalVar.Device_WriteData.SubAppSystem_WriteMemorys[subSysID];

            // try
            // {
            //     ScalingFactor_OK = false;

            //     AppLogger.Log_To_File_log(_category, $"[CommonData][modelNameChange_Task] ScalingFactor_OK = {ScalingFactor_OK}...", AppLogLevel.Trace);
            //     //1. 檢查CHECK是否有資料
            //     if(targetSubSystem_WriteMem.Setting_Check.Count == 0)
            //     {
                    
            //         await _writeProcess.assignSettingData_From_Framework_To_SubSysCheck(subSysInfo, targetSubSystem_WriteMem);
            //         AppLogger.Log_To_File_log(_category, $"[CommonData][modelNameChange_Task] Updated SubSys.Check, due to Check.Count is 0", AppLogLevel.Trace);
            //     }
                
            //     //2. 檢查Scaling是否有資料
            //     if(targetSubSystem_WriteMem.ScalingFactors.Count == 0)
            //     {
            //         //更新scaling
            //         _writeProcess.assignScalingFactors_To_SubSysWriteMem(subSysInfo, targetSubSystem_WriteMem);
            //         AppLogger.Log_To_File_log(_category, $"[CommonData][modelNameChange_Task] Updated SubSys.ScalingFactor, due to ScalingFactor.Count is 0", AppLogLevel.Trace);
            //     }

            //     //3. 更新CommonData的_ScalingFactors
            //     UpdateScalingFactors(targetSubSystem_WriteMem.ScalingFactors);

            //     //4. 告知其他Caller，CommonData內的ScalingFactor已經可以使用
            //     ScalingFactor_OK = true;
            //     await ScalingFactor_StateChanged?.Invoke();
            //     AppLogger.Log_To_File_log(_category, $"[CommonData][UpdateScalingFactors] ScalingFactor_OK = {ScalingFactor_OK}", AppLogLevel.Trace);

            //     //5. return
            //     return;
            // }
            // catch(Exception e)
            // {
            //     AppLogger.Log_To_File_log(_category, $"[CommonData][modelNameChange_Task] Error : {e}", AppLogLevel.Error);
            // }
        }

        

        public void UpdateScalingFactors(ConcurrentDictionary<string, double> ScalingFactors)
        {
            _ScalingFactors.Clear();

            foreach(var (cmd, factor) in ScalingFactors)
            {
                _ScalingFactors[cmd] = factor;
            }

            AppLogger.Log_To_File_log(_category, $"[CommonData][UpdateScalingFactors] _ScalingFactors re-assigned", AppLogLevel.Trace);
            
            
        }
    }
}