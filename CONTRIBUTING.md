# Contributing to LibPostal.Net

Thank you for your interest in contributing to LibPostal.Net! This document provides guidelines and instructions for contributing.

## Code of Conduct

This project adheres to a code of conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When creating a bug report, include:

- **Clear title and description**
- **Steps to reproduce** the issue
- **Expected behavior** vs. **actual behavior**
- **Sample code** demonstrating the problem
- **.NET version** and **OS version**
- **Stack trace** if applicable

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, include:

- **Clear use case** for the enhancement
- **Why this enhancement would be useful** to most users
- **Possible implementation** if you have ideas

### Code Contributions

#### Development Setup

1. **Fork and clone** the repository:
   ```bash
   git clone https://github.com/yourusername/LibPostal.Net.git
   cd LibPostal.Net
   ```

2. **Install .NET 9 SDK**:
   - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0)

3. **Restore dependencies**:
   ```bash
   dotnet restore LibPostal.Net.sln
   ```

4. **Build the solution**:
   ```bash
   dotnet build LibPostal.Net.sln
   ```

5. **Run tests**:
   ```bash
   dotnet test LibPostal.Net.sln
   ```

#### Development Process

We follow **Test-Driven Development (TDD)**:

1. **Write tests first** - Before implementing a feature, write failing tests
2. **Implement the feature** - Make the tests pass
3. **Refactor** - Improve code quality while keeping tests green

Example workflow:

```csharp
// 1. Write a failing test
[Fact]
public void ParseAddress_WithHouseNumber_ShouldExtractNumber()
{
    // Arrange
    var parser = new AddressParser();

    // Act
    var result = parser.Parse("123 Main St");

    // Assert
    result.HouseNumber.Should().Be("123");
}

// 2. Run test (it should fail)
// 3. Implement the feature
// 4. Run test (it should pass)
// 5. Refactor if needed
```

#### Coding Standards

**C# Style Guide:**

- Use **C# 13** features where appropriate
- Enable **nullable reference types** (`#nullable enable`)
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use **meaningful names** for variables, methods, and classes
- Keep methods **short and focused** (single responsibility)
- Add **XML documentation** for all public APIs

**Code Style:**

```csharp
namespace LibPostal.Net;

/// <summary>
/// Parses international addresses into labeled components.
/// </summary>
public class AddressParser : IDisposable
{
    private readonly ParserModel _model;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressParser"/> class.
    /// </summary>
    /// <param name="model">The trained parser model.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="model"/> is null.</exception>
    public AddressParser(ParserModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Parses an address string into labeled components.
    /// </summary>
    /// <param name="address">The address to parse.</param>
    /// <returns>The parsed address components.</returns>
    public AddressComponents Parse(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // Implementation here
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        _model?.Dispose();
        _disposed = true;
    }
}
```

#### Testing Guidelines

- **Unit tests** should be fast (<100ms) and isolated
- Use **FluentAssertions** for readable assertions
- Name tests clearly: `MethodName_Scenario_ExpectedBehavior`
- Test **both happy paths and edge cases**
- Aim for **high code coverage** (>80%)

```csharp
public class AddressParserTests
{
    [Fact]
    public void Parse_WithValidAddress_ShouldExtractComponents()
    {
        // Arrange
        var parser = CreateParser();
        var address = "123 Main St, Springfield, IL 62701";

        // Act
        var result = parser.Parse(address);

        // Assert
        result.Should().NotBeNull();
        result.HouseNumber.Should().Be("123");
        result.Road.Should().Be("main st");
        result.City.Should().Be("springfield");
        result.State.Should().Be("il");
        result.Postcode.Should().Be("62701");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_WithInvalidInput_ShouldThrowArgumentException(string? input)
    {
        // Arrange
        var parser = CreateParser();

        // Act & Assert
        var act = () => parser.Parse(input!);
        act.Should().Throw<ArgumentException>();
    }
}
```

#### Performance Guidelines

- Use **Span\<T\>** and **Memory\<T\>** for zero-allocation string operations
- Prefer **ArrayPool** for temporary buffers
- Use **ValueTask** for hot paths
- Profile with **BenchmarkDotNet** before optimizing

```csharp
// Good: Zero-allocation substring
ReadOnlySpan<char> span = text.AsSpan(start, length);

// Bad: Allocates string
string substring = text.Substring(start, length);
```

#### Pull Request Process

1. **Create a feature branch**:
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Make your changes** following TDD and coding standards

3. **Write/update tests** to cover your changes

4. **Update documentation** (XML docs, README, etc.)

5. **Ensure all tests pass**:
   ```bash
   dotnet test LibPostal.Net.sln
   ```

6. **Commit with clear messages**:
   ```bash
   git commit -m "Add address parsing for Japanese addresses"
   ```

7. **Push to your fork**:
   ```bash
   git push origin feature/my-feature
   ```

8. **Create a Pull Request** on GitHub

9. **Address review feedback**

#### Pull Request Checklist

- [ ] Code follows the project's style guidelines
- [ ] All tests pass locally
- [ ] New code has corresponding tests
- [ ] Public APIs have XML documentation
- [ ] README updated if needed
- [ ] No compiler warnings
- [ ] Commit messages are clear and descriptive

### Migrating Tests from libpostal

We're migrating all tests from the original libpostal C library. To contribute:

1. **Choose a test file** from `libpostal/test/` (see [plan.md](plan.md))
2. **Create corresponding C# test file**
3. **Port test cases** maintaining the same test logic
4. **Verify tests fail** before implementation
5. **Implement feature** to make tests pass

Example migration:

```c
// Original C test (libpostal/test/test_expand.c)
char *input = "30 W 26th St";
size_t num_expansions;
char **expansions = libpostal_expand_address(input, options, &num_expansions);
assert_true(num_expansions > 0);
```

```csharp
// Migrated C# test (LibPostal.Net.Tests/ExpansionTests.cs)
[Fact]
public void ExpandAddress_WithAbbreviatedStreet_ShouldGenerateExpansions()
{
    // Arrange
    var postal = new LibPostalService();
    var input = "30 W 26th St";

    // Act
    var expansions = postal.ExpandAddress(input);

    // Assert
    expansions.Should().NotBeEmpty();
}
```

## Project Structure

```
LibPostal.Net/
├── LibPostal.Net/              # Main library
│   ├── Core/                   # Core data structures
│   ├── Expansion/              # Address expansion
│   ├── Parsing/                # Address parsing (CRF)
│   ├── Classification/         # Language classification
│   ├── IO/                     # Data file readers
│   └── LibPostalService.cs    # Main API
├── LibPostal.Net.Data/         # Data package project
├── LibPostal.Net.Tests/        # Unit tests
│   ├── Fixtures/               # Test fixtures
│   └── TestData/               # Test data files
├── LibPostal.Net.Benchmarks/   # Performance benchmarks
└── plan.md                     # Implementation progress
```

## Development Phases

We're following a phased approach (see [plan.md](plan.md)):

- **Phase 1**: Project Setup ✅
- **Phase 2**: Core Data Structures (Current)
- **Phase 3-11**: See plan.md for details

Please coordinate with the team to avoid duplicate work.

## Questions?

- Open a [GitHub Discussion](https://github.com/yourusername/LibPostal.Net/discussions)
- Check existing [Issues](https://github.com/yourusername/LibPostal.Net/issues)
- Review the [plan.md](plan.md) progress tracker

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
