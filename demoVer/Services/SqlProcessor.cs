using Npgsql;
using demoVer.Models;
using demoVer.Services;
using demoVer.Utils;

namespace demoVer.Services
{
    public class SqlProcessor
    {
        private string _category;

        //資料庫指定連線：帳號、密碼、目標資料庫
        private readonly string _connectionString = "Host=localhost;Username=linaro;Password=meanwell;Database=cmu3_main";
        
        //5個semaphore lock
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5);
        //等待取鎖時間，最多3秒
        private const int maxWaitLock_Time = 3000;

        public SqlProcessor()
        {
            _category = GetType().FullName!;
        }

        //取得Table的Column header
        public async Task<List<string>?> GetTableColumn(string logType, string dataType = "", CancellationToken ct = default)
        {
            string targetTable = string.Empty;
            
            //嘗試取lock
            if(!await _semaphore.WaitAsync(maxWaitLock_Time, ct))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTableColumn] 等待超過 {maxWaitLock_Time}ms，無法取得Semaphore Lock", AppLogLevel.Trace);
                return null;
            }

            try
            {
                List<string> colsName = new();

                //參數檢查
                if(string.IsNullOrEmpty(logType))
                {
                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTableColumn] logType is Empty", AppLogLevel.Trace);
                    return null;
                }
                //參數檢查，指定targetTable
                if(string.Equals(logType, "event", StringComparison.OrdinalIgnoreCase))
                {
                    targetTable = "event_log";
                }
                else if(string.Equals(logType, "data", StringComparison.OrdinalIgnoreCase))
                {
                    if(string.Equals(dataType, "CAN", StringComparison.OrdinalIgnoreCase))
                    {
                        targetTable = "datalog_NTN-5K_CAN";
                    }
                    else if(string.Equals(dataType, "MOD", StringComparison.OrdinalIgnoreCase))
                    {
                        targetTable = "datalog_NTN-5K_MOD";
                    }
                }
                else
                {
                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTableColumn] logType is invalid", AppLogLevel.Trace);
                    return null;                    
                }

                //1. SQL 連線
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTableColumn] SQL link success", AppLogLevel.Trace);

                //2. 組合命令
                string sql = @"SELECT column_name FROM information_schema.columns WHERE table_name = @LOG_TYPE";
                await using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("LOG_TYPE", targetTable);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTableColumn] sql cmd ", AppLogLevel.Trace);

                //3. 撈ColName
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTableColumn] SQL Query success", AppLogLevel.Trace);
                
                //4. 組合ColName
                while(await reader.ReadAsync(ct))
                {
                    colsName.Add(reader.GetString(0));
                }
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTableColumn] column Composition success", AppLogLevel.Trace);
                return colsName;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTableColumn] Failed to Get Columns in the Table {targetTable}", AppLogLevel.Trace);
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<TableRangeResult?> GetTable_Range_AfterWHERE(string tableName, DateTime? startTime, DateTime? endTime, CancellationToken ct = default)
        {
            //輸入參數檢查
            if(startTime == DateTime.MinValue)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTable_Range_AfterWHERE] Failed, startTime 無效", AppLogLevel.Trace);
                return null;
            }
            if(endTime == DateTime.MinValue)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTable_Range_AfterWHERE] Failed, endTime 無效", AppLogLevel.Trace);
                return null;
            }
            if( string.IsNullOrEmpty(tableName) ||
                !(string.Equals(tableName, "event_log", StringComparison.Ordinal) || string.Equals(tableName, "datalog_NTN-5K_CAN", StringComparison.Ordinal) || string.Equals(tableName, "datalog_NTN-5K_MOD", StringComparison.Ordinal)))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTable_Range_AfterWHERE] Failed, tableName is NULL", AppLogLevel.Trace);
                return null;
            }

            //嘗試取lock
            if(!await _semaphore.WaitAsync(maxWaitLock_Time, ct))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTable_Range_AfterWHERE] 等待超過 {maxWaitLock_Time}ms，無法取得Semaphore Lock", AppLogLevel.Trace);
                return null;
            }
            
            string targetTable = "\"" + tableName + "\"";
            string timeColumn = tableName == "event_log" ? "time" : "timestamp";

            try
            {
                //1. SQL 連線
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTable_Range_AfterWHERE] SQL link success", AppLogLevel.Trace);

                string sql;
                await using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                //2. case判斷
                if(startTime is null && endTime is null)
                {   //case 1 : 非法參數
                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTable_Range_AfterWHERE] Failed : startTime 跟 endTime 同時為 NULL");
                    return null;
                }
                else if(startTime is null && endTime is not null)
                {   //case 2 : 從最早到 endTime
                    sql = $@"SELECT 
                                COUNT(*)    AS total_count,
                                MIN(id)     AS first_id,
                                MAX(id)     AS last_id
                            FROM {targetTable} WHERE ""{timeColumn}"" <= @endTime";
                    cmd.Parameters.AddWithValue("endTime", DateTime.SpecifyKind(endTime.Value, DateTimeKind.Utc));
                }
                else if(startTime is not null && endTime is null)
                {
                    //case 3 : 從 startTime 到最新
                    sql = $@"SELECT 
                                COUNT(*)    AS total_count,
                                MIN(id)     AS first_id,
                                MAX(id)     AS last_id
                            FROM {targetTable} WHERE ""{timeColumn}"" >= @startTime";
                    cmd.Parameters.AddWithValue("startTime", DateTime.SpecifyKind(startTime.Value, DateTimeKind.Utc));
                }
                else
                {
                    //case 4 : 區間查詢
                    sql = $@"SELECT
                                COUNT(*)    AS total_count,
                                MIN(id)     AS first_id,
                                MAX(id)     AS last_id 
                            FROM {targetTable} WHERE ""{timeColumn}"" BETWEEN @startTime AND @endTime";
                    cmd.Parameters.AddWithValue("startTime", DateTime.SpecifyKind(startTime.Value, DateTimeKind.Utc));
                    cmd.Parameters.AddWithValue("endTime", DateTime.SpecifyKind(endTime.Value, DateTimeKind.Utc));
                }
                
                cmd.CommandText = sql;

                //3. SQL query
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if(await reader.ReadAsync(ct))
                {
                    int count       = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    int? firstId    = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                    int? lastId     = reader.IsDBNull(2) ? null : reader.GetInt32(2);

                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTable_Range_AfterWHERE] SQL Query Done", AppLogLevel.Trace);
                    
                    return new TableRangeResult(count, firstId, lastId);
                }

                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTable_Range_AfterWHERE] SQL Query Fail", AppLogLevel.Trace);
                return null;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetTable_Range_AfterWHERE] Error : {e}", AppLogLevel.Trace);
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        //EventLog照篩選過的 時間 降序排列
        public async Task<List<Dictionary<string, object>>?> GetEventLog_WHERE_Time(DateTime? startTime, DateTime? endTime, int recordNum, CancellationToken ct = default)
        {
            //輸入參數檢查
            if(startTime == DateTime.MinValue)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] Failed, startTime 無效", AppLogLevel.Trace);
                return null;
            }
            if(endTime == DateTime.MinValue)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] Failed, endTime 無效", AppLogLevel.Trace);
                return null;
            }
            if(recordNum < 0)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] Failed, recordNum < 0", AppLogLevel.Trace);
                return null;
            }
            
            //嘗試取lock
            if(!await _semaphore.WaitAsync(maxWaitLock_Time, ct))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] 等待超過 {maxWaitLock_Time}ms，無法取得Semaphore Lock", AppLogLevel.Trace);
                return null;
            }

            //case 1. startTime == null, endTime == null, 非法參數直接跳出
            //case 2. startTime == null, endTime != null, 從頭撈資料到endTime之前
            //case 3. startTime != null, endTime == null, 從startTime撈資料到最尾端
            //case 4. startTime != null, endTime != null, 撈取特定區間的資料
            //嘗試撈資料
            try
            {
                var list = new List<Dictionary<string, object>>();
                

                //1. SQL 連線
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] SQL link success", AppLogLevel.Trace);

                string sql;
                await using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                //2. case判斷
                if(startTime is null && endTime is null)
                {   //case 1 : 非法參數
                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] Failed : startTime 跟 endTime 同時為 NULL");
                    return null;
                }
                else if(startTime is null && endTime is not null)
                {   //case 2 : 從最早到 endTime
                    sql = @"SELECT * FROM ""event_log"" WHERE ""time"" <= @endTime ORDER BY ""time"" DESC LIMIT @recordNum";
                    cmd.Parameters.AddWithValue("endTime", DateTime.SpecifyKind(endTime.Value, DateTimeKind.Utc));
                }
                else if(startTime is not null && endTime is null)
                {
                    //case 3 : 從 startTime 到最新
                    sql = @"SELECT * FROM ""event_log"" WHERE ""time"" >= @startTime ORDER BY ""time"" DESC LIMIT @recordNum";
                    cmd.Parameters.AddWithValue("startTime", DateTime.SpecifyKind(startTime.Value, DateTimeKind.Utc));
                }
                else
                {
                    //case 4 : 區間查詢
                    sql = @"SELECT * FROM ""event_log"" WHERE ""time"" BETWEEN @startTime AND @endTime ORDER BY ""time"" DESC LIMIT @recordNum";
                    cmd.Parameters.AddWithValue("startTime", DateTime.SpecifyKind(startTime.Value, DateTimeKind.Utc));
                    cmd.Parameters.AddWithValue("endTime", DateTime.SpecifyKind(endTime.Value, DateTimeKind.Utc));
                }
                cmd.Parameters.AddWithValue("recordNum", recordNum);
                cmd.CommandText = sql;

                //3. SQL query
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] SQL Query success", AppLogLevel.Trace);

                //4. 組合資料
                while(await reader.ReadAsync(ct))
                {
                    var row = new Dictionary<string, object>();

                    for(int i = 0 ; i < reader.FieldCount ; i ++)
                    {
                        string colName = reader.GetName(i);
                        object colValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[colName] = colValue;
                    }
                    list.Add(row);
                }
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] SQL Data Composition success", AppLogLevel.Trace);
                
                return list;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] Error : {e}", AppLogLevel.Trace);
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        //EventLog照篩選過的 Id 降序排列 (翻頁使用)
        public async Task<List<Dictionary<string, object>>?> GetEvetLog_WHERE_Id(string direction, long? startId, long? endId, int recordNum, CancellationToken ct = default)
        {
            //輸入參數檢查
            if(string.IsNullOrEmpty(direction))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEvetLog_WHERE_Id] Invalid Para : direction = {direction}", AppLogLevel.Trace);
                return null;
            }
            if(startId < 0 || endId < 0 || recordNum < 0)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEvetLog_WHERE_Id] Invalid Para : startId = {startId}, endId = {endId}, recordNum = {recordNum}", AppLogLevel.Trace);
                return null;
            }
            if(string.Equals(direction, "left", StringComparison.OrdinalIgnoreCase))
            {
                if(startId >= endId)
                {
                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEvetLog_WHERE_Id] Invalid Para : left, but startId >= endId ({startId} >= {endId})", AppLogLevel.Trace);
                    return null;
                }
            }
            else if(string.Equals(direction, "right", StringComparison.OrdinalIgnoreCase))
            {
                if(startId <= endId)
                {
                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEvetLog_WHERE_Id] Invalid Para : right, but startId <= endId ({startId} <= {endId})", AppLogLevel.Trace);
                    return null;
                }
            }

            //嘗試取lock
            if(!await _semaphore.WaitAsync(maxWaitLock_Time, ct))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEvetLog_WHERE_Id] 等待超過 {maxWaitLock_Time}ms，無法取得Semaphore Lock", AppLogLevel.Trace);
                return null;
            }

            //嘗試撈資料
            try
            {
                var list = new List<Dictionary<string, object>>();
                

                //1. SQL 連線
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEvetLog_WHERE_Id] SQL link success", AppLogLevel.Trace);

                string sql = string.Empty;
                await using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                
                //2. case判斷
                if(string.Equals(direction, "left", StringComparison.OrdinalIgnoreCase))
                {
                    sql = @"SELECT * FROM (SELECT * FROM ""event_log"" WHERE ""id"" > @startId AND ""id"" <= @endId ORDER BY id ASC LIMIT @recordNum) AS t ORDER BY id DESC";
                }
                else if(string.Equals(direction, "right", StringComparison.OrdinalIgnoreCase))
                {
                    sql = @"SELECT * FROM ""event_log"" WHERE ""id"" < @startId AND ""id"" >= @endId ORDER BY id DESC LIMIT @recordNum";
                }

                cmd.Parameters.AddWithValue("startId", startId);
                cmd.Parameters.AddWithValue("endId", endId);
                cmd.Parameters.AddWithValue("recordNum", recordNum);
                cmd.CommandText = sql;

                //3. SQL query
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEvetLog_WHERE_Id] SQL Query success", AppLogLevel.Trace);

                //4. 組合資料
                while(await reader.ReadAsync(ct))
                {
                    var row = new Dictionary<string, object>();

                    for(int i = 0 ; i < reader.FieldCount ; i ++)
                    {
                        string colName = reader.GetName(i);
                        object colValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[colName] = colValue;
                    }
                    list.Add(row);
                }
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEvetLog_WHERE_Id] SQL Data Composition success", AppLogLevel.Trace);
                
                return list;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEvetLog_WHERE_Id] Error : {e}", AppLogLevel.Trace);
                return null;
            }
            finally
            {
                _semaphore.Release();
            }

        }

        //DataLog照篩選過的 時間 降序排列
        public async Task<List<Dictionary<string, object>>?> GetDataLog_WHERE_Time(string targetTable, DateTime? startTime, DateTime? endTime, int recordNum, CancellationToken ct = default)
        {
            //輸入參數檢查
            if(startTime == DateTime.MinValue)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Time] Failed, startTime 無效", AppLogLevel.Trace);
                return null;
            }
            if(endTime == DateTime.MinValue)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Time] Failed, endTime 無效", AppLogLevel.Trace);
                return null;
            }
            if(recordNum < 0)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Time] Failed, recordNum < 0", AppLogLevel.Trace);
                return null;
            }
            if(string.IsNullOrEmpty(targetTable))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Time] Failed, targetTable = {targetTable}", AppLogLevel.Trace);
                return null;
            }


            //嘗試取lock
            if(!await _semaphore.WaitAsync(maxWaitLock_Time, ct))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Time] 等待超過 {maxWaitLock_Time}ms，無法取得Semaphore Lock", AppLogLevel.Trace);
                return null;
            }

            try
            {
                var list = new List<Dictionary<string, object>>();

                 //1. SQL 連線
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Time] SQL link success", AppLogLevel.Trace);

                string sql;
                await using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                //2. case判斷
                if(startTime is null && endTime is null)
                {   //case 1 : 非法參數
                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Time] Failed : startTime 跟 endTime 同時為 NULL");
                    return null;
                }
                else if(startTime is null && endTime is not null)
                {   //case 2 : 從最早到 endTime
                    
                    sql = $@"SELECT * FROM ""{targetTable}"" WHERE ""timestamp"" <= @endTime ORDER BY ""timestamp"" DESC LIMIT @recordNum";
                    cmd.Parameters.AddWithValue("endTime", DateTime.SpecifyKind(endTime.Value, DateTimeKind.Utc));
                }
                else if(startTime is not null && endTime is null)
                {
                    //case 3 : 從 startTime 到最新
                    sql = $@"SELECT * FROM ""{targetTable}"" WHERE ""timestamp"" >= @startTime ORDER BY ""timestamp"" DESC LIMIT @recordNum";
                    cmd.Parameters.AddWithValue("startTime", DateTime.SpecifyKind(startTime.Value, DateTimeKind.Utc));
                }
                else
                {
                    //case 4 : 區間查詢
                    sql = $@"SELECT * FROM ""{targetTable}"" WHERE ""timestamp"" BETWEEN @startTime AND @endTime ORDER BY ""timestamp"" DESC LIMIT @recordNum";
                    cmd.Parameters.AddWithValue("startTime", DateTime.SpecifyKind(startTime.Value, DateTimeKind.Utc));
                    cmd.Parameters.AddWithValue("endTime", DateTime.SpecifyKind(endTime.Value, DateTimeKind.Utc));
                }
                cmd.Parameters.AddWithValue("recordNum", recordNum);
                cmd.CommandText = sql;
                
                //3. SQL query
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Time] SQL Query success", AppLogLevel.Trace);

                //4. 組合資料
                // while (await reader.ReadAsync(ct))
                // {
                //     for (int i = 0; i < reader.FieldCount; i++)
                //     {
                //         string colName  = reader.GetName(i);           // 欄位名稱
                //         Type   colType  = reader.GetFieldType(i);      // C# 對應型別
                //         object colValue = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i);

                //         Console.WriteLine($"[{i}] {colName} ({colType.Name}) = {colValue}");
                //     }

                //     Console.WriteLine("──────────────────────");
                // }
                while(await reader.ReadAsync(ct))
                {
                    var row = new Dictionary<string, object>();

                    for(int i = 0 ; i < reader.FieldCount ; i ++)
                    {
                        string colName = reader.GetName(i);
                        object colValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[colName] = colValue;
                    }
                    list.Add(row);
                }
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Time] SQL Data Composition success", AppLogLevel.Trace);

                return list;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetEventLog_WHERE_Time] Error : {e}", AppLogLevel.Trace);
                return null;
            }
            finally
            {
                _semaphore.Release();
            }   
        }
        
        public async Task<List<Dictionary<string, object>>?> GetDataLog_WHERE_Id(string targetTable, string direction, long? startId, long? endId, int recordNum, CancellationToken ct = default)
        {
            //輸入參數檢查
            if(string.IsNullOrEmpty(direction))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] Invalid Para : direction = {direction}", AppLogLevel.Trace);
                return null;
            }
            if(startId < 0 || endId < 0 || recordNum < 0)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] Invalid Para : startId = {startId}, endId = {endId}, recordNum = {recordNum}", AppLogLevel.Trace);
                return null;
            }
            if(string.Equals(direction, "left", StringComparison.OrdinalIgnoreCase))
            {
                if(startId >= endId)
                {
                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] Invalid Para : left, but startId >= endId ({startId} > {endId})", AppLogLevel.Trace);
                    return null;
                }
            }
            else if(string.Equals(direction, "right", StringComparison.OrdinalIgnoreCase))
            {
                if(startId <= endId)
                {
                    AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] Invalid Para : right, but startId <= endId ({startId} < {endId})", AppLogLevel.Trace);
                    return null;
                }
            }
            if(string.IsNullOrEmpty(targetTable))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] Invalid Para : targetTable is null or empty", AppLogLevel.Trace);
                return null;
            }

            //嘗試取lock
            if(!await _semaphore.WaitAsync(maxWaitLock_Time, ct))
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] 等待超過 {maxWaitLock_Time}ms，無法取得Semaphore Lock", AppLogLevel.Trace);
                return null;
            }

            //嘗試撈資料
            try
            {
                var list = new List<Dictionary<string, object>>();
                

                //1. SQL 連線
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] SQL link success", AppLogLevel.Trace);

                string sql = string.Empty;
                await using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                
                //2. case判斷
                if(string.Equals(direction, "left", StringComparison.OrdinalIgnoreCase))
                {
                    sql = $@"SELECT * FROM (SELECT * FROM ""{targetTable}"" WHERE ""id"" > @startId AND ""id"" <= @endId ORDER BY id ASC LIMIT @recordNum) AS t ORDER BY id DESC";
                    // sql = @"SELECT * FROM (SELECT * FROM ""event_log"" WHERE ""id"" > @startId AND ""id"" <= @endId ORDER BY id ASC LIMIT @recordNum) AS t ORDER BY id DESC";
                }
                else if(string.Equals(direction, "right", StringComparison.OrdinalIgnoreCase))
                {
                    sql = $@"SELECT * FROM ""{targetTable}"" WHERE ""id"" < @startId AND ""id"" >= @endId ORDER BY id DESC LIMIT @recordNum";
                    // sql = @"SELECT * FROM ""event_log"" WHERE ""id"" < @startId AND ""id"" >= @endId ORDER BY id DESC LIMIT @recordNum";
                }

                cmd.Parameters.AddWithValue("startId", startId);
                cmd.Parameters.AddWithValue("endId", endId);
                cmd.Parameters.AddWithValue("recordNum", recordNum);
                cmd.CommandText = sql;

                //3. SQL query
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] SQL Query success", AppLogLevel.Trace);

                //4. 組合資料
                while(await reader.ReadAsync(ct))
                {
                    var row = new Dictionary<string, object>();

                    for(int i = 0 ; i < reader.FieldCount ; i ++)
                    {
                        string colName = reader.GetName(i);
                        object colValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[colName] = colValue;
                    }
                    list.Add(row);
                }
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] SQL Data Composition success", AppLogLevel.Trace);
                
                return list;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SqlProcessor][GetDataLog_WHERE_Id] Error : {e}", AppLogLevel.Trace);
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}