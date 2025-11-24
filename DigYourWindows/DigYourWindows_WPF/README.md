# DigYourWindows (WPF 版本)

一个使用 **C# + WPF** 开发的 **Windows 深度诊断工具**，采用现代化的 Windows 11 风格 UI（基于 WPF-UI），提供交互式的系统信息查看和诊断功能。

## 功能特性

### 🖥️ 系统概览
- **计算机名称** 和 **操作系统版本**
- **CPU 型号** 和 **核心数**
- **内存总量**（以 MB 为单位）

### 📊 可靠性监控
- 通过 WMI 读取 `Win32_ReliabilityRecords` 获取系统可靠性历史记录
- **可视化图表**（LiveCharts2）展示最近 7 天的故障趋势
- 支持应用程序故障、Windows 故障等分类

### 📋 事件日志分析
- 自动读取最近 **3 天**的 `System` 和 `Application` 日志
- 筛选并展示 **Error** 和 **Warning** 级别的事件
- 可在 DataGrid 中搜索、排序、查看详细信息

### 💾 硬件详情
- **磁盘信息**：名称、文件系统、总容量、剩余空间、使用率进度条
- **网络适配器**：名称、MAC 地址、IP 地址
- **USB 控制器**：识别 USB 协议版本（USB 2.0 / USB 3.x）
- **USB 设备**：设备名称、描述、制造商

## 技术栈

- **框架**: .NET 10.0 (兼容 .NET 8.0+)
- **UI 库**: [WPF-UI](https://github.com/lepoco/wpfui) 4.0 (Windows 11 风格)
- **图表库**: [LiveCharts2](https://github.com/beto-rodriguez/LiveCharts2) (数据可视化)
- **架构**: MVVM (使用 `CommunityToolkit.Mvvm` 源生成器)
- **核心库**:
  - `System.Management` - WMI 查询（硬件信息、可靠性记录）
  - `System.Diagnostics.EventLog` - 事件日志读取

## 环境要求

- **操作系统**: Windows 10 / 11
- **.NET SDK**: 10.0 或更高版本（已在您的系统上安装）
- **权限**: 建议以 **管理员权限** 运行，以获取完整的系统日志和 WMI 信息

## 目录结构

```
DigYourWindows_WPF/
├── DigYourWindows.sln              # 解决方案文件
├── DigYourWindows.Core/            # 核心业务逻辑层
│   ├── Models/                     # 数据模型 (HardwareInfo, EventLogEntry 等)
│   └── Services/                   # 数据采集服务 (HardwareService, ReliabilityService 等)
└── DigYourWindows.UI/              # WPF 用户界面层
    ├── ViewModels/                 # MVVM ViewModel
    ├── MainWindow.xaml             # 主窗口 XAML
    └── App.xaml                    # 应用程序资源和主题
```

## 使用方法

### 1. 克隆或打开项目

```powershell
cd DigYourWindows\DigYourWindows_WPF
```

### 2. 还原 NuGet 包

```powershell
dotnet restore
```

### 3. 构建项目

```powershell
dotnet build
```

### 4. 运行程序

```powershell
dotnet run --project DigYourWindows.UI
```

或者在 Visual Studio 2022 中直接打开 `DigYourWindows.sln`，按 `F5` 运行。

### 5. 使用界面

- 程序启动后会 **自动加载** 硬件信息、可靠性记录和事件日志
- 点击 **"刷新数据"** 按钮可重新采集最新信息
- 点击 **"导出报告"** 按钮（开发中）可将数据导出为 HTML 报告

## 注意事项

1. **管理员权限**：部分 WMI 查询和事件日志读取需要管理员权限，建议右键以管理员身份运行。
2. **WMI 依赖**：如果系统策略禁用了 WMI Provider（如 `Win32_ReliabilityRecords`），相关数据可能为空。
3. **兼容性警告**：由于使用了 .NET 10.0，某些依赖包（如 LiveCharts2）可能显示兼容性警告，但不影响实际运行。

## 特色亮点

✨ **现代化 UI**：基于 WPF-UI，提供 Windows 11 Mica 背景效果和流畅动画  
📈 **实时图表**：使用 LiveCharts2 绘制可靠性趋势  
⚡ **异步加载**：数据采集在后台线程执行，UI 不卡顿  
🎨 **深色主题**：默认启用深色模式，保护眼睛  
🔍 **详尽信息**：从 CPU 到 USB 设备，一网打尽

## 未来扩展

- [ ] 导出为 HTML 报告功能
- [ ] 支持自定义时间范围（1天/7天/30天）
- [ ] GPU 信息和温度监控
- [ ] 网络流量统计
- [ ] 自动诊断和建议功能

## 开源协议

本项目采用 MIT 协议开源，欢迎贡献代码或提出建议。

---

**与 Rust 版本的对比**：
- Rust 版本生成静态 HTML 报告，便于分享和保存
- WPF 版本提供交互式界面，支持实时刷新和筛选
- 两个版本可以并存使用，各有优势
