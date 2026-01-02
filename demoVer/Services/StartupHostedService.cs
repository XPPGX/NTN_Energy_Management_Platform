using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using demoVer.Utils;
using demoVer.Models;
using System.Text.Json;
using System.IO;
namespace demoVer.Services
{
    public class StartupHostedService : IHostedService
    {
        private string _category = "";
        private readonly VersionFileService _versionFileService;
        private readonly GlobalVar _globalVar;
        private readonly BottomRowConfig _bottomRowConfig;
        private readonly VisibleCardsConfig _visibleCardsConfig;
        private static int _hasStarted = 0; // 保險用: 避免重入

        private string _DataFolderPath = string.Empty;
        private string _UserSettingFolderPath = string.Empty;
        private string _HardCoded_CmdCodeFileName = "CmdCode.json";
        private string _HardCoded_CmdCodeReverseFileName = "CmdCodeReverse.json";
        private string _HardCoded_DecodeLogicFileName = "CmdDecodeLogic.json";
        
        public StartupHostedService(VersionFileService versionFileService, 
                                    GlobalVar globalVar, 
                                    BottomRowConfig bottomRowConfig,
                                    VisibleCardsConfig visibleCardsConfig)
        {
            _category = GetType().FullName!;
            _versionFileService = versionFileService;
            _globalVar = globalVar;
            _bottomRowConfig = bottomRowConfig;
            _visibleCardsConfig = visibleCardsConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _hasStarted, 1) == 1)
            {
                AppLogger.Log_To_File_log(_category, "StartupHostedService 已啟動過，本次略過。", AppLogLevel.Debug);
                return;
            }

            AppLogger.Log_To_File_log(_category, "應用啟動初始化開始…", AppLogLevel.Debug);

            try
            {
                
                //0. 設定路徑
                SetPath();
                AppLogger.Log_To_File_log(_category, "[StartupHostedService][0] 路徑設定完成", AppLogLevel.Debug);

                //1. 寫入版本檔案
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) await _versionFileService.WriteVersionFileAsync();
                AppLogger.Log_To_File_log(_category, "[StartupHostedService][1] 版本檔案寫入完成", AppLogLevel.Debug);

                //2. 載入 FixedCmdName => FixedCmdCode 對照表
                Load_HardCoded_CmdCode();
                AppLogger.Log_To_File_log(_category, "[StartupHostedService][2] CmdCode 載入完成", AppLogLevel.Debug);
                
                //3. 載入 FixedCmdName => DecodeLogic 對照表
                Load_HardCoded_DecodeLogic();
                AppLogger.Log_To_File_log(_category, "[StartupHostedService][3] DecodeLogic 載入完成", AppLogLevel.Debug);
                
                //4. 載入 BottomRow 設定
                _bottomRowConfig.Init();
                AppLogger.Log_To_File_log(_category, "[StartupHostedService][4] BottomRow 設定載入完成", AppLogLevel.Debug);
                
                //5. 載入 首頁卡片資料
                _visibleCardsConfig.Init();
                AppLogger.Log_To_File_log(_category, "[StartupHostedService][5] 首頁卡片資料載入完成", AppLogLevel.Debug);

                AppLogger.Log_To_File_log(_category, "應用啟動初始化完成。", AppLogLevel.Debug);
            }
            catch (OperationCanceledException)
            {
                AppLogger.Log_To_File_log(_category, "應用啟動初始化被取消。", AppLogLevel.Debug);
            }
            catch (Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"應用啟動初始化發生未預期錯誤。Exception: {ex}", AppLogLevel.Error);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // 若未來需要在關閉時釋放資源，可以在這裡收尾
            AppLogger.Log_To_File_log(_category, "StartupHostedService 停止。", AppLogLevel.Debug);
            return Task.CompletedTask;
        }

        #region Helper Methods
        public void SetPath()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _DataFolderPath = "/userdata/CMU3/Data/";
                _UserSettingFolderPath = "/userdata/CMU3/UserSetting/";
                AppLogger.Log_To_File_log(_category, () => $"Linux 環境，Data FolderPath 設定為: {_DataFolderPath}", AppLogLevel.Debug);
                AppLogger.Log_To_File_log(_category, () => $"Linux 環境，UserSetting FolderPath 設定為: {_UserSettingFolderPath}", AppLogLevel.Debug);
            }
            else
            {
                _DataFolderPath = "Data";
                _UserSettingFolderPath = "UserSetting";
                AppLogger.Log_To_File_log(_category, () => $"非 Linux 環境，Data FolderPath 設定為: {_DataFolderPath}", AppLogLevel.Debug);
                AppLogger.Log_To_File_log(_category, () => $"非 Linux 環境，UserSetting FolderPath 設定為: {_UserSettingFolderPath}", AppLogLevel.Debug);
            }
        }
        
        public void Load_HardCoded_CmdCode()
        {
            string cmdCodePath = Path.Combine(_DataFolderPath, _HardCoded_CmdCodeFileName);
            string cmdCodeReversePath = Path.Combine(_DataFolderPath, _HardCoded_CmdCodeReverseFileName);

            var forward = ReadJsonFile<Dictionary<string, Dictionary<string, string>>>(cmdCodePath, "CmdCode");
            var reverse = ReadJsonFile<Dictionary<string, Dictionary<string, string>>>(cmdCodeReversePath, "CmdCodeReverse");

            if (forward is not null && reverse is not null)
            {
                AssignHardCodes(forward, reverse);
                AppLogger.Log_To_File_log(_category, $"成功載入 CmdCode JSON 文件: {cmdCodePath} 與 {cmdCodeReversePath}", AppLogLevel.Information);
                return;
            }

            (forward, reverse) = HardCodedCmdCodeCreator.CreateBasic_HardCoded_CmdCode();

            WriteJsonFile(cmdCodePath, forward);
            WriteJsonFile(cmdCodeReversePath, reverse);
            AssignHardCodes(forward, reverse);

            AppLogger.Log_To_File_log(_category, $"CmdCode 對照表載入失敗，已建立預設檔案: {cmdCodePath}, {cmdCodeReversePath}", AppLogLevel.Warning);
        }

        public void Load_HardCoded_DecodeLogic()
        {
            string decodeLogicPath = Path.Combine(_DataFolderPath, _HardCoded_DecodeLogicFileName);

            var spec = ReadJsonFile<CommandBitFieldSpec>(decodeLogicPath, "DecodeLogic", s => s.BuildLookups());

            if (spec is not null)
            {
                AssignDecodeLogic(spec);
                AppLogger.Log_To_File_log(_category, $"成功載入 DecodeLogic JSON 文件: {decodeLogicPath}", AppLogLevel.Information);
                return;
            }

            spec = HardCoded_DecodeLogicCreator.CreateBasic_DecodeLogic();

            WriteJsonFile(decodeLogicPath, spec);
            AssignDecodeLogic(spec);

            AppLogger.Log_To_File_log(_category, $"DecodeLogic JSON 載入失敗，已建立預設檔案: {decodeLogicPath}", AppLogLevel.Warning);
        }

        public void Load_BottomRow_Setting()
        {
            
        }

        private T? ReadJsonFile<T>(string filePath, string tag, Action<T>? postProcess = null) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<T>(json);
                if (data is not null)
                {
                    postProcess?.Invoke(data);
                }
                return data;
            }
            catch (Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"載入 {tag} JSON 失敗: {filePath}, Exception: {ex.Message}", AppLogLevel.Warning);
                return null;
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

        private void AssignHardCodes(Dictionary<string, Dictionary<string, string>> forward,
                                     Dictionary<string, Dictionary<string, string>> reverse)
        {
            GlobalVar.HardCodes = forward;
            GlobalVar.HardCodesReverse = reverse;
        }

        private void AssignDecodeLogic(CommandBitFieldSpec spec)
        {
            spec.BuildLookups();
            GlobalVar.HardCodedDecodeLogic = spec;
        }
        #endregion Helper Methods
    }
}