using demoVer.Utils;
using System.Text.Json;
using System.Collections.Concurrent;
using demoVer.Services;

namespace demoVer.Models
{
    public class WriteAPI_Datas
    {
        //For log
        private string _category;

        //READ API Data Structure
        public ConcurrentDictionary<string, settingCommandRawData> Setting_CAN1_Check {get; set;}    //為檢查Write後，APP與Framework的data是否一致，所以要讀回來
        public ConcurrentDictionary<string, settingCommandRawData> Setting_CAN2_Check {get; set;}
        public ConcurrentDictionary<string, settingCommandRawData> Setting_MOD1_Check {get; set;}    //為檢查Write後，APP與Framework的data是否一致，所以要讀回來
        public ConcurrentDictionary<string, settingCommandRawData> Setting_MOD2_Check {get; set;}
        //Write API Data Structure
        public ConcurrentDictionary<string, settingCommandRawData> Setting_CAN1_Write {get; set;}    //準備寫到FrameWork的Write memory(CAN)
        public ConcurrentDictionary<string, settingCommandRawData> Setting_CAN2_Write {get; set;}
        public ConcurrentDictionary<string, settingCommandRawData> Setting_MOD1_Write {get; set;}    //準備寫到Framework的Write memory(MOD)
        public ConcurrentDictionary<string, settingCommandRawData> Setting_MOD2_Write {get; set;}

        public WriteAPI_Datas()
        {
            //For log
            _category = GetType().FullName!;

            //Declare object
            Setting_CAN1_Check              = new ConcurrentDictionary<string, settingCommandRawData>();
            Setting_CAN1_Write              = new ConcurrentDictionary<string, settingCommandRawData>();

            Setting_CAN2_Check              = new ConcurrentDictionary<string, settingCommandRawData>();
            Setting_CAN2_Write              = new ConcurrentDictionary<string, settingCommandRawData>();

            Setting_MOD1_Check              = new ConcurrentDictionary<string, settingCommandRawData>();
            Setting_MOD1_Write              = new ConcurrentDictionary<string, settingCommandRawData>();

            Setting_MOD2_Check              = new ConcurrentDictionary<string, settingCommandRawData>();
            Setting_MOD2_Write              = new ConcurrentDictionary<string, settingCommandRawData>();
        }

        public ConcurrentDictionary<string, settingCommandRawData> Get_CAN_or_MOD_by_port_selection(string port, string selection)
        {
            if(string.Equals(port, "CAN1", StringComparison.OrdinalIgnoreCase) && string.Equals(selection, "CHECK", StringComparison.OrdinalIgnoreCase))
            {
                //Get "CAN1, CHECK"
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][Get_CAN_or_MOD_by_port_selection] select CAN1, CHECK", AppLogLevel.Trace);
                return Setting_CAN1_Check;
            }

            if(string.Equals(port, "CAN1", StringComparison.OrdinalIgnoreCase) && string.Equals(selection, "WRITE", StringComparison.OrdinalIgnoreCase))
            {
                //Get "CAN1, WRITE"
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][Get_CAN_or_MOD_by_port_selection] select CAN1, WRITE", AppLogLevel.Trace);
                return Setting_CAN1_Write;
            }

            if(string.Equals(port, "CAN2", StringComparison.OrdinalIgnoreCase) && string.Equals(selection, "CHECK", StringComparison.OrdinalIgnoreCase))
            {
                //Get "CAN2, CHECK"
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][Get_CAN_or_MOD_by_port_selection] select CAN2, CHECK", AppLogLevel.Trace);
                return Setting_CAN2_Check;
            }

            if(string.Equals(port, "CAN2", StringComparison.OrdinalIgnoreCase) && string.Equals(selection, "WRITE", StringComparison.OrdinalIgnoreCase))
            {
                //Get "CAN2, WRITE"
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][Get_CAN_or_MOD_by_port_selection] select CAN2, WRITE", AppLogLevel.Trace);
                return Setting_CAN2_Write;
            }

            if(string.Equals(port, "MOD1", StringComparison.OrdinalIgnoreCase) && string.Equals(selection, "CHECK", StringComparison.OrdinalIgnoreCase))
            {
                //Get "MOD1, CHECK"
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][Get_CAN_or_MOD_by_port_selection] select MOD1, CHECK", AppLogLevel.Trace);
                return Setting_MOD1_Check;
            }

            if(string.Equals(port, "MOD1", StringComparison.OrdinalIgnoreCase) && string.Equals(selection, "WRITE", StringComparison.OrdinalIgnoreCase))
            {
                //Get "MOD1, WRITE"
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][Get_CAN_or_MOD_by_port_selection] select MOD1, WRITE", AppLogLevel.Trace);
                return Setting_MOD1_Write;
            }

            if(string.Equals(port, "MOD2", StringComparison.OrdinalIgnoreCase) && string.Equals(selection, "CHECK", StringComparison.OrdinalIgnoreCase))
            {
                //Get "MOD2, CHECK"
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][Get_CAN_or_MOD_by_port_selection] select MOD2, CHECK", AppLogLevel.Trace);
                return Setting_MOD2_Check;
            }

            if(string.Equals(port, "MOD2", StringComparison.OrdinalIgnoreCase) && string.Equals(selection, "WRITE", StringComparison.OrdinalIgnoreCase))
            {
                //Get "MOD2, WRITE"
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][Get_CAN_or_MOD_by_port_selection] select MOD2, WRITE", AppLogLevel.Trace);
                return Setting_MOD2_Write;
            }
            
            AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][Get_CAN_or_MOD_by_port_selection] select None", AppLogLevel.Trace);
            return null;
        }

        //儲存當前Write Memory的資料
        public void saveSettingData_ToCheck(string port, List<SingleRawSettingCommand_JsonFormat> rcvSettingData_JsonFormat)
        {
            //declare referencer
            ConcurrentDictionary<string, settingCommandRawData> selectedPort_Check;
            
            //reference by condition
            selectedPort_Check = Get_CAN_or_MOD_by_port_selection(port, "CHECK");
            if(selectedPort_Check is null) return;

            try
            {
                //1. save data by referencer
                foreach(var cmd in rcvSettingData_JsonFormat)
                {
                    AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][saveSettingData_ToCheck] cmdName = {cmd.commandName}, IsPerAddr = {cmd.isPerAddr}", AppLogLevel.Trace);
                    
                    selectedPort_Check.AddOrUpdate(
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
                
                //2. 移除 selectedPort_Check有但 rcvSettingData_JsonFormat沒有的 cmd
                var rcvCmdNames = rcvSettingData_JsonFormat.Select(c => c.commandName).ToHashSet();
                var toRemove = selectedPort_Check.Keys.Where(k => !rcvCmdNames.Contains(k)).ToList();
                foreach(var key in toRemove)
                {
                    AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][saveSettingData_ToCheck] Remove cmdName = {key}", AppLogLevel.Trace);
                    selectedPort_Check.TryRemove(key, out _);
                }

                printData_WithJson(selectedPort_Check, "Check");
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][saveSettingData_ToCheck] error : {e}", AppLogLevel.Error);
            }
        }

        public void printData_WithJson(object tempOBJ, string message)
        {
            string json = JsonSerializer.Serialize(tempOBJ, new JsonSerializerOptions{WriteIndented = true});
            Console.WriteLine($"===[printData_WithJson : {message}]===");
            Console.WriteLine(json);
        }

        // 取得當前可用的Write命令、格式有哪些(會在Write結構內改Value)
        public void clone_CheckToWrite(string port)
        {
            //declare referencer
            ConcurrentDictionary<string, settingCommandRawData> selectedPort_Write;
            ConcurrentDictionary<string, settingCommandRawData> selectedPort_Check;
            //reference by condition
            selectedPort_Write = Get_CAN_or_MOD_by_port_selection(port, "WRITE");
            selectedPort_Check = Get_CAN_or_MOD_by_port_selection(port, "CHECK");
            if(selectedPort_Write is null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][clone_CheckToWrite] {port}, WRITE structure is null", AppLogLevel.Trace);
                return;
            }

            if(selectedPort_Check is null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][clone_CheckToWrite] {port}, CHECK structure is null", AppLogLevel.Trace);
                return;
            }

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
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][clone_CheckToWrite] clone from CHECK to WRITE is done", AppLogLevel.Trace);
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][clone_CheckToWrite] error : {e}", AppLogLevel.Error);
            }
            
        }
        
        //從Write，取出一個cmd參考
        public settingCommandRawData getCmdData_Ref(string port, string cmdName)
        {
            ConcurrentDictionary<string, settingCommandRawData> selectedPort_Write;
            selectedPort_Write = Get_CAN_or_MOD_by_port_selection(port, "WRITE");

            //取出data
            try
            {
                if(selectedPort_Write.TryGetValue(cmdName, out var data))
                {
                    AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][getCmdData_Ref] Found {cmdName} Data", AppLogLevel.Trace);
                    return data;
                }
                else
                {
                    AppLogger.Log_To_File_log(_category, $"[WriteAPI_Datas][getCmdData_Ref] Not Found {cmdName} Data", AppLogLevel.Trace);
                    return null;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"[WriteAPI_Datas][getCmdData_Ref] Error : {e}");
                return null;
            }
        }

    }

    
}