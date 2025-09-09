using demoVer.Models;
using demoVer.Utils;
using demoVer.Interfaces;

namespace demoVer.Services
{
    public class WriteProcess : IDisposable
    {
        
        //For log
        private string _category;

        //injections
        private readonly GlobalVar _globalVar;
        private readonly ApiManager _apiManager;
        
        //Variables
        private readonly SemaphoreSlim _lock = new(1, 1); //之後需要多個子系統的時候，_lock應該要宣告在SubAppSystem裡面
        private bool IsWriteSuccess;


        public WriteProcess(GlobalVar globalVar,
                            ApiManager apiManager)
        {
            //For log
            _category = GetType().FullName!;

            //Injections
            _globalVar = globalVar;
            _apiManager = apiManager;
        }

        public void Dispose()
        {   

        }

        //[開發中] 目前只支援一個運行中的系統，待後續擴展
        public async Task<bool> Write_To_Framework(Dictionary<string, byte[]> packedByteData_dict, int subSysID)
        {
            if(_globalVar.ActiveSubAppSystemID is null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][Write_To_Framework] _globalVar.ActiveSubAppSystemID is null", AppLogLevel.Warning);
                return false;
            }

            //這裡是寫死的
            subSysID = (int)(_globalVar.ActiveSubAppSystemID);

            if(!_lock.Wait(0))
            {
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][Write_To_Framework] There are someone using this function, Skip", AppLogLevel.Warning);
                return false;
            }
            
            try
            {
                var subSysInfo = _globalVar.SubSystems[subSysID];
                var targetSubSystem_WriteMem = _globalVar.Device_WriteData.SubAppSystem_WriteMemorys[subSysID];
                if(targetSubSystem_WriteMem is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[WriteProcess][Write_To_Framework] targetSubSystem_WriteMem is null", AppLogLevel.Trace);
                    return false;
                }
                
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][Write_To_Framework] step 0...Done", AppLogLevel.Trace);
                
                try
                {
                    //1. 對應的SubAppSystem，其 CHECK 是否有資料，沒有的話去拉CHECK資料，並更新scaling
                    var selectedPort_Check = targetSubSystem_WriteMem.Setting_Check;
                    if(selectedPort_Check.Count == 0)
                    {   
                        //拉資料到CHECK
                        assignSettingData_From_Framework_To_SubSysCheck(subSysInfo, targetSubSystem_WriteMem);
                    }
                    if(targetSubSystem_WriteMem.ScalingFactors.Count == 0)
                    {
                        //更新scaling
                        assignScalingFactors_To_SubSysWriteMem(subSysInfo, targetSubSystem_WriteMem);
                    }

                    AppLogger.Log_To_File_log(_category, $"[WriteProcess][Write_To_Framework] step 1...Done", AppLogLevel.Trace);

                    //2. 轉好的UI值 存到 WRITE 結構 (要根據 CAN 或 MOD 進行 byte位移)
                    putPackedData_Into_SubSysWrite(subSysInfo, targetSubSystem_WriteMem, packedByteData_dict);
                    AppLogger.Log_To_File_log(_category, $"[WriteProcess][Write_To_Framework] step 2...Done", AppLogLevel.Trace);
                    
                    //3. 生成 WriteAPI 所需的 Body
                    var writeAPI_Body = Generate_WriteFormat(targetSubSystem_WriteMem);        
                    AppLogger.Log_To_File_log(_category, $"[WriteProcess][Write_To_Framework] step 3...Done", AppLogLevel.Trace);

                    //4. 發送 writeAPI_Body ，然後讀回CHECK，比對CHECK是否 == WRITE (嘗試3次，1秒1次)
                    bool isSettingSuccess = await Transfer_And_Compare(subSysInfo, targetSubSystem_WriteMem, writeAPI_Body);
                    AppLogger.Log_To_File_log(_category, $"[WriteProcess][Write_To_Framework] step 4...Done", AppLogLevel.Trace);

                    //5. return true
                    return isSettingSuccess;
                }
                catch(Exception e)
                {
                    AppLogger.Log_To_File_log(_category, $"[WriteProcess][assignScalingFactors_To_SubSysWriteMem] Error : {e}", AppLogLevel.Error);
                    return false;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task assignSettingData_From_Framework_To_SubSysCheck(SubAppSystem subSysInfo, SubAppSystem_WriteMemory targetSubSystem_WriteMem)
        {
            var result = await _apiManager.apiRead_SettingData(subSysInfo.port, subSysInfo.protocolFileName);
            if(result is null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][assignSettingData_From_Framework_To_SubSysCheck] apiRead_SettingData Failed", AppLogLevel.Trace);
                return;
            }
            targetSubSystem_WriteMem.saveSettingData_From_Framework_To_Check(result);
        }
 
        //只能在CHECK有資料的時候呼叫
        public void assignScalingFactors_To_SubSysWriteMem(SubAppSystem subSysInfo, SubAppSystem_WriteMemory targetSubSystem_WriteMem)
        {
            uint portStartAddr =(uint)(_globalVar.getPortStartAddr(subSysInfo.port));
            uint subSys_trueStartAddr = portStartAddr + subSysInfo.startAddr;
            uint subSys_trueEndAddr = subSys_trueStartAddr + subSysInfo.length;
        
            //0. 取範圍內，任一連線機器
            uint? firstLinkedAddr = _globalVar.LinkedDevices.GetFirstLinkedAddr(subSys_trueStartAddr, subSys_trueEndAddr);
            if(firstLinkedAddr is null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][assignScalingFactors_To_SubSysWriteMem] firstLinkedAddr is null", AppLogLevel.Trace);
                return;
            }
            AppLogger.Log_To_File_log(_category, $"[WriteProcess][assignScalingFactors_To_SubSysWriteMem] firstLinkedAddr = {firstLinkedAddr}", AppLogLevel.Trace);

            //1. 取得 firstLinkedAddr的所有CommandData from readMemory
            var tmp_oneDeviceData = _globalVar.Device_ReadData.Get_oneDevice_DataSnapshot((uint)firstLinkedAddr);
            
            //2. Traverse all writeCommands in readMemory, store the scaling factor
            targetSubSystem_WriteMem.assignScalingFactor(tmp_oneDeviceData);
        }
        
        //將UI數值 透過scaling factor還原成 通訊數值，return byte[2]
        public async Task<byte[]> UIvalueTransformToWriteFormat(double val, string cmd)
        {
            byte[] tmp_byteArray = new byte[2];
            if(_globalVar.ActiveSubAppSystemID is null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][UIvalueTransformToWriteFormat] _globalVar.ActiveSubAppSystemID = {_globalVar.ActiveSubAppSystemID}", AppLogLevel.Trace);
                return tmp_byteArray;
            }
            
            int activeSubSysID = (int)_globalVar.ActiveSubAppSystemID; 
            var subSysInfo = _globalVar.SubSystems[activeSubSysID];
            var targetSubSystem_WriteMem = _globalVar.Device_WriteData.SubAppSystem_WriteMemorys[activeSubSysID];

            //檢查CHECK是否為空
            if(targetSubSystem_WriteMem.Setting_Check.Count == 0)
            {
                //拉資料到CHECK
                await assignSettingData_From_Framework_To_SubSysCheck(subSysInfo, targetSubSystem_WriteMem);
            }

            //檢查Scaling Factor是否為空
            if(targetSubSystem_WriteMem.ScalingFactors.Count == 0)
            {
                //更新scaling
                assignScalingFactors_To_SubSysWriteMem(subSysInfo, targetSubSystem_WriteMem);
            }

            //1. get ScalingFactor
            if(!(targetSubSystem_WriteMem.ScalingFactors.TryGetValue(cmd, out var factor)))
            {
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][UIvalueTransformToWriteFormat] cmd = {cmd}, scaling factor is null", AppLogLevel.Trace);
                return tmp_byteArray;
            } 

            //2. UI值 還原成 通訊值
            var codedVal = ScalingComputer.DevideOperation_doubleVer(val, (double)factor);
            AppLogger.Log_To_File_log(_category, $"[WriteProcess][UIvalueTransformToWriteFormat] cmd = {cmd}, codedVal = {codedVal}", AppLogLevel.Trace);
            
            //3. 通訊值 轉 byte array (這邊可能要考慮 CAN、MOD的byte位移問題)
            uint uint_codedVal = (uint)codedVal;
            AppLogger.Log_To_File_log(_category, $"[WriteProcess][UIvalueTransformToWriteFormat] uint_codedVal = {uint_codedVal}", AppLogLevel.Trace);

            string activeSubSysPort = _globalVar.getActiveSubSysPort();
            Console.WriteLine($"[WriteProcess][UIvalueTransformToWriteFormat] activeSubSysPort = {activeSubSysPort}", AppLogLevel.Trace);
            
            tmp_byteArray[0] = (byte)(uint_codedVal & 0x000000FF);
            tmp_byteArray[1] = (byte)((uint_codedVal >> 8) & 0x000000FF);
            
            //準備資料不需要根據CAN、MOD位移byte，等到要發送的時候在位移就好了。

            AppLogger.Log_To_File_log(_category, $"[WriteProcess][UIvalueTransformToWriteFormat] tmp_byteArray[1] = {tmp_byteArray[1]:X2}, [0] = {tmp_byteArray[0]:X2}", AppLogLevel.Trace);
            
            return tmp_byteArray;
        }

        public void putPackedData_Into_SubSysWrite(SubAppSystem subSysInfo, SubAppSystem_WriteMemory targetSubSystem_WriteMem, Dictionary<string, byte[]> packedByteData_dict)
        {
            //把當前 CHECK 的內容 複製到 WRITE
            targetSubSystem_WriteMem.subAppSys_Clone_CheckToWrite();

            foreach(var pair in packedByteData_dict)
            {
                var cmd = pair.Key;
                var twoByte = pair.Value;

                //根據port位移Byte
                byte[] shiftedTwoByte = takeShiftMethod(subSysInfo.port, twoByte);
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][putPackedData_Into_SubSysWrite] cmd = {cmd}, shiftedByte[1] = {shiftedTwoByte[1]:X2}, shiftedByte[0] = {shiftedTwoByte[0]:X2}", AppLogLevel.Trace);
                
                //把位移後的Byte，放入WriteMemory中，對應命令的value的 [0][1]
                
                //取出WRITE中的命令Ref
                if(targetSubSystem_WriteMem.Setting_Write.TryGetValue(cmd, out var cmdData))
                {
                    AppLogger.Log_To_File_log(_category, $"WRITE, FOUND cmd = {cmd}", AppLogLevel.Trace);
                    if(cmdData.IsPerAddr is true)
                    {//針對 數值對應 單台INV (使用addrValues)
                        foreach(var addrVal in cmdData.AddrValues)
                        {
                            var byte_list = addrVal.value;
                            byte_list[0] = shiftedTwoByte[0];
                            byte_list[1] = shiftedTwoByte[1];
                            AppLogger.Log_To_File_log(_category, $"addr = {addrVal.addr}, data = [{string.Join(", ", addrVal.value)}]", AppLogLevel.Trace);
                        }
                    }
                    else
                    {//針對 數值對應 全部INV (使用targetValue)
                        var byte_list = cmdData.TargetValue;
                        byte_list[0] = shiftedTwoByte[0];
                        byte_list[1] = shiftedTwoByte[1];
                        AppLogger.Log_To_File_log(_category, $"[{string.Join(", ", cmdData.TargetValue)}]", AppLogLevel.Trace);
                    }
                }
                else
                {
                    AppLogger.Log_To_File_log(_category, $"WRITE, NOT FOUND cmd = {cmd}", AppLogLevel.Trace);   
                }
            }
        }

        private byte[] takeShiftMethod(string port, byte[] twoByte)
        {
            byte[] shiftedTwoByte = new byte[2];

            if(port.StartsWith("CAN"))
            {
                shiftedTwoByte[0] = twoByte[1];
                shiftedTwoByte[1] = twoByte[0];   
            }
            else if(port.StartsWith("MOD"))
            {
                shiftedTwoByte[0] = twoByte[0];
                shiftedTwoByte[1] = twoByte[1];
            }

            return shiftedTwoByte;
        }

        // //把目標port的Write structure生成 WriteAPI 需要的 body
        public List<SingleRawSettingCommand_JsonFormat> Generate_WriteFormat(SubAppSystem_WriteMemory targetSubSystem_WriteMem)
        {
            List<SingleRawSettingCommand_JsonFormat> Write_Body = new List<SingleRawSettingCommand_JsonFormat>();
            foreach(var pair in targetSubSystem_WriteMem.Setting_Write)
            {
                string cmdName = pair.Key;
                var data = pair.Value;

                var tmpData = new SingleRawSettingCommand_JsonFormat()
                {
                    commandName = cmdName,
                    isPerAddr = data.IsPerAddr,
                    targetValue = (data.IsPerAddr is true) ? null : data.TargetValue?.ToList(),
                    addrValues = (data.IsPerAddr is true) ? data.AddrValues?.Select(addrVal => addrVal.DeepClone()).ToList() : null
                };

                Write_Body.Add(tmpData);
            }

            AppLogger.Log_To_File_Json(Write_Body);
            
            return Write_Body;
        }

        public async Task<bool> Transfer_And_Compare(SubAppSystem subSysInfo, SubAppSystem_WriteMemory targetSubSystem_WriteMem, List<SingleRawSettingCommand_JsonFormat> writeBody)
        {
            //發送 writeBody 並檢查，最多三次，成功則提早退出
            try
            {
                for(int i = 0 ; i < 3 ; i ++)
                {    
                    //發送 writeBody
                    var response_apiWrite_bool = await _apiManager.apiWrite_SettingData(subSysInfo.port, subSysInfo.protocolFileName, writeBody);
                    if(response_apiWrite_bool is true)
                    {
                        await Task.Delay(100);

                        //把 FrameWork 當前的 write-memory 存進 CHECK
                        await assignSettingData_From_Framework_To_SubSysCheck(subSysInfo, targetSubSystem_WriteMem);

                        bool is_CHECK_and_WRITE_same = targetSubSystem_WriteMem.compare_CHECK_and_WRITE();
                        if(is_CHECK_and_WRITE_same is true)
                        {
                            AppLogger.Log_To_File_log(_category, $"[WriteProcess][Transfer_And_Compare] CHECK == WRITE", AppLogLevel.Trace);

                            return true;
                        }
                        AppLogger.Log_To_File_log(_category, $"[WriteProcess][Transfer_And_Compare] {i + 1}-th time, CHECK != WRITE", AppLogLevel.Trace);
                    }
                    await Task.Delay(100);
                }
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][Transfer_And_Compare] Error : {e}", AppLogLevel.Trace);
            }

            AppLogger.Log_To_File_log(_category, $"[WriteProcess][Transfer_And_Compare] Give up Retry.", AppLogLevel.Trace);
            return false;
        }

        // //service宣告在process.cs之後，要實際呼叫一次function或是使用內部變數，才會實際建立instance
        // //awake() function用於讓他實際建立instance
        public bool isDeviceReady()
        {
            return false;        
        }
    }
}