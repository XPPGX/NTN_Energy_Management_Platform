//1. 負責發送 READ_API, Write API
using demoVer.Models;
using demoVer.Utils;

namespace demoVer.Services
{
    
    public class ApiManager
    {
        private string _category;
        private readonly HttpClient _http;
        public ApiManager(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("ApiClient");
            _category = GetType().FullName!;
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
            catch (HttpRequestException ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][ReadSingleINV] Http_Error: {ex}", AppLogLevel.Debug);
                return null;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][ReadSingleINV] Error : {ex.Message}", AppLogLevel.Error);
                return null;
            }
        }

        
        //簡易版Write_API :
        public async Task<bool> apiSimpleWrite_allDeviceON()
        {
            try
            {
                string url = "api/ops/power-on-all";
                var res = await _http.PostAsync(url, null);

                if(res.IsSuccessStatusCode)
                {
                    string resText = await res.Content.ReadAsStringAsync();
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiSimpleWrite_allDeviceON]成功: {resText}", AppLogLevel.Trace);
                    return true;
                }
                else
                {
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiSimpleWrite_allDeviceON]失敗: {res.StatusCode}", AppLogLevel.Trace);
                    return false;
                }
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiSimpleWrite_allDeviceON] Error : {ex.Message}", AppLogLevel.Error);
                return false;
            }
        }

        public async Task<bool> apiSimpleWrite_allDeviceOff()
        {
            try
            {
                string url = "api/ops/power-off-all";
                var res = await _http.PostAsync(url, null);

                if(res.IsSuccessStatusCode)
                {
                    string resText = await res.Content.ReadAsStringAsync();
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiSimpleWrite_allDeviceOff]成功: {resText}", AppLogLevel.Trace);
                    return true;
                }
                else
                {
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiSimpleWrite_allDeviceOff]失敗: {res.StatusCode}", AppLogLevel.Trace);
                    return false;
                }
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiSimpleWrite_allDeviceOff] Error : {ex.Message}", AppLogLevel.Error);
                return false;
            }
        }

        // public List<string> GetProtocolFilesForPort(string port)
        // {
        //     try
        //     {
        //         return null;
        //     }
        //     catch(Exception e)
        //     {
        //         return null;
        //     }
        // }

        
        // public async Task<List<SettingData>?> apiRead_SettingData(string type, string protocolFileName)
        // {
        //     // foreach(var port in ports)
        //     // {

        //     // }
        //     return null;
        // }
    }
}