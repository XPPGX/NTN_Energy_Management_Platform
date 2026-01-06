using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Concurrent;
using demoVer.Models;
using demoVer.Utils;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using demoVer.Components;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.Connections;
using SubSystem = demoVer.Models.SubSystem;
namespace demoVer.Services
{
    public class SubSystemManager
    {
        private string _category = "";

        private readonly ApiManager _apiManager;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, SubSystem>> RegistedSubSystems { get; set; } = new(); // 從LinkStatus API讀到的partition資訊判斷，相同port，相同protocol為同一子系統
        public ConcurrentDictionary<string, (string, string)> Mapping_Addr_To_SubSys {get; set;} = new();
        public SubSystemManager(ApiManager apiManager)
        {
            _category = GetType().FullName!;
            _apiManager = apiManager;
        }

        public List<SubSystem> GetAllSubSystems_Ref_In_List()
        {
            List<SubSystem> result = new List<SubSystem>();
            foreach (var protocolDict in RegistedSubSystems.Values)
            {
                foreach (var subsys in protocolDict.Values)
                {
                    result.Add(subsys);
                }
            }
            return result;
        }

        public SubSystem? GetOneSubSystem_Ref(string port, string protocol)
        {
            if (RegistedSubSystems.TryGetValue(port, out var protocolDict))
            {
                if (protocolDict.TryGetValue(protocol, out var subsys))
                {
                    return subsys;
                }
            }
            return null;
        }

        /// <summary>
        /// 取得所有子系統的port, realAddr, storingAddr清單
        /// </summary>
        /// <returns>List : port, protocol, realAddr, storingAddr </returns>
        public List<(string, string,uint, uint)> GetAllSubSystem_PortAddr_pairInList()
        {
            List<(string, string, uint, uint)> portAddrPair_List = new();
            GetAllSubSystems_Ref_In_List().ForEach(subsys =>
            {
                var subsys_addrList = subsys.GetAddrList();
                foreach (var realAddr in subsys_addrList)
                {
                    string port = subsys.Port;
                    string protocol = subsys.Protocol;
                    uint storingAddr = realAddr + Custom.getAddrOffsetByPort(port);
                    portAddrPair_List.Add((port, protocol,realAddr, storingAddr));
                }
            });
            // Console.WriteLine($"[SubSystemManager][GetAllSubSystem_PortAddr_pairInList] AllSubSysRef_Count = {GetAllSubSystems_Ref_In_List().Count}, portAddrPair_Count = {portAddrPair_List.Count}");
            return portAddrPair_List;
        }

        public SemaphoreSlim? GetSubSys_FireAndForgetLock(string port, string protocol)
        {
            var subsys = GetOneSubSystem_Ref(port, protocol);
            if (subsys is not null)
            {
                return subsys.SubSystem_FireAndForget_Lock;
            }
            return null;
        }
        public async Task UpdateSubSystem_SettingRange(string port, string protocol)
        {
            try
            {
                if (RegistedSubSystems.TryGetValue(port, out var protocolDict))
                {
                    if (protocolDict.TryGetValue(protocol, out var subsys))
                    {

                        // Call SettingRange API to get the latest ranges
                        var response = await _apiManager.apiRead_SettingRange(port, protocol);
                        if (response is null)
                        {
                            AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_SettingRange] response is null for Port:{port}, Protocol:{protocol}", AppLogLevel.Debug);
                            return;
                        }

                        // Update SubSystem's SettingRanges
                        var RangesDict = response.ranges;
                        await subsys.UpdateSettingRanges_From(RangesDict);

                        //[Debug] 以 Json 格式印出 SettingRanges 
                        // var Json_Str = JsonSerializer.Serialize(subsys.SettingRanges, new JsonSerializerOptions{
                        //     WriteIndented = true
                        // });
                        // Console.WriteLine(Json_Str);

                    }
                }
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_SettingRange] Exception: {e.Message}", AppLogLevel.Error);
            }
        }

        public async Task UpdateSubSystem_WriteCmdInfo(string port, string protocol)
        {
            try
            {
                if (RegistedSubSystems.TryGetValue(port, out var protocolDict))
                {
                    if (protocolDict.TryGetValue(protocol, out var subsys))
                    {
                        // Call Get_Write_API to get the latest Write CMD Info
                        var response = await _apiManager.apiReadReal_SettingData(port, protocol);

                        // Check if response is null
                        if (response is null)
                        {
                            AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_WriteCmdInfo] response is null for Port:{port}, Protocol:{protocol}", AppLogLevel.Debug);
                            return;
                        }

                        // Check if the specific protocol data is null
                        if (response[protocol] is null)
                        {
                            AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_WriteCmdInfo] response[{protocol}] is null for Port:{port}, Protocol:{protocol}", AppLogLevel.Debug);
                            return;
                        }

                        // Update SubSystem's Write CMD info using the overwrite approach.
                        await subsys.UpdateInfosForWriteCmd_From(response[protocol]);
                    }
                }
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_WriteCmdInfo] Exception: {e.Message}", AppLogLevel.Error);
            }
        }

        public async Task UpdateSubSystem_CmdFormat(string port, string protocol)
        {
            try
            {
                //Call API
                var response = await _apiManager.apiRead_CmdFormat(port, protocol);

                //Check response
                if (response is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_CmdFormat] response is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                    return;
                }
                if (response.values is null || response.values.Count == 0)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_CmdFormat] response.values is null or empty for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                    return;
                }

                var subsys = GetOneSubSystem_Ref(port, protocol);
                if (subsys is null)
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSys temManager][UpdateSubSystem_CmdFormat] subsys is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                    return;
                }

                await subsys.Update_ReadCmdFormat(response);
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_CmdFormat] Exception: {e.Message}", AppLogLevel.Error);
            }
        }

        /// <summary>
        /// get FixedCmdName by FixedCmdCode in one SubSystem
        /// </summary>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        /// <param name="cmdCode">ClearCmdCode without "0x"</param>
        /// <returns></returns>
        public string cmdCode_MappingTo_CmdName_InOneSubSystem(string port, string protocol, string cmdCode)
        {
            if (RegistedSubSystems.TryGetValue(port, out var protocolDict))
            {
                if (protocolDict.TryGetValue(protocol, out var subsys))
                {
                    return subsys.cmdCode_mappingTo_cmdName(cmdCode);
                }
            }
            return string.Empty;
        }

        public Dictionary<string, string> GetCmd_Unit_Dict_InOneSubSystem(string port, string protocol)
        {
            var subsys = GetOneSubSystem_Ref(port, protocol);
            if (subsys is null)
            {
                return new Dictionary<string, string>();
            }
            return subsys.GetReadCmd_Unit_Dict();
        }

        /// <summary>
        /// create one write cmd data
        /// </summary>
        /// <param name="port">必填</param>
        /// <param name="protocol">必填</param>
        /// <param name="clearCmdCode">必填</param>
        /// <param name="RealNumber">可選</param>
        /// <param name="RealBits">可選</param>
        /// <param name="RealPerAddrValues">可選</param>
        /// <returns>一個 Post_RealSingleRawSettingCMD_JsonFormat型態的物件</returns>
        public Post_RealSingleRawSettingCMD_JsonFormat? createOneWriteCmdData(string port, string protocol, string clearCmdCode,
                    double? RealNumber = null, Dictionary<string, int>? RealBits = null, List<AddrValue>? RealPerAddrValues = null)
        {
            try
            {
                Post_RealSingleRawSettingCMD_JsonFormat writeCmdData = new Post_RealSingleRawSettingCMD_JsonFormat()
                {
                    Type = port,
                    Protocol = protocol,
                    CommandName = cmdCode_MappingTo_CmdName_InOneSubSystem(port, protocol, clearCmdCode),
                    Target = null,
                    AddrValues = null
                };

                var subsys = GetOneSubSystem_Ref(port, protocol);
                string DataFormat = string.Empty;
                bool isPerAddr = false;
                (DataFormat, isPerAddr) = subsys!.GetSettingCmdFormat_and_IsPerAddr_By_ClearCmdCode(clearCmdCode);

                WriteVal? Target_tmp = new WriteVal();
                switch (DataFormat)
                {
                    case "Numeric":
                        if (RealNumber is null)
                        {
                            AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] RealNumber is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                            return null;
                        }
                        // Check if the RealNumber is same as current value
                        if (RealNumber == subsys.InfosForWriteCmd[clearCmdCode].Target!.Number)
                        {
                            AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] RealNumber is same as current value for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Debug);
                            return null;
                        }
                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] Creating Numeric Write CMD Data for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode}), Real = {RealNumber}, Target.Number = {subsys.InfosForWriteCmd[clearCmdCode].Target!.Number}", AppLogLevel.Debug);
                        // assign RealNumber to Target
                        Target_tmp.Number = RealNumber;
                        Target_tmp.Text = null;
                        Target_tmp.Bits = null;
                        writeCmdData.Target = Target_tmp;
                        break;

                    case "BitField":
                        if (isPerAddr)
                        {
                            if (RealPerAddrValues is null)
                            {
                                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] RealPerAddrValues is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                                return null;
                            }
                            bool AddrValues_AllTheSame = true;
                            //取得目前SubSystem中該CmdCode的AddrValues
                            var nowAddrValues = GetAddrValues_Copy(port, protocol, clearCmdCode);
                            if(nowAddrValues is null)
                            {
                                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] nowAddrValues is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                                return null;
                            }

                            //!!目前只支援對整個子系統的AddrValues設定相同的值，所以只要
                            //取RealPerAddrValues的某一個Addr的Bits去比對nowAddrValues的其他Addr的Bits是否都相同即可
                            var Real_FirstAddrInSubSystem_Bits = RealPerAddrValues[0].Value.Bits;
                            foreach (var addrValue in nowAddrValues)
                            {
                                //比對每一個Addr的Bits是否都與Real_FirstAddrInSubSystem_Bits相同
                                foreach (var key_val_pair in Real_FirstAddrInSubSystem_Bits!)
                                {
                                    if (addrValue.Value.Bits != null && addrValue.Value.Bits.TryGetValue(key_val_pair.Key, out var nowBitVal))
                                    {
                                        if (nowBitVal == key_val_pair.Value)
                                        {
                                            AddrValues_AllTheSame = true;
                                        }
                                        else
                                        {
                                            AddrValues_AllTheSame = false;
                                            break;
                                        }
                                    }
                                }

                                if (AddrValues_AllTheSame is false) break;
                            }
                            if (AddrValues_AllTheSame is true)
                            {
                                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] RealPerAddrValues is same as current value for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode}), for all addr in the SubSystem", AppLogLevel.Debug);
                                return null;
                            }
                            writeCmdData.AddrValues = RealPerAddrValues; // By Reference，所以不用創建新物件(由用到的頁面負責創建並傳進來)
                        }
                        else
                        {
                            if (RealBits is null)
                            {
                                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] RealBits is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                                return null;
                            }
                            // Check if the RealBits is same as current value
                            bool same = true;
                            var nowBits = GetRealBitsFormat(port, protocol, clearCmdCode);
                            foreach (var key_val_pair in RealBits)
                            {
                                if (nowBits != null && nowBits.TryGetValue(key_val_pair.Key, out var nowBitVal))
                                {
                                    Console.WriteLine($"[SubSystemManager][createOneWriteCmdData] nowBitVal for key {key_val_pair.Key} = {nowBitVal}, RealBitVal = {key_val_pair.Value}");
                                    if (nowBitVal == key_val_pair.Value)
                                    {
                                        same = true;
                                    }
                                    else
                                    {
                                        same = false;
                                        break;
                                    }
                                }
                            }
                            if (same)
                            {
                                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] RealBits is same as current value for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Debug);
                                return null;
                            }
                            // assign RealBits to Target
                            Target_tmp.Number = null;
                            Target_tmp.Text = null;
                            Target_tmp.Bits = RealBits;
                            writeCmdData.Target = Target_tmp;
                        }
                        break;
                    default:
                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] Unknown DataFormat: {DataFormat} for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                        return null;
                }
                return writeCmdData;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][createOneWriteCmdData] Exception: {e.Message}", AppLogLevel.Error);
                return null;
            }
        }

        /// <summary>Get Real Bits Format from one SubSystem's Write CMD Info</summary>
        /// <returns>回傳一個子系統中clearCmdCode對應的Target.Bits的Copy</returns>
        public Dictionary<string, int>? GetRealBitsFormat(string port, string protocol, string clearCmdCode)
        {
            var subsys = GetOneSubSystem_Ref(port, protocol);
            if (subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetRealBitsFormat] subsys is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                return null;
            }

            if (!subsys.InfosForWriteCmd.TryGetValue(clearCmdCode, out var oneCmdData))
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetRealBitsFormat] oneCmdData is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }

            if (!string.Equals(oneCmdData.DataFormat, "BitField", StringComparison.OrdinalIgnoreCase))
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetRealBitsFormat] DataFormat is not BitField for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }

            try
            {
                Dictionary<string, int>? nowBits = new Dictionary<string, int>(oneCmdData.Target!.Bits!);
                return nowBits;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetRealBitsFormat] Exception: {e.Message}", AppLogLevel.Error);
                return null;
            }
        }
        
        public string? GetBitKey_FromSubSys(string port, string protocol, string clearCmdCode, int bitPostion)
        {
            var subsys = GetOneSubSystem_Ref(port, protocol);
            if (subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetBitKey_FromSubSys] subsys is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                return null;
            }

            return subsys.GetBitKey_FromCmdInfo(clearCmdCode, bitPostion);
        }

        public List<AddrValue>? GetAddrValues_Copy(string port, string protocol, string clearCmdCode)
        {
            var subsys = GetOneSubSystem_Ref(port, protocol);
            if (subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetAddrValues_Copy] subsys is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                return null;
            }
            if (!subsys.InfosForWriteCmd.TryGetValue(clearCmdCode, out var oneCmdData))
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetAddrValues_Copy] oneCmdData is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }

            if (!string.Equals(oneCmdData.DataFormat, "BitField", StringComparison.OrdinalIgnoreCase))
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetAddrValues_Copy] DataFormat is not BitField for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }
            try
            {

                List<AddrValue> addrValues_copy = subsys.InfosForWriteCmd[clearCmdCode].AddrValues!.Select(x => x.deepClone()).ToList();
                return addrValues_copy;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][GetAddrValues_Copy] Exception: {e.Message}", AppLogLevel.Error);
                return null;
            }
        }
        /// <summary>
        /// 比較某個命令新讀回來的資料與要設定到Framework的某個資料是否相同
        /// </summary>
        /// <param name="cmdInfo">新讀回來的資料</param>
        /// <param name="writeCmdData">要設定到Framework的資料</param>
        /// <returns>是否相同(bool)</returns>
        public bool IsSame_cmdInfo_writeCmdData(GET_RealSingleRawSettingCMD_JsonFormat cmdInfo, Post_RealSingleRawSettingCMD_JsonFormat writeCmdData)
        {
            if (cmdInfo is null || writeCmdData is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][IsSame_cmdInfo_writeCmdData] cmdInfo or writeCmdData is null", AppLogLevel.Warning);
                return false;
            }
            try
            {
                bool same = false;
                switch (cmdInfo.DataFormat)
                {
                    case "Numeric":
                        same = false; 
                        if (cmdInfo.Target!.Number == writeCmdData.Target!.Number) { same = true; }
                        break;
                    case "BitField":
                        // BitField 需要分兩種情況處理
                        if (cmdInfo.IsPerAddr is true)
                        {   // Bits holds by each addr
                            same = true;
                            var nowAddrValues = cmdInfo.AddrValues!.OrderBy(item => item.Addr).ToList(); //依照Addr排序
                            var writeAddrValues = writeCmdData.AddrValues!.OrderBy(item => item.Addr).ToList(); //依照Addr排序
                            if (nowAddrValues.Count != writeAddrValues.Count)
                            {
                                same = false;
                                break;
                            }

                            int addrLength = nowAddrValues.Count;
                            for(int i = 0 ; i < addrLength ; i ++)
                            {
                                if (nowAddrValues[i].Addr != writeAddrValues[i].Addr)
                                {
                                    same = false;
                                    break;
                                }
                                var nowOneAddrBits = nowAddrValues[i].Value.Bits;
                                var writeOneAddrBits = writeAddrValues[i].Value.Bits;
                                foreach (var key_val_pair in writeOneAddrBits!)
                                {
                                    if(nowOneAddrBits != null && nowOneAddrBits.TryGetValue(key_val_pair.Key, out var nowBitVal))
                                    {
                                        if (nowBitVal == key_val_pair.Value)
                                        {
                                            same = true;
                                        }
                                        else
                                        {
                                            same = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {   // Bits holds by the subSystem
                            same = true;
                            var nowBits = cmdInfo.Target!.Bits;
                            var writeBits = writeCmdData.Target!.Bits;
                            foreach (var key_val_pair in writeBits!)
                            {
                                if (nowBits != null && nowBits.TryGetValue(key_val_pair.Key, out var nowBitVal))
                                {
                                    if (nowBitVal == key_val_pair.Value)
                                    {
                                        same = true;
                                    }
                                    else
                                    {
                                        same = false;
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
                return same;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][IsSame_cmdInfo_writeCmdData] Exception: {e.Message}", AppLogLevel.Error);
                return false;
            }

        }


        /// <summary>
        /// 回傳尚未設定成功的CMD Data清單(如果長度為0，表示全部都已經設定成功)
        /// </summary>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        /// <param name="writeCmdData_List"></param>
        /// <returns></returns>
        public async Task<List<Post_RealSingleRawSettingCMD_JsonFormat>> Is_CmdDatas_AlreadySetting(string port, string protocol, List<Post_RealSingleRawSettingCMD_JsonFormat> writeCmdData_List)
        {
            List<Post_RealSingleRawSettingCMD_JsonFormat> WriteFailed_CmdData_List = new List<Post_RealSingleRawSettingCMD_JsonFormat>();
            //1. 取得新的當前值，並組裝成 Dictionary供比對
            var res = await _apiManager.apiReadReal_SettingData(port, protocol);
            if (res is null || res[protocol] is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Is_CmdData_AlreadySetting] response is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                WriteFailed_CmdData_List = writeCmdData_List;
                return WriteFailed_CmdData_List;
            }
            var latestCmdInfo = res[protocol];
            var new_InfosForWriteCmd_tmp = new Dictionary<string, GET_RealSingleRawSettingCMD_JsonFormat>(); //Key: CommandName
            foreach (var cmd in latestCmdInfo)
            {
                new_InfosForWriteCmd_tmp[cmd.CommandName] = cmd;
            }

            //2. 逐一比對
            foreach (var writeCmdData in writeCmdData_List)
            {
                string cmdName = writeCmdData.CommandName;
                if (!new_InfosForWriteCmd_tmp.TryGetValue(cmdName, out var cmdInfo))
                {
                    AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Is_CmdData_AlreadySetting] cmdInfo is null for (port, protocol, cmdName) = ({port}, {protocol}, {cmdName})", AppLogLevel.Trace);
                    continue;
                }
                bool isSame = IsSame_cmdInfo_writeCmdData(cmdInfo, writeCmdData);
                if (isSame is false)
                {
                    WriteFailed_CmdData_List.Add(writeCmdData);
                }
            }
            return WriteFailed_CmdData_List;
        }

        /// <summary>
        /// 取得目前某命令的值
        /// </summary>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        /// <param name="clearCmdCode"></param>
        /// <returns></returns>
        public double Get_Now_Cmd_value(string port, string protocol, string clearCmdCode)
        {
            var subsys = GetOneSubSystem_Ref(port, protocol);
            if(subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Get_Now_Cmd_value] subsys is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                return 0.0;
            }
            var value = GetNowValueHelper.parse_Numeric_Val(subsys.InfosForWriteCmd, clearCmdCode);
            
            return value;
        }
        
        public async Task<bool> Set_To_Default_BAT(string port, string protocol)
        {
            List<Post_RealSingleRawSettingCMD_JsonFormat> configurableCmdDatas = new List<Post_RealSingleRawSettingCMD_JsonFormat>();

            var subsys = GetOneSubSystem_Ref(port, protocol);
            if (subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_To_Default_BAT] subsys is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                return false;
            }

            try
            {
                //CC (0x00B0)
                var cc_Default_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00B0");
                if(cc_Default_WriteData is not null){configurableCmdDatas.Add(cc_Default_WriteData);}

                //CV (0x00B1)
                var cv_Default_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00B1");
                if(cv_Default_WriteData is not null){configurableCmdDatas.Add(cv_Default_WriteData);}

                //FV (0x00B2)
                var fv_Default_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00B2");
                if(fv_Default_WriteData is not null){configurableCmdDatas.Add(fv_Default_WriteData);}

                //BAT_RCHG (0x00BB)
                var fv_range = subsys.GetSettingRange_ByClearCmdCode("00BB");
                var BAT_RCHG_nowValue = Get_Now_Cmd_value(port, protocol, "00BB");
                if(fv_range is not null)
                {
                    if(BAT_RCHG_nowValue > fv_range.defaultVal)
                    {
                        var new_BAT_RCHG_value = fv_range.defaultVal;
                        var bat_rchg_WriteData = createOneWriteCmdData(port, protocol, "00BB", RealNumber: new_BAT_RCHG_value);
                        if(bat_rchg_WriteData is not null){configurableCmdDatas.Add(bat_rchg_WriteData);}
                    }
                }
                //TC (0x00B3)
                var tc_Default_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00B3");
                if(tc_Default_WriteData is not null){configurableCmdDatas.Add(tc_Default_WriteData);}

                //CCT (0x00B5)
                var cct_Default_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00B5");
                if(cct_Default_WriteData is not null){configurableCmdDatas.Add(cct_Default_WriteData);}
                
                //CVT (0x00B6)
                var cvt_Default_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00B6");
                if(cvt_Default_WriteData is not null){configurableCmdDatas.Add(cvt_Default_WriteData);}

                //FVT (0x00B7)
                var fvt_Default_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00B7");
                if(fvt_Default_WriteData is not null){configurableCmdDatas.Add(fvt_Default_WriteData);}

                //CurveStage in (0x00B4 "BIT_6")
                //CCT_Enable in (0x00B4 "BIT_8")
                //CVT_Enable in (0x00B4 "BIT_9")
                //FVT_Enable in (0x00B4 "BIT_10")
                var curveConfig_WriteData = Get_BitField_Cmd_Default_Write_Data(subsys, "00B4", new List<int>(){6,8,9,10});
                if(curveConfig_WriteData is not null){configurableCmdDatas.Add(curveConfig_WriteData);}

                bool isWriteSuccess = await WriteCmdsToFramework(port, protocol, configurableCmdDatas, 0);
                
                return isWriteSuccess;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_To_Default_BAT] Exception: {e.Message}", AppLogLevel.Error);
                return false;
            }
        }

        public async Task<bool> Set_To_Default_INV(string port, string protocol)
        {
            List<Post_RealSingleRawSettingCMD_JsonFormat> configurableCmdDatas = new List<Post_RealSingleRawSettingCMD_JsonFormat>();

            var subsys = GetOneSubSystem_Ref(port, protocol);
            if (subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_To_Default_INV] subsys is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                return false;
            }

            try
            {
                //Output_ACF_Set (0x0103 "BIT_0") programmer defined default value = 1
                var Output_ACF_Set_WriteData = Get_BitField_Cmd_Default_Write_Data(subsys, "0103", new List<int>(){0}, otherDefaultVal: 1);
                if(Output_ACF_Set_WriteData is not null){configurableCmdDatas.Add(Output_ACF_Set_WriteData);}

                //Output_ACV_Set (0x0102 "BIT_0") programmer defined default value = 1
                var Output_ACV_Set_WriteData = Get_BitField_Cmd_Default_Write_Data(subsys, "0102", new List<int>(){0}, otherDefaultVal: 1);
                if(Output_ACV_Set_WriteData is not null){configurableCmdDatas.Add(Output_ACV_Set_WriteData);}

                //CHG_Enable (0x0100 "BIT_2")
                //GRID_Enable (0x0100 "BIT_3")
                var INV_OPERATION = Get_AddrCmd_Default_Write_Data(subsys, "0100", new List<int>(){2, 3});
                if(INV_OPERATION is not null){configurableCmdDatas.Add(INV_OPERATION);}

                //OutputPrio (0x0101 "BIT_0")
                //ChargingPrio (0x0101 "BIT_2")
                var INV_CONFIG_WriteData = Get_BitField_Cmd_Default_Write_Data(subsys, "0101", new List<int>(){0, 2});
                if(INV_CONFIG_WriteData is not null){configurableCmdDatas.Add(INV_CONFIG_WriteData);}

                //Battery_Alarm_value
                var BAT_ALM_val_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00B9");
                if(BAT_ALM_val_WriteData is not null){configurableCmdDatas.Add(BAT_ALM_val_WriteData);}

                //Battery_Shutdown_value
                var BAT_SHDN_val_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00BA");
                if(BAT_SHDN_val_WriteData is not null){configurableCmdDatas.Add(BAT_SHDN_val_WriteData);}

                //Battery_recharge_value
                var BAT_RCHG_val_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00BB");
                if(BAT_SHDN_val_WriteData is not null){configurableCmdDatas.Add(BAT_RCHG_val_WriteData);}

                var BAT_OV_ALM_val_WriteData = Get_Numeric_Cmd_Default_Write_Data(subsys, "00BC");
                if(BAT_OV_ALM_val_WriteData is not null){configurableCmdDatas.Add(BAT_OV_ALM_val_WriteData);}

                return true;
            }
            catch(Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_To_Default_INV] Exception: {e.Message}", AppLogLevel.Error);
                return false;
            }
        }
        public Post_RealSingleRawSettingCMD_JsonFormat? Get_Numeric_Cmd_Default_Write_Data(SubSystem subsys, string clearCmdCode)
        {
            if(subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_Numeric_Cmd_To_Default] subsys is null", AppLogLevel.Warning);
                return null;
            }

            var port = subsys.Port;
            var protocol = subsys.Protocol;
            var default_val = subsys.GetSettingRange_ByClearCmdCode(clearCmdCode)?.defaultVal;
            if(default_val is not null)
            {
                var writeCmdData = createOneWriteCmdData(port, protocol, clearCmdCode, RealNumber: default_val);
                return writeCmdData;
            }

            return null;
        }

        public Post_RealSingleRawSettingCMD_JsonFormat? Get_BitField_Cmd_Default_Write_Data(SubSystem subsys, string clearCmdCode, List<int> startBitList, int? otherDefaultVal = null)
        {
            if(subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_BitField_Cmd_To_Default] subsys is null", AppLogLevel.Warning);
                return null;
            }

            var port = subsys.Port;
            var protocol = subsys.Protocol;
            
            int default_value_int = 0;
            //Get default value as int, for following bit operation
            if(otherDefaultVal is not null)
            {
                //For programmer self defined default value
                default_value_int = otherDefaultVal.Value;
            }
            else
            {
                //For protocol defined default value
                var default_value = subsys.GetSettingRange_ByClearCmdCode(clearCmdCode)?.defaultVal;
                default_value_int = Convert.ToInt32(default_value);
            }

            //Get cmdInfo, which will be used to get all Bit Keys
            if(!subsys.InfosForWriteCmd.TryGetValue(clearCmdCode, out var cmdInfo))
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_BitField_Cmd_To_Default] cmdInfo is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }
            if(cmdInfo.BitControl is null || cmdInfo.BitControl.Count == 0)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_BitField_Cmd_To_Default] cmdInfo.BitControl is null or empty for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }

            //Get notBits format, which we fill each default value into.
            var nowBits = GetRealBitsFormat(port, protocol, clearCmdCode);
            if(nowBits is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_BitField_Cmd_To_Default] nowBits is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }
            
            //Fill each Bit Key with default value
            foreach (var bitControl in cmdInfo.BitControl)
            {
                if(!startBitList.Contains(bitControl.Bit))
                {
                    //Skip bits not in startBitList
                    continue;
                }

                var bitKey = bitControl.Name;
                var bitPosition = bitControl.Bit;
                var bitLength = bitControl.Length;

                var defaultBitValue_for_thisKey = (default_value_int >> bitPosition) & ((1 << bitLength) - 1);
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_BitField_Cmd_To_Default] For (port, protocol, cmdCode, bitKey) = ({port}, {protocol}, {clearCmdCode}, {bitKey}), defaultBitValue = {defaultBitValue_for_thisKey}", AppLogLevel.Debug);    
                nowBits[bitKey] = defaultBitValue_for_thisKey;
            }

            var curveConfig_WriteData = createOneWriteCmdData(port, protocol, clearCmdCode, RealBits: nowBits);
            return curveConfig_WriteData;
        }

        public Post_RealSingleRawSettingCMD_JsonFormat? Get_AddrCmd_Default_Write_Data(SubSystem subsys, string clearCmdCode, List<int> startBitList, int? otherDefaultVal = null)
        {
            if(subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_AddrCmd_To_Default] subsys is null", AppLogLevel.Warning);
                return null;
            }

            var port = subsys.Port;
            var protocol = subsys.Protocol;

            int default_value_int = 0;
            //Get default value as int, for following bit operation
            if(otherDefaultVal is not null)
            {
                //For programmer self defined default value
                default_value_int = otherDefaultVal.Value;
            }
            else
            {
                //For protocol defined default value
                var default_value = subsys.GetSettingRange_ByClearCmdCode(clearCmdCode)?.defaultVal;
                default_value_int = Convert.ToInt32(default_value);
            }

            //Get cmdInfo, which will be used to get all Bit Keys
            if(!subsys.InfosForWriteCmd.TryGetValue(clearCmdCode, out var cmdInfo))
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_AddrCmd_To_Default] cmdInfo is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }
            if(cmdInfo.BitControl is null || cmdInfo.BitControl.Count == 0)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_AddrCmd_To_Default] cmdInfo.BitControl is null or empty for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }

            //Get AddrValues format, which we fill each default value into
            var nowAddrValues = GetAddrValues_Copy(port, protocol, clearCmdCode);
            if(nowAddrValues is null ||  nowAddrValues.Count == 0)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_AddrCmd_To_Default] nowAddrValues is null for (port, protocol, cmdCode) = ({port}, {protocol}, {clearCmdCode})", AppLogLevel.Warning);
                return null;
            }

            //Fill each Bit Key with default value for each Addr
            foreach (var addrValue in nowAddrValues)
            {
                //Reference to Bits
                var Bits_tmp = addrValue.Value.Bits;
                if(Bits_tmp is not null)
                {
                    foreach (var bitControl in cmdInfo.BitControl)
                    {
                        if(!startBitList.Contains(bitControl.Bit))
                        {
                            //Skip bits not in startBitList
                            continue;
                        }

                        var bitKey = bitControl.Name;
                        var bitPosition = bitControl.Bit;
                        var bitLength = bitControl.Length;

                        var defaultBitValue_for_thisKey = (default_value_int >> bitPosition) & ((1 << bitLength) - 1);
                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][Set_AddrCmd_To_Default] For (port, protocol, cmdCode, addr, bitKey) = ({port}, {protocol}, {clearCmdCode}, {addrValue.Addr}, {bitKey}), defaultBitValue = {defaultBitValue_for_thisKey}", AppLogLevel.Debug);    
                        Bits_tmp[bitKey] = defaultBitValue_for_thisKey;
                    }
                }
            }

            var addrCmd_WriteData = createOneWriteCmdData(port, protocol, clearCmdCode, RealPerAddrValues: nowAddrValues);
            return addrCmd_WriteData;
        }
        /// <summary>
        /// 將寫入命令資料發送到Framework
        /// </summary>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        /// <param name="writeCmdData_List">準備發送的寫入命令資料組成的List</param>
        /// <param name="PageSelection">指示由哪個頁面寫入SubSystem</param>
        /// <returns>發送成功回傳True，但不代表設定成功，要看Page對應的FailedCmd_List數量，等於0才是全部設定成功，沒設定成功的話要顯示在UI上，說哪些沒有設定成功</returns>
        public async Task<bool> WriteCmdsToFramework(string port, string protocol, List<Post_RealSingleRawSettingCMD_JsonFormat> writeCmdData_List, int PageSelection)
        {
            //1. 取得子系統
            var subsys = GetOneSubSystem_Ref(port, protocol);
            if (subsys is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][WriteCmdToFramework] subsys is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                return false;
            }
            if (writeCmdData_List.Count == 0)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][WriteCmdToFramework] writeCmdData_List is empty for (port, protocol) = ({port}, {protocol})", AppLogLevel.Debug);
                return false;
            }

            //2. 拿寫入鎖，確保同一時間，一個子系統只有一個寫入程序在執行
            var WriteLock = subsys.Get_WriteProcessFlowLock();
            if (WriteLock is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][WriteCmdToFramework] WriteLock is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                return false;
            }
            
            // 等待寫入鎖可用，才進到Try區塊
            await WriteLock.WaitAsync();
            try
            {
                //做3次嘗試，如果List長度還是大於0就代表有失敗的CMD，就退出這波寫入流程
                for(int i = 0 ; i < 2 ; i ++)
                {
                    //3. 逐一發送WriteAPI
                    foreach (var writeCmdData in writeCmdData_List)
                    {
                        var response = await _apiManager.apiWrite_SetSingleCMD(writeCmdData);
                        await Task.Delay(30); //避免短時間內發送過多請求導致Framework無法處理
                        if (response is false)
                        {
                            AppLogger.Log_To_File_log(_category, $"[SubSystemManager][WriteCmdToFramework] response is null for (port, protocol, cmdName) = ({port}, {protocol}, {writeCmdData.CommandName})", AppLogLevel.Warning);
                        }
                    }
                    //4. 發送完畢後，檢查是否有失敗的CMD，並以失敗的CmdData組成新的List，準備下一輪嘗試
                    writeCmdData_List = await Is_CmdDatas_AlreadySetting(port, protocol, writeCmdData_List);
                }
                //5. 將失敗的CMD記錄到子系統中對應PageSelection的FailedCmd_List
                subsys.Update_WriteFailedCmdsList_By_PageSelection(PageSelection, writeCmdData_List);

                //6. 更新子系統的Write CMD Info
                await UpdateSubSystem_WriteCmdInfo(port, protocol);

                return true;
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][WriteCmdToFramework] Exception: {e.Message}", AppLogLevel.Error);
                return false;
            }
            finally
            {
                WriteLock.Release();
            }
        }
    }
}