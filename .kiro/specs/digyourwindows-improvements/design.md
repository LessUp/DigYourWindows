# Design Document

## Overview

本设计文档描述了 DigYourWindows 项目的改进方案，重点解决当前架构中的技术债务，提升代码质量、性能和用户体验。设计遵循以下原则：

- **渐进式改进**：优先解决高影响、低风险的问题
- **保持简单**：遵循 KISS 原则，避免过度工程
- **向后兼容**：确保现有功能不受破坏
- **测试驱动**：为关键功能建立测试覆盖

## Architecture

### 当前架构分析

**WPF GUI 版本：**
- 使用 `System.Management` 访问 WMI 获取系统与可靠性信息
- 采用 MVVM，采集/分析逻辑集中在 `DigYourWindows.Core`
- 通过依赖注入（DI）管理服务生命周期与资源释放
- 异步加载保证 UI 响应

### 改进后的架构

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│                    WPF GUI (MVVM + WPF-UI)                   │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                      Service Layer                           │
│  HardwareService | ReliabilityService | EventLogService      │
│  PerformanceService | Report Export (HTML/JSON)              │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                      Data Access Layer                       │
│   WMI (System.Management) + Optional HW monitor providers    │
└─────────────────────────────────────────────────────────────┘
```

### 关键改进点

1. **统一的服务层接口**：在 Core 层定义标准的数据采集/分析接口，UI 只负责展示与交互
2. **WMI 抽象层**：封装 WMI 访问细节，提供统一的错误处理
3. **数据模型标准化**：使用统一的 JSON schema/标准化模型定义数据结构
4. **可测试性**：通过依赖注入和接口抽象实现单元测试

## Components and Interfaces

### 1. WMI Access (C#)

**目的**：在 Core 层集中封装 WMI 查询与错误映射，避免 UI 直接访问 WMI 或依赖 WMIC。

**实现要点**：

- 使用 `System.Management` 执行查询
- 对常见失败分类并抛出可操作的异常（`WmiException/ServiceException/ReportException`）
- 采集在后台线程执行，UI 线程只更新绑定状态

### 2. Hardware Service

**职责**：采集硬件信息（CPU、内存、磁盘、网络、USB、GPU）

**实现要点**：

- 输出统一契约 `HardwareData`
- GPU 监控由独立服务负责采集并交由 DI 管理生命周期

### 3. Reliability Service

**职责**：采集和分析 Windows 可靠性记录

**实现要点**：

- 输出统一契约 `ReliabilityRecordData`
- 对权限不足/查询失败给出明确提示（建议管理员运行等）

### 4. Event Log Service

**职责**：采集和过滤 Windows 事件日志

**实现要点**：

- 输出统一契约 `LogEventData`
- 支持按时间范围过滤（最近 N 天）

### 5. Performance Service

**职责**：分析系统性能并生成评分和建议

**实现要点**：

- 输入使用统一契约：`HardwareData`、`LogEventData`、`ReliabilityRecordData`
- 输出统一契约：`PerformanceAnalysisData`
- 评分范围约束在 `[0, 100]`

### 6. Report Service

**职责**：生成各种格式的报告（HTML、JSON）

**实现要点**：

- 以 `DiagnosticData` 作为统一报告载体
- 支持导出 HTML 与 JSON
- 支持从 JSON 导入恢复 UI 展示

## Data Models

### 核心数据结构

- 标准化数据契约在 `DigYourWindows.Core/Models/StandardizedModels.cs`：
  - `DiagnosticData`
  - `HardwareData`
  - `ReliabilityRecordData`
  - `LogEventData`
  - `PerformanceAnalysisData`

### JSON Schema 定义

为确保导出/导入数据的一致性，定义标准 JSON schema：

- `DigYourWindows/diagnostic-data-schema.json`

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "DiagnosticData",
  "type": "object",
  "required": ["hardware", "reliability", "events", "performance", "collectedAt"],
  "properties": {
    "hardware": { "$ref": "#/definitions/HardwareData" },
    "reliability": {
      "type": "array",
      "items": { "$ref": "#/definitions/ReliabilityRecord" }
    },
    "events": {
      "type": "array",
      "items": { "$ref": "#/definitions/LogEvent" }
    },
    "performance": { "$ref": "#/definitions/PerformanceAnalysis" },
    "collectedAt": {
      "type": "string",
      "format": "date-time"
    }
  }
}
```


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Error Type Specificity

*For any* data collection failure scenario (WMI access denied, timeout, invalid query, network error), the system should throw or return a specific error type that uniquely identifies the failure category.

**Validates: Requirements 1.3**

### Property 2: Error Message Actionability

*For any* WMI access failure, the error message should contain at least one actionable keyword (e.g., "administrator", "permission", "privilege", "access denied", "configuration") that helps users understand how to resolve the issue.

**Validates: Requirements 1.4, 8.4**

### Property 3: Schema Validation Correctness

*For any* JSON data that conforms to the DiagnosticData schema, parsing should succeed and produce a valid data structure; for any JSON data that violates the schema, parsing should fail with a validation error.

**Validates: Requirements 2.1**

### Property 4: HTML Report Completeness

*For any* DiagnosticData instance, the generated HTML report should contain all required sections (hardware info, reliability records, event logs, performance analysis) and all non-null fields from the input data should appear in the output.

**Validates: Requirements 2.2**

### Property 5: Score Range Invariant

*For any* combination of hardware data, event logs, and reliability records, all calculated performance scores (system health, stability, performance, memory usage, disk health) must be within the range [0.0, 100.0] inclusive.

**Validates: Requirements 2.3**

### Property 6: Malformed Input Resilience

*For any* event log entry with missing fields, invalid timestamps, or malformed message text, the system should either successfully parse it with default values or skip it with a warning, but never crash or panic.

**Validates: Requirements 2.4**

### Property 7: JSON Serialization Round-Trip

*For any* DiagnosticData instance, serializing to JSON and then deserializing should produce a data structure equivalent to the original (allowing for floating-point precision differences).

**Validates: Requirements 2.5**

### Property 8: Memory Type Distinction

*For any* hardware data collected, the memory information should include distinct, non-overlapping fields for physical memory, virtual memory, and page file usage, with each value being non-negative.

**Validates: Requirements 4.3**

### Property 9: Reliability Event Categorization

*For any* reliability record with a recognized event type field, the categorization function should map it to exactly one category from the Windows reliability taxonomy (Application Failure, Windows Failure, Miscellaneous Failure, Warning, Information).

**Validates: Requirements 4.4**

### Property 10: USB Protocol Version Extraction

*For any* USB controller description string containing version indicators ("USB 2.0", "USB 3.0", "USB 3.1", "xHCI", "EHCI"), the parsing function should correctly extract the protocol version.

**Validates: Requirements 4.5**

### Property 11: Score Explanation Completeness

*For any* PerformanceAnalysis with non-zero scores, the output should include explanatory text for each score component (health, stability, performance, memory, disk).

**Validates: Requirements 5.1**

### Property 12: Recommendation Severity Ordering

*For any* list of recommendations generated by the system, recommendations marked as "critical" should appear before those marked as "warning", which should appear before those marked as "info".

**Validates: Requirements 5.2**

### Property 13: Critical Issues Prominence

*For any* diagnostic report containing critical issues (events with EventType="Error" or reliability failures), the summary section should appear at the beginning of the report and explicitly mention the count of critical issues.

**Validates: Requirements 5.4**

### Property 14: Partial Collection Resilience

*For any* data collection process where at least one service (hardware, reliability, events) fails, the system should still return results from successful services and include warnings about failed services in the output.

**Validates: Requirements 5.5**

### Property 15: Score Determinism

*For any* identical `DiagnosticData` input, repeated analysis should produce stable scores (allowing for floating-point precision differences).

**Validates: Requirements 6.2**

### Property 16: JSON Backward Compatibility

*For any* JSON report exported by the System, the WPF GUI should be able to import and parse it without errors (within supported schema versions).

**Validates: Requirements 6.5**

### Property 17: Error Logging Completeness

*For any* error or exception that occurs during system operation, at least one log entry should be written containing the error type, error message, and timestamp.

**Validates: Requirements 7.3**

### Property 18: Privilege Level Detection Accuracy

*For any* execution context, the system should correctly detect whether it is running with administrator privileges or standard user privileges.

**Validates: Requirements 8.1**

### Property 19: Sensitive Data Sanitization

*For any* diagnostic data containing IP addresses, computer names, or user names, the sanitized export should replace these values with placeholders (e.g., "XXX.XXX.XXX.XXX", "[COMPUTER_NAME]", "[USER]") when sanitization is enabled.

**Validates: Requirements 8.3**

### Property 20: Graceful Degradation

*For any* restricted environment where some WMI queries fail due to permissions, the system should successfully collect data from accessible sources and return partial results with clear indication of which data could not be collected.

**Validates: Requirements 8.5**

## Error Handling

### Error Hierarchy

- Use typed exceptions in `DigYourWindows.Core.Exceptions` to categorize failures:
  - `WmiException` (`WmiErrorType`)
  - `ServiceException` (`ServiceErrorType`)
  - `ReportException` (`ReportErrorType`)

### Error Recovery Strategy

1. **Retry Logic**: WMI queries that timeout should be retried up to 3 times with exponential backoff
2. **Partial Results**: If one service fails, continue with others and report partial results
3. **Graceful Degradation**: If elevated privileges are unavailable, collect what's accessible
4. **User Guidance**: All errors should include actionable guidance for resolution

### Logging Strategy

- (Planned) Log to file for troubleshooting.
- File: `%APPDATA%/DigYourWindows/logs/digyourwindows.log`

## Testing Strategy

### Unit Testing

**WPF GUI:**
- Use xUnit or NUnit for C# testing
- Mock services using interfaces (IHardwareService, etc.)
- Test ViewModels independently from Views
- Test data binding and command execution

**Key Unit Test Areas:**
- Data parsing and validation
- Score calculation algorithms
- Error message generation
- JSON serialization/deserialization
- HTML template rendering with various data combinations

### Property-Based Testing

**Framework Selection:**
- **C#**: Use `FsCheck` library (port of QuickCheck, works well with xUnit)

**Configuration:**
- Each property test should run minimum 100 iterations
- Use custom generators for domain-specific types (DiagnosticData, HardwareData, etc.)
- Implement shrinking for better failure diagnosis

**Property Test Implementation:**

 Each property-based test MUST be tagged with a comment referencing the design document:

```csharp
// Feature: digyourwindows-improvements, Property 5: Score Range Invariant
[PropertyTest]
public void Prop_ScoreRangeInvariant()
{
    // 生成输入并调用分析后断言所有评分都在 [0, 100]
}
```

**Property Test Coverage:**

Each correctness property from the design document MUST be implemented as a single property-based test:

- Property 1-2: Error handling properties
- Property 3: Schema validation
- Property 4: HTML completeness
- Property 5: Score ranges (shown above)
- Property 6: Malformed input handling
- Property 7: JSON round-trip
- Property 8-10: Data parsing properties
- Property 11-14: Report generation properties
- Property 15-16: Score determinism and JSON backward compatibility
- Property 17-20: Error handling and security properties

### Integration Testing

**Scope:**
- End-to-end data collection on real Windows systems
- Import/export compatibility (export JSON and import it back)
- Performance benchmarks with large datasets
- UI responsiveness testing for WPF GUI

**Test Environments:**
- Windows 10 (21H2 or later)
- Windows 11
- Both standard user and administrator contexts
- Virtual machines with limited WMI access

### Test Data Generators

**Custom Generators for Property Tests:**

- Use FsCheck `Arbitrary<T>` to generate standardized models (`HardwareData`, `LogEventData`, `ReliabilityRecordData`).
- Include generators for malformed data (Property 6).

## Performance Considerations

### Optimization Targets

1. **Data Collection**: Complete within 10 seconds for 30 days of event logs
2. **Memory Usage**: Peak memory < 500MB for typical datasets
3. **Report Generation**: < 2 seconds for HTML with 10,000 events
4. **UI Responsiveness**: WPF GUI should remain responsive during all operations

### Optimization Strategies

**WPF GUI:**
- All data collection on background threads (`Task.Run`)
- Use `ObservableCollection` with virtualization for large lists
- Implement progressive loading for event logs (load first 1000, then on-demand)
- Use `async`/`await` consistently to avoid blocking UI thread

### Caching Strategy

- Cache WMI connection for multiple queries
- Cache hardware info (rarely changes during execution)
- Invalidate cache on explicit refresh

## Security Considerations

### Privilege Management

1. **Detection**: Check privilege level at startup
2. **Elevation Prompt**: If needed, provide clear reason and allow user to decline
3. **Graceful Degradation**: If user declines, collect what's available

### Data Sanitization

- (Planned) Provide options to sanitize sensitive fields (IP addresses, computer names, user names, paths) before export.
- Replace values with placeholders (e.g., `XXX.XXX.XXX.XXX`, `[COMPUTER_NAME]`, `[USER]`) with user consent.

### Audit Logging

- Log all WMI queries attempted
- Log privilege elevation requests
- Log data export operations with sanitization status

## Deployment and Distribution

### WPF GUI

**Build Process:**
```powershell
# Publish self-contained
dotnet publish -c Release -r win-x64 --self-contained

# Create installer (optional)
# Use WiX Toolset or Inno Setup
```

**Distribution:**
- Self-contained deployment (includes .NET runtime)
- Optional MSI installer for enterprise deployment
- Portable mode available

## Migration Path

### Phase 1: Foundation (Weeks 1-2)
- Consolidate WMI access and exception mapping in C# Core services
- Add error types and handling
- Create data model with JSON schema
- Set up testing infrastructure

### Phase 2: Core Services (Weeks 3-4)
- Refactor hardware service
- Refactor reliability service
- Refactor event log service
- Add property tests for each service

### Phase 3: Report Generation (Week 5)
- Refactor report service
- Update HTML templates
- Add JSON import/export
- Test import/export compatibility

### Phase 4: Performance & Polish (Week 6)
- Performance optimization
- Add progress indicators
- Improve error messages
- Documentation updates

### Phase 5: Testing & Validation (Week 7)
- Complete property test coverage
- Integration testing
- User acceptance testing
- Bug fixes

## Future Enhancements

### Potential Features (Not in Current Scope)

1. **Real-time Monitoring**: Continuous monitoring mode with alerts
2. **Historical Tracking**: Store diagnostic snapshots over time
3. **Comparison Tool**: Compare two diagnostic reports
4. **Plugin System**: Allow third-party diagnostic modules
5. **Cloud Sync**: Upload reports to cloud storage
6. **Multi-machine Dashboard**: Aggregate data from multiple machines
7. **Automated Remediation**: Suggest and execute fixes for common issues
8. **SMART Disk Monitoring**: Detailed disk health analysis
9. **Network Diagnostics**: Bandwidth testing, latency monitoring
10. **Custom Report Templates**: User-defined report layouts

### Technical Debt to Address

1. Replace remaining `wmic` calls with native API
2. Add comprehensive logging throughout
3. Implement proper configuration management
4. Add telemetry (opt-in) for crash reporting
5. Improve test coverage to >80%
6. Add benchmarks for performance regression detection
7. Document all public APIs
8. Create developer guide for contributors
