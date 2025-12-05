# DigYourWindows

一个用于 **深挖 Windows 深层信息** 的小工具，使用 Rust 开发，运行后自动生成一个可交互的 HTML 报告（`report.html`），在浏览器中查看。

## 功能特性

- **硬件信息**
  - CPU 型号与核心数
  - 内存总量
  - 磁盘（文件系统、总容量、剩余空间）
  - 网络适配器（名称、MAC、IP 地址）
  - USB 设备（设备 ID、名称、描述）
  - USB 控制器（名称、制造商、控制器描述，用于判断 USB 协议代际）

- **Windows 可靠性信息**
  - 通过 WMI 读取 `Win32_ReliabilityRecords`
  - 统计并可视化最近一段时间的故障记录数量趋势（Chart.js 图表）

- **事件查看器错误信息**
  - 通过 WMI 读取 `Win32_NTLogEvent`
  - 默认抓取最近 **3 天** 内 `System` 和 `Application` 日志中的 **Error / Warning** 事件
  - 在 HTML 中以可搜索、可排序的表格形式展示（DataTables）

## 环境要求

- 操作系统：Windows 10 / 11
- Rust 工具链：`cargo` 可用（已在本机安装）
- 需要能够访问 WMI 和 Windows 事件日志（建议在 **管理员权限** 终端下运行）

## 目录结构（核心部分）

- `src/main.rs`：程序入口，负责调度采集并生成报告
- `src/hardware.rs`：硬件与 USB、网卡等信息采集
- `src/reliability.rs`：可靠性记录（`Win32_ReliabilityRecords`）采集
- `src/events.rs`：事件日志错误 / 警告采集（`Win32_NTLogEvent`）
- `src/report.rs`：使用 Tera 模板引擎生成 HTML 报告
- `src/template.html`：报告 HTML 模板（Bootstrap + Chart.js + DataTables）

## 使用方法

```powershell
# 进入项目目录
cd DigYourWindows\DigYourWindows_Rust

# 构建并运行（首次运行会自动拉取依赖，时间略长）
cargo run
```

运行成功后：

- 控制台会输出采集进度信息；
- 在当前目录生成 `report.html`；
- 程序会自动调用系统默认浏览器打开 `report.html`。

如果浏览器未自动打开，可以手动双击 `report.html` 或在浏览器中打开该文件。

## 命令行参数概览

可执行文件支持如下常用参数（更详细帮助可执行 `DigYourWindows_Rust.exe --help` 查看）：

```text
DigYourWindows_Rust.exe [OPTIONS]

Options:
  -d, --days <DAYS>          查询事件日志的天数，例如 1 / 3 / 7 / 30，默认 3
  -f, --format <FORMAT>      输出格式：html / json / both，默认 html
  -o, --output <PATH>        输出文件名（不含扩展名），默认 report
      --output-dir <DIR>     输出目录（默认当前目录）
      --no-open              生成报告后不自动在浏览器中打开 HTML
  -h, --help                 显示帮助信息
```

示例：

- 只生成最近 7 天的 HTML 报告：

  ```powershell
  .\DigYourWindows_Rust.exe --days 7 --format html --output weekly_report
  ```

- 只导出 JSON 报告到指定目录：

  ```powershell
  .\DigYourWindows_Rust.exe --days 30 --format json --output-dir D:\Reports --output win_diagnostics
  ```

- 同时生成 HTML + JSON 报告：

  ```powershell
  .\DigYourWindows_Rust.exe --days 7 --format both --output system_health
  ```

## 注意事项

- 某些信息（尤其是系统事件日志）可能需要 **管理员权限** 才能完整读取，建议以管理员身份打开 PowerShell/终端后再 `cargo run`。
- 本工具目前只针对 Windows 平台设计，不考虑跨平台运行。
- 由于依赖 WMI，若系统策略禁用了相关 Provider，部分数据可能为空或采集失败。

## 后续扩展方向（想法）

- 增加命令行参数：自定义时间范围（例如最近 1 天 / 7 天）、导出为 JSON 等。
- 对错误事件进行分类和简单诊断建议。
- 将 HTML 报告打包为单个可分发的 ZIP 或加入导出 PDF 功能。
