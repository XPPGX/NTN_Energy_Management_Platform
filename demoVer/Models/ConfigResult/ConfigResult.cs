namespace demoVer.Models
{
    public enum ConfigResult
    {
        Success = 0,
        FileNotExist = 1,
        FileContentError = 2,
        OverListCount = 3, //List.Count = 4
        OverStringLength = 4, //DisplayName Length should be <= 7
        FileWriteError = 5,
        UnknownError = -99,
    }
}