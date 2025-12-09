from __future__ import annotations

from datetime import timedelta, timezone
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent

API_MEMORY_DIR = Path("app") / "api" / "memory"
LINK_STATUS_DATA_DIR = API_MEMORY_DIR / "link_status" / "data"
READ_MEMORY_DATA_DIR = API_MEMORY_DIR / "read_memory" / "data"
READ_REAL_DATA_DIR = API_MEMORY_DIR / "read_real" / "data"
WRITE_MEMORY_DATA_DIR = API_MEMORY_DIR / "write_memory" / "data"
SETTING_RANGE_DIR = API_MEMORY_DIR / "setting_range" / "data"
READ_CMD_FORMAT_DIR = API_MEMORY_DIR / "read_CmdFormat" / "data"
EVENTLOG_NOTIFY_DIR = API_MEMORY_DIR / "notify"

INV_DATA_FILE = READ_MEMORY_DATA_DIR / "INV_DATA.json"
INV_DATA_V1_1_FILE = READ_REAL_DATA_DIR / "INV_DATA_V1-1.json"
LINK_STATUS_FILE = LINK_STATUS_DATA_DIR / "link_status.json"
DEFAULT_MEMORY_FILE = WRITE_MEMORY_DATA_DIR / "WriteMemory_JsonFormat.json"
DEFAULT_REAL_WRITE_FILE = WRITE_MEMORY_DATA_DIR / "REAL_WriteMemory_JsonFormat.json"
PARTITION_STATUS_FILE = SETTING_RANGE_DIR / "setting_range.json"
READ_CMD_FORMAT_FILE = READ_CMD_FORMAT_DIR / "read_CmdFormat.json"
EVENTLOG_NOTIFICATION_FILE = EVENTLOG_NOTIFY_DIR / "eventlog_notification.json"
TZ_TAIPEI = timezone(timedelta(hours=8))
USE_RANDOM_DATA = True
LINK_STATUS_REFRESH_INTERVAL = 2.0  # seconds

V1_1_FIXED_CONFIG = {
    "READ_OP_LD_PCNT": False,
    "READ_OP_WATT": False,
    "MFR_MODEL": True,
    "READ_VIN": False,
    "READ_IIN": False,
    "READ_FREQ": False,
    "INV_STATUS": True,
}

V1_1_FIXED_VALUES = {
    "INV_STATUS": 1,
}

MFR_MODEL_BY_PORT = {
    "CAN1": "NTN-5K-124  ",
    "CAN2": "NTN-5K-224  ",
    "MOD1": "NTN-5K-124  ",
    "MOD2": "NTN-5K-224  ",
}

MFR_MODEL_ASCII = {
    "group0_y": [ord(c) for c in "NTN-5K"],
    "group1_y": [ord(c) for c in "-124  "],
    "group0": [ord(c) for c in "\x00\x00\x00\x00\x00\x00"],
    "group1": [ord(c) for c in "\x00\x00\x00\x00\x00\x00"],
}

INV_STATUS_DECODE_BY_PORT = {
    "CAN1": {
        "INV_MODE": True,
    },
    "CAN2": {
        "BYPASS_MODE": True,
    },
    "MOD1": {
        "INV_MODE": True,
        "SAVING_MODE": True,
    },
    "MOD2": {
        "AC_OK": True,
    },
}

INV_STATUS_PHASE_BY_PORT_ADDR = {
    "CAN1": {
        "0": "Phase 0°",
        "1": "Phase 180°",
        "2": "Phase 120°",
        "7": "Phase 0",
        "10": "Phase 240°",
    },
    "CAN2":{
        "0": "Phase 0°",
    }
}

READ_REAL_VALUE_OVERRIDES = {
    "READ_VIN": {
        "CAN1": {
            "0": 109.8,
            "1": 110.1,
            "2": 110.0,
            "7": 200,
            "10" : 200
        }
    },
    "READ_OP_VA":{
        "CAN1":{
            "0": 600.0,
            "1": 700.0,
            "2": 800.0,
            "7": 500.0,
            "10": 1000.0
        }
    },
    "INV_FAULT": {
        "CAN1": {
            "1": 0,  # 設定 CAN1 addr 1 的 INV_FAULT 為 BitField 值 123
        }
    }
}

READ_CMD_FORMAT_CODE_OVERRIDES = {
    "MFR_MODEL": {
        "CAN": "0x0082",
        "MOD": "0x0086",
    },
    "MFR_REVISION_B0B5": {
        "CAN": "0x0084",
        "MOD": "0x008C",
    },
}

READ_REAL_COMMAND_CODES = {
    "SYSTEM_CONFIG": "0x00C2",
    "INV_OPERATION": "0x0100",
    "MFR_MODEL": "0x0082",
    "INV_CONFIG": "0x0101",
    "MFR_REVISION_B0B5": "0x0084",
    "Output_ACV_Set": "0x0102",
    "Output_ACF_Set": "0x0103",
    "READ_VBAT": "0x011A",
    "INV_STATUS": "0x011D",
    "READ_BP_VA": "0x0125",
    "READ_AC_IOUT": "0x012B",
    "READ_TEMPERATURE_1": "0x0062",
    "READ_AC_FOUT": "0x0105",
    "READ_AC_VOUT": "0x0108",
    "READ_OP_LD_PCNT": "0x010B",
    "READ_OP_WATT": "0x010E",
    "READ_OP_VA": "0x0114",
    "READ_FAN_SPEED_1": "0x0070",
    "READ_FAN_SPEED_2": "0x0071",
    "READ_CHG_CURR": "0x011B",
    "INV_FAULT": "0x011E",
    "SCALING_FACTOR": "0x00C0",
    "READ_BP_WATT": "0x011F",
    "READ_VIN": "0x0050",
    "READ_IIN": "0x0053",
    "READ_FREQ": "0x0056",
    "CURVE_CC": "0x00B0",
    "CURVE_CV": "0x00B1",
    "CURVE_FV": "0x00B2",
    "CURVE_TC": "0x00B3",
    "CURVE_CONFIG": "0x00B4",
    "CURVE_CC_TIMEOUT": "0x00B5",
    "CURVE_CV_TIMEOUT": "0x00B6",
    "CURVE_FV_TIMEOUT": "0x00B7",
    "CHG_STATUS": "0x00B8",
    "BAT_ALM_VOLT": "0x00B9",
    "BAT_SHDN_VOLT": "0x00BA",
    "BAT_RCHG_VOLT": "0x00BB",
    "MFR_ID": "0x0080",
    "MFR_LOCATION_B0B2": "0x0085",
    "MFR_DATE_B0B5": "0x0086",
    "MFR_SERIAL": "0x0087"
}

READ_REAL_RULES = {
    "SYSTEM_CONFIG": [
        {
            "name": "CAN_CTRL",
            "bit": 0,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "OPERATION_INIT",
            "bit": 1,
            "length": 2,
            "valueMap": {"0": "Off", "1": "On", "2": "last-time"},
        },
        {
            "name": "PEAK_EN",
            "bit": 3,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "PA_EN",
            "bit": 6,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "EEP_CONFIG",
            "bit": 8,
            "length": 2,
            "valueMap": {
                "0": "immediately",
                "1": "1Min delay",
                "2": "10Min delay",
                "3": "PSU off save",
            },
        },
        {
            "name": "EEP_OFF",
            "bit": 10,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
    ],
    "INV_OPERATION": [
        {
            "name": "OP_CTRL",
            "bit": 0,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "OP_EN",
            "bit": 1,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "CHG_EN",
            "bit": 2,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "GRID_EN",
            "bit": 3,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
    ],
    "INV_CONFIG": [
        {
            "name": "INV_PRIO",
            "bit": 0,
            "length": 2,
            "valueMap": {
                "0": "Utility power",
                "1": "Battery Power",
                "2": "Solar power",
            },
        },
        {
            "name": "CHG_PRIO",
            "bit": 2,
            "length": 2,
            "valueMap": {
                "0": "Utility power",
                "1": "Solar power",
            },
        },
        {
            "name": "EEP_CONFIG",
            "bit": 8,
            "length": 2,
            "valueMap": {
                "0": "immediately",
                "1": "1Min delay",
                "2": "10Min delay",
                "3": "PSU off save",
            },
        },
        {
            "name": "EEP_OFF",
            "bit": 10,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
    ],
    "Output_ACV_Set": [
        {
            "name": "ACV_MODE",
            "bit": 4,
            "length": 3,
            "valueMap": {
                "1": "100/200",
                "2": "110/220",
                "3": "115/230",
                "4": "120/240",
            },
        }
    ],
    "Output_ACF_Set": [
        {
            "name": "ACF_MODE",
            "bit": 0,
            "length": 2,
            "valueMap": {
                "0": "disable(by DPI SW)",
                "1": "50Hz",
                "2": "60Hz",
            },
        }
    ],
    "INV_STATUS": [
        {
            "name": "INV_MODE",
            "bit": 0,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "BYPASS_MODE",
            "bit": 1,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "AC_OK",
            "bit": 2,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "CHG_ON",
            "bit": 3,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "SOLAR_CHG",
            "bit": 4,
            "length": 1,
            "valueMap": {"0": "OFF", "1": "ON"},
        },
        {
            "name": "SAVING_MODE",
            "bit": 5,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "BAT_LOW_ALM",
            "bit": 6,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "PHASE",
            "bit": 8,
            "length": 2,
            "valueMap": {
                "0": "Phase 0\u00b0",
                "1": "Phase 180\u00b0",
                "2": "Phase 120\u00b0",
                "3": "Phase 240\u00b0",
            },
        },
    ],
    "INV_FAULT": [
        {
            "name": "OLP100",
            "bit": 0,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "OLP115",
            "bit": 1,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "OLP150",
            "bit": 2,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "OTP",
            "bit": 3,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "INV_UVP",
            "bit": 4,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "INV_OVP",
            "bit": 5,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "INV_FAULT",
            "bit": 6,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "EEP_Err",
            "bit": 7,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "SHDN",
            "bit": 8,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "FanFail",
            "bit": 9,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "BAT_UVP",
            "bit": 10,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "BAT_OVP",
            "bit": 11,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
    ],
    "CURVE_CONFIG": [
        {
            "name": "CUVS",
            "bit": 0,
            "length": 2,
            "valueMap": {"0": "user", "1": "default1", "2": "default2", "3": "default"},
        },
        {
            "name": "TCS",
            "bit": 2,
            "length": 2,
            "valueMap": {
                "0": "disable",
                "1": "-3 mV/\u2103",
                "2": "-4 mV/\u2103",
                "3": "-5 mV/\u2103",
            },
        },
        {
            "name": "STGS",
            "bit": 6,
            "length": 1,
            "valueMap": {"0": "three-stage-charging", "1": "two-stage-charging"},
        },
        {
            "name": "CUVE",
            "bit": 7,
            "length": 1,
            "valueMap": {"0": "VI mode", "1": "Curve mode"},
        },
        {
            "name": "CCTOE",
            "bit": 8,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "CVTOE",
            "bit": 9,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "FVTOE",
            "bit": 10,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
    ],
    "CHG_STATUS": [
        {
            "name": "FULL_BAT",
            "bit": 0,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "CC_MODE",
            "bit": 1,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "CV_MODE",
            "bit": 2,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "FV_MODE",
            "bit": 3,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "NTC_ERR",
            "bit": 10,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "BAT_CONN",
            "bit": 11,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "CC_TIMEOUT",
            "bit": 13,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "CV_TIMEOUT",
            "bit": 14,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
        {
            "name": "FV_TIMEOUT",
            "bit": 15,
            "length": 1,
            "valueMap": {"0": "False", "1": "True"},
        },
    ],
}
