namespace demoVer.Interfaces
{
    public interface PausableWorker
    {
        Task EnableAsync();
        Task DisableAsync(TimeSpan? delay = null);
    }
}