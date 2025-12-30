using demoVer.Models;
using demoVer.Utils;
using System.Text.Json;
using System.Runtime.InteropServices;
using System;
using MudBlazor;

namespace demoVer.Services
{
    public class BottomRowConfig
    {
        private const int _MaxDisplayNameLength = 7;
        private const int _MaxItemCount = 4;
        private string _category = "";


        public string _DirName = string.Empty; // Set in SetPath()
        private string _appPort = string.Empty; // Set in SetPath()
        private string _fileName = string.Empty; // Set in SetPath()
        private string _fullPath = string.Empty; // Set in SetPath()

        public event EventHandler? OnConfigChanged;        
        private Dictionary<string, string> Mapping_Key_To_Route_Fixed {get; set;} = new Dictionary<string, string>()
        {
            {"NTN_Home", "/"},
            {"INV_Setting", "/Inverter_Setting"},
            {"BAT_Setting", "/Battery_Setting"},
            {"Link_Status", "/Link_Status"},
            {"Log", "/Log"},
        };
        private List<BottomRowItem> List_UserDefined {get; set;} = new List<BottomRowItem>(_MaxItemCount);
        
        public BottomRowConfig()
        {
            _category = GetType().FullName!;
            for(int i = 0 ; i < _MaxItemCount ; i ++)
            {
                List_UserDefined.Add(new BottomRowItem());
            }
        }
        
        public void Init()
        {
            try
            {
                //0. Set Base Path
                SetPath();

                //1. Read from file
                _fullPath = System.IO.Path.Combine(_DirName, _appPort, _fileName);
                var readRes = ReadFromFile(_fullPath, out var readlist);
                if(readRes == ConfigResult.Success && readlist != null)
                {
                    List_UserDefined = readlist;
                    AppLogger.Log_To_File_log(_category, "BottomRowConfig 從檔案載入設定成功。", AppLogLevel.Debug);
                    return;
                }
                
                //2. fail to read from file, use Default
                DefaultConfig(); //The List_UserDefined is default now. 
                
                //3. Write to file if using Default
                WriteJsonFile(_fullPath, List_UserDefined);

                //4. Notify Config Changed
                OnConfigChanged?.Invoke(this, EventArgs.Empty);

                //Debug
                Debug();
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, () => $"BottomRowConfig 初始化發生未預期錯誤。Exception: {ex}", AppLogLevel.Error);
            }
        }

        public void SetPath()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
               _DirName = "/userdata/CMU3/UserSetting";
               _appPort = "5040";
               _fileName = "BottomRowConfig.json";
               _fullPath = System.IO.Path.Combine(_DirName, _appPort, _fileName);
            }
            else
            {
                _DirName = "UserSetting";
                _appPort = "5040";
                _fileName = "BottomRowConfig.json";
                _fullPath = System.IO.Path.Combine(_DirName, _appPort, _fileName);
            }
        }

        public ConfigResult ReadFromFile(string filePath, out List<BottomRowItem>? readResult)
        {
            List<BottomRowItem> result = new List<BottomRowItem>();
            try
            {
                if(!File.Exists(filePath))
                {
                    AppLogger.Log_To_File_log(_category, $"ReadFromFile 檔案不存在。Path: {filePath}", AppLogLevel.Warning);
                    readResult = null;
                    return ConfigResult.FileNotExist;
                }
                
                string jsonString = File.ReadAllText(filePath);
                result = JsonSerializer.Deserialize<List<BottomRowItem>>(jsonString) ?? new List<BottomRowItem>();
                if(result.Count < 0 || result.Count > _MaxItemCount)
                {
                    readResult = null;
                    return ConfigResult.FileContentError;
                }
                
                readResult = result;
                return ConfigResult.Success;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);

                readResult = null;
                return ConfigResult.UnknownError;
            }
        }

        private void WriteJsonFile<T>(string filePath, T content)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, JsonSerializer.Serialize(content, options));
        }

        public ConfigResult UpdateOneItem(int index, string Route, string DisplayName)
        {
            if(index < 0 || index >= _MaxItemCount)
            {
                return ConfigResult.OverListCount;
            }
            
            if(DisplayName.Length > _MaxDisplayNameLength)
            {
                return ConfigResult.OverStringLength;
            }

            try
            {
                List_UserDefined[index].Route = Route;
                List_UserDefined[index].DisplayName = DisplayName;
                return ConfigResult.Success;
            }
            catch(Exception ex)
            {   
                AppLogger.Log_To_File_log(_category, $"UpdateOneItem 發生未預期錯誤。Exception: {ex}", AppLogLevel.Error);
                return ConfigResult.UnknownError;   
            }
        }
        

        public void DefaultConfig()
        {
            ConfigResult res; //just using for checking whether success or not in each step
            
            res = UpdateOneItem(0, "/", "Home");
            res = UpdateOneItem(1, "/Inverter_Setting", "INV");
            res = UpdateOneItem(2, "/Battery_Setting", "BAT");
            res = UpdateOneItem(3, "/Link_Status", "Linking");
        }

        
        private void Debug()
        {
            foreach(var item in List_UserDefined)
            {
                Console.WriteLine($"Route: {item.Route}, DisplayName: {item.DisplayName}");
            }
        }
    }

    public class BottomRowItem
    {
        public string Route {get; set;} = string.Empty; // Page Route
        public string DisplayName {get; set;} = string.Empty; // User Defined Name
    }

    public enum ConfigResult
    {
        Success = 0,
        FileNotExist = 1,
        FileContentError = 2,
        OverListCount = 3, //List.Count = 4
        OverStringLength = 4, //DisplayName Length should be <= 7
        UnknownError = -99,
    }
}