using System.Text.Json.Serialization;

namespace demoVer.Models
{
    public class Notifications
    {
        [JsonPropertyName("eventLogs")]
        public List<NotificationItem> EventLogs { get; set; } = new List<NotificationItem>();

        [JsonPropertyName("unreadCount")]
        public int UnreadCount {get; set;} = 0;
        
        [JsonPropertyName("epoch")]
        public long Epoch {get; set;} = 0;
    }
    public sealed class NotificationItem
    {
        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;
        [JsonPropertyName("port")]
        public string Port { get; set; } = string.Empty;
        [JsonPropertyName("addr")]
        public uint Addr { get; set; } = 0;
        [JsonPropertyName("commandName")]
        public string CommandName { get; set; } = string.Empty;
        [JsonPropertyName("eventText")]
        public string EventText { get; set; } = string.Empty;
        [JsonPropertyName("isRead")]
        public bool IsRead { get; set; }
    }
}
