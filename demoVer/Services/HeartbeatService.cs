using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using demoVer.Utils;
using demoVer.Models;

namespace demoVer.Services
{
	public class HeartbeatService : BackgroundService
	{
		private string _category = "";
		public int Counter { get; private set; } = 0;
		public event Func<int, Task>? OnHeartbeatAsync;
		public event Func<Task>? OnTick;

		public HeartbeatService(ILogger<HeartbeatService> logger)
		{			
			_category = GetType().FullName!;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					AppLogger.Log_To_File_log(_category, $"[Heartbeat] Tick = {Counter}", AppLogLevel.Trace);
					await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
					
					Counter++;
					
					// 安全地調用所有訂閱者，隔離每個訂閱者的異常
					if (OnTick is not null)
					{
						await InvokeSubscribersSafely(OnTick, stoppingToken);
					}
				}
			}
			catch (TaskCanceledException)
			{
				// 正常關閉，不需要記錄
				AppLogger.Log_To_File_log(_category, "[Heartbeat] Service is stopping (TaskCanceled)", AppLogLevel.Debug);
			}
			catch (Exception ex)
			{
				// 記錄非預期的異常，但不要讓服務崩潰
				AppLogger.Log_To_File_log(_category, $"[Heartbeat] Unexpected error in ExecuteAsync: {ex.Message}", AppLogLevel.Error);
			}
			finally
			{
				AppLogger.Log_To_File_log(_category, "[Heartbeat] Service stopped", AppLogLevel.Information);
			}
		}

		/// <summary>
		/// 安全地調用所有訂閱者，隔離每個訂閱者的異常，避免一個訂閱者的錯誤影響其他訂閱者
		/// </summary>
		private async Task InvokeSubscribersSafely(Func<Task> eventDelegate, CancellationToken stoppingToken)
		{
			if (eventDelegate == null) return;

			var invocationList = eventDelegate.GetInvocationList();
			foreach (var handler in invocationList)
			{
				if (stoppingToken.IsCancellationRequested) break;

				try
				{
					if (handler is Func<Task> taskHandler)
					{
						await taskHandler().ConfigureAwait(false);
					}
				}
				catch (JSDisconnectedException ex)
				{
					// Blazor circuit 斷開，這是預期的情況，記錄為 Trace 級別
					AppLogger.Log_To_File_log(_category, 
						$"[Heartbeat] Subscriber disconnected (JSDisconnectedException): {handler.Target?.GetType().Name ?? "Unknown"} - {ex.Message}", 
						AppLogLevel.Trace);
				}
				catch (JSException ex)
				{
					// JS 執行錯誤，通常也是 circuit 斷開相關
					AppLogger.Log_To_File_log(_category, 
						$"[Heartbeat] JS error in subscriber {handler.Target?.GetType().Name ?? "Unknown"}: {ex.Message}", 
						AppLogLevel.Trace);
				}
				catch (ObjectDisposedException ex)
				{
					// 組件已被釋放，這也是預期的情況
					AppLogger.Log_To_File_log(_category, 
						$"[Heartbeat] Subscriber disposed: {handler.Target?.GetType().Name ?? "Unknown"} - {ex.Message}", 
						AppLogLevel.Trace);
				}
				catch (TaskCanceledException)
				{
					// 任務被取消，通常發生在服務關閉時
					AppLogger.Log_To_File_log(_category, 
						$"[Heartbeat] Task canceled for subscriber: {handler.Target?.GetType().Name ?? "Unknown"}", 
						AppLogLevel.Debug);
				}
				catch (Exception ex)
				{
					// 其他未預期的異常，記錄但不中斷其他訂閱者
					AppLogger.Log_To_File_log(_category, 
						$"[Heartbeat] Error in subscriber {handler.Target?.GetType().Name ?? "Unknown"}: {ex.GetType().Name} - {ex.Message}", 
						AppLogLevel.Warning);
				}
			}
		}

		public override Task StopAsync(CancellationToken stoppingToken)
		{
			AppLogger.Log_To_File_log(_category, "[Heartbeat] StopAsync called", AppLogLevel.Information);
			return base.StopAsync(stoppingToken);
		}
	}
}
