using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Concurrent;
using demoVer.Models;
using demoVer.Utils;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using demoVer.Components;

namespace demoVer.Services
{
    public class SubSystemManager
    {
        private string _category = "";

        private readonly ApiManager _apiManager;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, SubSystem>> RegistedSubSystems { get; set; } = new(); // 從LinkStatus API讀到的partition資訊判斷，相同port，相同protocol為同一子系統

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
                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_SettingRange] Start for (port, protocol) = ({port}, {protocol})", AppLogLevel.Debug);
                        await subsys.UpdateSettingRanges_From(RangesDict);

                        //[Debug] 以 Json 格式印出 SettingRanges 
                        // var Json_Str = JsonSerializer.Serialize(subsys.SettingRanges, new JsonSerializerOptions{
                        //     WriteIndented = true
                        // });
                        // Console.WriteLine(Json_Str);

                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_SettingRange] done for (port, protocol) = ({port}, {protocol})", AppLogLevel.Debug);
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
                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_WriteCmdInfo] Start for (port, protocol) = ({port}, {protocol})", AppLogLevel.Debug);
                        await subsys.UpdateInfosForWriteCmd_From(response[protocol]);
                        AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_WriteCmdInfo] done for (port, protocol) = ({port}, {protocol})", AppLogLevel.Debug);
                    }
                }
            }
            catch (Exception e)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][UpdateSubSystem_WriteCmdInfo] Exception: {e.Message}", AppLogLevel.Error);
            }
        }

        /// <summary>
        /// get cmdName by cmdCode in one SubSystem
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
                            if(same)
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
        /// 將寫入命令資料發送到Framework
        /// </summary>
        /// <param name="port"></param>
        /// <param name="protocol"></param>
        /// <param name="writeCmdData_List">準備發送的寫入命令資料組成的List</param>
        /// <param name="PageSelection">選擇SubSystem的鎖</param>
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

            //2. 拿Page對應的寫入鎖，確保同一時間，一個子系統只有一個寫入程序在執行
            var pageWriteLock = subsys.Get_WriteProcessFlowLock_By_PageSelection(PageSelection);
            if (pageWriteLock is null)
            {
                AppLogger.Log_To_File_log(_category, $"[SubSystemManager][WriteCmdToFramework] pageWriteLock is null for (port, protocol) = ({port}, {protocol})", AppLogLevel.Warning);
                return false;
            }
            
            // 等待寫入鎖可用，才進到Try區塊
            await pageWriteLock.WaitAsync();
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
                pageWriteLock.Release();
            }
        }
    }
}