using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Concurrent;
using demoVer.Models;
using demoVer.Utils;
using System.Text.Json;
using System.Threading.Tasks;

namespace demoVer.Services
{
    public class SubSystemManager
    {
        private string _category = "";

        private readonly ApiManager _apiManager;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, SubSystem>> RegistedSubSystems { get; set; } = new(); // 從LinkStatus API讀到的partition資訊判斷，相同port，相同protocol為同一子系統
        
        public SubSystemManager(ApiManager apiManager)
        {
            _category = GetType().FullName!;
            _apiManager = apiManager;
        }

        public List<SubSystem> GetAllSubSystems_Ref_In_List()
        {
            List<SubSystem> result = new List<SubSystem>();
            foreach (var protocolDict in RegistedSubSystems.Values)
            {
                foreach (var subsys in protocolDict.Values)
                {
                    result.Add(subsys);
                }
            }
            return result;
        }

        public async Task GetSubSystem_SettingRange(string port, string protocol)
        {
            try
            {
                if (RegistedSubSystems.TryGetValue(port, out var protocolDict))
                {
                    if (protocolDict.TryGetValue(protocol, out var subsys))
                    {
                        
                        // Call SettingRange API to get the latest ranges
                        var response = await _apiManager.apiRead_SettingRange(port, protocol);
                        if (response is null)
                        {
                            AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetSubSystem_SettingRange] response is null for Port:{port}, Protocol:{protocol}", AppLogLevel.Debug);
                            return;
                        }

                        // Update SubSystem's SettingRanges
                        var RangesDict = response.ranges;
                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetSubSystem_SettingRange] Start for (port, protocol) = ({port}, {protocol})", AppLogLevel.Debug);
                        subsys.UpdateSettingRanges_From(RangesDict);

                        //[Debug] 以 Json 格式印出 SettingRanges 
                        // var Json_Str = JsonSerializer.Serialize(subsys.SettingRanges, new JsonSerializerOptions{
                        //     WriteIndented = true
                        // });
                        // Console.WriteLine(Json_Str);

                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetSubSystem_SettingRange] done for (port, protocol) = ({port}, {protocol})", AppLogLevel.Debug);
                    }
                }
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetSubSystem_SettingRange] Exception: {e.Message}", AppLogLevel.Error);
            }
        }
    
        public async Task GetSubSystem_WriteCmdInfo(string port, string protocol)
        {
            try
            {
                if (RegistedSubSystems.TryGetValue(port, out var protocolDict))
                {
                    if (protocolDict.TryGetValue(protocol, out var subsys))
                    {
                        
                        // Call Get_Write_API to get the latest Write CMD Info
                        var response = await _apiManager.apiReadReal_SettingData(port, protocol);

                        // Check if response is null
                        if (response is null)
                        {
                            AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetSubSystem_WriteCmdInfo] response is null for Port:{port}, Protocol:{protocol}", AppLogLevel.Debug);
                            return;
                        }

                        // Check if the specific protocol data is null
                        if (response[protocol] is null)
                        {
                            AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetSubSystem_WriteCmdInfo] response[{protocol}] is null for Port:{port}, Protocol:{protocol}", AppLogLevel.Debug);
                            return;
                        }
                        
                        // Update SubSystem's Write CMD info using the overwrite approach.
                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetSubSystem_WriteCmdInfo] Start for (port, protocol) = ({port}, {protocol})", AppLogLevel.Debug);
                        subsys.UpdateInfosForWriteCmd_From(response[protocol]);

                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetSubSystem_WriteCmdInfo] done for (port, protocol) = ({port}, {protocol})", AppLogLevel.Debug);
                    }
                }
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetSubSystem_WriteCmdInfo] Exception: {e.Message}", AppLogLevel.Error);
            }
        }
    }
    
}