using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using demoVer.Models;


namespace demoVer.Utils
{
    public static class AppLogger
    {   
        public static readonly ConcurrentDictionary<string, int> CategoryLevels = 
            new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        public class LogLevelJsonFormat
        {
            string category {get; set;}
            int level {get; set;}
        }

        private static readonly string LogLevelSetting_Directory = Path.GetFullPath("StartupSetting");
        private static readonly string LogLevel_FilePath = Path.Combine(LogLevelSetting_Directory, "logLevels.json");
        public static bool DEBUG_MODE = true;
        
        private static readonly string LogDirectory = Path.GetFullPath("ProgramLog");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "log.json");
        private static readonly string LogFilePath_log = Path.Combine(LogDirectory, "log.log");
        
        public static void readLogLevelSettings()
        {
            try
            {
                string json = File.ReadAllText(LogLevel_FilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                Dictionary<string, int> rawJsonData = new Dictionary<string, int>();

                rawJsonData = JsonSerializer.Deserialize<Dictionary<string, int>>(json, options) ?? new();

                var rawJsonData_String = JsonSerializer.Serialize(rawJsonData, new JsonSerializerOptions{
                    WriteIndented = true
                });

                Console.WriteLine(rawJsonData_String);

                foreach(var pair in rawJsonData)
                {
                    
                    CategoryLevels[pair.Key] = pair.Value;
                }

                Console.WriteLine($"[readLogLevelSettings] CategoryLevels.Count = {CategoryLevels.Count}");
                Console.WriteLine("[readLogLevelSettings] done...");
            }
            catch(Exception e)
            {
                Console.WriteLine($"[readLogLevelSettings] Error : {e}");
            }
        }


        
        public static void Log(string message, bool local_debug)
        {
            if (DEBUG_MODE && local_debug)
            {
                Console.WriteLine($"[Debug] {message}");
            }
        }
        
        public static void Log_To_File_Json(object instance)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true
                };


                var logObject = new
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    data = instance
                };
                string tmp_json = JsonSerializer.Serialize(logObject, options);

                Console.WriteLine($"LogDirectory = {LogDirectory}");
                Console.WriteLine($"LogFilePath = {LogFilePath}");
                // 確保資料夾存在
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);

                File.WriteAllText(LogFilePath, tmp_json);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[Logger] Failed to write log: {ex.Message}");
            }
        }
        
        public static void Log_To_File_log(string category, string message, AppLogLevel level = AppLogLevel.Trace)
        {
            // 用 TryGetValue 避免兩次查表
            if (CategoryLevels.Count == 0) readLogLevelSettings();
            if (!CategoryLevels.TryGetValue(category, out var minLevel)) return;
            if (level < (AppLogLevel)minLevel) return;

            try
            {
                Directory.CreateDirectory(LogDirectory); // 多次呼叫也安全

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"[{timestamp}] {message}{Environment.NewLine}";

                // 跨行程命名 Mutex，序列化所有寫入
                // 建議名字帶上產品名稱，避免跟別的程式衝突
                using var mutex = new System.Threading.Mutex(false, @"Global\NTN_Energy_Management_LogMutex");

                // 最長等 200ms，避免卡死；你可依需求調整
                bool lockTaken = mutex.WaitOne(200);
                if (!lockTaken)
                {
                    // 拿不到鎖就放棄或改成丟例外／重試
                    return;
                }

                try
                {
                    // 用 FileStream 明確指定 Share 模式
                    using var fs = new FileStream(
                        LogFilePath_log,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite // 允許其他行程讀/寫同檔案（我們靠 Mutex 保證順序）
                    );
                    using var sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                    sw.Write(logEntry);
                    sw.Flush(); // 需要時可省略
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            catch (IOException ioEx)
            {
                // 可選：加入退避重試
                // for (int i=0; i<3; i++) { Thread.Sleep(10 << i); ... }

                Console.WriteLine($"[Logger][IO] Failed to write log: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger] Failed to write log: {ex.Message}");
            }
        }
    }

    public static class Custom
    {
        public static string getPortByAddr(uint addr)
        {
            if(addr >= 192)
            {
                return "MOD2";
            }
            if(addr >= 128)
            {
                return "MOD1";
            }
            if(addr >= 64)
            {
                return "CAN2";
            }
            if(addr >= 0)
            {
                return "CAN1";
            }
            return "MOD2";
        }
    }    

    public static class ScalingComputer
    {
        public static float DevideOperation(float value, float factor)
        {
            switch(AutoSnapToDecimal(factor))
            {
                case 0.001f: return value * 1000;
                case 0.01f:  return value * 100;
                case 0.1f:   return value * 10;
                case 1.0f:   return value * 1;
                // case 10f:    return value / 10;
                // case 100f:   return value / 100;
                default:    return -1;
            }
        }

        public static float MultOperation(float value, float factor)
        {   
            float temp_val = AutoSnapToDecimal(value);
            float temp_factor = AutoSnapToDecimal(factor);
            return AutoSnapToDecimal(temp_val * temp_factor, 3);
        }

        public static float AutoSnapToDecimal(float value, int decimals = 2)
        {
            float rounded = (float)Math.Round(value, decimals);
            float tolerance = MathF.Pow(10, -decimals) * 5;
            return Math.Abs(value - rounded) < tolerance ? rounded : value;
        }

        #region ComputationWithDoubleVer
        public static double DevideOperation_doubleVer(double value, double factor)
        {   
            Console.WriteLine($"[DevideOperation_doubleVer]value = {value}, factor = {factor}, processed_factor = {AutoSnapToDecimal_doubleVer(factor)}");
            switch(AutoSnapToDecimal_doubleVer(factor))
            {
                case 0.001: return AutoSnapToDecimal_doubleVer(value * 1000);
                case 0.01:  return AutoSnapToDecimal_doubleVer(value * 100);
                case 0.1:   return AutoSnapToDecimal_doubleVer(value * 10);
                case 1.0:   return AutoSnapToDecimal_doubleVer(value * 1);
                // case 10f:    return value / 10;
                // case 100f:   return value / 100;
                default:    return -1;
            }
        }

        public static double MultOperation_doubleVer(double value, double factor)
        {
            
            double return_val = AutoSnapToDecimal_doubleVer(value * factor, 3);
            // Console.WriteLine($"[Mult] value = {value}, factor = {factor}, return_val = {return_val}");
            return return_val;
        }

        public static double AutoSnapToDecimal_doubleVer(double value, int decimals = 2)
        {
            double rounded = (double)Math.Round(value, decimals);
            double tolerance = Math.Pow(10, -decimals) * 5;
            return Math.Abs(value - rounded) < tolerance ? rounded : value;
        }
        #endregion ComputationWithDoubleVer
    }
    
    public static class AppJsonManager
    {
        public static bool DEBUG_MODE = true;
        public static string Get_Writable_DataPath(string filename)
        {
            string basePath;
            // if(OperatingSystem.IsLinux())
            // {
            //     string home = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";
            //     basePath = Path.Combine(home, ".config", "App_Data");
            // }
            // else if(OperatingSystem.IsWindows())
            // {
            //     string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            //     basePath = Path.Combine(localAppData, "App_Data");
            // }
            // else
            // {
            //     basePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
            // }

            basePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");

            Directory.CreateDirectory(basePath);
            return Path.Combine(basePath, filename);
        }

        public static void SavePartialJson(string filePath, string key, object data)
        {
            JsonObject root;
            
            if(File.Exists(filePath))
            {
                var text = File.ReadAllText(filePath);
                root = JsonSerializer.Deserialize<JsonObject>(text) ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            var node = JsonSerializer.SerializeToNode(data);
            root[key] = node;

            var options = new JsonSerializerOptions{WriteIndented = true};

            File.WriteAllText(filePath, JsonSerializer.Serialize(root, options));
        }
    }
}
