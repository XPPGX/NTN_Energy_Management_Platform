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
        public async Task<List<string>?> GetTableColumn(string logType, CancellationToken ct = default)
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
                    targetTable = "datalog_NTN-5K_CAN"; //"datalog_NTN-5K_MOD" 與 "datalog_NTN-5K_CAN" 的欄位是一樣的
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
        public async Task<List<EventLog_Item>?> GetEventLog_WHERE_Time(DateTime? startTime, DateTime? endTime, int recordNum, CancellationToken ct = default)
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
                var list = new List<EventLog_Item>();
                

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
        public async Task<List<EventLog_Item>?> GetEvetLog_WHERE_Id(string direction, long? startId, long? endId, int recordNum, CancellationToken ct = default)
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
                var list = new List<EventLog_Item>();
                

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
        public async Task<List<DataLog_Item>?> GetDataLog_WHERE_Time(string targetTable, DateTime? startTime, DateTime? endTime, int recordNum, CancellationToken ct = default)
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
                var list = new List<DataLog_Item>();

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
                    list.Add(new DataLog_Item
                    {
                        Id                  = reader.GetInt64(reader.GetOrdinal("id")),             // long
                        Timestamp           = reader.IsDBNull(reader.GetOrdinal("timestamp")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("timestamp")),          // DateTime
                        Port                = reader.IsDBNull(reader.GetOrdinal("port")) ? (string?)null : reader.GetString(reader.GetOrdinal("port")),            // string
                        device_addr         = reader.IsDBNull(reader.GetOrdinal("device_addr")) ? null : reader.GetInt32(reader.GetOrdinal("device_addr")),             // int
                        READ_VIN            = reader.IsDBNull(reader.GetOrdinal("READ_VIN")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_VIN")),           // decimal
                        READ_IIN            = reader.IsDBNull(reader.GetOrdinal("READ_IIN")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_IIN")),
                        READ_TEMPERATURE_1  = reader.IsDBNull(reader.GetOrdinal("READ_TEMPERATURE_1")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_TEMPERATURE_1")),
                        READ_FAN_SPEED_1    = reader.IsDBNull(reader.GetOrdinal("READ_FAN_SPEED_1")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_FAN_SPEED_1")),
                        READ_FAN_SPEED_2    = reader.IsDBNull(reader.GetOrdinal("READ_FAN_SPEED_2")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_FAN_SPEED_2")),
                        READ_AC_VOUT        = reader.IsDBNull(reader.GetOrdinal("READ_AC_VOUT")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_AC_VOUT")),
                        READ_OP_WATT        = reader.IsDBNull(reader.GetOrdinal("READ_OP_WATT")) ? null : reader.GetDecimal(reader.GetOrdinal("READ_OP_WATT")),
                        READ_VBAT           = reader.IsDBNull(reader.GetOrdinal("READ_VBAT")) ? null : reader.GetDecimal(reader.GetOrdinal("READ_VBAT")),
                        READ_CHG_CURR       = reader.IsDBNull(reader.GetOrdinal("READ_CHG_CURR")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_CHG_CURR")),
                        READ_AC_IOUT        = reader.IsDBNull(reader.GetOrdinal("READ_AC_IOUT")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_AC_IOUT")),
                        MFR_MODEL           = reader.IsDBNull(reader.GetOrdinal("MFR_MODEL")) ? (string?)null : reader.GetString(reader.GetOrdinal("MFR_MODEL")),           // string
                        MFR_SERIAL          = reader.IsDBNull(reader.GetOrdinal("MFR_SERIAL")) ? (string?)null : reader.GetString(reader.GetOrdinal("MFR_SERIAL")),
                        INV_STATUS          = reader.IsDBNull(reader.GetOrdinal("INV_STATUS")) ? (string?)null : reader.GetString(reader.GetOrdinal("INV_STATUS")),
                        INV_FAULT           = reader.IsDBNull(reader.GetOrdinal("INV_FAULT")) ? (string?)null : reader.GetString(reader.GetOrdinal("INV_FAULT"))
                    });
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
        
        public async Task<List<DataLog_Item>?> GetDataLog_WHERE_Id(string targetTable, string direction, long? startId, long? endId, int recordNum, CancellationToken ct = default)
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
                var list = new List<DataLog_Item>();
                

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
                    list.Add(new DataLog_Item
                    {
                        Id                  = reader.GetInt64(reader.GetOrdinal("id")),             // long
                        Timestamp           = reader.IsDBNull(reader.GetOrdinal("timestamp")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("timestamp")),          // DateTime
                        Port                = reader.IsDBNull(reader.GetOrdinal("port")) ? (string?)null : reader.GetString(reader.GetOrdinal("port")),            // string
                        device_addr         = reader.IsDBNull(reader.GetOrdinal("device_addr")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("device_addr")),             // int
                        READ_VIN            = reader.IsDBNull(reader.GetOrdinal("READ_VIN")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_VIN")),           // decimal
                        READ_IIN            = reader.IsDBNull(reader.GetOrdinal("READ_IIN")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_IIN")),
                        READ_TEMPERATURE_1  = reader.IsDBNull(reader.GetOrdinal("READ_TEMPERATURE_1")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_TEMPERATURE_1")),
                        READ_FAN_SPEED_1    = reader.IsDBNull(reader.GetOrdinal("READ_FAN_SPEED_1")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_FAN_SPEED_1")),
                        READ_FAN_SPEED_2    = reader.IsDBNull(reader.GetOrdinal("READ_FAN_SPEED_2")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_FAN_SPEED_2")),
                        READ_AC_VOUT        = reader.IsDBNull(reader.GetOrdinal("READ_AC_VOUT")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_AC_VOUT")),
                        READ_OP_WATT        = reader.IsDBNull(reader.GetOrdinal("READ_OP_WATT")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_OP_WATT")),
                        READ_VBAT           = reader.IsDBNull(reader.GetOrdinal("READ_VBAT")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_VBAT")),
                        READ_CHG_CURR       = reader.IsDBNull(reader.GetOrdinal("READ_CHG_CURR")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_CHG_CURR")),
                        READ_AC_IOUT        = reader.IsDBNull(reader.GetOrdinal("READ_AC_IOUT")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("READ_AC_IOUT")),
                        MFR_MODEL           = reader.IsDBNull(reader.GetOrdinal("MFR_MODEL")) ? (string?)null : reader.GetString(reader.GetOrdinal("MFR_MODEL")),           // string
                        MFR_SERIAL          = reader.IsDBNull(reader.GetOrdinal("MFR_SERIAL")) ? (string?)null : reader.GetString(reader.GetOrdinal("MFR_SERIAL")),
                        INV_STATUS          = reader.IsDBNull(reader.GetOrdinal("INV_STATUS")) ? (string?)null : reader.GetString(reader.GetOrdinal("INV_STATUS")),
                        INV_FAULT           = reader.IsDBNull(reader.GetOrdinal("INV_FAULT")) ? (string?)null : reader.GetString(reader.GetOrdinal("INV_FAULT"))
                    });
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