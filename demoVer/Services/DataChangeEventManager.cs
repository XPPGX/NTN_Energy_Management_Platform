using demoVer.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using demoVer.Broadcast;
namespace demoVer.Services
{
    public class VariableSubscription
    {
        public string ConnectionId = string.Empty;
        public uint DeviceAddr;
        public string CommandName = string.Empty;
        public ModCommandData CommandData = null!;
    }

    public class DataChangeEventManager
    {
        private readonly IHubContext<DataHub> _hubContext;

        // 每個連線訂閱了哪些變數 (可多筆)
        private readonly ConcurrentDictionary<string, List<VariableSubscription>> _clientSubscriptions = new();

        // 每筆資料要通知哪些連線 (一筆資料可被多人訂閱)
        private readonly ConcurrentDictionary<(uint addr, string cmd), List<string>> _dataSubscribers = new();

        //紀錄哪些ModCommandData有被綁過，有可能雖然是同個cmd的data，但是在不同的addr之下，所以是不同的instance
        private readonly HashSet<ModCommandData> _alreadyHooked = new(); 

        public DataChangeEventManager(IHubContext<DataHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void Subscribe(string connectionId, uint deviceAddr, string commandName, ModCommandData commandData)
        {
            var key = (deviceAddr, commandName);

            // 1. 加到 client → subscriptions
            var clientList = _clientSubscriptions.GetOrAdd(connectionId, _ => new());

            if (clientList.Any(s => s.DeviceAddr == deviceAddr && s.CommandName == commandName))
                return; // 避免重複訂閱

            var sub = new VariableSubscription
            {
                ConnectionId = connectionId,
                DeviceAddr = deviceAddr,
                CommandName = commandName,
                CommandData = commandData
            };
            clientList.Add(sub);

            // 2. 加到資料 → clients
            var clientIds = _dataSubscribers.GetOrAdd(key, _ => new());
            if (!clientIds.Contains(connectionId))
                clientIds.Add(connectionId);

            // 3. 綁定事件，只綁定一次
            if (_alreadyHooked.Add(commandData))
            {
                commandData.OnChanged += () => OnDataChanged(deviceAddr, commandName, commandData);
            }
        }

        private bool IsAlreadyHooked(ModCommandData cmdData, (uint addr, string cmd) key)
        {
            // 若該筆資料已經被綁定過 OnChanged，就不再重綁（用 _dataSubscribers 判斷）
            return _dataSubscribers.TryGetValue(key, out var list) && list.Count > 1;
        }

        private void OnDataChanged(uint addr, string cmd, ModCommandData data)
        {
            var key = (addr, cmd);

            if (_dataSubscribers.TryGetValue(key, out var connList))
            {
                var value = data.DisplayValue?.ToString() ?? "N/A";

                foreach (var connId in connList)
                {
                    _hubContext.Clients.Client(connId).SendAsync("UpdateCommandValue", addr, cmd, value);
                }
            }
        }

        public void UnsubscribeAll(string connectionId)
        {
            if (_clientSubscriptions.TryRemove(connectionId, out var list))
            {
                foreach (var sub in list)
                {
                    var key = (sub.DeviceAddr, sub.CommandName);

                    // 從 data → clients 中移除
                    if (_dataSubscribers.TryGetValue(key, out var connList))
                    {
                        connList.Remove(connectionId);
                        if (connList.Count == 0)
                            _dataSubscribers.TryRemove(key, out _);
                    }
                }
            }
        }
    }
}
