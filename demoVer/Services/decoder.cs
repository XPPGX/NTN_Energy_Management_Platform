using demoVer.Models;
using demoVer.Interfaces;
using demoVer.Utils;

namespace demoVer.Services
{
    public class GroupsDataDecoder : IGroupsDataDecoder
    {
        public object? Decode(Group_CommandRawData groupData, string cmdName = "")
        {
            try
            {
                if (groupData.Groups.TryGetValue(0, out var cmdRawData))
                {
                    AppLogger.Log_To_File_txt($"[Decode][cmd : {cmdName}][Get group (0)] exist");

                    // 1. 處理 bitField -> output : string
                    if (cmdRawData.BitFields != null && cmdRawData.BitFields.Any())
                    {
                        AppLogger.Log_To_File_txt($"[Decode : BitFields] processing...");
                        int rawValue = BytesToInt(cmdRawData.Data, cmdRawData.Signed.GetValueOrDefault());
                        // AppLogger.Log_To_File_txt($"rawValue = {rawValue}");
                        var activeFields = new List<string>();
                        foreach (var bf in cmdRawData.BitFields)
                        {   
                           
                            int bit = (int)(bf.bit ?? 0);
                            int length = (int)(bf.length ?? 0);
                            int mask = ((1 << length) - 1) << bit;
                            int extracted = (rawValue & mask) >> bit;

                            if (bf.valueMap != null && bf.valueMap.TryGetValue(extracted.ToString(), out var mapped))
                            {
                                // AppLogger.Log_To_File_txt($"bit = {bit}, length = {length}, mask = {mask}, extracted = {extracted}, mapped = {mapped}");
                                if (!string.IsNullOrEmpty(mapped))
                                {
                                    activeFields.Add(mapped);
                                }
                            }
                        }
                        var return_val = (activeFields.Count > 0) ? string.Join(",", activeFields) : null;
                        AppLogger.Log_To_File_txt($"return val = {return_val}");
                        AppLogger.Log_To_File_txt($"[Decode : BitFields] done.");
                        return return_val.ToString();
                    }
                    // 2. 處理 Mask -> 暫時不處理
                    else if (cmdRawData.Mask != null && cmdRawData.Mask.Any())
                    {
                        AppLogger.Log_To_File_txt($"[Decode][cmd : {cmdName}] Mask not implemented");
                    }
                    // 3. 處理 split
                    else if (!string.IsNullOrEmpty(cmdRawData.Split))
                    {
                        AppLogger.Log_To_File_txt($"[Decode : split] processing...");

                        int size = int.Parse(cmdRawData.Split[0].ToString());
                        string seperator = cmdRawData.Split.Substring(1); // comma

                        var results = new List<string>();

                        for (int i = 0; i < cmdRawData.Data.Count; i += size)
                        {
                            var slice = cmdRawData.Data.Skip(i).Take(size).ToList();
                            if (slice.Count < size)
                                break;

                            var decoded = ApplyFormat(slice, cmdRawData);
                            
                            // if (decoded is double d)
                            // {
                            //     results.Add(d.ToString("G"));
                            // }
                            // else
                            // {
                            //     results.Add(decoded?.ToString() ?? "");
                            // }
                            results.Add(decoded?.ToString() ?? "");
                        }
                        string return_val = string.Join(seperator, results);
                        AppLogger.Log_To_File_txt($"return_val = {return_val}");
                        AppLogger.Log_To_File_txt($"[Decode : split] done...");
                        return return_val;
                    }
                    // 4. 處理 Group (Group 內的 count > 1)
                    else if (groupData.Groups.Count > 1)
                    {
                        AppLogger.Log_To_File_txt($"[Decode : group] processing...");
                        // 先對group內部依照index排序(同一塊記憶體只是sorted看見的記憶體順序不同)
                        var sorted = groupData.Groups.OrderBy(pair => pair.Key);
                        List<byte> tmpConcatByteData_List = new List<byte>();

                        foreach (var cmdData in groupData.Groups.Values) // 只traverse values
                        {
                            tmpConcatByteData_List.AddRange(cmdData.Data);
                        }

                        if (tmpConcatByteData_List.Count == 0)
                        {
                            return null;
                        }

                        var formatAppliedData = ApplyFormat(tmpConcatByteData_List, cmdRawData);
                        AppLogger.Log_To_File_txt($"return_val = {formatAppliedData}");
                        AppLogger.Log_To_File_txt($"[Decode : group] done...");
                        return formatAppliedData; // return string for ASCII, double for numeric
                    }

                    // 5. 處理基本 ASCII / Numeric
                    AppLogger.Log_To_File_txt($"[Decode : Format] processing...");
                    var temp_return_val = ApplyFormat(cmdRawData.Data, cmdRawData);
                    AppLogger.Log_To_File_txt($"temp_return_val = {temp_return_val}");
                    AppLogger.Log_To_File_txt($"[Decode : Format] done...");
                    return temp_return_val;
                }
                else
                {

                    AppLogger.Log_To_File_txt($"[Decode][cmd : {cmdName}][Get group (0)] not exist");
                    return null;
                }

                return null;
            }
            catch (Exception ex)
            {
                AppLogger.Log_To_File_txt($"[Decode Error] : {ex.Message}");
                return null;
            }
        }

        private object ApplyFormat(List<byte> data, CommandRawData cmdRawData)
        {
            // Case : ASCII
            if (string.Equals(cmdRawData.DataFormat, "ASCII", StringComparison.OrdinalIgnoreCase))
            {
                
                return System.Text.Encoding.ASCII.GetString(data.ToArray()).Trim('\0');
            }
            // Case : Numeric
            else
            {
                int tmp_val = BytesToInt(data, cmdRawData.Signed.GetValueOrDefault());
                double result = ScalingComputer.MultOperation_doubleVer(tmp_val, cmdRawData.Scaling);

                // return result.ToString("G"); // 可能有精度問題
                return result;
            }
        }

        private int BytesToInt(List<byte> data, bool signed, int shiftBytes = 0)
        {
            if (data.Count > 4)
            {
                throw new NotSupportedException($"BytesToInt 不支援超過 4 bytes (len={data.Count})");
            }

            var effectiveData = (shiftBytes > 0 && shiftBytes < data.Count) ? data.Skip(shiftBytes).ToList() : data;

            int value = 0;
            for (int i = 0; i < effectiveData.Count && i < 4; i++)
            {
                value |= effectiveData[i] << (8 * (effectiveData.Count - 1 - i));
            }

            if (signed == true)
            {
                int bits = effectiveData.Count * 8;
                int signBit = 1 << (bits - 1);

                //??
                if ((value & signBit) != 0)
                {
                    value -= (1 << bits);
                }
            }

            return value;
        }
    }
}