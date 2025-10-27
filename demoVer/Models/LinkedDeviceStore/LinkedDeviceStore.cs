using demoVer.Services;
using demoVer.Utils;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage; // for ToArray/Except/ToList

namespace demoVer.Models
{
    public class LinkedDeviceStore
    {
        // 記錄連線Device，以ConcurrentDictionary當集合
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

        public uint? GetFirstLinkedAddr(uint? min, uint? max)
        {
            if(min is null){min = 0;}
            if(max is null){max = 255;}
            
            uint? firstLinkedAddr = null;

            foreach(var linkedAddr in _links.Keys)
            {
                if(linkedAddr <= max && linkedAddr >= min)
                {
                    firstLinkedAddr = linkedAddr;
                    break;
                }
            }

            if(firstLinkedAddr is null)
            {
                Console.WriteLine($"[LinkedDeviceStore][GetFirstLinkedAddr] firstLinkedAddr is null");
                return null;
            }

            Console.WriteLine($"[LinkedDeviceStore][GetFirstLinkedAddr] firstLinkedAddr = {firstLinkedAddr}");
            return firstLinkedAddr;
        }
    }

}