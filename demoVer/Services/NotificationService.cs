namespace demoVer.Services;

/// <summary>
/// 通知服務 - 用於在應用程式各處發送通知到通知鈴鐺
/// </summary>
public class NotificationService
{
    /// <summary>
    /// 當有新通知時觸發的事件
    /// </summary>
    public event Action<NotificationItem>? OnNotificationReceived;

    /// <summary>
    /// 發送通知
    /// </summary>
    /// <param name="message">通知訊息</param>
    /// <param name="type">通知類型: info, warning, success</param>
    public void SendNotification(string message, string type = "info")
    {
        var notification = new NotificationItem
        {
            Id = GenerateId(),
            Message = message,
            Time = GetTimeAgo(DateTime.Now),
            Type = type,
            IsRead = false,
            Timestamp = DateTime.Now
        };

        OnNotificationReceived?.Invoke(notification);
    }

    /// <summary>
    /// 發送資訊通知
    /// </summary>
    public void SendInfo(string message)
    {
        SendNotification(message, "info");
    }

    /// <summary>
    /// 發送警告通知
    /// </summary>
    public void SendWarning(string message)
    {
        SendNotification(message, "warning");
    }

    /// <summary>
    /// 發送成功通知
    /// </summary>
    public void SendSuccess(string message)
    {
        SendNotification(message, "success");
    }

    private int _idCounter = 1000;
    private int GenerateId()
    {
        return Interlocked.Increment(ref _idCounter);
    }

    private string GetTimeAgo(DateTime timestamp)
    {
        var timeSpan = DateTime.Now - timestamp;
        
        if (timeSpan.TotalMinutes < 1)
            return "剛剛";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} 分鐘前";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} 小時前";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} 天前";
        
        return timestamp.ToString("yyyy/MM/dd");
    }
}

/// <summary>
/// 通知項目資料模型
/// </summary>
public class NotificationItem
{
    public int Id { get; set; }
    public string Message { get; set; } = "";
    public string Time { get; set; } = "";
    public string Type { get; set; } = "info"; // info, warning, success
    public bool IsRead { get; set; } = false;
    public DateTime Timestamp { get; set; }
}
