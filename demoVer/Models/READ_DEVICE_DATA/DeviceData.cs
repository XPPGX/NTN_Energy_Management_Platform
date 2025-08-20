using demoVer.Services;
using demoVer.Utils;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Linq; // for ToArray/Except/ToList

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
    }

    public class allDevice_Data
    {
        private string _category;

        private ConcurrentDictionary<uint, oneDevice_Data> _AllDevice_Data = new(); // uint addr 對應每一個oneDevice_Data

        // private readonly HashSet<uint> _linkedDeviceAddr = new(); //紀錄
        // private oneDevice_Data oneDeviceData_reuseReader = new();

        public allDevice_Data()
        {
            _category = GetType().FullName!;
        }

        public void Read_oneDevice_Data(uint addr, List<SingleRawCommand_JsonFormat> oneDevice_JsonData)
        {
            // 讀 key 用快照，避免列舉時被修改
            AppLogger.Log_To_File_log(_category,
                $"目前已有裝置 : {string.Join(", ", _AllDevice_Data.Keys.ToArray())}",
                AppLogLevel.Trace);

            // 暫存一個Device的json資料（區域變數，避免共用欄位的競態）
            var oneDeviceData_reuseReader = new oneDevice_Data();
            oneDeviceData_reuseReader.LoadSingleDeviceData(oneDevice_JsonData);

            if (_AllDevice_Data.TryGetValue(addr, out oneDevice_Data TargetDevice_Data))
            {
                // 已有device的資料在該addr → 合併來源資料到既有物件
                // 內部是普通 Dictionary/List，為避免多執行緒同時寫，對該物件加鎖
                lock (TargetDevice_Data)
                {
                    var targetDict = TargetDevice_Data.AllCommandData;
                    var sourceDict = oneDeviceData_reuseReader.AllCommandData;

                    foreach (var (cmdName, sourceGroupsData) in sourceDict)
                    {
                        if (!targetDict.TryGetValue(cmdName, out var targetGroupsData))
                        {
                            targetGroupsData = new Group_CommandRawData();
                            targetDict[cmdName] = targetGroupsData;
                        }

                        // 合併每個 group
                        foreach (var (groupIndex, groupData) in sourceGroupsData.Groups)
                        {
                            if (targetGroupsData.Groups.TryGetValue(groupIndex, out var existingCmdData))
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

                        // 移除 target 有但 source 沒有的 group
                        var groupsToRemove = targetGroupsData.Groups.Keys
                            .Except(sourceGroupsData.Groups.Keys)
                            .ToList();

                        foreach (var gidx in groupsToRemove)
                            targetGroupsData.Groups.Remove(gidx);
                    }
                }
            }
            else
            {
                // 沒有device的資料在該addr，建立新的device資料
                var newDevice = new oneDevice_Data();
                foreach (var (cmdName, groupsData) in oneDeviceData_reuseReader.AllCommandData)
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

                // 直接賦值（原子替換該 key 的值；若不存在即新增）
                _AllDevice_Data[addr] = newDevice;

                // _linkedDeviceAddr.Add(addr);
                AppLogger.Log_To_File_log(_category, $"New DeviceData, addr = {addr}", AppLogLevel.Trace);
            }
        }

        public void Remove_oneDevice_Data(uint addr)
        {
            // 行為不變：若不存在直接 return；若存在只 ReleaseMemory（不移除 key）
            if (_AllDevice_Data.ContainsKey(addr) == false) return;

            if (_AllDevice_Data.TryGetValue(addr, out var deviceData))
            {
                // 若其他執行緒可能仍讀取這個物件，這裡只清其內容，外部要確保一致性需求
                lock (deviceData)
                {
                    deviceData.ReleaseMemory();
                }
                //移除斷開連線的資料 !!!! 
                _AllDevice_Data.TryRemove(addr, out var remove_oneDevice_Data);
            }
        }

        public Group_CommandRawData? GetCommandGroups(uint addr, string cmd)
        {
            if (_AllDevice_Data.TryGetValue(addr, out var deviceData))
            {
                // AppLogger.Log_To_File_log(_category, $"[DeviceData][GetCommandGroups]Get DeviceData", AppLogLevel.Debug);
                if (deviceData.AllCommandData.TryGetValue(cmd, out var groups))
                {
                    // AppLogger.Log_To_File_log(_category, $"[DeviceData][GetCommandGroups]Get groups, addr = {addr}", AppLogLevel.Debug);
                    // var copy = new Group_CommandRawData();
                    // foreach (var (groupIndex, cmdData) in groups.Groups)
                    // {
                    //     var cmdCopy = new CommandRawData();
                    //     cmdCopy.UpdateFrom(cmdData); // 複製來源的內容
                    //     copy.Groups[groupIndex] = cmdCopy;
                    // }
                    // return copy;
                    return groups; // 傳參考（行為不變）
                }
            }
            return null;
        }

        


        public oneDevice_Data Get_oneDeviceData(uint addr)
        {
            // 行為不變：找不到回 null（保持你原本的語義）
            if (_AllDevice_Data.TryGetValue(addr, out var oneDeviceData))
            {
                return oneDeviceData;
            }
            
            return null;
        }

        public oneDevice_Data? Get_oneDevice_DataSnapshot(uint addr)
        {
            if (!_AllDevice_Data.TryGetValue(addr, out var deviceData))
                return null;

            var copy = new oneDevice_Data();
            foreach (var (cmdName, groupData) in deviceData.AllCommandData)
            {
                var newGroup = new Group_CommandRawData();
                foreach (var (groupIndex, cmdData) in groupData.Groups)
                {
                    var cmdCopy = new CommandRawData();
                    cmdCopy.UpdateFrom(cmdData); // 你已有的複製方法
                    newGroup.Groups[groupIndex] = cmdCopy;
                }
                copy.AllCommandData[cmdName] = newGroup;
            }
            return copy;
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
        private string _category = "";
        public Dictionary<string, Group_CommandRawData> AllCommandData = new(); //<cmd, Groups>

        public oneDevice_Data()
        {
            _category = GetType().FullName!;
        }

        public void LoadSingleDeviceData(List<SingleRawCommand_JsonFormat> oneDevice_JsonData)
        {
            // 清除 reuse變數 暫存的資料
            ReleaseMemory();

            // 解析json，並把每個CMD對應的資料存入AllCommandData
            foreach (var cmd in oneDevice_JsonData)
            {
                if (string.IsNullOrEmpty(cmd.commandName)) continue;

                var tmpRawData = new CommandRawData()
                {
                    Data = cmd.data,
                    Scaling = cmd.scaling,
                    BaseUnit = cmd.baseUnit,
                    DataFormat = cmd.dataFormat,
                    Split = cmd.split,
                    Signed = cmd.signed,
                    Mask = cmd.mask,
                    GroupIndex = cmd.groupIndex,
                    BitFields = cmd.bitFields,
                    isSNnumber = cmd.isSNnumber,
                };

                string cmdName = cmd.commandName;
                uint groupIndex = cmd.groupIndex ?? 0;

                // group 不存在於對應的 cmdName
                if (!AllCommandData.TryGetValue(cmdName, out var groupData))
                {
                    groupData = new Group_CommandRawData();
                    AllCommandData[cmdName] = groupData;
                }

                groupData.Groups[groupIndex] = tmpRawData;
            }
        }

        public void ReleaseMemory()
        {
            foreach (var cmdData in AllCommandData)
            {
                foreach (var oneCmdData in cmdData.Value.Groups.Values)
                {
                    // 解掛 OnChanged
                    oneCmdData.ClearAllActions();
                    oneCmdData.Data.Clear();
                    oneCmdData.Data.TrimExcess(); // 縮小 capacity
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

        public Dictionary<string, string> Get_Cmd_unit_Dict()
        {
            try
            {
                Dictionary<string, string> cmd_unit_dict = new Dictionary<string, string>();

                foreach(var (cmd, groups) in AllCommandData)
                {
                    //取 index 0 的 group(groups內一定有 index 0 的 Data)
                    var unit_str = groups.Groups[0].BaseUnit;

                    //把cmd 跟 unit 加入 cmd_unit_dict 中
                    cmd_unit_dict[cmd] = unit_str;

                    AppLogger.Log_To_File_log(_category, $"[oneDevice_Data][Get_Cmd_unit_Dict] cmd : {cmd}, unit_str = {unit_str}", AppLogLevel.Trace);
                }

                return cmd_unit_dict;  
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[oneDevice_Data][Get_Cmd_unit_Dict] Error : {e}", AppLogLevel.Error);
            }

            return null;
        }
    }
}
