namespace demoVer.Models
{
    public class TableControlBlock
    {
        public int nowTablePage {get; set;} = 1;
        public int nowQueryRecordsNum {get; set;} = 0;
        public int nowTotalPages {get; set;} = 0;
        public DateTime? startTime {get; set;}
        public DateTime? endTime {get; set;}
        public DateTime lastedTime_InTable {get; set;}
        public DateTime earliestTime_InTable {get; set;}
        public int? startId {get; set;}
        public int? endId {get; set;}
        public int lastedId_InTable {get; set;}
        public int earliestId_InTable {get; set;}
    }

    public record TableRangeResult(int Count, int? FirstId, int? LastId);
}