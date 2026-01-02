using demoVer.Models;
using demoVer.Utils;
using System.Text.Json;
using System.Runtime.InteropServices;
using System;
using MudBlazor;

using System.Collections.Immutable;
using demoVer.Shared;

namespace demoVer.Services
{
    public class BottomRowConfig
    {
        public const int _MaxDisplayNameLength = 7;
        private const int _MaxItemCount = 4;
        private string _category = "";


        public string _DirName = string.Empty; // Set in SetPath()
        private string _appPort = string.Empty; // Set in SetPath()
        private string _fileName = string.Empty; // Set in SetPath()
        private string _fullPath = string.Empty; // Set in SetPath()

        public event EventHandler? OnConfigChanged;

        /// <summary>
        /// 這邊寫各模組用到的 key => route 對照表
        /// key : 要給使用者看的頁面名稱，讓使用者選擇，比如 "NTN_Home", "INV_Setting"
        /// route : 實際的頁面路由，比如 "/", "/Inverter_setting"
        /// 這個字典初始化後不可修改!!!!
        /// </summary>
        private ImmutableDictionary<string, string> FixedRouteToKeyMapping {get; } = ImmutableDictionary.CreateRange(new Dictionary<string, string>()
        {
            {"/", "NTN_Home"},
            {"/Battery_setting", "BAT_Setting"},
            {"/Inverter_setting", "INV_Setting"},
            {"/Link_status", "Link_Status"},
            {"/Log", "Log"},
        });
        private List<BottomRowItem> List_UserDefined {get; set;} = new List<BottomRowItem>(_MaxItemCount);
        
        public BottomRowConfig()
        {
            _category = GetType().FullName!;
            for(int i = 0 ; i < _MaxItemCount ; i ++)
            {
                List_UserDefined.Add(new BottomRowItem());
            }
        }
        
        /// <summary>
        /// 初始化流程如下:
        /// 1. 設定路徑
        /// 2. 從檔案讀取設定，成功則結束
        /// 
        /// 3. 若讀取失敗則使用預設值
        /// 4. 若使用預設值則寫入檔案
        /// 5. 廣播設定已變更事件
        /// </summary>
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

        private ConfigResult WriteJsonFile<T>(string filePath, T content)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(filePath, JsonSerializer.Serialize(content, options));
                return ConfigResult.Success;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"WriteJsonFile 發生未預期錯誤。Exception: {ex}", AppLogLevel.Error);
                return ConfigResult.FileWriteError;
            }
        }

        /// <summary>
        /// List_UserDefined 的單一項目更新
        /// </summary>
        /// <param name="index">第幾個element</param>
        /// <param name="Route">跳轉路徑</param>
        /// <param name="DisplayName">User設定的顯示名稱</param>
        /// <returns>回傳enum錯誤代碼</returns>
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
        
        public ConfigResult UpdateAllItem(List<BottomRowItem> newList)
        {
            try
            {
                //1. Check element count
                if(newList.Count != _MaxItemCount)
                {
                    return ConfigResult.OverListCount;
                }

                //2. Update each item
                ConfigResult res;
                res = UpdateOneItem(0, newList[0].Route, newList[0].DisplayName);
                if(res != ConfigResult.Success) return res;
                res = UpdateOneItem(1, newList[1].Route, newList[1].DisplayName);
                if(res != ConfigResult.Success) return res;
                res = UpdateOneItem(2, newList[2].Route, newList[2].DisplayName);
                if(res != ConfigResult.Success) return res;
                res = UpdateOneItem(3, newList[3].Route, newList[3].DisplayName);
                if(res != ConfigResult.Success) return res;

                return ConfigResult.Success;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"UpdateListItem 發生未預期錯誤。Exception: {ex}", AppLogLevel.Error);
                return ConfigResult.UnknownError;   
            }
        }

        /// <summary>
        /// 要改預設值請在這裡修改
        /// Default Config:
        /// List[0] : Route = "/", DisplayName = "Home"
        /// </summary>
        public void DefaultConfig()
        {
            ConfigResult res; //just using for checking whether success or not in each step
            
            res = UpdateOneItem(0, "/", "Home");
            res = UpdateOneItem(1, "/Inverter_Setting", "INV");
            res = UpdateOneItem(2, "/Battery_Setting", "BAT");
            res = UpdateOneItem(3, "/Link_Status", "Linking");
        }

        public Dictionary<string, string> Get_FixedRouteToKeyMapping_snapshot()
        {
            return FixedRouteToKeyMapping.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public List<BottomRowItem> Get_UserDefinedList_snapshot()
        {
            // 深拷贝：创建新的 BottomRowItem 对象，而不是引用同一个对象
            return List_UserDefined.Select(item => new BottomRowItem 
            { 
                Route = item.Route, 
                DisplayName = item.DisplayName 
            }).ToList();
        }
        
        public ConfigResult UpdateAndSave(List<BottomRowItem> newList)
        {
            ConfigResult res = UpdateAllItem(newList);
            if(res != ConfigResult.Success)
            {
                return res;
            }

            res = WriteJsonFile(_fullPath, List_UserDefined);
            
            if(res == ConfigResult.Success)
            {
                // 触发配置变更事件
                OnConfigChanged?.Invoke(this, EventArgs.Empty);
            }
            
            return res;
        }
        public ConfigResult DefaultAndSave()
        {
            DefaultConfig();
            ConfigResult res = WriteJsonFile(_fullPath, List_UserDefined);
            
            if(res == ConfigResult.Success)
            {
                // 触发配置变更事件
                OnConfigChanged?.Invoke(this, EventArgs.Empty);
            }
            
            return res;
        }
        private void Debug()
        {
            foreach(var item in List_UserDefined)
            {
                Console.WriteLine($"Route: {item.Route}         | DisplayName: {item.DisplayName}");
            }
        }
    
        
    }

    public class BottomRowItem
    {
        public string Route {get; set;} = string.Empty; // Page Route
        public string DisplayName {get; set;} = string.Empty; // User Defined Name
    }   
}