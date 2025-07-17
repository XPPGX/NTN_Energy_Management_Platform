using demoVer.Services;
using demoVer.Utils;
namespace demoVer.Models
{
    public class allDevice_ModData
    {
        private readonly DataChangeEventManager _eventManager;
        public allDevice_ModData(DataChangeEventManager eventManager)
        {
            _eventManager = eventManager;
        }

        private Dictionary<uint, oneDevice_ModData> _AllDevice_ModData = new();
        private HashSet<uint> _linkedDeviceAddr = new();
        
        private oneDevice_ModData oneDeviceReader_Reuse = new();

        public void Read_oneDevice_ModData(uint addr, List<ModSingleRawCommandFormat> oneDevice_ModData_json)
        {
            Console.WriteLine("Fake");
            
            Console.WriteLine($"目前已有裝置：{string.Join(", ", _AllDevice_ModData.Keys)}");
            //暫存一個Device的Json資料，並判斷是否有ModelName的命令
            bool isModelNameCmdExist = oneDeviceReader_Reuse.LoadSingleDeviceINVData(oneDevice_ModData_json);

            //嘗試取出原本Data中對應addr的DeviceData
            if(_AllDevice_ModData.TryGetValue(addr, out oneDevice_ModData TargetDevice_Data))
            {
                var targetDict = TargetDevice_Data.AllCommandData;
                var sourceDict = oneDeviceReader_Reuse.AllCommandData;

                foreach (var key_value_pair in sourceDict)
                {
                    if(targetDict.TryGetValue(key_value_pair.Key, out var Existing))
                    {
                        Existing.UpdateFrom(key_value_pair.Value);
                    }
                    else
                    {
                        var newData = new ModCommandData();
                        newData.UpdateFrom(key_value_pair.Value);
                        targetDict[key_value_pair.Key] = newData;
                    }
                }
                
                var keysToRemove = targetDict.Keys.Except(sourceDict.Keys).ToList();
                foreach(var key in keysToRemove)
                    targetDict.Remove(key);

                Console.WriteLine("Existing DeviceData refreshed");
                
            }
            else
            {
                var newDevice = new oneDevice_ModData();
                foreach (var kv in oneDeviceReader_Reuse.AllCommandData)
                {
                    var copy = new ModCommandData();
                    copy.UpdateFrom(kv.Value);
                    newDevice.AllCommandData[kv.Key] = copy;
                }

                _AllDevice_ModData[addr] = newDevice;
                Console.WriteLine("New DeviceData");
            }
        }

        public ModCommandData? GetCommandData(uint addr, string cmd)
        {
            if(_AllDevice_ModData.TryGetValue(addr, out var deviceData))
            {
                if(deviceData.AllCommandData.TryGetValue(cmd, out var cmdData))
                {
                    return cmdData;
                }
            }
            return null;
        }
    }

    public class oneDevice_ModData
    {
        // public string ModelName_cache;
        // public string LinkStatus_cache;
        public Dictionary<string, ModCommandData> AllCommandData = new();

        public bool LoadSingleDeviceINVData(List<ModSingleRawCommandFormat> oneDevice_ModData_json)
        {
            bool isModelNameCmdExist = false;
            //清除 reuse變數 暫存的資料
            AllCommandData.Clear();
            
            //解析json，並把每個CMD對應的資料存入AllCommandData
            foreach (var cmd in oneDevice_ModData_json)
            {
                if(string.IsNullOrEmpty(cmd.commandName)) continue;
                if(cmd.commandName == "MFR_MODEL_B0B5"){isModelNameCmdExist = true;}
                var data = new ModCommandData()
                {
                    Data = cmd.data,
                    Scaling = cmd.scaling,
                    BaseUnit = cmd.baseUnit,
                    DataFormat = cmd.dataFormat,
                    Split = cmd.split
                };
                
                //將data放入
                AllCommandData[cmd.commandName] = data;
            }

            return isModelNameCmdExist;
        }
        
    }

}

// if(TargetDevice_Data.AllCommandData.TryGetValue("MFR_MODEL_B0B5", out List<byte> ModelNameData_ori))
// {
//     if(ModelNameData_ori.SequenceEqual(oneDeviceReader_Reuse.AllCommandData["MFR_MODEL_B0B5"]))
//     {
//         Console.WriteLine($"ModelName remains the same : {TargetDevice_Data.ModelName_cache}, use cache");
//     }
//     else
//     {
//         Console.WriteLine($"cache_old = {TargetDevice_Data.ModelName_cache}");
//         //Update Raw
//         TargetDevice_Data.AllCommandData["MFR_MODEL_B0B5"] = oneDeviceReader_Reuse.AllCommandData["MFR_MODEL_B0B5"].ToList();
//         TargetDevice_Data.AllCommandData["MFR_MODEL_B6B11"] = oneDeviceReader_Reuse.AllCommandData["MFR_MODEL_B6B11"].ToList();

//     }
// }