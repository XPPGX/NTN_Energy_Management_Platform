using demoVer.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using demoVer.Broadcast;
using demoVer.Utils;
using demoVer.Interfaces;
using System.Reflection.PortableExecutable;

namespace demoVer.Services
{
    #region ChangeEventManager for ReadOnly cmdData
    public class CmdLevelSubscription
    {
        public string ConnectionId = string.Empty;
        public uint DeviceAddr;
        public string CommandName = string.Empty;
    }

    public sealed record CmdDataChangeArgs
    (
        uint Addr,
        string CommandName,
        object RealValue,
        DateTimeOffset Timestamp
    );


    //針對 Read_API 的 ChangeEventManager
    public class DataChangeEventManager
    {
        private string _category;

        private readonly IHubContext<DataHub> _hubContext;
        // JS : 每個連線connectionId  → 訂閱了哪些 (addr, cmd)
        private readonly GlobalVar _globalVar;


        private readonly ConcurrentDictionary<string, List<CmdLevelSubscription>> _clientSubscriptions = new();
        // JS : 每個 SingleCommandData 資料變動時要通知哪些連線(一個Cmd可被多人訂閱)
        private readonly ConcurrentDictionary<(uint addr, string cmd), HashSet<string>> _cmdsSubscribers = new();

        // 每個CmdData的變動事件處理器，只會綁定一次
        private readonly ConcurrentDictionary<(uint addr, string cmd), Action> _hookedHandlers = new();

        // C# : 訂閱(addr, cmd) -> handlers
        // private readonly ConcurrentDictionary<(uint addr, string cmd), List<Action<CmdDataChangeArgs>>> _groupsSubscribersCs = new();
        private readonly ConcurrentDictionary<(uint addr, string cmd), List<Action<CmdDataChangeArgs>>> _cmdDataCsSubscriptions = new();
        // 已經綁過事件的 CommandRawData(避免重複綁)
        private readonly HashSet<SingleCommandData> _alreadyHooked = new();

        public DataChangeEventManager(IHubContext<DataHub> hubContext, GlobalVar globalVar)
        {
            _hubContext = hubContext;
            _globalVar = globalVar;
            _category = GetType().FullName!;
        }

        public void SubscribeCmd(string connectionId, uint addr, string commandName, SingleCommandData cmdData)
        {

            //1. 紀錄 : Client 訂閱哪些 (Addr, cmd)，connectionId → (ConnecitonId, DeviceAddr, CommandName)
            var clientList = _clientSubscriptions.GetOrAdd(connectionId, _ => new());
            if (clientList.Any(s => s.DeviceAddr == addr && s.CommandName == commandName))
                return; //避免重複訂閱
            var sub = new CmdLevelSubscription
            {
                ConnectionId = connectionId,
                DeviceAddr = addr,
                CommandName = commandName
            };
            clientList.Add(sub);

            //2. 紀錄 : (Addr, Cmd) 有哪些ConnectionId訂閱它
            var key = (addr, commandName);
            var connSet = _cmdsSubscribers.GetOrAdd(key, _ => new HashSet<string>());
            connSet.Add(connectionId);

            //3. 綁定事件：綁在SingleCommandData.OnChanged，只綁一次，以Cmd判斷
            EnsureHook(addr, commandName, cmdData);

            //4. 第一次訂閱，主動推送一次目前值
            AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][SubscribeCmd] First Push Data", AppLogLevel.Debug);
            OnCmdDataChanged(addr, commandName, cmdData);
        }

        public void UnsubscribeAll(string connectionId)
        {
            AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][UnsubscribeAll] connectionId = {connectionId} ...", AppLogLevel.Trace);
            //從clientSubscriptions移除所有訂閱(addr, cmd)
            if (_clientSubscriptions.TryRemove(connectionId, out var subs))
            {

                foreach (var sub in subs)
                {
                    var key = (sub.DeviceAddr, sub.CommandName);

                    //同時也去cmdsSubscribers移除connectionId
                    if (_cmdsSubscribers.TryGetValue(key, out var connSet))
                    {
                        AppLogger.Log_To_File_log(_category, $"(Addr, CmdName) = {key}, found connectionId = {connectionId}", AppLogLevel.Trace);
                        connSet.Remove(connectionId);
                        if (connSet.Count == 0)
                        {
                            _cmdsSubscribers.TryRemove(key, out _);

                            //移除綁定的事件
                            if (_hookedHandlers.TryRemove(key, out var handler))
                            {
                                var cmdData = _globalVar.Real_Devices_ReadData.Get_oneDevice_CmdData_Ref(sub.DeviceAddr, sub.CommandName);
                                if (cmdData is not null)
                                {
                                    cmdData.OnChanged -= handler;
                                }
                                AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][UnsubscribeAll] Unhook handler for : {key}", AppLogLevel.Trace);
                                //無法直接移除事件，只能用旗標方式
                                // cmdData.OnChanged -= handler;
                            }
                        }
                    }
                }
            }
            AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][UnsubscribeAll] done.", AppLogLevel.Trace);
        }

        /// <summary>
        /// 確保只綁一次底層 OnChanged
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="cmdName"></param>
        /// <param name="cmdData"></param>
        private void EnsureHook(uint addr, string cmdName, SingleCommandData cmdData)
        {
            if (cmdData == null)
            {
                AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][EnsureHook] cmdData is null", AppLogLevel.Trace);
                return;
            }

            var key = (addr, cmdName);

            if (_hookedHandlers.ContainsKey(key))
            {
                AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][EnsureHook] already hooked: {cmdName}@{addr}", AppLogLevel.Trace);
                return;
            }

            Action handler = () =>
            {
                AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][EnsureHook][CmdData.OnChanged] {cmdName}@{addr} changed", AppLogLevel.Trace);
                if (_cmdsSubscribers.TryGetValue(key, out var connIds) && connIds.Count > 0)
                {
                    OnCmdDataChanged(addr, cmdName, cmdData);
                }
            };

            cmdData.OnChanged += handler;
            _hookedHandlers.TryAdd(key, handler);

            AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][EnsureHook] Hooked handler for : {cmdName}@{addr}", AppLogLevel.Trace);


            // if (_alreadyHooked.Add(cmdData))
            // {

            //     AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][EnsureHook] hook raw events: {cmdName}@{addr}", AppLogLevel.Trace);
            //     cmdData.OnChanged += () =>
            //     {
            //         AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][EnsureHook][CmdData.OnChanged] {cmdName}@{addr} changed", AppLogLevel.Trace);
            //         var key = (addr, cmdName);
            //         if (_cmdsSubscribers.TryGetValue(key, out var connIds) && connIds.Count > 0)
            //         {
            //             OnCmdDataChanged(addr, cmdName, cmdData);
            //         }
            //     };
            // }

            // if (_alreadyHooked.Add(groups))
            // {
            //     AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][EnsureHook] hook raw events: {cmdName}@{addr}", AppLogLevel.Trace);
            //     foreach (var raw in groups.Groups.Values)
            //     {
            //         raw.OnChanged += () =>
            //         {
            //             AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][Raw.OnChanged] groupIndex={raw.GroupIndex}", AppLogLevel.Trace);
            //             OnGroupDataChanged(addr, cmdName, groups);
            //         };
            //     }
            // }
        }


        private void OnCmdDataChanged(uint addr, string cmdName, SingleCommandData cmdData)
        {
            var key = (addr, cmdName);
            AppLogger.Log_To_File_log(_category, $"[OnCmdDataChanged] {cmdName}@{addr}", AppLogLevel.Trace);

            try
            {
                // 1. 取資料
                string valueType = cmdData.type ?? string.Empty;
                object sendValue;
                switch (valueType)
                {
                    case "Numeric":
                        //在第2步送出時，直接送value
                        sendValue = cmdData.value;
                        break;
                    case "ASCII":
                        //在第2步送出時，直接送value
                        sendValue = cmdData.value;
                        break;
                    case "BitField":
                        // BitField類型的值，需先呼叫BitFieldParser內function去解析
                        var decodeList = cmdData.DeepClone_decode();
                        var parsedStr = BitFieldParser.FitParserFunction(cmdName, decodeList ?? new List<decodeContent>());
                        sendValue = parsedStr;
                        break;
                    default:
                        sendValue = cmdData.value ?? string.Empty;
                        break;
                }

                // 2. JS：SignalR 批次送
                if (_cmdsSubscribers.TryGetValue(key, out var jsConnIds) && jsConnIds.Count > 0)
                {
                    _hubContext.Clients.Clients(jsConnIds).SendAsync(
                        "UpdateDecodedVal", addr, cmdName, sendValue
                    );
                }

                // 3. C#: 呼叫所有委派
                if (_cmdDataCsSubscriptions.TryGetValue(key, out var handlers) && handlers.Count > 0)
                {
                    List<Action<CmdDataChangeArgs>> snapshot;
                    lock (handlers)
                        snapshot = handlers.ToList();

                    var args = new CmdDataChangeArgs(
                        Addr: addr,
                        CommandName: cmdName,
                        RealValue: sendValue,
                        Timestamp: DateTimeOffset.UtcNow
                    );

                    foreach (var h in snapshot)
                    {
                        try { h(args); } catch { /* 避免單一 handler 影響其他人 */ }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[OnCmdDataChanged] Error: {ex}", AppLogLevel.Error);
            }

        }


        // C#訂閱 : 回傳 IDisposable，方便在呼叫端 using 或 Dispose 解除訂閱
        public IDisposable SubscribeCmdCs(uint addr, string commandName, SingleCommandData cmdData, Action<CmdDataChangeArgs> handler)
        {
            var key = (addr, commandName);
            var list = _cmdDataCsSubscriptions.GetOrAdd(key, _ => new List<Action<CmdDataChangeArgs>>());

            lock (list)
            {
                list.Add(handler);
            }

            EnsureHook(addr, commandName, cmdData);

            // 首次推播一次
            OnCmdDataChanged(addr, commandName, cmdData);


            AppLogger.Log_To_File_log(_category, $"[DataChangeEventManager][SubscribeGroupCs] {commandName}@{addr}", AppLogLevel.Trace);

            return new Unsubscriber(() =>
            {
                if (_cmdDataCsSubscriptions.TryGetValue(key, out var handlers))
                {
                    lock (handlers)
                    {
                        handlers.Remove(handler);
                        if (handlers.Count == 0)
                            _cmdDataCsSubscriptions.TryRemove(key, out _);
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
    }
    #endregion ChangeEventManager for ReadOnly cmdData

    #region ChangeEventManager for Writable cmdData
    public class SubSysLevelSubscription
    {
        public string ConnectionId = string.Empty;
        public string port = string.Empty;
        public string protocol = string.Empty;
    }

    //針對 Writable Cmd 的 ChangeEventManager
    public class WriteDataChangeEventManager
    {
        private string _category = string.Empty;

        private readonly IHubContext<DataHub> _hubContext;

        private readonly SubSystemManager _subsystemManager;
        // JS : 每個連線connectionId  → 訂閱了哪些 (port, protocol)
        private readonly ConcurrentDictionary<string, List<SubSysLevelSubscription>> _clientSubscriptions = new();
        // JS : 每個 SubSystem 資料變動時要通知哪些連線(一個SubSystem可被多人訂閱)
        private readonly ConcurrentDictionary<(string port, string protocol), HashSet<string>> _subSysSubscribers = new();
        private readonly ConcurrentDictionary<(string port, string protocol), Action> _hookedHandlers = new();
        public WriteDataChangeEventManager(IHubContext<DataHub> hubContext, SubSystemManager subsystemManager)
        {
            _hubContext = hubContext;
            _subsystemManager = subsystemManager;
            _category = GetType().FullName!;
        }

        public void SubscribeSubSystem(string connectionId, string port, string protocol, SubSystem subsys)
        {
            //1. 紀錄 : Client 訂閱哪些 (port, protocol)，connectionId → (ConnectionId, port, protocol)
            var clientList = _clientSubscriptions.GetOrAdd(connectionId, _ => new());
            if (clientList.Any(s => s.port == port && s.protocol == protocol))
                return; //避免重複訂閱
            var sub = new SubSysLevelSubscription
            {
                ConnectionId = connectionId,
                port = port,
                protocol = protocol
            };
            clientList.Add(sub);

            //2. 紀錄 : (port, protocol) 有哪些ConnectionId訂閱它
            var key = (port, protocol);
            var connSet = _subSysSubscribers.GetOrAdd(key, _ => new HashSet<string>());
            connSet.Add(connectionId);

            //3. 綁定事件：綁在SubSystem的SettingChanged事件，只綁一次，以(port, protocol)判斷
            EnsureHook(port, protocol, subsys);
        }

        /// <summary>
        /// 確保只綁一次底層 OnChanged
        /// </summary>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        public void EnsureHook(string port, string protocol, SubSystem subsys)
        {
            if (subsys == null)
            {
                AppLogger.Log_To_File_log(_category, $"[WriteDataChangeEventManager][EnsureHook] subsys is null", AppLogLevel.Trace);
                return;
            }

            var key = (port, protocol);
            if (_hookedHandlers.ContainsKey(key))
            {
                AppLogger.Log_To_File_log(_category, $"[WriteDataChangeEventManager][EnsureHook] already hooked: {protocol}@{port}", AppLogLevel.Trace);
                return;
            }
            
            Action handler = () =>
            {
                AppLogger.Log_To_File_log(_category, $"[WriteDataChangeEventManager][EnsureHook][SubSystem.SettingChanged] {protocol}@{port} changed", AppLogLevel.Trace);
                if (_subSysSubscribers.TryGetValue(key, out var connIds) && connIds.Count > 0)
                {
                    
                }
            };
            _hookedHandlers[key] = handler;
        }
    }
    #endregion ChangeEventManager for Writable cmdData
}