# Implementation Plan

## Phase 1: Foundation and Infrastructure

- [x] 1. Set up testing infrastructure for C# projects





  - Install and configure `FsCheck` library for C# property-based testing
  - Integrate tests with xUnit/NUnit and ensure CI can run them
  - Organize test projects and folders: `Unit/`, `Property/`, `Integration/`
  - Configure test runners to execute minimum 100 iterations for property tests
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [x] 2. Implement WMI access and exception handling in C#





  - [x] 2.1 Use `System.Management` for WMI access (avoid spawning WMIC)


    - Keep WMI queries inside Core services and wrap failures with actionable exceptions
    - _Requirements: 1.1_
  
  - [ ] 2.2 (Optional) Extract a minimal WMI query abstraction for testability


    - Centralize query execution and error mapping in one place
    - Avoid duplicated query strings and parsing logic
    - _Requirements: 1.1_
  
  - [x] 2.3 Implement `WmiException` with specific failure categories

    - Add variants for AccessDenied, Timeout, InvalidQuery, ParseError, ConnectionFailed
    - Include actionable error messages with context
    - _Requirements: 1.3, 1.4_
  
  - [x] 2.4 Write property test for error type specificity


    - **Property 1: Error Type Specificity**
    - **Validates: Requirements 1.3**
  
  - [ ] 2.5 Write property test for error message actionability


    - **Property 2: Error Message Actionability**
    - **Validates: Requirements 1.4, 8.4**

- [x] 3. Define standardized data models with JSON schema





  - [x] 3.1 Create C# standardized models with JSON serialization


    - Define `HardwareData`, `ReliabilityRecordData`, `LogEventData`, `PerformanceAnalysisData`, `DiagnosticData`
    - Add JSON serialization attributes for consistent JSON format
    - _Requirements: 2.1, 2.5_
  
  - [x] 3.2 Ensure UI and Core share the same standardized contracts


    - Avoid duplicate model definitions across projects
    - Keep UI bindings stable via alias properties where necessary
    - _Requirements: 2.1, 2.5, 6.2_
  
  - [x] 3.3 Create JSON schema definition file


    - Define schema for `DiagnosticData` with all nested types
    - Add validation rules for required fields and data types
    - _Requirements: 2.1_
  
  - [ ] 3.4 Write property test for schema validation


    - **Property 3: Schema Validation Correctness**
    - **Validates: Requirements 2.1**
  
  - [ ] 3.5 Write property test for JSON round-trip serialization


    - **Property 7: JSON Serialization Round-Trip**
    - **Validates: Requirements 2.5**

- [x] 4. Implement error handling infrastructure





  - [x] 4.1 Create exception type hierarchy in C#


    - Define `WmiException`, `ServiceException`, `ReportException`
    - Keep error categories explicit for actionable UX
    - _Requirements: 1.3, 1.4_
  

  - [x] 4.2 Use exception types consistently across Core services and UI

    - Define `WmiException`, `ServiceException`, `ReportException` classes
    - Include inner exceptions and detailed messages
    - _Requirements: 1.3_
  

  - [ ] 4.3 Implement logging infrastructure

    - Set up file and console logging with configurable levels
    - Create log directory at `%APPDATA%/DigYourWindows/logs/`
    - _Requirements: 7.3_
  
  - [x] 4.4 Write property test for error logging completeness


    - **Property 17: Error Logging Completeness**
    - **Validates: Requirements 7.3**

## Phase 2: Core Service Refactoring (C#)

- [x] 5. Refactor Hardware Service in C#
  - [x] 5.1 Create `HardwareService` with dependency injection
    - Use constructor injection for dependent services (e.g., GPU monitor)
    - _Requirements: 1.2, 4.1, 4.2, 4.3, 4.5_
  
  - [ ] 5.2 Implement CPU and memory information collection
    - Query `Win32_Processor` for CPU details
    - Query `Win32_PhysicalMemory` and `Win32_OperatingSystem` for memory info
    - _Requirements: 4.3_
  
  - [ ] 5.3 Implement disk information collection
    - Query `Win32_LogicalDisk` for all mounted volumes
    - Include network drives and virtual disks
    - _Requirements: 4.2_
  
  - [ ] 5.4 Implement network adapter collection
    - Query `Win32_NetworkAdapterConfiguration` for active adapters
    - Extract IP addresses, MAC addresses, and adapter names
    - _Requirements: 4.1_
  
  - [ ] 5.5 Implement USB device and controller collection
    - Query `Win32_USBController` and `Win32_PnPEntity` for USB devices
    - Parse controller descriptions to extract USB protocol version
    - _Requirements: 4.5_
  
  - [ ] 5.6 Write property test for memory type distinction
    - **Property 8: Memory Type Distinction**
    - **Validates: Requirements 4.3**
  
  - [ ] 5.7 Write property test for USB protocol version extraction
    - **Property 10: USB Protocol Version Extraction**
    - **Validates: Requirements 4.5**

- [x] 6. Refactor Reliability Service in C#
  - [x] 6.1 Keep reliability queries and mapping inside `ReliabilityService`
    - Implement methods for querying reliability records by date range
    - _Requirements: 4.4_
  
  - [ ] 6.2 Implement reliability record collection
    - Query `Win32_ReliabilityRecords` with date filtering
    - Handle timeout and access denied errors gracefully
    - _Requirements: 4.4_
  
  - [ ] 6.3 Implement failure categorization logic
    - Map Windows event types to reliability taxonomy categories
    - Create `FailureCategories` struct with counts per category
    - _Requirements: 4.4_
  
  - [ ] 6.4 Write property test for reliability event categorization
    - **Property 9: Reliability Event Categorization**
    - **Validates: Requirements 4.4**

- [x] 7. Refactor Event Log Service in C#
  - [x] 7.1 Keep event log access and filtering inside `EventLogService`
    - Implement date range filtering for event queries
    - _Requirements: 2.4, 5.5_
  
  - [ ] 7.2 Implement event log collection with error handling
    - Query `Win32_NTLogEvent` for System and Application logs
    - Filter by event type (Error, Warning) and date range
    - Handle malformed log entries gracefully
    - _Requirements: 2.4, 5.5_
  
  - [ ] 7.3 Implement event pattern analysis
    - Group events by source and event ID
    - Calculate frequency and identify recurring issues
    - _Requirements: 5.2_
  
  - [ ] 7.4 Write property test for malformed input resilience
    - **Property 6: Malformed Input Resilience**
    - **Validates: Requirements 2.4**
  
  - [ ] 7.5 Write property test for partial collection resilience
    - **Property 14: Partial Collection Resilience**
    - **Validates: Requirements 5.5**

- [x] 8. Refactor Performance Service in C#
  - [x] 8.1 Refactor `PerformanceService` with scoring algorithms
    - Implement constructor and analysis method
    - _Requirements: 2.3, 5.1, 5.2, 6.2_
  
  - [ ] 8.2 Implement health score calculation
    - Calculate system health based on error counts, reliability, and hardware
    - Ensure score is always in range [0, 100]
    - _Requirements: 2.3, 5.1_
  
  - [ ] 8.3 Implement stability and performance score calculations
    - Calculate stability from reliability records
    - Calculate performance from resource usage
    - _Requirements: 2.3, 5.1_
  
  - [ ] 8.4 Implement memory and disk health score calculations
    - Calculate memory score from usage percentage
    - Calculate disk score from free space and health indicators
    - _Requirements: 2.3, 5.1_
  
  - [ ] 8.5 Implement recommendation generation
    - Generate prioritized recommendations based on scores
    - Order by severity (critical, warning, info)
    - _Requirements: 5.1, 5.2_
  
  - [ ] 8.6 Write property test for score range invariant
    - **Property 5: Score Range Invariant**
    - **Validates: Requirements 2.3**
  
  - [ ] 8.7 Write property test for score explanation completeness
    - **Property 11: Score Explanation Completeness**
    - **Validates: Requirements 5.1**
  
  - [ ] 8.8 Write property test for recommendation severity ordering
    - **Property 12: Recommendation Severity Ordering**
    - **Validates: Requirements 5.2**

## Phase 3: Report Generation and Import/Export

- [x] 9. Implement report export (HTML/JSON)
  - [x] 9.1 Implement HTML report export
    - Keep HTML generation simple and self-contained
    - _Requirements: 1.2, 2.2, 5.4_
  
  - [x] 9.2 Ensure HTML report contains all diagnostic data sections
    - Add summary section at the top with critical issues
    - Ensure all non-null fields are rendered
    - _Requirements: 2.2, 5.4_
  
  - [x] 9.3 Implement JSON report export
    - Serialize `DiagnosticData` to formatted JSON
    - Include metadata (version, timestamp)
    - _Requirements: 2.5, 6.5_
  
  - [ ] 9.4 Implement report summary generation
    - Create text summary highlighting critical issues
    - Include key metrics and recommendations
    - _Requirements: 5.4_
  
  - [ ] 9.5 Write property test for HTML report completeness
    - **Property 4: HTML Report Completeness**
    - **Validates: Requirements 2.2**
  
  - [ ] 9.6 Write property test for critical issues prominence
    - **Property 13: Critical Issues Prominence**
    - **Validates: Requirements 5.4**

- [x] 10. Update WPF UI workflow
  - [x] 10.1 Refactor WPF startup to use dependency injection
    - Build ServiceProvider in App and create MainWindow via DI
    - _Requirements: 1.1, 1.2_
  
  - [ ] 10.2 Implement privilege level detection
    - Check if running with administrator privileges
    - Provide clear guidance if elevation is needed
    - _Requirements: 8.1, 8.4_
  
  - [ ] 10.3 Implement graceful degradation for restricted environments
    - Attempt all data collection operations
    - Continue on partial failures
    - Report which data could not be collected
    - _Requirements: 8.5_
  
  - [ ] 10.4 Write property test for privilege level detection
    - **Property 18: Privilege Level Detection Accuracy**
    - **Validates: Requirements 8.1**
  
  - [ ] 10.5 Write property test for graceful degradation
    - **Property 20: Graceful Degradation**
    - **Validates: Requirements 8.5**

- [ ] 11. Implement data sanitization feature
  - [ ] 11.1 Create `SanitizationOptions` configuration struct
    - Add flags for IP addresses, computer names, user names, paths
    - _Requirements: 8.3_
  
  - [ ] 11.2 Implement sanitization methods for DiagnosticData
    - Replace sensitive information with placeholders
    - Apply sanitization before export
    - _Requirements: 8.3_
  
  - [ ] 11.3 Add UI option for enabling sanitization
    - Prompt user for confirmation when exporting
    - _Requirements: 8.3_
  
  - [ ] 11.4 Write property test for sensitive data sanitization
    - **Property 19: Sensitive Data Sanitization**
    - **Validates: Requirements 8.3**

- [x] 12. Align WPF GUI with the new architecture
  - [x] 12.1 Ensure Core services expose standardized models
    - Keep method signatures stable and UI-friendly
    - Use consistent error handling patterns
    - _Requirements: 6.1, 6.2_
  
  - [x] 12.2 Update PerformanceService in C# to use the standardized scoring algorithm
    - Ensure calculations are stable and deterministic
    - _Requirements: 6.2_
  
  - [x] 12.3 Implement JSON import functionality in WPF GUI
    - Add "Import Report" button and command
    - Parse JSON and populate UI with imported data
    - _Requirements: 6.5_
  
  - [ ] 12.4 Write property test for score determinism
    - **Property 15: Score Determinism**
    - **Validates: Requirements 6.2**
  
  - [ ] 12.5 Write property test for JSON backward compatibility
    - **Property 16: JSON Backward Compatibility**
    - **Validates: Requirements 6.5**

## Phase 4: Testing and Quality Assurance

- [ ] 13. Implement custom property test generators
  - [ ] 13.1 Create generators for all data types
    - Implement generators for HardwareData/LogEventData/ReliabilityRecordData
    - Include generators for malformed data
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  
  - [ ] 13.2 Create C# generators using FsCheck
    - Ensure generators align with standardized models
    - Ensure generated data is compatible with the standardized JSON schema
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [ ] 14. Complete property test coverage
  - [ ] 14.1 Verify all 20 properties have corresponding tests
    - Review test files and ensure each property is tested
    - Add missing property tests if any
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_
  
  - [ ] 14.2 Configure property tests to run 100+ iterations
    - Update test configuration in test projects
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_
  
  - [ ] 14.3 Run full property test suite and fix any failures
    - Execute all property tests
    - Investigate and fix any property violations
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

- [ ] 15. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Phase 5: Documentation and Polish

- [ ] 16. Update documentation
  - [ ] 16.1 Add inline documentation to all public APIs
    - Add XML documentation comments to all public C# classes and methods
    - _Requirements: 7.1, 7.2_
  
  - [ ] 16.2 Create architectural documentation
    - Document service layer architecture
    - Explain WMI abstraction and error handling patterns
    - Provide examples for adding new diagnostic modules
    - _Requirements: 7.2_
  
  - [ ] 16.3 Update README
    - Document WPF features and report import/export
    - Update build and installation instructions
    - Add troubleshooting section
    - _Requirements: 7.5_
  
  - [ ] 16.4 Create developer setup guide
    - Document development environment setup for C#/.NET
    - List all dependencies and installation steps
    - Provide quick start guide for contributors
    - _Requirements: 7.5_

- [ ] 17. Performance optimization
  - [ ] 17.1 Profile with large datasets
    - Identify bottlenecks in data collection and report generation
    - Optimize hot paths in data collection and report generation
    - _Requirements: 3.1, 3.3_
  
  - [ ] 17.2 Implement parallel data collection where safe
    - Ensure thread safety of WMI connections
    - _Requirements: 3.1_
  
  - [ ] 17.3 Optimize WPF GUI responsiveness
    - Ensure all data collection uses background threads
    - Implement progressive loading for large event log tables
    - Add loading indicators and progress bars
    - _Requirements: 3.2, 3.5_

- [ ] 18. Final integration testing
  - [ ] 18.1 Test on Windows 10 and Windows 11
    - Verify functionality on both OS versions
    - Test with both standard user and administrator privileges
    - _Requirements: 8.1, 8.5_
  
  - [ ] 18.2 Test import/export compatibility
    - Export JSON and import it back in WPF GUI
    - Verify scores and data are preserved
    - _Requirements: 6.1, 6.2, 6.5_
  
  - [ ] 18.3 Test error scenarios
    - Simulate WMI access failures
    - Test with restricted permissions
    - Verify error messages and graceful degradation
    - _Requirements: 1.4, 5.5, 8.4, 8.5_

- [ ] 19. Final Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
