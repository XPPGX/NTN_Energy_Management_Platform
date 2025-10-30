//1. 負責發送 READ_API, Write API
using demoVer.Models;
using demoVer.Utils;
using System.Text.Json;

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

        //READ_API : 讀當前連線狀態
        public async Task<LinkStatus_JsonFormat?> apiRead_LinkStatus()
        {
            try
            {
                string url = $"api/memory/link-status";
                var result = await _http.GetFromJsonAsync<LinkStatus_JsonFormat>(url);
            
                if(result == null)
                {
                    throw new Exception($"[apiRead_LinkStatus] Read_API_JSON == null");
                }
                return result;
            }
            catch(HttpRequestException ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiRead_LinkStatus] Http_Error: {ex}", AppLogLevel.Debug);
                return null;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiRead_LinkStatus] Error: {ex}", AppLogLevel.Debug);
                return null;
            }
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


        //讀回settingData的格式、當前數值
        public async Task<List<SingleRawSettingCommand_JsonFormat>?> apiRead_SettingData(string type, string protocolFileName)
        {
            try
            {
                string url = $"api/memory/write-memory?type={type}&protocolFileName={protocolFileName}";
                var result = await _http.GetFromJsonAsync<List<SingleRawSettingCommand_JsonFormat>>(url);
                
                if(result == null)
                {
                     throw new Exception($"[apiRead_SettingData] Read_API_Json == null");
                }
                return result;

            }
            catch (HttpRequestException ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiRead_SettingData] Http_Error: {ex}", AppLogLevel.Debug);
                return null;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiRead_SettingData] Error : {ex.Message}", AppLogLevel.Error);
                return null;
            }
        }

        //按照讀回的格式、當前數值，寫入Framework的Write Memory
        public async Task<bool> apiWrite_SettingData(string type, string protocolFileName, List<SingleRawSettingCommand_JsonFormat> body)
        {
            try
            {
                string url = $"api/memory/write-memory?type={type}&protocolFileName={protocolFileName}";

                using var response = await _http.PostAsJsonAsync(url, body);
                
                if(response.IsSuccessStatusCode)
                {
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiWrite_SettingData]成功: {(int)response.StatusCode}", AppLogLevel.Trace);
                    return true;
                }
                else
                {
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiWrite_SettingData]失敗: {(int)response.StatusCode}", AppLogLevel.Trace);
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[WriteAPI] HttpRequestException: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WriteAPI] Unexpected Error: {ex.Message}");
                return false;
            }
        }

        //GET API READ_Memory_V1.1 : 讀 單個device的資料
        public async Task<Real_SingleDeviceData_JsonFormat> apiReadReal_OneDeviceData(string type, uint addr)
        {
            try
            {
                string url = $"api/memory/read-real?type={type}&addr={addr}";
                var result = await _http.GetFromJsonAsync<Real_SingleDeviceData_JsonFormat>(url);

                if(result == null)
                {
                    throw new Exception($"[apiRead_OneDeviceData] ReadReal_API_Json == null");
                }
                return result;

            }
            catch (HttpRequestException ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiReadReal_OneDeviceData] Http_Error: {ex}", AppLogLevel.Debug);
                return null;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiReadReal_OneDeviceData] Error : {ex.Message}", AppLogLevel.Error);
                return null;
            }
        }

        //GET API WRITE_Memory_V1.1 : 讀某個(port)的所有協定的所有命令格式、值
        public async Task<Dictionary<string, List<GET_RealSingleRawSettingCMD_JsonFormat>>> apiReadReal_SettingData_AllProtocol_Within_OnePort(string type)
        {
            try
            {
                if(string.IsNullOrEmpty(type))
                {
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiReadReal_SettingData_AllProtocol_Within_OnePort] type is invalid", AppLogLevel.Trace);
                    return null;
                }
                
                string url = $"api/memory/write-api?type={type}";
                var result = await _http.GetFromJsonAsync<Dictionary<string, List<GET_RealSingleRawSettingCMD_JsonFormat>>>(url);
                
                if(result == null)
                {
                     throw new Exception($"[ApiManager][apiReadReal_SettingData_AllProtocol_Within_OnePort] ReadReal_SettingData is null");
                }
                return result;
            }
            catch (HttpRequestException ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiReadReal_SettingData_AllProtocol_Within_OnePort] Http_Error: {ex}", AppLogLevel.Debug);
                return null;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiReadReal_SettingData_AllProtocol_Within_OnePort] Error : {ex.Message}", AppLogLevel.Error);
                return null;
            }
        }

        //GET API WRITE_Memory_V1.1 : 讀某個(Port, protocolFileName)所有命令格式、當前值
        public async Task<Dictionary<string, List<GET_RealSingleRawSettingCMD_JsonFormat>>> apiReadReal_SettingData(string type, string protocolFileName)
        {
            try
            {
                if(string.IsNullOrEmpty(type))
                {
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiReadReal_SettingData] type is invalid", AppLogLevel.Trace);
                    return null;
                }
                if(string.IsNullOrEmpty(protocolFileName))
                {
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiReadReal_SettingData] protocol is invalid", AppLogLevel.Trace);
                    return null;
                }
                
                
                string url = $"api/memory/write-api?type={type}&protocol={protocolFileName}";
                var result = await _http.GetFromJsonAsync<Dictionary<string, List<GET_RealSingleRawSettingCMD_JsonFormat>>>(url);
                
                if(result == null)
                {
                     throw new Exception($"[ApiManager][apiReadReal_SettingData] ReadReal_SettingData is null");
                }
                return result;
            }
            catch (HttpRequestException ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiReadReal_SettingData] Http_Error: {ex}", AppLogLevel.Debug);
                return null;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiReadReal_SettingData] Error : {ex.Message}", AppLogLevel.Error);
                return null;
            }
        }

        //POST API WRITE_Memory_V1.1 : 寫某個(port, protocolFileName)的某個(cmd)的值
        public async Task<bool> apiWrite_SetSingleCMD(Post_RealSingleRawSettingCMD_JsonFormat api_body, CancellationToken ct = default)
        {
            try
            {
                string url = $"api/memory/write-api";
                using var response = await _http.PostAsJsonAsync(url, api_body, ct);

                if (response.IsSuccessStatusCode)
                {
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiWrite_SetSingleCMD]成功: {(int)response.StatusCode}", AppLogLevel.Trace);
                    return true;
                }
                else
                {
                    var text = await response.Content.ReadAsStringAsync(ct);
                    AppLogger.Log_To_File_log(_category, $"[ApiManager][apiWrite_SetSingleCMD]失敗: {(int)response.StatusCode}, Failed Message : {text}", AppLogLevel.Trace);
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiWrite_SetSingleCMD] Http_Error: {ex}", AppLogLevel.Debug);
                return false;
            }
            catch (Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiWrite_SetSingleCMD] Error : {ex.Message}", AppLogLevel.Error);
                return false;
            }
        }
        
        public async Task<Setting_Range> apiRead_SettingRange(string type, string protocolFileName)
        {
            try
            {
                string url = $"/api/memory/partition-status?type={type}&protocol={protocolFileName}";
                var result = await _http.GetFromJsonAsync<Setting_Range>(url);

                if(result == null)
                {
                    throw new Exception($"[apiRead_SettingRange] Read_API_Json == null");
                }
                return result;

            }
            catch (HttpRequestException ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiRead_SettingRange] Http_Error: {ex}", AppLogLevel.Debug);
                return null;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[ApiManager][apiRead_SettingRange] Error : {ex.Message}", AppLogLevel.Error);
                return null;
            }
        }
    }
}