using System.Text.Json;
using System.Text.Json.Serialization;
namespace demoVer.Models
{
    public enum InitStage
    {
        FromJson    = 1,
        FromDevice  = 2,
        Done        = 3
    }

    public class Sys_InitData
    {
        [JsonPropertyName("mdl_name")]
        public string mdlName {get; set;}
    }
}