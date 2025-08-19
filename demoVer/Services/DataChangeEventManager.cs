using demoVer.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using demoVer.Broadcast;
using demoVer.Utils;
using demoVer.Interfaces;

namespace demoVer.Services
{
    public class VariableSubscription
    {
        public string ConnectionId = string.Empty;
        public uint DeviceAddr;
        public string CommandName = string.Empty;
        public ModCommandData CommandData = null!;
    }

    public record ContainerKeys(uint addr, string cmd);
    public class GroupLevelSubscription
    {
        public string ConnectionId = string.Empty;
        public uint DeviceAddr;
        public string CommandName = string.Empty;
    }

    public sealed record GroupDataChangedArgs
    (
        uint Addr,
        string CommandName,
        object DecodedValue,
        IReadOnlyList<byte> RawBytes,
        DateTimeOffset Timestamp
    );



    public class DataChangeEventManager
    {
        private string _category;

        private readonly IHubContext<DataHub> _hubContext;
        private readonly IGroupsDataDecoder _decoder;
        // JS : 每個連線connectionId  → 訂閱了哪些 (addr, cmd)
        private readonly ConcurrentDictionary<string , List<GroupLevelSubscription>> _clientSubscriptions = new();
        
        // JS : 每個 Group_CommandRawData 資料變動時要通知哪些連線(一個Group可被多人訂閱)
        private readonly ConcurrentDictionary<(uint addr, string cmd), HashSet<string>> _groupsSubscribers = new();

        // C# : 訂閱(addr, cmd) -> handlers
        private readonly ConcurrentDictionary<(uint addr, string cmd), List<Action<GroupDataChangedArgs>>> _groupsSubscribersCs = new();

        // 已經綁過事件的 CommandRawData(避免重複綁)
        private readonly HashSet<Group_CommandRawData> _alreadyHooked = new();

        public DataChangeEventManager(IHubContext<DataHub> hubContext, IGroupsDataDecoder decoder)
        {
            _hubContext = hubContext;
            _decoder = decoder;
            _category = GetType().FullName!;
        }

        public void SubscribeGroup(string connectionId, uint addr, string commandName, Group_CommandRawData groups)
        {
            //1) 記錄 Client 訂閱，connectionId → (ConnecitonId, DeviceAddr, CommandName)
            var clientList = _clientSubscriptions.GetOrAdd(connectionId, _ => new());
            if(clientList.Any(s => s.DeviceAddr == addr && s.CommandName == commandName))
                return; //避免重複訂閱
            var sub = new GroupLevelSubscription
            {
                ConnectionId = connectionId,
                DeviceAddr = addr,
                CommandName = commandName
            };
            clientList.Add(sub);

            //2)groups記錄有哪些 connectionId訂閱它
            var key = (addr, commandName);
            var connSet  = _groupsSubscribers.GetOrAdd(key, _ => new HashSet<string>());
            connSet.Add(connectionId);
            
            //3)綁定事件：綁在CommandRawData.OnChanged，只綁一次，以Groups判斷
            EnsureHook(groups, addr, commandName);
            // if(_alreadyHooked.Add(groups))
            // {
            //     AppLogger.Log_To_File_log(_category, $"[SubscribeGroup] connectionId = {connectionId}, addr = {addr}, cmdName = {commandName}", AppLogLevel.Trace);
            //     foreach(var Raw in groups.Groups.Values)
            //     {
            //         AppLogger.Log_To_File_log(_category, $"[SubscribeGroup] groupIndex = {Raw.GroupIndex}", AppLogLevel.Trace);
            //         Raw.OnChanged += () =>
            //         {
            //             AppLogger.Log_To_File_log(_category, $"[Raw.OnChanged] groupIndex = {Raw.GroupIndex}", AppLogLevel.Trace);
            //             OnGroupDataChanged(addr, commandName, groups);
            //         };
            //     }
            // }

            //4)第一次訂閱，主動推送一次目前值
            AppLogger.Log_To_File_log(_category, $"[SubscribeGroup] First Push Data", AppLogLevel.Debug);
            OnGroupDataChanged(addr, commandName, groups);
        }

        private void OnGroupDataChanged(uint addr, string cmdName, Group_CommandRawData groups)
        {
            var key = (addr, cmdName);
            AppLogger.Log_To_File_log(_category, $"[OnGroupDataChanged] {cmdName}@{addr}", AppLogLevel.Trace);
            
            try
            {
                // 1) decode 一次，兩邊共用
                var decoded = _decoder.Decode(groups, cmdName);

                var bytes = new List<byte>();
                foreach (var raw in groups.Groups.Values)
                    bytes.AddRange(raw.Data);

                var args = new GroupDataChangedArgs(addr, cmdName, decoded, bytes, DateTimeOffset.UtcNow);

                // 2) JS：SignalR 批次送
                if (_groupsSubscribers.TryGetValue(key, out var jsConnIds) && jsConnIds.Count > 0)
                {
                    _hubContext.Clients.Clients(jsConnIds).SendAsync(
                        "UpdateDecodedVal", addr, cmdName, decoded, bytes
                    );
                }

                // 3) C#：呼叫所有委派
                if (_groupsSubscribersCs.TryGetValue(key, out var handlers) && handlers.Count > 0)
                {
                    List<Action<GroupDataChangedArgs>> snapshot;
                    lock (handlers)
                        snapshot = handlers.ToList();

                    foreach (var h in snapshot)
                    {
                        try { h(args); } catch { /* 避免單一 handler 影響其他人 */ }
                    }
                }
            }
            catch(Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[OnGroupDataChanged] Error: {ex}", AppLogLevel.Error);
            }
            
            // if(_groupsSubscribers.TryGetValue(key, out var subscribers) && subscribers.Count > 0)
            // {
            //     try
            //     {
            //         //1.decode
            //         var tmpDecodedData = _decoder.Decode(groups, cmdName);
            //         List<byte> data_byteList = new List<byte>();
            //         foreach(var Raw in groups.Groups.Values)
            //         {
            //             data_byteList.AddRange(Raw.Data);
            //         }
            //         AppLogger.Log_To_File_log(_category, $"[OnGroupDataChanged] cmdName = {cmdName}, DecodeData = {tmpDecodedData}", AppLogLevel.Trace);
            //         //2.signalR批量發送 decode後的data
            //         AppLogger.Log_To_File_log(_category, $"[OnGroupDataChanged] transfer...", AppLogLevel.Trace);
                    
            //         _hubContext.Clients.Clients(subscribers).SendAsync("UpdateDecodedVal", addr, cmdName, tmpDecodedData, data_byteList);
                   
            //         AppLogger.Log_To_File_log(_category, $"[OnGroupDataChanged] done.", AppLogLevel.Trace); 
            //     }
            //     catch(Exception ex)
            //     {
            //         Console.WriteLine($"Push failed for {cmdName}@{addr}: {ex.Message}");
            //     }
            // }
        }

        public void UnsubscribeAll(string connectionId)
        {
            AppLogger.Log_To_File_log(_category, $"[UnsubscribeAll] connectionId = {connectionId} ...", AppLogLevel.Debug);
            if(_clientSubscriptions.TryRemove(connectionId, out var subs))
            {
                foreach (var sub in subs)
                {
                    var key = (sub.DeviceAddr, sub.CommandName);
                    if(_groupsSubscribers.TryGetValue(key, out var connSet))
                    {
                        AppLogger.Log_To_File_log(_category, $"(Addr, CmdName) = {key}, found connectionId = {connectionId}", AppLogLevel.Trace);
                        connSet.Remove(connectionId);
                        if(connSet.Count == 0)
                            _groupsSubscribers.TryRemove(key, out _);
                    }
                }
            }
            AppLogger.Log_To_File_log(_category, $"[UnsubscribeAll] done.", AppLogLevel.Debug);
        }

        //確保只綁一次底層 OnChanged
        private void EnsureHook(Group_CommandRawData groups, uint addr, string cmdName)
        {
            if(groups == null)
            {
                AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][EnsureHook] groups is null", AppLogLevel.Trace);
            }

            if (_alreadyHooked.Add(groups))
            {
                AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][EnsureHook] hook raw events: {cmdName}@{addr}", AppLogLevel.Trace);
                foreach (var raw in groups.Groups.Values)
                {
                    raw.OnChanged += () =>
                    {
                        AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][Raw.OnChanged] groupIndex={raw.GroupIndex}", AppLogLevel.Trace);
                        OnGroupDataChanged(addr, cmdName, groups);
                    };
                }
            }
        }
        
        // C#訂閱 : 回傳 IDisposable，方便在呼叫端 using 或 Dispose 解除訂閱
        public IDisposable SubscribeGroupCs(uint addr, string commandName, Group_CommandRawData groups, Action<GroupDataChangedArgs> handler)
        {
            var key = (addr, commandName);
            var list = _groupsSubscribersCs.GetOrAdd(key, _ => new List<Action<GroupDataChangedArgs>>());

            lock (list)
            {
                list.Add(handler);
            }

            EnsureHook(groups, addr, commandName);

            // 首次推播一次
            OnGroupDataChanged(addr, commandName, groups);


            AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][SubscribeGroupCs] {commandName}@{addr}", AppLogLevel.Trace);

            return new Unsubscriber(() =>
            {
                if (_groupsSubscribersCs.TryGetValue(key, out var handlers))
                {
                    lock (handlers)
                    {
                        handlers.Remove(handler);
                        if (handlers.Count == 0)
                            _groupsSubscribersCs.TryRemove(key, out _);
                    }
                }
            });
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly Action _dispose;
            private int _disposed;
            public Unsubscriber(Action dispose) => _dispose = dispose;
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    _dispose();
            }
        }

        // // 每筆資料要通知哪些連線 (一筆資料可被多人訂閱)
        // private readonly ConcurrentDictionary<(uint addr, string cmd), List<string>> _dataSubscribers = new();

        // //紀錄哪些ModCommandData有被綁過，有可能雖然是同個cmd的data，但是在不同的addr之下，所以是不同的instance
        // private readonly HashSet<ModCommandData> _alreadyHooked = new(); 

        // public DataChangeEventManager(IHubContext<DataHub> hubContext)
        // {
        //     _hubContext = hubContext;
        // }

        // public void Subscribe(string connectionId, uint deviceAddr, string commandName, ModCommandData commandData)
        // {
        //     var key = (deviceAddr, commandName);

        //     // 1. 加到 client → subscriptions
        //     var clientList = _clientSubscriptions.GetOrAdd(connectionId, _ => new());

        //     if (clientList.Any(s => s.DeviceAddr == deviceAddr && s.CommandName == commandName))
        //         return; // 避免重複訂閱

        //     var sub = new VariableSubscription
        //     {
        //         ConnectionId = connectionId,
        //         DeviceAddr = deviceAddr,
        //         CommandName = commandName,
        //         CommandData = commandData
        //     };
        //     clientList.Add(sub);

        //     // 2. 加到資料 → clients
        //     var clientIds = _dataSubscribers.GetOrAdd(key, _ => new());
        //     if (!clientIds.Contains(connectionId))
        //         clientIds.Add(connectionId);

        //     // 3. 綁定事件，只綁定一次
        //     if (_alreadyHooked.Add(commandData))
        //     {
        //         commandData.OnChanged += () => OnDataChanged(deviceAddr, commandName, commandData);
        //     }

        //     //4. 第一次訂閱的時候，主動推送一次目前值
        //     OnDataChanged(deviceAddr, commandName, commandData);
        // }

        // private bool IsAlreadyHooked(ModCommandData cmdData, (uint addr, string cmd) key)
        // {
        //     // 若該筆資料已經被綁定過 OnChanged，就不再重綁（用 _dataSubscribers 判斷）
        //     return _dataSubscribers.TryGetValue(key, out var list) && list.Count > 1;
        // }

        // private void OnDataChanged(uint addr, string cmd, ModCommandData data)
        // {
        //     var key = (addr, cmd);

        //     if (_dataSubscribers.TryGetValue(key, out var connList))
        //     {
        //         foreach (var connId in connList)
        //         {
        //             try
        //             {
        //                 _hubContext.Clients.Client(connId).SendAsync("UpdateCommandValue", addr, cmd, data);

        //                 Console.WriteLine($"connID = {connId}, addr = {addr}, cmd = {cmd}");
        //             }
        //             catch(Exception ex)
        //             {
        //                 Console.WriteLine($"Push failed to {connId} for {cmd}@{addr}: {ex.Message}");
        //             }
                    
        //         }
        //     }
        // }

        // public void UnsubscribeAll(string connectionId)
        // {
        //     if (_clientSubscriptions.TryRemove(connectionId, out var list))
        //     {
        //         foreach (var sub in list)
        //         {
        //             var key = (sub.DeviceAddr, sub.CommandName);

        //             // 從 data → clients 中移除
        //             if (_dataSubscribers.TryGetValue(key, out var connList))
        //             {
        //                 connList.Remove(connectionId);
        //                 if (connList.Count == 0)
        //                     _dataSubscribers.TryRemove(key, out _);
        //             }
        //         }
        //     }
        // }
    }
}
