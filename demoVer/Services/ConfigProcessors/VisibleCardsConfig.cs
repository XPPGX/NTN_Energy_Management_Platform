using demoVer.Models;
using demoVer.Utils;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace demoVer.Services
{
    public class VisibleCardsConfig
    {
        private string _category = "";

        public string _DirName = string.Empty;
        public string _appPort = string.Empty;
        private string _fileName = string.Empty;
        private string _fullPath = string.Empty;

        private List<CardInfo> _visibleCards_In_memory = new List<CardInfo>();

        public VisibleCardsConfig()
        {
            _category = GetType().FullName!;
        }

        public void Init()
        {
            try
            {
                // 0. Set path
                SetPath();
                
                // 1. Read from file
                var readRes = ReadFromFile(_fullPath, out var readCards);
                if(readRes == ConfigResult.Success && readCards is not null)
                {
                    _visibleCards_In_memory = readCards;
                    AppLogger.Log_To_File_log(_category, "[VisibleCardsConfig] 從檔案載入設定成功", AppLogLevel.Debug);
                    return;
                } 

                // 2. Fail to read from file, use default config
                DefaultConfig();
                
                // 3. Write to file if using default
                WriteJsonFile(_fullPath, _visibleCards_In_memory);
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[VisibleCardsConfig] 初始化發生未預期錯誤。Exception: {ex}", AppLogLevel.Error);
            }
        }

        public void SetPath()
        {
            _appPort = "5040";
            _fileName = "visibleCards.json";
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _DirName = "/userdata/CMU3/UserSetting";
            }
            else
            {
                _DirName = "UserSetting";
            }
            _fullPath = Path.Combine(_DirName, _appPort, _fileName);
        }

        public ConfigResult ReadFromFile(string filePath, out List<CardInfo>? readInfos)
        {
            List<CardInfo> result = new List<CardInfo>();
            try
            {
                if(!File.Exists(filePath))
                {
                    AppLogger.Log_To_File_log(_category, $"[VisibleCardsConfig][ReadFromFile] 檔案不存在。Path: {filePath}", AppLogLevel.Warning);
                    readInfos = null;
                    return ConfigResult.FileNotExist;
                }

                string jsonString = File.ReadAllText(filePath);
                result = JsonSerializer.Deserialize<List<CardInfo>>(jsonString) ?? new List<CardInfo>();
                
                if(result.Count == 0)
                {
                    readInfos = null;
                    return ConfigResult.FileContentError;
                }
                
                result = result.OrderBy(info => info.showSequence).ToList();
                readInfos = result;
                
                return ConfigResult.Success;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[VisibleCardsConfig][ReadFromFile] 讀取失敗: {ex.Message}", AppLogLevel.Error);
                readInfos = null;
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
                AppLogger.Log_To_File_log(_category, $"[VisibleCardsConfig][WriteJsonFile] 發生未預期錯誤。Exception: {ex}", AppLogLevel.Error);
                return ConfigResult.FileWriteError;
            }
        }

        /// <summary>
        /// Load default configuration from hardcoded JSON
        /// </summary>
        public void DefaultConfig()
        {
            try
            {
                // Get hardcoded default configuration from static class
                string jsonContent = VisibleCardsConfigDefaults.GetDefaultJson();
                
                // Deserialize to validate and load into memory
                List<CardInfo>? defaultCards = JsonSerializer.Deserialize<List<CardInfo>>(jsonContent);
                
                if(defaultCards is null)
                {
                    AppLogger.Log_To_File_log(_category, "[VisibleCardsConfig][DefaultConfig] 預設配置反序列化失敗", AppLogLevel.Error);
                    return;
                }

                // Update in-memory data
                _visibleCards_In_memory = defaultCards;
                
                AppLogger.Log_To_File_log(_category, "[VisibleCardsConfig][DefaultConfig] 已載入預設配置", AppLogLevel.Information);
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[VisibleCardsConfig][DefaultConfig] 失敗: {ex.Message}", AppLogLevel.Error);
            }
        }

        public ConfigResult UpdateAndSave(List<CardInfo>? newConfig)
        {
            if(newConfig is null || newConfig.Count == 0)
            {
                AppLogger.Log_To_File_log(_category, "[VisibleCardsConfig][UpdateAndSave] 更新失敗: 提供的配置為空", AppLogLevel.Warning);
                return ConfigResult.FileContentError;
            }

            try
            {
                // Update in-memory data
                _visibleCards_In_memory = newConfig;

                // Write to file
                ConfigResult res = WriteJsonFile(_fullPath, newConfig);
                
                if(res == ConfigResult.Success)
                {
                    AppLogger.Log_To_File_log(_category, $"[VisibleCardsConfig][UpdateAndSave] 配置已更新並保存到: {_fullPath}", AppLogLevel.Information);
                }
                
                return res;
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[VisibleCardsConfig][UpdateAndSave] 發生未預期錯誤。Exception: {ex}", AppLogLevel.Error);
                return ConfigResult.UnknownError;
            }
        }

        public ConfigResult DefaultAndSave()
        {
            DefaultConfig();
            ConfigResult res = WriteJsonFile(_fullPath, _visibleCards_In_memory);
            
            if(res == ConfigResult.Success)
            {
                AppLogger.Log_To_File_log(_category, "[VisibleCardsConfig][DefaultAndSave] 已使用預設配置並保存", AppLogLevel.Information);
            }
            
            return res;
        }

        public List<CardInfo> GetVisibleCards_snapshot()
        {
            return _visibleCards_In_memory.Select(card => card.DeepClone()).ToList();
        }
    }
}