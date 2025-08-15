using demoVer.Services;
using demoVer.Utils;
using System.Text.Json;
using System.Collections.Concurrent;

namespace demoVer.Models
{
    public class LinkedDeviceStore
    {
        //記錄連線Device，以ConcurrentDictionary當集合
        private readonly ConcurrentDictionary<uint, byte> _links = new();
        public event Action? linkChanged;

        public bool Link(uint addr)
        {
            if(_links.TryAdd(addr, 0))
            {
                linkChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool Unlink(uint addr)
        {
            if(_links.TryRemove(addr, out _))
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

    public class allDevice_Data
    {
        private string _category;
        
        private Dictionary<uint, oneDevice_Data> _AllDevice_Data = new(); //uint addr 對應每一個oneDevice_Data
        
        // private readonly HashSet<uint> _linkedDeviceAddr = new(); //紀錄
        private oneDevice_Data oneDeviceData_reuseReader = new();

        public allDevice_Data()
        {
            _category = GetType().FullName!;
        }

        public void Read_oneDevice_Data(uint addr, List<SingleRawCommand_JsonFormat> oneDevice_JsonData)
        {
            Console.WriteLine($"目前已有裝置 : {string.Join(", ", _AllDevice_Data.Keys)}");

            //暫存一個Device的json資料
            oneDeviceData_reuseReader.LoadSingleDeviceData(oneDevice_JsonData);

            bool isAddrExist = _AllDevice_Data.TryGetValue(addr, out oneDevice_Data TargetDevice_Data);
            
            if(isAddrExist)
            {//已有deivce的資料在該addr
                
                var targetDict = TargetDevice_Data.AllCommandData;
                var sourceDict = oneDeviceData_reuseReader.AllCommandData;

                foreach(var (cmdName, sourceGroupsData) in sourceDict)
                {
                    if(!targetDict.TryGetValue(cmdName, out var targetGroupsData))
                    {
                        targetGroupsData = new Group_CommandRawData();
                        targetDict[cmdName] = targetGroupsData;
                    }

                    foreach(var (groupIndex, groupData) in sourceGroupsData.Groups)
                    {
                        if(targetGroupsData.Groups.TryGetValue(groupIndex, out var existingCmdData))
                        {
                            existingCmdData.UpdateFrom(groupData);
                        }
                        else
                        {
                            var newCmdData = new CommandRawData();
                            newCmdData.UpdateFrom(groupData);
                            targetGroupsData.Groups[groupIndex] = newCmdData;
                        }
                    }

                    var groupsToRemove = targetGroupsData.Groups.Keys
                        .Except(sourceGroupsData.Groups.Keys)
                        .ToList();
                    
                    foreach(var gidx in groupsToRemove)
                        targetGroupsData.Groups.Remove(gidx);
                }
            }
            else
            {//沒有device的資料在該addr，建立新的device資料
                
                var newDevice = new oneDevice_Data();
                foreach(var (cmdName, groupsData) in oneDeviceData_reuseReader.AllCommandData)
                {
                    var newGroup = new Group_CommandRawData();
                    foreach (var (groupIndex, cmdData) in groupsData.Groups)
                    {
                        var cmdCopy = new CommandRawData();
                        cmdCopy.UpdateFrom(cmdData);
                        newGroup.Groups[groupIndex] = cmdCopy;
                    }
                    
                    newDevice.AllCommandData[cmdName] = newGroup;
                }
                _AllDevice_Data[addr] = newDevice;
                // _linkedDeviceAddr.Add(addr);
                AppLogger.Log_To_File_log(_category, $"New DeviceData, addr = {addr}", AppLogLevel.Trace);
            }
        }
        
        public void Remove_oneDevice_Data(uint addr)
        {
            if(_AllDevice_Data.ContainsKey(addr) == false) return;

            if(_AllDevice_Data.TryGetValue(addr, out var deviceData))
            {
                deviceData.ReleaseMemory();
            }
        }

        public Group_CommandRawData? GetCommandGroups(uint addr, string cmd)
        {
            if(_AllDevice_Data.TryGetValue(addr, out var deviceData))
            {
                if(deviceData.AllCommandData.TryGetValue(cmd, out var groups))
                {
                    return groups;
                }
            }
            return null;
        }

        // public IEnumerable<uint> GetLinkAddresses()
        // {
        //     return _linkedDeviceAddr;
        // }

        public void debug_PrintAllDeviceData()
        {
            // var options = new JsonSerializerOptions
            // {
            //     WriteIndented = true,
            //     IncludeFields = true
            // };

            // string tmp_json = JsonSerializer.Serialize(_AllDevice_Data, options);
            // AppLogger.Log_To_File_Json(_AllDevice_Data);
        }
    }

    public class oneDevice_Data
    {
        
        public Dictionary<string, Group_CommandRawData> AllCommandData = new(); //<cmd, Groups>
        
        public void LoadSingleDeviceData(List<SingleRawCommand_JsonFormat> oneDevice_JsonData)
        {
            //清除 reuse變數 暫存的資料
            ReleaseMemory();
            
            //解析json，並把每個CMD對應的資料存入AllCommandData
            foreach (var cmd in oneDevice_JsonData)
            {
                if(string.IsNullOrEmpty(cmd.commandName)) continue;
                var tmpRawData = new CommandRawData()
                {
                    Data        = cmd.data,
                    Scaling     = cmd.scaling,
                    BaseUnit    = cmd.baseUnit,
                    DataFormat  = cmd.dataFormat,
                    Split       = cmd.split,
                    Signed      = cmd.signed,
                    Mask        = cmd.mask,
                    GroupIndex  = cmd.groupIndex,
                    BitFields   = cmd.bitFields,
                    isSNnumber  = cmd.isSNnumber,
                };

                string cmdName = cmd.commandName;
                uint groupIndex = cmd.groupIndex ?? 0;
                
                //group不存在於對應的cmdName
                if(!AllCommandData.TryGetValue(cmdName, out var groupData))
                {
                    groupData = new Group_CommandRawData();
                    AllCommandData[cmdName] = groupData;
                }
                
                groupData.Groups[groupIndex] = tmpRawData;

            }
        }

        public void ReleaseMemory()
        {
            foreach(var cmdData in AllCommandData)
            {
                foreach(var oneCmdData in cmdData.Value.Groups.Values)
                {
                    //解掛OnChanged
                    oneCmdData.ClearAllActions();
                    oneCmdData.Data.Clear();
                    oneCmdData.Data.TrimExcess(); //縮小capacity
                    oneCmdData.BitFields?.Clear();
                    oneCmdData.BitFields = null;
                }
            }
            AllCommandData.Clear();
        }

        public void debug_PrintAllCommandData()
        {
            // var options = new JsonSerializerOptions
            // {
            //     WriteIndented = true,
            //     IncludeFields = true
            // };

            // string tmp_json = JsonSerializer.Serialize(AllCommandData, options);
            // AppLogger.Log_To_File_Json(AllCommandData);
        }
    }
    
}