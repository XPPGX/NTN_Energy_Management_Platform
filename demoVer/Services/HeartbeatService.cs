using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
					if (OnTick is not null)
					{
						await OnTick.Invoke().ConfigureAwait(false);
					}
				}
			}
			catch (TaskCanceledException)
			{
			
			}
			finally
			{
			
			}
		}

		public override Task StopAsync(CancellationToken stoppingToken)
		{
			return base.StopAsync(stoppingToken);
		}
	}
}
