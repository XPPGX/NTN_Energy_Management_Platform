// public class DataCenter
// {
//     private readonly HeartbeatService _heartbeat;

//     public DataCenter(HeartbeatService heartbeat)
//     {
//         _heartbeat = heartbeat;

//         // 方式 1：每秒通知
//         _heartbeat.OnTick += OnHeartbeatTick;

//         // 方式 2：如果你用 OnHeartbeatAsync 的版本
//         // _heartbeat.OnHeartbeatAsync += async (count) => { Console.WriteLine($"tick #{count}"); };
//     }

//     private Task OnHeartbeatTick()
//     {
//         Console.WriteLine("✅ Tick received at " + DateTime.Now);
//         return Task.CompletedTask;
//     }
// }
