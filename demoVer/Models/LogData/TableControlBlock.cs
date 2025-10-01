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
        public long? startId {get; set;}
        public long? endId {get; set;}
        public long lastedId_InTable {get; set;}
        public long earliestId_InTable {get; set;}
    }

    public record TableRangeResult(int Count, int? FirstId, int? LastId);

    public enum DATALOG_TABLE
    {
        CAN_TABLE = 1,
        MOD_TABLE = 2,
        BOTH_TABLE = 3
    }
}