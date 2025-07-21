namespace demoVer.Interfaces
{
    public interface CommandDecoder
    {
        object? Decode(List<byte> data, float scaling);
    }
}