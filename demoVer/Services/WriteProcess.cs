using demoVer.Models;
using demoVer.Utils;
using demoVer.Interfaces;

namespace demoVer.Services
{
    public class WriteProcess
    {
        //For log
        private string _category;
        
        //injections
        private readonly GlobalVar _globalVar;
        private readonly ApiManager _apiManager;
        
        //Variables
        private bool IsWriteSuccess;
        
        public WriteProcess(GlobalVar globalVar,
                            ApiManager apiManager)
        {
            _globalVar = globalVar;
            _apiManager = apiManager;
        }

        //把要發送的資料，存進對應port的Write structure裡面
        public void Write_To_Framework(string port)
        {
            var selectedPort_Write = _globalVar.Device_WriteData.Get_CAN_or_MOD_by_port_selection(port, "WRITE");
            if(selectedPort_Write is null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][Write_To_Framework] {port}, WRITE structure is null", AppLogLevel.Trace);
                return;
            }

            
        }

        //把目標port的Write structure生成 WriteAPI 需要的 body
        public List<SingleRawSettingCommand_JsonFormat> Generate_WriteFormat(string port)
        {
            var selectedPort_Write = _globalVar.Device_WriteData.Get_CAN_or_MOD_by_port_selection(port, "WRITE");
            if(selectedPort_Write is null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteProcess][Generate_WriteFormat] {port}, WRITE structure is null", AppLogLevel.Trace);
                return null;
            }

            List<SingleRawSettingCommand_JsonFormat> Write_Body = new List<SingleRawSettingCommand_JsonFormat>();
            foreach(var pair in selectedPort_Write)
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
            
            return Write_Body;
        }
    }
}