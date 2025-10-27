using System.Collections.Concurrent;
using demoVer.Models;

namespace demoVer.Services
{
    public class LinkAddrManager
    {
        private readonly ConcurrentDictionary<uint, byte> _links = new();
        public event Action? linkChanged;

        public bool Link(uint addr)
        {
            if (_links.TryAdd(addr, 0))
            {
                linkChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool Unlink(uint addr)
        {
            if (_links.TryRemove(addr, out _))
            {
                linkChanged?.Invoke();
                return true;
            }
            return false;
        }

        public uint[] Snapshot() => _links.Keys.ToArray();

        public uint[] SnapshotSorted()
        {
            var arr = _links.Keys.ToArray();
            Array.Sort(arr);
            return arr;
        }
    }
}