using demoVer.Models;
using demoVer.Interfaces;
using demoVer.Utils;

namespace demoVer.Services
{
    public class GroupsDataDecoder : IGroupsDataDecoder
    {
        private string _category;

        public GroupsDataDecoder()
        {
            _category = GetType().FullName!;
        }

        public object? Decode(Group_CommandRawData groupData, string cmdName = "")
        {
            try
            {
                if (groupData.Groups.TryGetValue(0, out var cmdRawData))
                {
                    AppLogger.Log_To_File_log(_category, $"[Decode][cmd : {cmdName}][Get group (0)] exist", AppLogLevel.Trace);

                    // 1. 處理 bitField -> output : string
                    if (cmdRawData.BitFields != null && cmdRawData.BitFields.Any())
                    {
                        AppLogger.Log_To_File_log(_category, $"[Decode : BitFields] processing...", AppLogLevel.Trace);
                        int rawValue = BytesToInt(cmdRawData.Data, cmdRawData.Signed.GetValueOrDefault());
                        // AppLogger.Log_To_File_log($"rawValue = {rawValue}");
                        var activeFields = new List<string>();
                        foreach (var bf in cmdRawData.BitFields)
                        {   
                           
                            int bit = (int)(bf.bit ?? 0);
                            int length = (int)(bf.length ?? 0);
                            int mask = ((1 << length) - 1) << bit;
                            int extracted = (rawValue & mask) >> bit;

                            if (bf.valueMap != null && bf.valueMap.TryGetValue(extracted.ToString(), out var mapped))
                            {
                                // AppLogger.Log_To_File_log($"bit = {bit}, length = {length}, mask = {mask}, extracted = {extracted}, mapped = {mapped}");
                                if (!string.IsNullOrEmpty(mapped))
                                {
                                    activeFields.Add(mapped);
                                }
                            }
                        }
                        var return_val = (activeFields.Count > 0) ? string.Join(",", activeFields) : null;
                        AppLogger.Log_To_File_log(_category, $"return val = {return_val}", AppLogLevel.Trace);
                        AppLogger.Log_To_File_log(_category, $"[Decode : BitFields] done.", AppLogLevel.Trace);
                        return return_val.ToString();
                    }
                    // 2. 處理 Mask -> 暫時不處理
                    else if (cmdRawData.Mask != null && cmdRawData.Mask.Any())
                    {
                        AppLogger.Log_To_File_log(_category, $"[Decode][cmd : {cmdName}] Mask not implemented", AppLogLevel.Trace);
                    }
                    // 3. 處理 split
                    else if (!string.IsNullOrEmpty(cmdRawData.Split))
                    {
                        AppLogger.Log_To_File_log(_category, $"[Decode : split] processing...", AppLogLevel.Trace);

                        int size = int.Parse(cmdRawData.Split[0].ToString());
                        string seperator = cmdRawData.Split.Substring(1); // comma

                        var results = new List<string>();

                        for (int i = 0; i < cmdRawData.Data.Count; i += size)
                        {
                            var slice = cmdRawData.Data.Skip(i).Take(size).ToList();
                            if (slice.Count < size)
                                break;

                            AppLogger.Log_To_File_log(_category, $"[Decode : split] {slice[0]}", AppLogLevel.Trace);
                            if(slice[0] == 255 && string.Equals(cmdName, "MFR_REVISION_B0B5", StringComparison.Ordinal))
                            {
                                results.Add("X");
                                continue;
                            }
                            
                            var decoded = ApplyFormat(slice, cmdRawData);
        
                            //For Cmd : Revision
                            
                            
                            results.Add(decoded?.ToString() ?? "");
                        
                        }
                        string return_val = string.Join(seperator, results);
                        AppLogger.Log_To_File_log(_category, $"return_val = {return_val}", AppLogLevel.Trace);
                        AppLogger.Log_To_File_log(_category, $"[Decode : split] done...", AppLogLevel.Trace);
                        return return_val;
                    }
                    // 4. 處理 Group (Group 內的 count > 1)
                    else if (groupData.Groups.Count > 1)
                    {
                        AppLogger.Log_To_File_log(_category, $"[Decode : group] processing...", AppLogLevel.Trace);
                        // 先對group內部依照index排序(同一塊記憶體只是sorted看見的記憶體順序不同)
                        var sorted = groupData.Groups.OrderBy(pair => pair.Key);
                        List<byte> tmpConcatByteData_List = new List<byte>();
                        

                        
                        // var values = groupData.Groups.Values.ToList();
                        // for(int i = values.Count - 1 ; i >= 0 ; i ++)
                        // {
                        //     tmpConcatByteData_List.AddRange(values[i].Data);
                        // }
                        if(groupData.Groups[0].DataFormat == "ASCII")
                        {
                            foreach (var cmdData in groupData.Groups.Values) // 只traverse values
                            {
                                tmpConcatByteData_List.AddRange(cmdData.Data);
                            }

                        }
                        else if(groupData.Groups[0].DataFormat == "Numeric")
                        {
                            foreach (var pair in groupData.Groups.OrderByDescending(pair => pair.Key))
                            {
                                var RawData = pair.Value;
                                tmpConcatByteData_List.AddRange(RawData.Data);
                            }
                            
                        }

                        if (tmpConcatByteData_List.Count == 0)
                        {
                            return null;
                        }

                        
                        AppLogger.Log_To_File_log(_category, $"group_List = {string.Join(", ", tmpConcatByteData_List)}", AppLogLevel.Trace);

                        var formatAppliedData = ApplyFormat(tmpConcatByteData_List, cmdRawData);
                        AppLogger.Log_To_File_log(_category, $"return_val = {formatAppliedData}", AppLogLevel.Trace);
                        AppLogger.Log_To_File_log(_category, $"[Decode : group] done...", AppLogLevel.Trace);
                        return formatAppliedData; // return string for ASCII, double for numeric
                    }

                    // 5. 處理基本 ASCII / Numeric
                    AppLogger.Log_To_File_log(_category, $"[Decode : Format] processing...", AppLogLevel.Trace);
                    var temp_return_val = ApplyFormat(cmdRawData.Data, cmdRawData);
                    AppLogger.Log_To_File_log(_category, $"temp_return_val = {temp_return_val}", AppLogLevel.Trace);
                    AppLogger.Log_To_File_log(_category, $"[Decode : Format] done...", AppLogLevel.Trace);
                    return temp_return_val;
                }
                else
                {

                    AppLogger.Log_To_File_log(_category, $"[Decode][cmd : {cmdName}][Get group (0)] not exist", AppLogLevel.Trace);
                    return null;
                }

                return null;
            }
            catch (Exception ex)
            {
                AppLogger.Log_To_File_log(_category, $"[Decode Error] : {ex.Message}", AppLogLevel.Trace);
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
            // for (int i = 0; i < effectiveData.Count && i < 4; i++)
            // {
            //     value |= effectiveData[i] << (8 * (effectiveData.Count - 1 - i));
            // }
            for(int i = effectiveData.Count - 1 ; i >= 0 && i < 4 ; i --)
            {
                value |= effectiveData[i] << (8 * i);
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