using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using demoVer.Services;
namespace demoVer.Broadcast
{
    public class DataHub : Hub
    {
        private readonly DataCenter _dataCenter;
        private readonly DataChangeEventManager _eventManager;
        private readonly GlobalVar _globalVar;
        
        public DataHub( DataCenter dataCenter,
                        DataChangeEventManager eventManager,
                        GlobalVar globalVar)
        {
            _dataCenter = dataCenter;
            _eventManager = eventManager;
            _globalVar = globalVar;
        }

        /// <summary>
        /// 訂閱整個 (addr, commandName) 的 Group_CommandRawData。
        /// 只要 group 內任一 CommandRawData.Data 變更，就重新解碼整包並推送一次。
        /// </summary>
        public async Task SubscribeGroup(uint addr, string commandName)
        {
            var groups = _globalVar.Device_ReadData.GetCommandGroups(addr, commandName);
            if(groups is null)
            {
                Console.WriteLine($"無法訂閱 {commandName}@{addr}, 資料不存在");
                return;
            }

            _eventManager.SubscribeGroup(Context.ConnectionId, addr, commandName, groups);
        }

        public async Task UnsubscribeAllGroups()
        {
            _eventManager.UnsubscribeAll(Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            _eventManager.UnsubscribeAll(Context.ConnectionId);
            await base.OnDisconnectedAsync(ex);
        }
    }
}
