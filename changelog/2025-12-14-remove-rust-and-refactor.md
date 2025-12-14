## 2025-12-14：移除 Rust 模块 & C# 架构重构（第一阶段）

### 决策

- 采用全 C# 方案（WPF + C# 采集/分析），不再维护 Rust 模块。

### 变更

- 将原 `DigYourWindows/DigYourWindows_Rust/changelog/*` 迁移到仓库根目录 `changelog/`，以保留历史记录。
- 删除 `DigYourWindows/DigYourWindows_Rust` 模块（含源码、测试、release 产物、脚本、target 等）。
- 更新 `DigYourWindows_WPF/README.md`：移除对 Rust 版本的对比说明。
- 更新 `DigYourWindows/TEST_INFRASTRUCTURE_SETUP.md`：移除 Rust 测试基础设施说明，仅保留 C# WPF。
- WPF 引入 DI（组合根在 `DigYourWindows.UI/App.xaml.cs`）：
  - `DigYourWindows.UI` 增加 `Microsoft.Extensions.DependencyInjection` 依赖。
  - `App.xaml` 移除 `StartupUri`，改为启动时从容器创建并显示 `MainWindow`。
  - `MainWindow` / `MainViewModel` 改为构造函数注入。
- 修复异步加载的线程安全问题：后台线程只做采集/计算，回到 UI 线程统一更新绑定属性与集合。
- 对齐目标框架：
  - `DigYourWindows.Core` 由 `net9.0` 调整为 `net10.0-windows`，与项目实际 Windows 专属 API 使用场景一致，并与 UI 对齐。
  - `DigYourWindows.Tests` 由 `net9.0` 调整为 `net10.0-windows`，避免测试项目引用 Core 时出现目标框架不匹配。
- 修复 `GpuMonitorService` 生命周期：
  - `HardwareService` 改为注入 `GpuMonitorService`，由 DI 容器统一释放（`ServiceProvider.Dispose()`）。
- 修复编译错误：
  - `ReportException` 静态方法与属性同名冲突（CS0102）。
  - `WmiException.Query` 只读导致无法在工厂方法中赋值（CS0200）。

### 影响

- 删除 Rust 后，仓库仅保留 C# WPF 版本；后续演进以 .NET 生态为主。
