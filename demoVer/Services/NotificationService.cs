using System;
using System.Collections.Generic;
using System.Threading;
using demoVer.Models;
using demoVer.Utils;

namespace demoVer.Services
{
    public sealed class NotificationsUpdatedEventArgs : EventArgs
    {
        public NotificationsUpdatedEventArgs(Notifications payload)
        {
            Payload = payload;
        }

        public Notifications Payload { get; }
        public IReadOnlyList<NotificationItem> EventLogs => Payload.EventLogs;
        public int UnreadCount => Payload.UnreadCount;
        public long Epoch => Payload.Epoch;
    }

    public class NotificationService
    {
        private readonly string _category = "";
        private readonly ApiManager _apiManager;
        private readonly SemaphoreSlim _refreshGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _markReadGate = new SemaphoreSlim(1, 1);
        private long epoch = 0;
        private int unreadCount = 0;


        public List<NotificationItem> LastNotifications { get; private set; } = new();
        public event EventHandler<NotificationsUpdatedEventArgs>? NotificationsUpdated;
        
        public NotificationService(ApiManager apiManager)
        {
            _category = GetType().FullName!;
            _apiManager = apiManager;
        }

        public void CheckEpoch(long newEpoch)
        {
            //比對是否有變更
            if(newEpoch == epoch) return;

            //有變更，更新epoch並觸發更新
            epoch = newEpoch;
            AppLogger.Log_To_File_log(_category, $"Link Status NotifyEpoch changed to {newEpoch}. Start to call eventlog API", AppLogLevel.Debug);

            TriggerEventLogRefresh();
        }

        private void TriggerEventLogRefresh()
        {
            if (!_refreshGate.Wait(0))
            {
                AppLogger.Log_To_File_log(_category, "Eventlog refresh already running; skip re-entry.", AppLogLevel.Trace);
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    AppLogger.Log_To_File_log(_category, "Begin background fetch of eventlog notifications.", AppLogLevel.Trace);
                    var notifications = await _apiManager.apiRead_eventLog_Notify().ConfigureAwait(false);

                    if (notifications == null)
                    {
                        AppLogger.Log_To_File_log(_category, "Eventlog API returned null notifications.", AppLogLevel.Trace);
                        return;
                    }

                    unreadCount = notifications.UnreadCount;
                    LastNotifications = notifications.EventLogs;
                    AppLogger.Log_To_File_log(
                        _category,
                        $"Eventlog API succeeded. Epoch={notifications.Epoch}, Count={notifications.EventLogs.Count}.",
                        AppLogLevel.Trace);

                    NotifySubscribers(notifications);
                }
                catch (Exception ex)
                {
                    AppLogger.Log_To_File_log(_category, $"Background eventlog fetch failed: {ex}", AppLogLevel.Debug);
                }
                finally
                {
                    _refreshGate.Release();
                }
            });
        }

        private void NotifySubscribers(Notifications payload)
        {
            var handlers = NotificationsUpdated;
            if (handlers == null)
            {
                return;
            }

            var args = new NotificationsUpdatedEventArgs(payload);

            foreach (var del in handlers.GetInvocationList())
            {
                if (del is not EventHandler<NotificationsUpdatedEventArgs> handler)
                {
                    continue;
                }

                try
                {
                    handler.Invoke(this, args);
                }
                catch (Exception ex)
                {
                    AppLogger.Log_To_File_log(_category, $"Notification subscriber threw an exception: {ex}", AppLogLevel.Debug);
                }
            }
        }

        public void Mark_AllEventLogs_AsRead()
        {
            if(!_markReadGate.Wait(0))
            {
                AppLogger.Log_To_File_log(_category, "Eventlog mark-as-read already running; skip re-entry.", AppLogLevel.Trace);
                return;
            }
            
            _ = Task.Run(async () => 
            {
                try
                {
                    AppLogger.Log_To_File_log(_category, "Begin background mark-all-eventlogs-as-read.", AppLogLevel.Trace);
                    var success = await _apiManager.apiPut_eventLog_MarkAsRead().ConfigureAwait(false);

                    if (!success)
                    {
                        AppLogger.Log_To_File_log(_category, "Mark_AllEventLogs_AsRead API returned failure.", AppLogLevel.Trace);
                        return;
                    }

                    //成功後，更新本地狀態
                    unreadCount = 0;
                    foreach(var item in LastNotifications)
                    {
                        item.IsRead = true;
                    }

                    AppLogger.Log_To_File_log(_category, "Mark_AllEventLogs_AsRead API succeeded.", AppLogLevel.Trace);

                    //通知訂閱者
                    var payload = new Notifications
                    {
                        EventLogs = LastNotifications,
                        UnreadCount = unreadCount,
                        Epoch = epoch
                    };
                    NotifySubscribers(payload);
                }
                catch(Exception ex)
                {
                    AppLogger.Log_To_File_log(_category, $"Mark_AllEventLogs_AsRead failed: {ex}", AppLogLevel.Debug);
                }
                finally
                {
                    _markReadGate.Release();
                }
            });
        }
    }

    
}