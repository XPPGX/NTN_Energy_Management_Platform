using demoVer.Utils;
using System.Text.Json;
using System.Collections.Concurrent;
using demoVer.Services;

namespace demoVer.Models
{
    public class SubAppSystem_WriteMemory
    {
        //Log
        private string _category;

        public int subSystem_WriteMemory_ID {get; set;}

        //The Data structure, which is READ from WriteMemory in framework
        public ConcurrentDictionary<string, settingCommandRawData> Setting_Check {get; set;}
        
        //The Data structure, which will be Written to WriteMemory in framework
        public ConcurrentDictionary<string, settingCommandRawData> Setting_Write {get; set;}

        //The Scaling Factors got from globalVar.Device_ReadData
        public ConcurrentDictionary<string, double> ScalingFactors; //新版WriteAPI會給這些值，而不是從ReadAPI那邊拉過來

        public SubAppSystem_WriteMemory(int subSysID)
        {   
            //Log
            _category                   = GetType().FullName!;

            //allocate memory
            subSystem_WriteMemory_ID    = subSysID;
            Setting_Check               = new ConcurrentDictionary<string, settingCommandRawData>();
            Setting_Write               = new ConcurrentDictionary<string, settingCommandRawData>();
            ScalingFactors              = new ConcurrentDictionary<string, double>();
        }

        public void saveSettingData_From_Framework_To_Check(List<SingleRawSettingCommand_JsonFormat> rcvSettingData_JsonFormat)
        {
            //1. 取出subSys_MemoryWrite_Check
            var subSys_MemWrite_Check = Setting_Check;

            try
            {
                //2. save data into subSys_MemoryWrite_Check
                foreach(var cmd in rcvSettingData_JsonFormat)
                {
                    AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][saveSettingData_From_Framework_To_Check] cmdName = {cmd.commandName}, IsPerAddr = {cmd.isPerAddr}", AppLogLevel.Trace);
                    subSys_MemWrite_Check.AddOrUpdate(
                        cmd.commandName,
                        // 如果不存在 → 新增
                        new settingCommandRawData
                        {
                            IsPerAddr = cmd.isPerAddr,
                            TargetValue = (cmd.isPerAddr) ? null : cmd.targetValue?.ToList(),
                            AddrValues = (cmd.isPerAddr) ? cmd.addrValues?.Select(addrVal => addrVal.DeepClone()).ToList() : null
                        },
                        // 如果存在 → 更新
                        (key, oldValue) =>
                        {
                            oldValue.IsPerAddr = cmd.isPerAddr;
                            oldValue.TargetValue = (oldValue.IsPerAddr) ? null : cmd.targetValue?.ToList();
                            oldValue.AddrValues = (oldValue.IsPerAddr) ? cmd.addrValues?.Select(addrVal => addrVal.DeepClone()).ToList() : null;
                            return oldValue;
                        }
                    );
                }
                //3. 移除 subSys_MemWrite_Check 有但 rcvSettingData_JsonFormat沒有的 cmd
                var rcvCmdNames = rcvSettingData_JsonFormat.Select(c => c.commandName).ToHashSet();
                var toRemove = subSys_MemWrite_Check.Keys.Where(k => !rcvCmdNames.Contains(k)).ToList();
                foreach(var key in toRemove)
                {
                    AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][saveSettingData_From_Framework_To_Check] Remove cmdName = {key}", AppLogLevel.Trace);
                    subSys_MemWrite_Check.TryRemove(key, out _);
                }

                printData_WithJson(subSys_MemWrite_Check, "Check");

            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][saveSettingData_From_Framework_To_Check] error = {e}, ", AppLogLevel.Error);
            }
        }

        public void assignScalingFactor(oneDevice_Data? representDeviceData)
        {
            var subSys_MemWrite_Check = Setting_Check;
            var subSys_MemWrite_ScalingFactors = ScalingFactors;
            
            subSys_MemWrite_ScalingFactors.Clear();

            if(representDeviceData is null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][assignScalingFactor] representDeviceData is null", AppLogLevel.Trace);
                return;
            }

            foreach(var cmd in subSys_MemWrite_Check.Keys)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteAPI][assignScalingFactor] cmd = {cmd}", AppLogLevel.Trace);
                if(representDeviceData.AllCommandData.TryGetValue(cmd, out var GroupsData))
                {
                    var firstGroup = GroupsData.Groups[0];
                    subSys_MemWrite_ScalingFactors[cmd] = ScalingComputer.AutoSnapToDecimal_doubleVer((double)firstGroup.Scaling);
                    // _globalVar.Device_WriteData.ScalingFactors[cmd] = ScalingComputer.AutoSnapToDecimal_doubleVer((double)firstGroup.Scaling);
                    AppLogger.Log_To_File_log(_category, $"[WriteProcess][assignScalingFactor] scalingfactor = {subSys_MemWrite_ScalingFactors[cmd]}", AppLogLevel.Trace);
                }
            }

        }

        public void subAppSys_Clone_CheckToWrite()
        {
            var selectedPort_Write = Setting_Write;
            var selectedPort_Check = Setting_Check;

            //清除Write            
            selectedPort_Write.Clear(); //Perhaps it will cause memory leak, since the inner memory in the dictionary is still referenced by somewhere.

            try
            {
                //DeepClone from Check to Write
                foreach (var pair in selectedPort_Check)
                {
                    selectedPort_Write[pair.Key] = pair.Value.DeepClone();
                }

                printData_WithJson(selectedPort_Write, "Write");
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][subAppSys_Clone_CheckToWrite] clone from CHECK to WRITE is done", AppLogLevel.Trace);
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][subAppSys_Clone_CheckToWrite] error : {e}", AppLogLevel.Error);
            }
        }

        public void printData_WithJson(object tempOBJ, string message)
        {
            string json = JsonSerializer.Serialize(tempOBJ, new JsonSerializerOptions{WriteIndented = true});
            Console.WriteLine($"===[printData_WithJson : {message}]===");
            Console.WriteLine(json);
        }
        
        public bool compare_CHECK_and_WRITE()
        {
            if(Setting_Check.Count != Setting_Write.Count) return false;

            foreach(var (cmd_WRITE, cmdData_WRITE) in Setting_Write)
            {
                if(!Setting_Check.TryGetValue(cmd_WRITE, out var cmdData_CHECK)) return false;

                if(!cmdData_WRITE.Equals(cmdData_CHECK)) return false;
            }

            return true;
        }

    }

    public class WriteAPI_Datas
    {
        //For log
        private string _category;

        public List<SubAppSystem_WriteMemory> SubAppSystem_WriteMemorys {get; set;}

        public WriteAPI_Datas()
        {
            //For log
            _category = GetType().FullName!;

            //Declare object
            SubAppSystem_WriteMemorys = new List<SubAppSystem_WriteMemory>();
        }
    }

    
}