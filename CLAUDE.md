# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

### Building
```bash
# Build all projects
dotnet build --no-restore

# Build specific project
dotnet build --no-restore LibPostal.Net/LibPostal.Net.csproj
```

### Testing
```bash
# Run all tests
dotnet test --no-build

# Run tests with verbose output
dotnet test --no-build --verbosity normal

# Run specific test class
dotnet test --no-build --filter "ClassName=LibPostal.Net.Tests.Core.TrieTests"

# Run tests matching pattern
dotnet test --no-build --filter "ClassName~TokenizerTests"
```

### Project Structure

The solution consists of 4 projects:

- **LibPostal.Net** - Main library implementation
- **LibPostal.Net.Tests** - Test suite (xUnit + FluentAssertions)
- **LibPostal.Net.Data** - NuGet data package (pre-trained models, ~2GB)
- **LibPostal.Net.Benchmarks** - Performance benchmarking (BenchmarkDotNet)

## Architecture Overview

LibPostal.Net is a pure .NET 9 port of libpostal (international address parsing library) organized in a layered architecture:

### Module Structure

```
LibPostal.Net/
├── Core/                  - Foundational data structures
│   ├── Trie<T>           - Generic trie for efficient lookup
│   ├── StringUtils       - Unicode-aware string manipulation
│   └── Token structs     - Token types and containers
├── IO/                    - Binary file serialization/deserialization
│   ├── BigEndianBinaryReader/Writer - Binary I/O operations
│   ├── DictionaryLoader  - Parse pipe-delimited text dictionaries
│   ├── FileSignature     - Validate binary file headers
│   └── TrieReader        - Read trie binary format
├── Tokenization/          - Text breaking and normalization
│   ├── Tokenizer         - Pattern-based text tokenization
│   ├── TokenizedString   - Container for tokenized text
│   ├── StringNormalizer  - Unicode normalization (NFD/NFC, accents, case)
│   ├── TokenNormalizer   - Per-token transformations
│   ├── UnicodeScriptDetector - Detect writing systems (Latin, CJK, etc.)
│   └── Token types       - 38 distinct token types (Word, Number, Email, URL, etc.)
├── Expansion/             - Address expansion and normalization
│   ├── AddressExpander   - Main orchestrator for address expansion
│   ├── ExpansionOptions  - Configuration flags for expansion behavior
│   ├── AddressDictionaryReader - Load binary address dictionaries
│   ├── GazetteerClassifier - Classify tokens as geographic entities
│   ├── PhraseClassifier  - Classify phrases into address components
│   ├── StringTree        - Generate all alternative expansions
│   └── RootExpansionPreAnalyzer - Analyze text for root expansion mode
├── ML/                    - Machine learning infrastructure
│   ├── SparseMatrix<T>   - CSR (Compressed Sparse Row) sparse matrix
│   └── LogisticRegression - Multi-class logistic regression classifier
└── LanguageClassifier/    - Language detection
    ├── LanguageClassifier - Language detection (logistic regression)
    ├── LanguageFeatureExtractor - Extract n-gram features
    └── LanguageResult    - Language detection result (code + confidence)
```

### Data Flow

```
Input Address
    ↓
[Tokenization] → tokens with offsets
    ↓
[Normalization] → normalized tokens
    ↓
[Dictionary Lookup] → phrases with expansions
    ↓
[StringTree] → generate all combinations
    ↓
[Token Normalization] → final expanded forms
    ↓
Output: List<string> (all variations)
```

### Key Design Patterns

1. **Layered Architecture** - Clear separation of concerns across tokenization → expansion → parsing
2. **TDD Methodology** - 375+ tests with 100% pass rate (1.3:1 test-to-code ratio)
3. **Record Types** - Immutable data structures (AddressExpansion, LanguageResult)
4. **Generic Components** - Trie<TData>, SparseMatrix<T> for reusability
5. **Composition** - LanguageClassifier uses LogisticRegression + LanguageFeatureExtractor
6. **Enum Flags** - Composable configuration (ExpansionOptions, AddressComponent)
7. **IDisposable Pattern** - Resource management for streams and large objects

## Important Implementation Details

### Unicode Support
- Full Unicode normalization (NFD/NFC)
- Grapheme-cluster-aware string reversal
- Script detection for 12+ writing systems
- Accent stripping and case-folding
- 60+ language support

### Address Components
The `AddressComponent` enum defines 16+ components that can be extracted:
- House number, road, unit, level, postcode, city, state, country
- Suburb, city_district, island, state_district, po_box, entrance, staircase

### Dictionary Types
The `DictionaryType` enum categorizes dictionary phrases:
- Street types (Street, Avenue, Boulevard, etc.)
- Directionals (North, South, East, West)
- Building types (Building, Office, Hospital, etc.)
- And others (prefix, suffix, stopwords)

### Sparse Matrices (ML)
The `SparseMatrix<T>` uses CSR (Compressed Sparse Row) format for memory-efficient storage of ML model weights. This is used by LogisticRegression for language classification.

## Phase Status

**Current**: Phase 6 complete - 375/375 tests passing

**Completed Phases:**
- ✅ Phase 1: Project Setup & Infrastructure
- ✅ Phase 2: Core Data Structures (Trie, StringUtils)
- ✅ Phase 3: Data File I/O (Binary readers/writers)
- ✅ Phase 4A: Core Tokenization & Normalization
- ✅ Phase 5A-5B: Address Expansion with root mode
- ✅ Phase 6: Language Classifier & ML Infrastructure

**Not Yet Implemented:**
- Address Parsing (CRF model integration)
- Transliteration support
- Numeric expression expansion
- Service integration (LibPostalService)
- Data package and NuGet distribution

See [plan.md](plan.md) for detailed progress tracking.

## TDD: Red-Green-Refactor Development

**MANDATORY**: All changes to this codebase MUST follow the Test-Driven Development (TDD) Red-Green-Refactor workflow. This is not optional.

### The TDD Workflow

```
RED → GREEN → REFACTOR → Repeat
```

**RED Phase**: Write failing tests first

- Create a new test file or add tests to existing test class
- Write test cases for the feature you want to implement
- Tests will FAIL (this is expected - the "red" phase)
- Verify tests fail with meaningful error messages

**GREEN Phase**: Write minimal implementation to pass tests

- Write the simplest code that makes the tests pass
- Don't over-engineer - focus on passing tests only
- All tests should now PASS (the "green" phase)
- Do not add extra features or optimizations yet

**REFACTOR Phase**: Improve code quality while keeping tests green

- Improve readability, performance, and design
- Extract common patterns into helper methods
- Optimize data structures if needed
- Ensure all tests STILL PASS after refactoring

### TDD Command Workflow

```bash
# 1. RED: Write tests and watch them fail
dotnet test --no-build

# 2. GREEN: Implement minimal code to pass tests
dotnet build --no-restore
dotnet test --no-build

# 3. REFACTOR: Improve while keeping tests green
dotnet build --no-restore
dotnet test --no-build

# 4. Verify no regressions across entire suite
dotnet test --no-build --verbosity normal
```

### Test Organization

- **Test files**: `LibPostal.Net.Tests/[Module]/[Feature]Tests.cs`
- **Test classes**: One per public class being tested
- **Test methods**: Clear, descriptive names describing the scenario
  - Good: `TokenizerHandlesEmailAddressesCorrectly`
  - Bad: `Test1`
- **Assertions**: Use FluentAssertions for readable assertions
  - Good: `result.Should().ContainSingle().Which.Should().Be("expected");`
  - Bad: `Assert.Single(result); Assert.Equal("expected", result[0]);`

## Common Development Tasks

### Adding a New Feature - TDD Red-Green-Refactor

1. **RED Phase - Write Failing Tests**

   ```bash
   # Create or edit test file: LibPostal.Net.Tests/[Module]/[Feature]Tests.cs
   # Write test cases for the new feature
   dotnet test --no-build --filter "ClassName~[Feature]Tests"
   # Tests will FAIL - this is correct (red phase)
   ```

2. **GREEN Phase - Implement Minimal Code**

   ```bash
   # Create or edit implementation file: LibPostal.Net/[Module]/[Feature].cs
   # Write the minimal code to pass tests
   dotnet build --no-restore
   dotnet test --no-build --filter "ClassName~[Feature]Tests"
   # Tests should now PASS (green phase)
   ```

3. **REFACTOR Phase - Improve Code Quality**

   ```bash
   # Review and refactor implementation:
   # - Extract duplicated logic
   # - Improve variable names
   # - Optimize algorithms
   # - Add XML documentation comments
   dotnet build --no-restore
   dotnet test --no-build --filter "ClassName~[Feature]Tests"
   # Tests must STILL PASS (refactor phase)
   ```

4. **Full Regression Testing**

   ```bash
   # Run entire test suite to ensure no regressions
   dotnet test --no-build --verbosity normal
   # All 375+ tests must PASS
   ```

5. **Update Documentation**

   - Update [plan.md](plan.md) with progress
   - Add XML documentation comments to public APIs
   - Update this CLAUDE.md if architecture changed

### Fixing a Bug - TDD Red-Green-Refactor

1. **RED Phase - Write Test Reproducing Bug**

   - Create a test case that reproduces the bug
   - Test should fail with the current code (demonstrate the bug)

2. **GREEN Phase - Fix with Minimal Changes**

   - Implement the fix (smallest change that passes the test)
   - Do not refactor or reorganize

3. **REFACTOR Phase - Improve If Needed**

   - Only if the fix needs improvement for maintainability

4. **Regression Testing**

   - Run full test suite to ensure fix doesn't break anything else

### Working with Tokenization
- Use `Tokenizer` for breaking text into tokens
- Use `StringNormalizer` for text-level transformations
- Use `TokenNormalizer` for token-level transformations
- Token types are in the `TokenType` enum (38 types)

### Working with Address Expansion
- `AddressExpander.Expand()` is the main entry point
- Uses `ExpansionOptions` to configure behavior
- Internally uses dictionary lookup, tokenization, and StringTree for combinations
- `ExpandRoot()` provides an alternative expansion mode

### Language Detection
- Use `LanguageClassifier.ClassifyLanguage()` for detection
- Returns `LanguageResult[]` with language codes and confidence scores
- Uses n-gram features extracted by `LanguageFeatureExtractor`
- Backed by `LogisticRegression` for multi-class classification

## Testing Approach

- **Framework**: xUnit with FluentAssertions
- **Test Structure**: Parallel to main code structure (tests/ mirrors src/ organization)
- **Coverage**: 375+ tests with 100% pass rate
- **Methodology**: Test-first (TDD) - write tests before implementation
- **Test Data**: Fixtures in `LibPostal.Net.Tests/Fixtures/` and `TestData/`

### Running Tests
```bash
# All tests
dotnet test --no-build

# With coverage
dotnet test --no-build /p:CollectCoverage=true

# Specific test file
dotnet test --no-build --filter "ClassName=LibPostal.Net.Tests.Tokenization.TokenizerTests"
```

## Key Files & Locations

**Core Implementation:**
- [LibPostal.Net/Core/Trie.cs](LibPostal.Net/Core/Trie.cs) - Generic trie structure
- [LibPostal.Net/Core/StringUtils.cs](LibPostal.Net/Core/StringUtils.cs) - Unicode utilities
- [LibPostal.Net/Tokenization/Tokenizer.cs](LibPostal.Net/Tokenization/Tokenizer.cs) - Main tokenizer
- [LibPostal.Net/Expansion/AddressExpander.cs](LibPostal.Net/Expansion/AddressExpander.cs) - Address expansion engine
- [LibPostal.Net/LanguageClassifier/LanguageClassifier.cs](LibPostal.Net/LanguageClassifier/LanguageClassifier.cs) - Language detection

**Configuration:**
- [LibPostal.Net.sln](LibPostal.Net.sln) - Solution file
- [plan.md](plan.md) - Implementation progress tracker
- [README.md](README.md) - Project documentation

## Debugging Tips

1. **Token Offset Issues** - Tokens have Offset and Length properties for position tracking in original text
2. **Unicode Handling** - Use `StringUtils` for grapheme-aware operations, not simple string indexing
3. **Expansion Not Working** - Check `ExpansionOptions` configuration and dictionary loading
4. **Memory Usage** - SparseMatrix uses CSR format to minimize memory for ML models

## References

- Original libpostal: https://github.com/openvenues/libpostal
- Trained on 1B+ addresses from OpenStreetMap and OpenAddresses
- Target: 10,000+ addresses/second, 99.45% accuracy
- License: MIT

