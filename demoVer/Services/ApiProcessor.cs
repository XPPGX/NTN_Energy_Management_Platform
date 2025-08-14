//1. 負責發送 READ_API, Write API
using demoVer.Models;
using demoVer.Utils;

namespace demoVer.Services
{
    
    public class ApiManager
    {
        private readonly HttpClient _http;
        public ApiManager(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("ApiClient");
        }


        //READ_API : 讀 單個 device 的 資料
        public async Task<List<SingleRawCommand_JsonFormat?>> apiRead_OneDeviceData(string type, uint addr)
        {
            try
            {
                string url = $"api/memory/read-memory?type={type}&addr={addr}";
                var result = await _http.GetFromJsonAsync<List<SingleRawCommand_JsonFormat>>(url);
                
                if(result == null)
                {
                     throw new Exception($"[apiRead_OneDeviceData] Read_API_Json == null");
                }
                return result;

            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log($"[ApiManager][ReadSingleINV] Error : {ex.Message}");
                return null;
            }
        }

        
        public List<string> GetProtocolFilesForPort(string port)
        {
            try
            {
                return null;
            }
            catch(Exception e)
            {
                return null;
            }
        }

        
        public async Task<List<SettingData>?> apiRead_SettingData(string type, string protocolFileName)
        {
            // foreach(var port in ports)
            // {

            // }
            return null;
        }
    }
}