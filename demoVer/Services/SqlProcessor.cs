using Npgsql;
using demoVer.Models;
using demoVer.Services;
using demoVer.Utils;

namespace demoVer.Services
{
    public class SqlProcessor
    {
        private string _category;
        private readonly string _connectionString;
        
        //5個semaphore lock
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5);
        //等待取鎖時間，最多3秒
        private const int maxWaitLock_Time = 3000;

        public SqlProcessor()
        {
            _category = GetType().FullName!;
            _connectionString = "Host=localhost;Username=linaro;Password=meanwell;Database=cmu3_main";
        }


        public async Task<List<EventLog_Item>>? GetEventLog(int startIndex, int recordNum, CancellationToken ct = default)
        {
            // 加入 CancellationToken 讓外部有可以中斷該Task的手段
            if(!await _semaphore.WaitAsync(maxWaitLock_Time, ct))
            {
                throw new TimeoutException($"[SqlProcessor][GetEventLog] 等待超過 {maxWaitLock_Time}ms，無法取得Semaphore Lock");
                return null;
            }

            try
            {
                var list = new List<EventLog_Item>();
                
                //1. SQL 連線
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog] SQL link success", AppLogLevel.Trace);

                //2. SQL query (目前這個命令只能查從頭開始的前五筆)
                var sql = $"SELECT * FROM \"event_log\" ORDER BY id LIMIT {recordNum} OFFSET {startIndex}";
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog] SQL Query success", AppLogLevel.Trace);

                //3. 組合資料
                while(await reader.ReadAsync(ct))
                {
                    list.Add(new EventLog_Item
                    {
                        Id              = reader.GetInt32(0),
                        Time            = reader.GetDateTime(1),
                        Port            = reader.GetString(2),
                        Addr            = reader.GetInt32(3),
                        SerialNumber    = reader.IsDBNull(4) ? null : reader.GetString(4),
                        CommandName     = reader.GetString(5),
                        Event           = reader.IsDBNull(6) ? null : reader.GetString(6),
                        TriggerState    = reader.GetString(7)
                    });
                }
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog] SQL Data Composition success", AppLogLevel.Trace);
                
                return list;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog] : Error: {ex}", AppLogLevel.Error);
                return null;
            }
            finally
            {
                _semaphore.Release();
            }   
        }
    }
}