# 硬編碼命令名稱位置總結

## 概述
以下是代碼中硬編碼的命令名稱（command names），這些地方直接使用字符串來調用 `parseCmdData` 或相關解析函數，而不是從配置讀取。這些位置可能需要重構以支持配置化。

## 詳細位置

### 1. BitFieldParser.cs
- **位置**：`FitParserFunction` 方法的 switch case（行號 19-22）。
- **硬編碼內容**：
  - `"INV_FAULT"`：用於選擇 `Parse_INV_FAULT`。
  - `"INV_STATUS"`：用於選擇 `Parse_INV_STATUS`。
- **影響**：如果添加新命令，需要手動修改 switch。
- **內部字段**：解析函數內部也硬編碼字段名稱，如 `"INV_MODE"`, `"BYPASS_MODE"` 等。

### 2. SubSystem.cs
- **多個位置**：在 `ComputeMode`, `ComputePhase`, `Compute_INV_IP_V`, `Compute_INV_IP_F`, `Compute_INV_OP_V`, `Compute_INV_OP_F`, `Compute_INV_OP_A`, `Compute_INV_OP_Load`, `Compute_INV_OP_VA`, `Compute_INV_BAT_V` 等方法中。
- **硬編碼內容**：
  - `"INV_FAULT"`：在 `ComputeMode` 中（行號 375）。
  - `"INV_STATUS"`：在多個方法中（行號 389, 462, 513, 599, 693, 779, 872, 984）。
  - `"READ_VIN"`：在 `Compute_INV_IP_V` 中（行號 521）。
  - `"READ_FREQ"`：在 `Compute_INV_IP_F` 中（行號 608）。
  - `"READ_AC_VOUT"`：在 `Compute_INV_OP_V` 和 `Compute_INV_OP_A` 中（行號 701, 881）。
  - `"READ_AC_FOUT"`：在 `Compute_INV_OP_F` 中（行號 788）。
  - `"READ_OP_VA"`：在 `Compute_INV_OP_A` 和 `Compute_INV_OP_VA` 中（行號 890, 993）。
  - `"READ_OP_LD_PCNT"`：在 `Compute_INV_OP_Load` 中（行號 963）。
  - `"READ_VBAT"`：在 `Compute_INV_BAT_V` 中（行號 1066）。
- **影響**：這些直接傳遞給 `parseCmdData`，如果命令名稱改變，需要修改所有出現處。

### 3. GlobalData.cs
- **位置**：`Get_INV_Mode`, `Parse_INV_FAULT`（部分註釋），以及其他方法（行號 391, 633, 659-660, 269, 918）。
- **硬編碼內容**：
  - `"INV_STATUS"`：在 `Get_INV_Mode` 和 `Parse_INV_Mode` 中。
  - `"MFR_MODEL"`：在獲取方法中。
- **影響**：直接硬編碼在變數賦值中。

## 建議
- 將命令名稱移到 `ConstDefinition.cs` 或配置文件中。
- 使用映射來處理解析函數選擇。