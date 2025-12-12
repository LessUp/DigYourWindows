# Test Directory Structure

This directory contains all tests for the DigYourWindows Rust CLI project.

## Directory Organization

- **unit/**: Unit tests for individual functions and modules
- **property/**: Property-based tests using proptest (minimum 100 iterations)
- **integration/**: Integration tests for end-to-end functionality

## Running Tests

### Run all tests
```bash
cargo test
```

### Run only unit tests
```bash
cargo test --test unit/*
```

### Run only property tests
```bash
cargo test --test property/*
```

### Run with verbose output
```bash
cargo test -- --nocapture
```

## Property-Based Testing

Property tests use the `proptest` crate and are configured to run a minimum of 100 iterations per test (see `proptest.toml` in the project root).

### Example Property Test
```rust
use proptest::prelude::*;

proptest! {
    #[test]
    fn my_property_test(x in 0..100i32) {
        prop_assert!(x >= 0);
    }
}
```

## Configuration

- Property test configuration: `proptest.toml` in project root
- Test cases per property: 100 (minimum)
- Timeout per test case: 5000ms

## Requirements

Tests validate requirements 2.1, 2.2, 2.3, 2.4, and 2.5 from the specification.
