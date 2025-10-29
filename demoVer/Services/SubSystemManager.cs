using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Concurrent;
using demoVer.Models;
using demoVer.Utils;

namespace demoVer.Services
{
    public class SubSystemManager
    {
        private string _category = "";
        public ConcurrentDictionary<string, ConcurrentDictionary<string, SubSystem>> RegistedSubSystems { get; set; } = new(); // 從LinkStatus API讀到的partition資訊判斷相同protocol得到的子系統
        public SubSystemManager()
        {
            _category = GetType().FullName!;
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

        
        
        
    }
}