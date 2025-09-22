using System;
namespace demoVer.Models
{
    public class EventLog_Item
    {
        public int Id {get; set;}

        public DateTime Time {get; set;}

        public string Port {get; set;} = string.Empty;

        public int Addr {get; set;}

        public string? SerialNumber {get; set;}

        public string CommandName {get; set;} = string.Empty;

        public string? Event {get; set;}

        public string TriggerState {get; set;} = string.Empty;
    }
}