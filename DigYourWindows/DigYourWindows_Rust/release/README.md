# DigYourWindows - Windows系统信息诊断工具

## 简介
DigYourWindows是一个专为Windows设计的系统信息诊断工具，能够深入分析系统硬件、可靠性记录和事件日志，生成详细的诊断报告。

## 使用方法
1. 直接运行 `run.bat` 或 `DigYourWindows_Rust.exe`
2. 程序将自动收集系统信息并生成HTML报告
3. 报告将显示系统硬件信息、可靠性记录和错误事件

## 命令行选项
```
DigYourWindows_Rust.exe [选项]

选项:
  -d, --days <天数>            查询事件日志的天数（例如 1 / 3 / 7 / 30）[默认: 3]
  -f, --format <格式>          输出格式 (html, json, both) [默认: html]
  -o, --output <文件名>        输出文件名（不含扩展名）[默认: report]
      --output-dir <目录>      输出目录（默认当前目录）
      --no-open                跳过自动打开 HTML 报告
  -h, --help                   显示帮助信息
```

## 示例
1. 生成最近 7 天的 HTML 报告：
   ```
   DigYourWindows_Rust.exe --days 7 --format html --output weekly_report
   ```

2. 只生成 JSON 格式的报告：
   ```
   DigYourWindows_Rust.exe --days 30 --format json --output system_data
   ```

3. 同时生成 HTML + JSON 报告到指定目录：
   ```
   DigYourWindows_Rust.exe --days 7 --format both --output-dir D:\Reports --output system_health
   ```

## 系统要求
- Windows操作系统
- 管理员权限（推荐，以获取完整的系统信息）

## 报告内容
- **硬件信息**：CPU、内存、硬盘、USB设备等
- **可靠性记录**：应用程序崩溃、系统故障等
- **事件日志**：系统错误、警告等事件

## 版本信息
版本：0.1.0