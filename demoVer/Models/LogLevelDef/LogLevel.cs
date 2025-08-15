namespace demoVer.Models
{
    public enum AppLogLevel
    {
        Trace       = 0,    //訊息全開
        Debug       = 1,    //模組級別          的訊息
        Information = 2,    //狀態切換、系統級別 的訊息
        Warning     = 3,    
        Error       = 4,    //拋出例外          的訊息
        Critical    = 5,
        None        = 6
    }
}