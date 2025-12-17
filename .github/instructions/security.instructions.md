---
excludeAgent: [coding-agent]
applyTo: "**/*.cs, **/*.csproj"
---

# Security Review Instructions

These instructions guide security-focused PR reviews for C# code changes. Focus on HIGH-CONFIDENCE vulnerabilities with clear exploitation paths.

---

## Review Objective

Identify exploitable security vulnerabilities in PR changes, not general code quality issues.

**Only flag issues where:**
- Exploitation path is clear and concrete
- Impact is significant (data breach, unauthorized access, RCE)
- Confidence level is >80%

## Scope

- **IN SCOPE**: New code changes in this PR
- **OUT OF SCOPE**: Pre-existing vulnerabilities, theoretical issues, test code

---

## Security Vulnerability Categories

### 1. Input Validation Vulnerabilities

#### SQL Injection

Flag unsanitized user input in SQL queries:
- String concatenation or interpolation in SQL commands
- Look for `FromSqlRaw`, `ExecuteSqlRaw` with string building
- Recommend parameterized queries or Entity Framework LINQ

```csharp
// VULNERABILITY: SQL Injection
public List<User> SearchUsers(string username)
{
    var sql = $"SELECT * FROM Users WHERE Username = '{username}'";
    return _context.Users.FromSqlRaw(sql).ToList();
}

// SECURE: Parameterized query
public List<User> SearchUsers(string username)
{
    var sql = "SELECT * FROM Users WHERE Username = {0}";
    return _context.Users.FromSqlRaw(sql, username).ToList();
}

// BETTER: Use LINQ (always safe)
public List<User> SearchUsers(string username)
{
    return _context.Users.Where(u => u.Username == username).ToList();
}
```

#### Command Injection

Flag user input passed to system commands:
- `Process.Start()` with user-controlled arguments
- `ProcessStartInfo` with untrusted input
- Shell command execution with string concatenation

```csharp
// VULNERABILITY: Command injection
public void ConvertFile(string fileName)
{
    var process = Process.Start("convert", $"{fileName} output.pdf");
}

// SECURE: Validate and whitelist
public void ConvertFile(string fileName)
{
    // Validate filename
    if (!Regex.IsMatch(fileName, @"^[a-zA-Z0-9_\-\.]+$"))
        throw new ArgumentException("Invalid filename");

    var safeFileName = Path.GetFileName(fileName);
    var process = new ProcessStartInfo
    {
        FileName = "convert",
        Arguments = $"\"{safeFileName}\" output.pdf",
        UseShellExecute = false
    };
    Process.Start(process);
}
```

#### Path Traversal

Flag user input used in file paths without validation:
- `Path.Combine()` with untrusted input
- File operations with user-controlled paths
- Missing validation against allowed directories

```csharp
// VULNERABILITY: Path traversal
public byte[] ReadUserFile(string fileName)
{
    var filePath = Path.Combine(_uploadsDir, fileName);
    return File.ReadAllBytes(filePath); // Can read ../../etc/passwd
}

// SECURE: Validate and sanitize
public byte[] ReadUserFile(string fileName)
{
    // Remove directory parts
    var safeFileName = Path.GetFileName(fileName);

    // Build full path and validate
    var filePath = Path.GetFullPath(Path.Combine(_uploadsDir, safeFileName));
    var uploadsFullPath = Path.GetFullPath(_uploadsDir);

    if (!filePath.StartsWith(uploadsFullPath))
        throw new SecurityException("Access denied");

    return File.ReadAllBytes(filePath);
}
```

---

### 2. Authentication & Authorization

#### Authentication Bypass

Flag authentication logic that can be bypassed:
- Missing authentication checks on sensitive endpoints
- Logic errors in authentication validation
- Verify `[Authorize]` attributes on controller actions

```csharp
// VULNERABILITY: Missing authentication
[HttpGet("admin/users")]
public IActionResult GetAllUsers()
{
    return Ok(_userService.GetAllUsers()); // Anyone can access!
}

// SECURE: Require authentication and authorization
[HttpGet("admin/users")]
[Authorize(Roles = "Admin")]
public IActionResult GetAllUsers()
{
    return Ok(_userService.GetAllUsers());
}
```

#### Privilege Escalation

Flag operations where user permissions aren't verified:
- User ID parameters taken from client without verification
- Role checks that can be circumvented
- Missing server-side permission validation

```csharp
// VULNERABILITY: User ID from client
[HttpDelete("users/{userId}")]
[Authorize]
public IActionResult DeleteUser(int userId)
{
    _userService.Delete(userId); // Any user can delete any other user!
    return NoContent();
}

// SECURE: Use authenticated user's identity
[HttpDelete("users/me")]
[Authorize]
public IActionResult DeleteAccount()
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    _userService.Delete(userId); // Can only delete own account
    return NoContent();
}

// Or verify permission
[HttpDelete("users/{userId}")]
[Authorize]
public IActionResult DeleteUser(int userId)
{
    var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

    if (currentUserId != userId && !User.IsInRole("Admin"))
        return Forbid();

    _userService.Delete(userId);
    return NoContent();
}
```

---

### 3. Cryptography & Secrets

#### Hardcoded Secrets

Flag API keys, passwords, tokens, connection strings in code:
- String literals with sensitive data
- Hardcoded credentials
- Recommend using configuration and environment variables

```csharp
// VULNERABILITY: Hardcoded credentials
public class ApiClient
{
    private const string ApiKey = "sk-1234567890abcdef";
    private const string Password = "MyP@ssw0rd123";
}

// SECURE: Use configuration
public class ApiClient
{
    private readonly string _apiKey;

    public ApiClient(IConfiguration config)
    {
        _apiKey = config["ApiSettings:ApiKey"]
            ?? throw new InvalidOperationException("API key not configured");
    }
}
```

#### Weak Cryptography

Flag weak or deprecated cryptographic algorithms:
- MD5, SHA1 for password hashing (use BCrypt, PBKDF2, Argon2)
- DES, RC4 encryption (use AES)
- Missing or static IV/salt for encryption

```csharp
// VULNERABILITY: MD5 for password hashing
public string HashPassword(string password)
{
    using var md5 = MD5.Create();
    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hash);
}

// SECURE: Use ASP.NET Core Identity PasswordHasher
public string HashPassword(string password)
{
    var hasher = new PasswordHasher<User>();
    return hasher.HashPassword(null, password);
}

// VULNERABILITY: DES encryption
var des = DES.Create();
des.Key = key;

// SECURE: AES encryption with proper IV
using var aes = Aes.Create();
aes.GenerateKey();
aes.GenerateIV(); // Random IV each time
```

---

### 4. Deserialization & Code Execution

#### Unsafe Deserialization

Flag dangerous deserialization methods:
- `BinaryFormatter` (exploitable RCE - deprecated)
- `XmlSerializer` with untrusted input without type restrictions
- Recommend JSON serializers with type validation

```csharp
// VULNERABILITY: BinaryFormatter RCE
public object DeserializeData(Stream stream)
{
    var formatter = new BinaryFormatter();
    return formatter.Deserialize(stream); // Remote code execution!
}

// SECURE: Use JSON with type restrictions
public T DeserializeData<T>(string json)
{
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    return JsonSerializer.Deserialize<T>(json, options);
}

// For complex scenarios, validate type before deserialization
public object DeserializeData(string json, Type expectedType)
{
    if (!IsAllowedType(expectedType))
        throw new SecurityException("Type not allowed");

    return JsonSerializer.Deserialize(json, expectedType);
}
```

---

### 5. Cross-Site Scripting (XSS)

**Note:** Razor/Blazor auto-escapes output by default.

**Only flag XSS if:**
- Using `Html.Raw()` or `@Html.Raw()` with user input
- Using `MarkupString` with unsanitized input in Blazor
- Directly writing to Response stream without encoding

```csharp
// VULNERABILITY: XSS with Html.Raw
<div>
    @Html.Raw(Model.UserComment) // Executes user's JavaScript!
</div>

// SECURE: Let Razor escape automatically
<div>
    @Model.UserComment // Automatically HTML-encoded
</div>

// VULNERABILITY: Blazor MarkupString
private MarkupString GetContent()
{
    return new MarkupString(userInput); // XSS!
}

// SECURE: Use sanitizer library or avoid MarkupString
private string GetContent()
{
    return userInput; // Automatically escaped
}
```

---

### 6. Data Exposure

#### Sensitive Data Logging

Flag logging of sensitive information:
- Passwords, tokens, API keys in log statements
- Credit card numbers, SSNs, or PII
- Stack traces with secrets in production

```csharp
// VULNERABILITY: Logging password
_logger.LogInformation("User login: {Username} with password {Password}",
    username, password);

// VULNERABILITY: Logging token
_logger.LogDebug("API request with token: {Token}", apiToken);

// SECURE: Don't log sensitive data
_logger.LogInformation("User login: {Username}", username);
_logger.LogDebug("API request authenticated");
```

#### API Response Leakage

Flag returning full entities with internal/sensitive fields:
- Returning entities with passwords or tokens
- Exposing internal IDs or system information
- Missing DTOs for API responses

```csharp
// VULNERABILITY: Exposing internal fields
[HttpGet("{id}")]
public IActionResult GetUser(int id)
{
    var user = _context.Users.Find(id);
    return Ok(user); // Returns password hash, internal flags, etc.
}

// SECURE: Use DTOs
[HttpGet("{id}")]
public IActionResult GetUser(int id)
{
    var user = _context.Users.Find(id);
    var dto = new UserDto
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email
        // No password, no internal fields
    };
    return Ok(dto);
}
```

---

## Security Review Process

### Step 1: Identify Changes

Focus on new or modified code in the PR:
- Identify where user input enters the system
- Trace data flow to sensitive operations (database, file system, auth)
- Look for new endpoints, file operations, or database queries

### Step 2: Assess Exploitability

For each potential issue, answer:
1. **Can an attacker control the input?** (query params, request body, headers)
2. **Is there a clear exploitation path?** (specific steps to exploit)
3. **What is the realistic impact?** (RCE, data breach, privilege escalation)
4. **Are there existing mitigations?** (framework protections, validation)

### Step 3: Confidence Scoring

Only report issues with confidence >80%:

- **90-100%**: Obvious vulnerability with proof-of-concept possible
- **80-90%**: Clear vulnerability pattern, standard exploitation method
- **70-80%**: Suspicious pattern, requires specific conditions
- **<70%**: Don't report (too speculative)

### Step 4: Verify Against Exclusions

Check the exclusions list below - don't report excluded issue types.

---

## What NOT to Report

### Automatic Exclusions

**1. Test Files & Test Code**
- Security issues in `**/Tests/**/*.cs` are not exploitable in production
- Test databases and mock data are not production concerns

**2. Denial of Service (DOS)**
- Resource exhaustion or memory consumption issues
- Regex DOS or algorithmic complexity attacks
- Rate limiting concerns

**3. Non-Exploitable Patterns**
- Secrets stored on disk if encrypted/protected by OS permissions
- Environment variables or CLI flags (trusted in secure environments)
- UUIDs/GUIDs (assumed cryptographically random and unguessable)
- Client-side validation (server-side is enforced separately)

**4. Low-Impact Issues**
- Missing audit logs or monitoring
- Log spoofing (unsanitized input in logs without injection)
- Lack of security hardening measures
- Theoretical race conditions without demonstrated exploit

**5. Framework-Protected**
- XSS in Razor views (auto-escaped unless using `Html.Raw`)
- CSRF in ASP.NET Core (has built-in anti-forgery protection)
- SQL injection in EF Core LINQ queries (always parameterized)

### Conditional Exclusions

**Only report if exploitation is concrete:**
- Command injection in build scripts (rarely have untrusted input)
- GitHub Actions workflow vulnerabilities (need specific attack path)
- Logging of non-PII/non-secret data (even if somewhat sensitive)

---

## Signal Quality Check

Before reporting a finding, verify:

- [ ] Concrete exploitation path identified (not theoretical)
- [ ] Real security risk with significant impact
- [ ] Specific code location with line numbers
- [ ] Actionable fix recommendation with code example
- [ ] Confidence score 8+ out of 10

---

## Security Finding Report Format

Use this structure for each vulnerability:

```
### [Severity] Category: Brief Description (File:Line)

**Confidence**: [8-10]/10

**Description**:
Clear explanation of the vulnerability and what code pattern causes it.

**Exploitation Scenario**:
Specific steps an attacker would take to exploit this vulnerability and what they gain access to.

**Recommendation**:
Concrete fix with code example.

```csharp
// Vulnerable code
[show current vulnerable code]

// Secure fix
[show corrected code with proper validation/escaping/parameterization]
```
```

### Severity Guidelines

**CRITICAL**
- Remote Code Execution (RCE)
- Authentication bypass allowing full system access
- Direct database access bypass
- Hardcoded admin credentials in production code

**HIGH**
- SQL Injection with clear exploitation path
- Path traversal allowing access to sensitive files
- Privilege escalation allowing unauthorized actions
- Unsafe deserialization with RCE potential
- XSS leading to session hijacking or data theft
- Hardcoded API keys or passwords

**MEDIUM**
- Input validation issues requiring specific conditions
- Weak cryptography in use (MD5, DES)
- Information disclosure with limited scope
- Missing authorization checks on non-critical operations

**LOW** (generally don't report unless obvious)
- Defense-in-depth recommendations
- Minor information leakage without clear impact

---

## Example Security Finding

```
### [HIGH] SQL Injection in User Search (Controllers/UserController.cs:127)

**Confidence**: 9/10

**Description**:
The search functionality concatenates user input directly into a SQL query using string interpolation, allowing SQL injection attacks. The `searchTerm` parameter from the HTTP request is embedded without parameterization on line 127.

**Exploitation Scenario**:
An attacker sends a crafted search request:
```
GET /api/users/search?term='; DROP TABLE Users; --
```

This would execute arbitrary SQL commands, potentially:
- Exfiltrating all user data including passwords
- Modifying or deleting database records
- Elevating privileges by modifying user roles
- Complete database compromise

**Recommendation**:
Use parameterized queries or Entity Framework LINQ:

```csharp
// VULNERABLE (Line 127)
var sql = $"SELECT * FROM Users WHERE Name LIKE '%{searchTerm}%'";
var results = _context.Users.FromSqlRaw(sql).ToList();

// SECURE FIX - Option 1: Parameterized query
var sql = "SELECT * FROM Users WHERE Name LIKE {0}";
var results = _context.Users.FromSqlRaw(sql, $"%{searchTerm}%").ToList();

// SECURE FIX - Option 2: LINQ (preferred)
var results = _context.Users
    .Where(u => EF.Functions.Like(u.Name, $"%{searchTerm}%"))
    .ToList();
```
```

---

## C#/.NET 8.0 Specific Security Patterns

### ASP.NET Core Security Features

**Built-in Protections (don't flag as missing):**
- CSRF tokens (automatic with `[ValidateAntiForgeryToken]` or middleware)
- Request size limits (configurable in middleware)
- Model binding validation (automatic)
- HTTPS redirection in middleware

**Configuration to Verify:**
- `app.UseHsts()` configured in production
- `app.UseHttpsRedirection()` is present
- Secure cookie settings: `HttpOnly = true`, `Secure = true`, `SameSite = SameSiteMode.Strict`

```csharp
// Verify secure cookie configuration
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });
```

### Entity Framework Core Security

**Safe Patterns (don't flag):**
```csharp
// SAFE: LINQ queries are always parameterized
var user = _context.Users.FirstOrDefault(u => u.Username == username);

// SAFE: Parameterized FromSqlRaw
var users = _context.Users.FromSqlRaw(
    "SELECT * FROM Users WHERE Age > {0}",
    minAge
).ToList();
```

**Vulnerable Patterns (flag these):**
```csharp
// VULNERABILITY: String interpolation in SQL
var users = _context.Users.FromSqlRaw(
    $"SELECT * FROM Users WHERE Name = '{name}'"
).ToList();

// VULNERABILITY: String concatenation
var sql = "SELECT * FROM Users WHERE Id = " + userId;
var user = _context.Users.FromSqlRaw(sql).FirstOrDefault();
```

### Common .NET Crypto APIs

**Insecure (flag these):**
- `MD5.Create()` or `SHA1.Create()` for password hashing
- `DESCryptoServiceProvider`, `RC2CryptoServiceProvider`
- `new Random()` for security-sensitive random numbers

**Secure (recommend these):**
- `PasswordHasher<TUser>` from ASP.NET Core Identity
- `Aes.Create()` for symmetric encryption
- `RandomNumberGenerator.Fill()` for cryptographic random numbers
- `HMACSHA256` or `HMACSHA512` for message authentication

```csharp
// INSECURE: Weak hashing
var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(password));

// SECURE: Proper password hashing
var hasher = new PasswordHasher<User>();
var hash = hasher.HashPassword(user, password);

// INSECURE: Non-cryptographic random
var random = new Random();
var token = random.Next();

// SECURE: Cryptographically secure random
var tokenBytes = new byte[32];
RandomNumberGenerator.Fill(tokenBytes);
var token = Convert.ToBase64String(tokenBytes);
```

### Configuration & Secrets Management

**Flag:**
- Connection strings with passwords in code or committed `appsettings.json`
- Hardcoded credentials anywhere in source code
- Secrets in configuration files in source control

**Recommend:**
- User Secrets for development: `dotnet user-secrets set "ApiKey" "value"`
- Azure Key Vault or environment variables for production
- `IConfiguration` injection, never hardcoded values

```csharp
// VULNERABILITY: Hardcoded in appsettings.json (checked into git)
{
  "ConnectionStrings": {
    "Default": "Server=prod;Database=db;User=admin;Password=P@ssw0rd123"
  }
}

// SECURE: Use environment variable or Key Vault
{
  "ConnectionStrings": {
    "Default": "" // Set via environment variable or Key Vault
  }
}

// In code, inject IConfiguration
public class DataService
{
    private readonly string _connectionString;

    public DataService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string not configured");
    }
}
```

---

## Summary

Focus security reviews on:
- **HIGH and CRITICAL findings only**
- **Concrete exploitation paths** with >80% confidence
- **Clear, actionable recommendations** with code examples
- **Real security impact**, not theoretical concerns

Skip:
- Test code vulnerabilities
- DOS/resource exhaustion
- Theoretical issues without clear exploitation
- Framework-protected patterns (CSRF, basic XSS in Razor)

Provide detailed reports with exploitation scenarios and secure code examples for any findings.
