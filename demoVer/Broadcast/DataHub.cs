using System;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using demoVer.Services;
using demoVer.Utils;
using demoVer.Models;
namespace demoVer.Broadcast
{
    public class DataHub : Hub
    {
        [Flags]
        private enum SubscriptionType
        {
            None = 0,
            ReadData = 1,
            ConfigurableData = 2,
        }

        private string _category = string.Empty;
        private readonly DataCenter _dataCenter;
        private readonly DataChangeEventManager _eventManager;
        private readonly GlobalVar _globalVar;
        private readonly SubSystemManager _subSystemManager;

        private readonly WriteDataChangeEventManager _writeDataChangeEventManager;
        private readonly ConcurrentDictionary<string, SubscriptionType> _connectionSubscriptions = new(); // 追蹤每條連線目前的訂閱狀態
        public DataHub( DataCenter dataCenter,
                        DataChangeEventManager eventManager,
                        GlobalVar globalVar,
                        SubSystemManager subSystemManager,
                        WriteDataChangeEventManager writeDataChangeEventManager)
        {
            _dataCenter = dataCenter;
            _eventManager = eventManager;
            _globalVar = globalVar;
            _subSystemManager = subSystemManager;
            _writeDataChangeEventManager = writeDataChangeEventManager;

            _category = GetType().FullName!;
        }

        #region ReadData Subscription
        /// <summary>
        /// 訂閱整個 (addr, commandName) 的 Group_CommandRawData。
        /// 只要 group 內任一 CommandRawData.Data 變更，就重新解碼整包並推送一次。
        /// </summary>
        public async Task SubscribeCmd(uint addr, string commandName)
        {
            var cmdData = _globalVar.Real_Devices_ReadData.Get_oneDevice_CmdData_Ref(addr, commandName);
            if (cmdData is null)
            {
                AppLogger.Log_To_File_log(_category, $"[DataHub][SubscribeCmd] 無法訂閱 {commandName}@{addr}, 資料不存在", AppLogLevel.Warning);
                return;
            }

            _eventManager.SubscribeCmd(Context.ConnectionId, addr, commandName, cmdData);
            _connectionSubscriptions.AddOrUpdate(Context.ConnectionId,
                                                 SubscriptionType.ReadData,
                                                 (_, existing) => existing | SubscriptionType.ReadData);
        }

        /// <summary>
        /// 取消訂閱所有 CommandRawData 變更事件。
        /// </summary>
        /// <returns></returns>
        public async Task UnsubscribeAllCmds()
        {
            _eventManager.UnsubscribeAll(Context.ConnectionId);
            UpdateConnectionSubscription(Context.ConnectionId, SubscriptionType.ReadData, removeFlag: true);
        }

        
        #endregion ReadCmd Subscription

        #region ConfigurableData Subscription
        /// <summary>
        /// 訂閱 SubSystem 的 nowConfigurableVars 變更事件。
        /// </summary>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        public async Task SubscribeSubSys(string port, string protocol)
        {
            var subsys = _subSystemManager.GetOneSubSystem_Ref(port, protocol);
            if (subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[DataHub][SubscribeSubSys] 無法訂閱 SubSystem {protocol}@{port}, 子系統不存在", AppLogLevel.Warning);
                return;
            }

            _writeDataChangeEventManager.SubscribeSubSystem(Context.ConnectionId, port, protocol, subsys);
            _connectionSubscriptions.AddOrUpdate(Context.ConnectionId,
                                                 SubscriptionType.ConfigurableData,
                                                 (_, existing) => existing | SubscriptionType.ConfigurableData);
        }

        /// <summary>
        /// 取消訂閱所有 SubSystem 的 nowConfigurableVars 變更事件。
        /// </summary>
        /// <returns></returns>
        public async Task UnsubscribeAllSubSys()
        {
            _writeDataChangeEventManager.UnsubscribeAll(Context.ConnectionId);
            UpdateConnectionSubscription(Context.ConnectionId, SubscriptionType.ConfigurableData, removeFlag: true);
        }
        
        #endregion ConfigurableData Subscription
    
        #region Common Hub Overrides
        /// <summary>
        /// 連線中斷時，取消訂閱所有 CommandRawData 變更事件。
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var subscriptionType))
            {
                if (subscriptionType.HasFlag(SubscriptionType.ReadData))
                {
                    _eventManager.UnsubscribeAll(Context.ConnectionId);
                }

                if (subscriptionType.HasFlag(SubscriptionType.ConfigurableData))
                {
                    _writeDataChangeEventManager.UnsubscribeAll(Context.ConnectionId);
                }

                _connectionSubscriptions.TryRemove(Context.ConnectionId, out _);
            }
            else
            {
                // 安全起見，沒有紀錄時仍解除 ReadData 訂閱，避免殘留
                _eventManager.UnsubscribeAll(Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(ex);
        }
        #endregion Common Hub Overrides

        // 追蹤每條連線目前的訂閱狀態，動態加入或移除指定的訂閱旗標
        private void UpdateConnectionSubscription(string connectionId, SubscriptionType type, bool removeFlag)
        {
            _connectionSubscriptions.AddOrUpdate(
                connectionId,
                _ => removeFlag ? SubscriptionType.None : type,
                (_, existing) =>
                {
                    var updated = removeFlag ? existing & ~type : existing | type;
                    return updated;
                });

            if (_connectionSubscriptions.TryGetValue(connectionId, out var updatedType) && updatedType == SubscriptionType.None)
            {
                _connectionSubscriptions.TryRemove(connectionId, out _);
            }
        }
    }
}
