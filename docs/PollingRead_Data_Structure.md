# PollingRead Data Structure Overview

## Relationships

- `PollingRead` writes single-device snapshots via `_globalVar.Real_Devices_ReadData.SaveReal_oneDevice_Data(addr, res)` and removes them with `Remove_oneDevice_Data` when data is invalid.
- `Real_allDeviceData` stores each device in a `ConcurrentDictionary<uint, Real_SingleDeviceData_JsonFormat>`, locking individual entries during updates to avoid concurrent writes.
- `Real_SingleDeviceData_JsonFormat` keeps per-device metadata and a `values` map keyed by command name, each entry being a `SingleCommandData` instance.
- `SingleCommandData` tracks command type, value, unit, decoded detail, and change notifications; supporting types `ruleContent` and `decodeContent` hold auxiliary metadata.

```mermaid
classDiagram
    class PollingRead {
        -GlobalVar _globalVar
        -Task PollOneStepAsync(CancellationToken)
        +SaveRealData(addr,res)
    }

    class GlobalVar {
        +Real_allDeviceData Real_Devices_ReadData
    }

    class Real_allDeviceData {
        -ConcurrentDictionary<uint, Real_SingleDeviceData_JsonFormat> _AllDevice_Data
        +SaveReal_oneDevice_Data(uint, Real_SingleDeviceData_JsonFormat)
        +Remove_oneDevice_Data(uint)
        +Get_oneDevice_DataSnapshot(uint) Real_SingleDeviceData_JsonFormat?
    }

    class Real_SingleDeviceData_JsonFormat {
        +string port
        +uint addr
        +string protocolName
        +DateTimeOffset timestamp
        +Dictionary<string, SingleCommandData> values
        +UpdateSelf_From(source)
        +DeepClone() Real_SingleDeviceData_JsonFormat
    }

    class SingleCommandData {
        +string type
        +object value
        +string? unit
        +List<ruleContent>? rule
        +List<decodeContent>? decode
        +event Action? OnChanged
        +DeepClone() SingleCommandData
    }

    class ruleContent {
        +string? name
        +uint bit
        +uint length
        +Dictionary<string,string>? valueMap
    }

    class decodeContent {
        +string? name
        +string? value
    }

    PollingRead --> GlobalVar : uses
    GlobalVar --> Real_allDeviceData : exposes
    Real_allDeviceData o--> Real_SingleDeviceData_JsonFormat : stores
    Real_SingleDeviceData_JsonFormat o--> SingleCommandData : values
    SingleCommandData o--> ruleContent : rule
    SingleCommandData o--> decodeContent : decode
```
