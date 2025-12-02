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
        private static int _hasStarted = 0; // 保險用: 避免重入

        private string _DataFolderPath = string.Empty;
        private string _HardCoded_CmdCodeFileName = "CmdCode.json";
        private string _HardCoded_CmdCodeReverseFileName = "CmdCodeReverse.json";
        
        public StartupHostedService(VersionFileService versionFileService, GlobalVar globalVar)
        {
            _category = GetType().FullName!;
            _versionFileService = versionFileService;
            _globalVar = globalVar;
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

                //1. 寫入版本檔案
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) await _versionFileService.WriteVersionFileAsync();
                
                //2. 載入 FixedCmdName => FixedCmdCode 對照表
                Load_HardCoded_CmdCode();


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


        public void SetPath()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _DataFolderPath = "/userdata/CMU3/Data/";
                AppLogger.Log_To_File_log(_category, $"Linux 環境，Data FolderPath 設定為: {_DataFolderPath}", AppLogLevel.Debug);
            }
            else
            {
                _DataFolderPath = "Data";
                AppLogger.Log_To_File_log(_category, $"非 Linux 環境，Data FolderPath 設定為: ", AppLogLevel.Debug);
            }
        }
        
        public void Load_HardCoded_CmdCode()
        {
            string cmdCodePath = Path.Combine(_DataFolderPath, _HardCoded_CmdCodeFileName);
            string cmdCodeReversePath = Path.Combine(_DataFolderPath, _HardCoded_CmdCodeReverseFileName);

            var forward = ReadCmdCodeFile(cmdCodePath, "CmdCode");
            var reverse = ReadCmdCodeFile(cmdCodeReversePath, "CmdCodeReverse");

            if (forward is not null && reverse is not null)
            {
                AssignHardCodes(forward, reverse);
                AppLogger.Log_To_File_log(_category, $"成功載入 CmdCode JSON 文件: {cmdCodePath} 與 {cmdCodeReversePath}", AppLogLevel.Information);
                return;
            }

            HardCodedCmdCodeCreator.CreateBasic_HardCoded_CmdCode();
            forward = HardCodedCmdCodeCreator.HardCoded_CmdCode;
            reverse = HardCodedCmdCodeCreator.HardCoded_CmdCode_Reverse;

            WriteCmdCodeFile(cmdCodePath, forward);
            WriteCmdCodeFile(cmdCodeReversePath, reverse);
            AssignHardCodes(forward, reverse);

            AppLogger.Log_To_File_log(_category, $"CmdCode 對照表載入失敗，已建立預設檔案: {cmdCodePath}, {cmdCodeReversePath}", AppLogLevel.Warning);
        }

        private Dictionary<string, Dictionary<string, string>>? ReadCmdCodeFile(string filePath, string tag)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            }
            catch (Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"載入 {tag} JSON 失敗: {filePath}, Exception: {ex.Message}", AppLogLevel.Warning);
                return null;
            }
        }

        private void WriteCmdCodeFile(string filePath, Dictionary<string, Dictionary<string, string>> content)
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
            HardCodedCmdCodeCreator.HardCoded_CmdCode = forward;
            HardCodedCmdCodeCreator.HardCoded_CmdCode_Reverse = reverse;
            _globalVar.HardCodes = forward;
            _globalVar.HardCodesReverse = reverse;
        }
    }
}