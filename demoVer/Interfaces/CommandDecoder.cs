using demoVer.Models;

namespace demoVer.Interfaces
{
    public interface IGroupsDataDecoder
    {
        object? Decode(Group_CommandRawData groups, string cmdName = "");
    }
}