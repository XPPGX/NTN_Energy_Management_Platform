using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using demoVer.Services;
namespace demoVer.Broadcast
{
    public class DataHub : Hub
    {
        private readonly DataCenter _dataCenter;
        private readonly DataChangeEventManager _eventManager;

        public DataHub(DataCenter dataCenter, DataChangeEventManager eventManager)
        {
            _dataCenter = dataCenter;
            _eventManager = eventManager;
        }

        public async Task SubscribeCommand(string commandName, uint addr)
        {
            var cmdData = _dataCenter.MOD_DATA.GetCommandData(addr, commandName);
            if (cmdData != null)
            {
                _eventManager.Subscribe(Context.ConnectionId, addr, commandName, cmdData);
            }
            else
            {
                Console.WriteLine($"無法訂閱 {commandName}@{addr}，資料不存在");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            _eventManager.UnsubscribeAll(Context.ConnectionId);
            await base.OnDisconnectedAsync(ex);
        }
    }
}
