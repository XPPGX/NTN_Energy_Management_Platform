namespace demoVer.Services;

/// <summary>
/// Bottom Row 功能的集中配置
/// 移植到其他专案时，只需修改这个文件即可
/// </summary>
public static class BottomRowConstants
{
    /// <summary>
    /// 应用程序端口号，用于用户设置目录路径
    /// </summary>
    public const string AppPort = "5040";

    /// <summary>
    /// 最大按钮数量
    /// </summary>
    public const int MaxButtonCount = 4;

    /// <summary>
    /// 图片上传最大大小（字节）
    /// </summary>
    public const long MaxUploadBytes = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 默认图标的路径（对应按钮 1-4）
    /// 修改这里可以更换默认图标
    /// </summary>
    public static readonly string[] DefaultImagePaths = new[]
    {
        "/images/btmRowSvgs/home.svg",    // Button 1: Home
        "/images/btmRowSvgs/ac.svg",      // Button 2: AC/Inverter
        "/images/btmRowSvgs/battery.svg", // Button 3: Battery
        "/images/btmRowSvgs/wire.svg"     // Button 4: Link/Wire
    };

    /// <summary>
    /// 默认路由配置（对应按钮 1-4）
    /// </summary>
    public static readonly (string Route, string ImagePath)[] DefaultButtonConfigs = new[]
    {
        ("/", DefaultImagePaths[0]),
        ("/Inverter_setting", DefaultImagePaths[1]),
        ("/Battery_setting", DefaultImagePaths[2]),
        ("/Link_status", DefaultImagePaths[3])
    };

    /// <summary>
    /// 配置文件名称
    /// </summary>
    public const string ConfigFileName = "BottomRowConfig.json";
}
