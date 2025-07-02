// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;

// namespace MyBlazorServerApp.Services
// {
// 	public class HeartbeatService : BackgroundService
// 	{
// 		public int Counter { get; private set; } = 0;
// 		public event Func<int, Task>? OnHeartbeatAsync;
// 		public event Func<Task>? OnTick;

// 		public HeartbeatService(ILogger<HeartbeatService> logger)
// 		{
			
// 		}

// 		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
// 		{
// 			try
// 			{
// 				while (!stoppingToken.IsCancellationRequested)
// 				{
// 					await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
					
// 					Counter++;
// 					if (OnTick is not null)
// 					{
// 						await OnTick.Invoke().ConfigureAwait(false);
// 					}
// 				}
// 			}
// 			catch (TaskCanceledException)
// 			{
			
// 			}
// 			finally
// 			{
			
// 			}
// 		}

// 		public override Task StopAsync(CancellationToken stoppingToken)
// 		{
// 			return base.StopAsync(stoppingToken);
// 		}
// 	}
// }
