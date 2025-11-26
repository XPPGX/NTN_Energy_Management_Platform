using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using demoVer.Utils;
using demoVer.Models;
namespace demoVer.Services
{
    public class StartupHostedService : IHostedService
    {
        private string _category = "";
        private readonly VersionFileService _versionFileService;
        private static int _hasStarted = 0; // 保險用: 避免重入

        public StartupHostedService(VersionFileService versionFileService)
        {
            _category = GetType().FullName!;
            _versionFileService = versionFileService;
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
                
                //1. 寫入版本檔案
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    await _versionFileService.WriteVersionFileAsync();
                }
                

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
    }
}