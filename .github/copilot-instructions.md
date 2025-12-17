---
excludeAgent: ["coding-agent"]
---

# GitHub Copilot PR Review Instructions

These instructions guide PR reviews for this .NET 8.0 C# project. Focus on code quality, maintainability, security, and adherence to established patterns.

---

## Review Philosophy

- Provide assertive, actionable feedback on substantive issues
- Focus on correctness, security, performance, and maintainability
- Reference existing patterns in the codebase
- Be specific: cite line numbers and provide concrete examples
- Prioritize high-impact issues over minor style preferences

---

## C# Code Review Guidelines

### Coding Conventions & Best Practices

#### Naming Conventions
- Use PascalCase for public members, classes, interfaces: `public class UserService`
- Use camelCase for private fields with underscore prefix: `private readonly ILogger _logger;`
- Use descriptive names avoiding abbreviations: `customerRepository` not `custRepo`
- Interface names must start with 'I': `IUserRepository`
- Flag single-letter variables except loop counters

#### File Organization
- One public type per file
- File name must match the type name
- Order members: fields → constructors → properties → methods
- Group related methods together
- Keep methods focused and under 50 lines when possible

#### Code Examples to Flag:
```csharp
// BAD - Poor naming
public class usr { }
private string n;
public void Proc() { }

// GOOD - Clear naming
public class UserService { }
private string _userName;
public void ProcessOrder() { }
```

---

### SOLID Principles & Design Patterns

#### Single Responsibility Principle
- Flag classes handling multiple concerns (e.g., data access + business logic + presentation)
- Each class should have one reason to change
- Suggest splitting when a class has multiple unrelated responsibilities

```csharp
// BAD - Multiple responsibilities
public class OrderService
{
    public void SaveOrder(Order order) { /* DB access */ }
    public void SendConfirmationEmail(Order order) { /* Email */ }
    public decimal CalculateTotal(Order order) { /* Business logic */ }
}

// GOOD - Single responsibility
public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly IEmailService _emailService;

    public async Task<Order> CreateOrderAsync(Order order)
    {
        // Only business logic coordination
        var total = CalculateTotal(order);
        order.Total = total;
        await _repository.SaveAsync(order);
        await _emailService.SendConfirmationAsync(order);
        return order;
    }
}
```

#### Dependency Injection
- Constructor injection is preferred for required dependencies
- Property injection only for optional dependencies
- Avoid service locator pattern
- Register services with appropriate lifetime (Singleton/Scoped/Transient)
- Flag direct instantiation of dependencies with `new` keyword

```csharp
// BAD - Direct instantiation, tight coupling
public class OrderService
{
    private readonly EmailSender _emailSender = new EmailSender();
    private readonly SqlOrderRepository _repository = new SqlOrderRepository();
}

// GOOD - Constructor injection, loose coupling
public class OrderService
{
    private readonly IEmailService _emailService;
    private readonly IOrderRepository _repository;

    public OrderService(IEmailService emailService, IOrderRepository repository)
    {
        _emailService = emailService;
        _repository = repository;
    }
}
```

#### Open/Closed Principle
- Flag modification of existing classes when extension is possible
- Recommend interfaces and abstract classes for extensibility
- Use strategy pattern for varying algorithms

#### Interface Segregation
- Flag "fat" interfaces with many unrelated methods
- Recommend splitting into smaller, focused interfaces
- Clients should not depend on methods they don't use

---

### Async/Await Patterns

#### Required Patterns
- Use `async Task` or `async Task<T>`, not `async void` (except event handlers)
- Await async methods - don't use `.Result` or `.Wait()` (causes deadlocks)
- Use `ConfigureAwait(false)` in library code (not required in ASP.NET Core)
- Suffix async methods with 'Async': `GetUserAsync()`, `SaveDataAsync()`
- Return `Task` or `Task<T>`, not `void` for async methods

#### Common Anti-Patterns to Flag

```csharp
// BAD - Blocking on async code (causes deadlocks in UI/ASP.NET)
var result = GetDataAsync().Result;
var users = SaveUserAsync().GetAwaiter().GetResult();

// BAD - async void (exceptions can crash app)
public async void ProcessOrder()
{
    await SaveAsync();
}

// BAD - Missing Async suffix
public async Task<User> GetUser(int id) { }

// GOOD
var result = await GetDataAsync();
var users = await SaveUserAsync();

public async Task ProcessOrderAsync()
{
    await SaveAsync();
}

public async Task<User> GetUserAsync(int id) { }
```

#### Performance Considerations
- Use `ValueTask<T>` for hot paths that may complete synchronously
- Avoid unnecessary async state machines for synchronous operations
- Use `Task.WhenAll()` for parallel operations, not sequential awaits
- Use `Task.WhenAny()` for timeout or cancellation scenarios

```csharp
// BAD - Sequential awaits (slow)
var user = await GetUserAsync(id);
var orders = await GetOrdersAsync(id);
var profile = await GetProfileAsync(id);

// GOOD - Parallel execution (fast)
var userTask = GetUserAsync(id);
var ordersTask = GetOrdersAsync(id);
var profileTask = GetProfileAsync(id);
await Task.WhenAll(userTask, ordersTask, profileTask);
```

---

### Exception Handling & Resource Disposal

#### IDisposable Pattern
- Implement IDisposable for classes managing unmanaged resources
- Use `using` statements or `using` declarations for disposable objects
- Dispose resources in finally blocks if not using `using`
- Flag missing Dispose calls for: DbContext, HttpClient, FileStream, SqlConnection, etc.

```csharp
// BAD - Resource leak
var context = new ApplicationDbContext();
var users = context.Users.ToList();
// context never disposed!

// GOOD - Proper disposal (using statement)
using (var context = new ApplicationDbContext())
{
    var users = context.Users.ToList();
}

// BETTER - Using declaration (C# 8.0+)
using var context = new ApplicationDbContext();
var users = context.Users.ToList();
// Automatically disposed at end of scope
```

#### Exception Handling Best Practices
- Catch specific exceptions, not generic `catch (Exception ex)` unless at application boundary
- Don't swallow exceptions silently
- Include context in exception messages
- Use exception filters for logging: `catch (Exception ex) when (Log(ex))`
- Avoid throwing from finally blocks
- Don't catch exceptions you can't handle

```csharp
// BAD - Swallowing exceptions
try
{
    await SaveAsync();
}
catch { }

// BAD - Catching too broadly
try
{
    await SaveAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error");
    return null; // Hides problem!
}

// GOOD - Specific exception handling
try
{
    await SaveAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning(ex, "Concurrency conflict for user {UserId}", userId);
    throw new BusinessException("User data was modified by another process", ex);
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Failed to save user {UserId}", userId);
    throw;
}
```

#### Throw Guidelines
- Use `throw;` to rethrow exceptions (preserves stack trace)
- Don't use `throw ex;` (resets stack trace)
- Use `ArgumentNullException.ThrowIfNull(param)` for .NET 6+
- Throw specific exception types, not generic `Exception`

---

### Nullable Reference Types & Null Handling

#### .NET 8.0 Nullable Context
- Project enables nullable reference types (`<Nullable>enable</Nullable>`)
- Use `?` for nullable reference types: `string? optionalValue`
- Use `!` null-forgiving operator sparingly and only when certain value is not null
- Flag missing null checks before dereferencing
- Prefer nullable value types over `Nullable<T>`: `int?` not `Nullable<int>`

#### Null Checking Patterns

```csharp
// BAD - Potential null reference
public void ProcessUser(User user)
{
    Console.WriteLine(user.Name.ToUpper()); // What if user or Name is null?
}

// GOOD - Proper null checks
public void ProcessUser(User? user)
{
    ArgumentNullException.ThrowIfNull(user);

    if (string.IsNullOrEmpty(user.Name))
        throw new ArgumentException("Name cannot be empty", nameof(user));

    Console.WriteLine(user.Name.ToUpper());
}

// GOOD - Null-conditional operator
public string? GetUserDisplayName(User? user)
{
    return user?.Profile?.DisplayName ?? "Guest";
}

// GOOD - Pattern matching
if (user is not null && user.IsActive)
{
    ProcessActiveUser(user);
}
```

#### Common Issues to Flag
- Unnecessary null-forgiving operators: `value!` when value is guaranteed non-null
- Missing null checks on method parameters
- Dereferencing potentially null properties without checks
- Using `!` to suppress warnings instead of proper null handling

#### Recommendations
- Use null-coalescing: `value ?? defaultValue` instead of ternary
- Use null-conditional: `obj?.Property` instead of `obj != null ? obj.Property : null`
- Use pattern matching for null checks: `if (obj is not null)`
- Use `ArgumentNullException.ThrowIfNull()` for parameter validation

---

### Performance Considerations

#### LINQ Efficiency
- Use `Any()` instead of `Count() > 0` for existence checks
- Use `FirstOrDefault()` instead of `Where().First()`
- Avoid multiple enumeration - materialize with `ToList()` if needed
- Use `AsNoTracking()` for read-only EF Core queries
- Consider `AsEnumerable()` vs `AsQueryable()` implications

```csharp
// BAD - Inefficient
if (users.Count() > 0)
if (users.Where(u => u.IsActive).Count() > 0)
var first = users.Where(u => u.Age > 18).First();

// GOOD - Efficient
if (users.Any())
if (users.Any(u => u.IsActive))
var first = users.First(u => u.Age > 18);

// BAD - Multiple enumeration
var activeUsers = GetUsers().Where(u => u.IsActive);
var count = activeUsers.Count();
var names = activeUsers.Select(u => u.Name).ToList();

// GOOD - Single enumeration
var activeUsers = GetUsers().Where(u => u.IsActive).ToList();
var count = activeUsers.Count;
var names = activeUsers.Select(u => u.Name).ToList();
```

#### Memory Allocations
- Use `StringBuilder` for string concatenation in loops
- Prefer `stackalloc` for small, short-lived arrays (< 1KB)
- Use `ArrayPool<T>` for temporary buffers
- Avoid unnecessary boxing of value types
- Use `Span<T>` and `Memory<T>` for zero-allocation slicing

```csharp
// BAD - String concatenation in loop
string result = "";
foreach (var item in items)
{
    result += item.ToString(); // Allocates new string each iteration!
}

// GOOD - StringBuilder
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.Append(item.ToString());
}
string result = sb.ToString();
```

#### Collection Initialization
- Specify capacity for collections with known sizes: `new List<T>(capacity)`
- Use collection expressions (C# 12): `[1, 2, 3]` instead of `new[] { 1, 2, 3 }`
- Prefer `List<T>` over `ArrayList`, `Dictionary<K,V>` over `Hashtable`
- Use `HashSet<T>` for uniqueness checks instead of `List<T>.Contains()`

```csharp
// BAD - No capacity specified
var users = new List<User>();
for (int i = 0; i < 1000; i++)
{
    users.Add(GetUser(i)); // Multiple reallocations!
}

// GOOD - Capacity specified
var users = new List<User>(1000);
for (int i = 0; i < 1000; i++)
{
    users.Add(GetUser(i)); // Single allocation
}
```

---

### Thread Safety & Concurrency

#### Thread-Safe Operations
- Flag shared mutable state accessed from multiple threads
- Use `lock` statement for critical sections
- Prefer `ConcurrentDictionary`, `ConcurrentQueue`, `ConcurrentBag` for concurrent collections
- Use `Interlocked` for atomic operations on counters
- Consider immutable types for thread safety

```csharp
// BAD - Race condition
private int _counter;
public void Increment()
{
    _counter++; // Not atomic!
}

// GOOD - Thread-safe with Interlocked
private int _counter;
public void Increment()
{
    Interlocked.Increment(ref _counter);
}

// Or use lock for complex operations
private readonly object _lock = new();
private int _counter;
public void UpdateState()
{
    lock (_lock)
    {
        _counter++;
        // Other operations...
    }
}
```

#### Async Concurrency
- Avoid `async void` to prevent unhandled exceptions
- Use `SemaphoreSlim` for async locking (not `lock` statement)
- Configure cancellation tokens for long-running operations
- Use `CancellationTokenSource.CreateLinkedTokenSource()` for combined cancellation

```csharp
// BAD - Lock with async (don't do this!)
lock (_lock)
{
    await DoWorkAsync(); // Blocks thread!
}

// GOOD - SemaphoreSlim for async
private readonly SemaphoreSlim _semaphore = new(1, 1);

public async Task DoWorkAsync()
{
    await _semaphore.WaitAsync();
    try
    {
        await PerformOperationAsync();
    }
    finally
    {
        _semaphore.Release();
    }
}
```

#### Common Thread Safety Issues
- Static mutable fields accessed from multiple threads
- Lazy initialization without proper synchronization
- Async void methods (exceptions escape to synchronization context)
- Capturing loop variables in closures

---

## Test Code Review Guidelines

Apply these rules when reviewing test files (under `**/Tests/**/*.cs`):

### Test Structure & Organization

#### Test Framework Usage
- Verify consistent test framework usage throughout the project
- **xUnit**: Use `[Fact]` for parameterless tests, `[Theory]` with `[InlineData]` for parameterized tests
- **NUnit**: Use `[Test]` and `[TestCase]` appropriately
- **MSTest**: Use `[TestMethod]` and `[DataTestMethod]`

#### Naming Conventions
- Test method names should describe what is being tested
- Pattern: `MethodName_Scenario_ExpectedBehavior`
- Examples:
  - `GetUser_WithValidId_ReturnsUser`
  - `SaveOrder_WhenDatabaseFails_ThrowsException`
  - `CalculateTotal_WithDiscount_AppliesCorrectPercentage`
- Test class names should match the class under test with "Tests" suffix
  - `UserService` → `UserServiceTests`
  - `OrderRepository` → `OrderRepositoryTests`

### Arrange-Act-Assert (AAA) Pattern

Every test must follow the AAA pattern with clear separation:

```csharp
[Fact]
public async Task CreateUser_WithValidData_SavesUserToDatabase()
{
    // Arrange - Setup test data and dependencies
    var mockRepo = new Mock<IUserRepository>();
    var service = new UserService(mockRepo.Object);
    var userData = new CreateUserRequest { Name = "John Doe", Email = "john@example.com" };

    // Act - Execute the operation being tested
    var result = await service.CreateUserAsync(userData);

    // Assert - Verify expected outcomes
    Assert.NotNull(result);
    Assert.Equal("John Doe", result.Name);
    mockRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
}
```

### Mock Usage & Test Isolation

#### Mocking Best Practices
- Mock external dependencies (databases, APIs, file system, time providers)
- Don't mock the class under test
- Verify important interactions with mocks
- Use `It.IsAny<T>()` sparingly - prefer specific value matching when possible
- Setup return values for all mocked method calls used in tests

```csharp
// BAD - Not mocking external dependencies
[Fact]
public async Task TestRealDatabase()
{
    var context = new ApplicationDbContext(); // Real DB!
    var service = new UserService(context);
    // ...
}

// GOOD - Mocking external dependencies
[Fact]
public async Task CreateUser_WithValidData_CallsRepository()
{
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(new User { Id = 1, Name = "John" });

    var service = new UserService(mockRepo.Object);

    await service.CreateUserAsync(new CreateUserRequest { Name = "John" });

    mockRepo.Verify(r => r.AddAsync(It.Is<User>(u => u.Name == "John")), Times.Once);
}
```

#### Test Isolation
- Each test should be independent and not rely on other tests
- Don't share mutable state between tests
- Clean up resources in Dispose or cleanup methods
- Avoid static state that persists between tests
- Use test fixtures for expensive setup (but carefully!)

### Test Coverage & Edge Cases

Flag missing test coverage for:
- **Null/empty input validation**: null parameters, empty strings, empty collections
- **Boundary conditions**: max/min values, zero, negative numbers
- **Exception scenarios**: database failures, network errors, validation failures
- **Different code paths**: if/else branches, switch cases, early returns
- **Async cancellation**: CancellationToken scenarios
- **Concurrent access**: thread safety if applicable

```csharp
// Example: Comprehensive test coverage
public class CalculatorTests
{
    [Fact]
    public void Add_WithPositiveNumbers_ReturnsSum() { }

    [Fact]
    public void Add_WithNegativeNumbers_ReturnsSum() { }

    [Fact]
    public void Add_WithZero_ReturnsOtherNumber() { }

    [Fact]
    public void Add_WithMaxValue_ThrowsOverflowException() { }

    [Theory]
    [InlineData(5, 3, 8)]
    [InlineData(-5, 3, -2)]
    [InlineData(0, 0, 0)]
    public void Add_WithVariousInputs_ReturnsExpectedResult(int a, int b, int expected) { }
}
```

### Common Test Anti-Patterns

```csharp
// BAD - Testing multiple scenarios in one test
[Fact]
public void TestUserService()
{
    // Tests creation, update, and deletion all at once
    var user = service.CreateUser(userData);
    service.UpdateUser(user);
    service.DeleteUser(user.Id);
    // Too much in one test!
}

// BAD - No assertions
[Fact]
public void CreateUser()
{
    service.CreateUser(user); // No Assert - what are we testing?
}

// BAD - Catches and ignores expected exception
[Fact]
public void ShouldThrowException()
{
    try
    {
        service.InvalidOperation();
    }
    catch { } // Don't do this!
}

// BAD - Testing implementation details
[Fact]
public void ProcessOrder_CallsPrivateMethod()
{
    // Don't test private methods directly!
}

// GOOD - Single responsibility, clear assertions
[Fact]
public void CreateUser_WithValidData_ReturnsCreatedUser()
{
    var user = service.CreateUser(validData);

    Assert.NotNull(user);
    Assert.Equal("John", user.Name);
    Assert.True(user.Id > 0);
}

// GOOD - Testing exception properly (xUnit)
[Fact]
public void CreateUser_WithNullData_ThrowsArgumentNullException()
{
    Assert.Throws<ArgumentNullException>(() => service.CreateUser(null));
}

// GOOD - Testing exception properly (async)
[Fact]
public async Task CreateUserAsync_WithInvalidEmail_ThrowsValidationException()
{
    var exception = await Assert.ThrowsAsync<ValidationException>(
        () => service.CreateUserAsync(invalidData)
    );
    Assert.Contains("email", exception.Message.ToLower());
}
```

---

## Project File Review (.csproj)

### Package Management
- Verify package versions are current and non-vulnerable
- Flag deprecated packages
- Check for duplicate package references across `<ItemGroup>` sections
- Ensure consistent versions across projects (for multi-project solutions)
- Verify `<PackageReference>` includes `Version` attribute

### Target Framework
- Confirm `<TargetFramework>net8.0</TargetFramework>` is used
- Flag outdated framework targets (net6.0, net7.0, netcoreapp3.1)
- For libraries, consider multi-targeting if needed

### Configuration Properties
- Verify `<Nullable>enable</Nullable>` is set (required for this project)
- Check for `<ImplicitUsings>enable</ImplicitUsings>` for .NET 6+ projects
- Consider `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in Release builds
- Flag unnecessary package references or imports

### Example Review:

```xml
<!-- BAD - Outdated framework -->
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
</PropertyGroup>

<!-- BAD - Missing nullable -->
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>

<!-- GOOD - Correct configuration -->
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

---

## What NOT to Review

Skip feedback on:
- Auto-generated files (`*.g.cs`, `*.Designer.cs`, files in `obj/` or `bin/`)
- Minor style preferences without substantive impact (spacing, line breaks)
- Subjective design choices that follow existing patterns in the codebase
- Test files for theoretical edge cases with no practical impact
- Documentation files unless there are technical inaccuracies
- Build/CI configuration files (covered by separate processes)
- Migration files or generated code from Entity Framework

Focus energy on substantive issues affecting:
- **Correctness**: Bugs, logic errors, incorrect behavior
- **Security**: Vulnerabilities, data exposure, authentication issues
- **Performance**: Inefficient algorithms, memory leaks, unnecessary allocations
- **Maintainability**: Code clarity, SOLID violations, tight coupling

---

## Review Output Guidelines

### Comment Structure

When providing feedback, use this structure:

1. **State the issue clearly**: What is wrong and where (with line number)
2. **Explain the impact**: Why it matters (security, performance, correctness, maintainability)
3. **Provide recommendation**: How to fix it with code example if applicable
4. **Cite specific locations**: Reference line numbers or code snippets

### Example Review Comment Format:

```
**Issue: Potential NullReferenceException** (Line 42 in UserService.cs)

The `user.Profile.Name` access doesn't check if `Profile` is null, which will throw NullReferenceException if a user has no profile.

**Impact**: Application will crash when processing users without profiles, causing poor user experience and potential data loss.

**Recommendation**: Use null-conditional operator or add explicit null check:

```csharp
// Option 1: Null-conditional with default
var name = user.Profile?.Name ?? "Unknown";

// Option 2: Explicit null check
if (user.Profile is null)
    throw new InvalidOperationException("User must have a profile");
var name = user.Profile.Name;
```
```

### Severity Levels

Use these severity levels to prioritize feedback:

- **Critical**: Security vulnerabilities, data corruption, application crashes
- **High**: Correctness issues, resource leaks, significant performance problems
- **Medium**: Code quality issues, maintainability concerns, best practice violations
- **Low**: Style preferences, minor optimizations, suggestions

**Focus primarily on Critical and High severity issues in PR reviews.**

---

## .NET 8.0 Specific Requirements

This project targets .NET 8.0. Verify adherence to these requirements:

### Framework Features
- Use C# 12 features where appropriate: collection expressions, primary constructors
- Leverage nullable reference types (required in this project)
- Use implicit usings (enabled in project)
- Prefer `required` keyword for mandatory properties over constructor parameters

### ASP.NET Core (if applicable)
- Use minimal APIs or controller-based APIs consistently
- Verify middleware order in `Program.cs`
- Use built-in dependency injection
- Apply `[ApiController]` attribute on API controllers

### Entity Framework Core (if applicable)
- Use EF Core 8.0 features
- Always use `AsNoTracking()` for read-only queries
- Use compiled queries for frequently-executed queries
- Verify migrations are generated correctly

---

This completes the C# code review guidelines. Apply these rules consistently and focus on providing actionable, high-value feedback that improves code quality and security.
