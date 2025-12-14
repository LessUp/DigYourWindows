## 2025-12-04：CLI/WMI/性能分析与打包体验改进

### B. CLI 功能扩展

- 在 `src/main.rs` 中：
  - 新增 `OutputFormat` 枚举，支持 `html` / `json` / `both` 三种输出格式；
  - 增加 `--output-dir` 参数，可指定输出目录；
  - 保留 `--days` 参数，并在帮助中强调常用范围（1 / 3 / 7 / 30 天）；
  - 当选择 `both` 时，同时生成 HTML 与 JSON 报告，并仍然自动打开 HTML 报告（除非 `--no-open`）。
- 更新根目录 `README.md` 与 `release/README.md` 中的命令行说明与示例，体现上述新参数与用法。

### C. WMI 可靠性与鲁棒性增强

- 在 `src/wmi_impl.rs` 中：
  - 引入 `run_wmic_with_timeout` 辅助函数，将 `wmic` 调用放入独立线程并设置 30 秒超时，超时后返回错误，避免主程序卡死；
  - 统一使用该函数调用 `win32_usbdevice` / `win32_usbcontroller` / `win32_reliabilityrecords` / `win32_ntlogevent`，简化重复代码；
  - 若 `wmic` 标准错误包含 “access is denied”，则输出友好提示，建议以管理员权限运行当前工具。

### D. 性能分析模块增强

- 在 `src/performance.rs` 中：
  - 使用 `sysinfo` 获取当前 CPU 与内存使用率，新增字段：
    - `cpu_usage_percent`：当前 CPU 平均使用率；
    - `memory_used_percent`：当前内存使用率；
    - `high_disk_usage_count`：高占用磁盘数量（剩余空间低于阈值）；
    - `active_network_adapters`：活跃网络适配器数量（有 IP 的适配器）。
  - 将上述指标写入 `performance_metrics`，并在 CPU/内存占用过高时追加相应优化建议。

### E. 工程化/打包体验

- 在 `package.bat` 中：
  - 打包前自动执行 `cargo build --release`，确保始终使用最新 Release 二进制；
  - 从 `target\release\DigYourWindows_Rust.exe` 拷贝可执行文件，从 `src` 拷贝模板 HTML，以减少手工同步步骤；
  - 简化输出日志，分三步显示编译 / 生成目录 / 压缩 ZIP 的进度，保持一键运行体验。
