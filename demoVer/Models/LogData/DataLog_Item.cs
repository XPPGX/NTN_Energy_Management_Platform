using System;
namespace demoVer.Models
{
    public class DataLog_Item
    {
        public long Id {get; set;}

        public DateTime? Timestamp {get; set;}

        public string? Port {get; set;} = string.Empty;

        public int? device_addr {get; set;}

        public decimal? READ_VIN {get; set;}

        public decimal? READ_IIN {get; set;}

        public decimal? READ_TEMPERATURE_1 {get; set;}

        public decimal? READ_FAN_SPEED_1 {get; set;}

        public decimal? READ_FAN_SPEED_2 {get; set;}

        public decimal? READ_AC_VOUT {get; set;}

        public decimal? READ_OP_WATT {get; set;}

        public decimal? READ_VBAT {get; set;}

        public decimal? READ_CHG_CURR {get; set;}
        
        public decimal? READ_AC_IOUT {get; set;}

        public string? MFR_MODEL {get; set;} = string.Empty;

        public string? MFR_SERIAL {get; set;} = string.Empty;

        public string? INV_STATUS {get; set;} = string.Empty;

        public string? INV_FAULT {get; set;} = string.Empty;
    }
}