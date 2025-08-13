//1. 負責發送 READ_API, Write API
using demoVer.Models;

namespace demoVer.Services
{
    
    public class ApiManager
    {
        private readonly HttpClient _http;
        public ApiManager(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("ApiClient");
        }

        public async Task<List<SingleRawCommand_JsonFormat?>> apiRead_OneDeviceData(string type, uint addr)
        {
            try
            {
                string url = $"api/memory/read-memory?type={type}&addr={addr}";
                var result = await _http.GetFromJsonAsync<List<SingleRawCommand_JsonFormat>>(url);
                
                return result;

            }
            catch(Exception ex)
            {
                Console.WriteLine($"[ApiManager][ReadSingleINV] Error : {ex.Message}");
                return null;
            }
        }
        
    }
}