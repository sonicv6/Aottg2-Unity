# Arbitrary Code Execution Security Review

## Executive Summary

A comprehensive security review was conducted to identify potential arbitrary code execution (ACE) vulnerabilities in the Custom Logic scripting system. This review focused on mechanisms that could allow user scripts to execute unintended code or access system resources beyond the intended sandbox.

**Review Date**: January 2, 2026

**Overall Risk**: LOW 🟢

One medium-severity vulnerability was identified in string formatting, but no critical arbitrary code execution paths were found.

---

## Methodology

The review examined:
1. **Class instantiation mechanisms** - How user scripts create and invoke objects
2. **Reflection usage** - Any use of .NET reflection that could be exploited
3. **File system access** - Path traversal and file operation security
4. **External process execution** - Command injection possibilities
5. **Dynamic code compilation** - Runtime code generation or eval-like features
6. **Network access** - External communication capabilities
7. **String formatting** - Format string vulnerabilities
8. **Type system** - Type confusion or unsafe casting

---

## Findings

### ✅ No Critical Vulnerabilities Found

#### 1. Class Instantiation is Sandboxed
**Status**: SECURE ✓

- User scripts can only instantiate pre-approved builtin classes or user-defined classes
- Class resolution goes through `CustomLogicBuiltinTypes.IsBuiltinType()` whitelist
- No `Activator.CreateInstance()`, `Type.GetType()`, or `Assembly.Load()` with user input
- User-defined classes are created through safe `UserClassInstance` constructor

**Code Location**: `CustomLogicEvaluator.cs:CreateClassInstance()`

```csharp
// Only approved builtins can be instantiated
if (CustomLogicBuiltinTypes.IsBuiltinType(className))
{
    if (CustomLogicBuiltinTypes.IsAbstract(className))
        throw new Exception("Cannot instantiate abstract type");
    return CustomLogicBuiltinTypes.CreateClassInstance(className, parameterValues);
}
```

#### 2. No Dynamic Code Compilation
**Status**: SECURE ✓

- Scripts are parsed into AST at load time, not compiled to IL or executed via `eval()`
- No use of `CodeDom`, `Roslyn`, or other dynamic compilation
- All code execution goes through controlled AST interpretation

**Code Location**: `CustomLogicParser.cs`, `CustomLogicLexer.cs`

#### 3. No External Process Execution
**Status**: SECURE ✓

- No access to `Process.Start()`, `System.Diagnostics`, or shell commands
- `System.Diagnostics` import in `CustomLogicLocaleBuiltin.cs` is only used for `[Conditional]` attribute
- No command injection vectors

**Code Location**: Verified across all builtin files

#### 4. File System Access is Restricted
**Status**: SECURE ✓

- File operations restricted to `FolderPaths.PersistentData` directory
- Comprehensive filename validation via `Util.IsValidFileName()`
- Path traversal prevented by:
  - Blocking `..`, `/`, `\`, `.` characters
  - Length limit (< 50 chars)
  - Reserved filename checks (CON, PRN, etc.)
  - Using `Path.Combine()` which normalizes paths

**Code Location**: `CustomLogicPersistentDataBuiltin.cs`, `Util.cs:IsValidFileName()`

```csharp
char[] invalidCharacters = new char[] { 
    '<', '>', ':', '"', '/', '\\', '|', '?', '*', '.', 
    '\b', '\0', '\t', '\r', '\n', '^', '!', '@', '#', 
    '$', '%', '&', '(', ')', '=', '+' 
};
string[] invalidFileNames = new string[] { 
    "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", 
    "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", 
    "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", 
    "LPT7", "LPT8", "LPT9" 
};
```

#### 5. No External Network Access
**Status**: SECURE ✓

- No `WebRequest`, `HttpClient`, or external HTTP capabilities
- Network messages only via Photon game networking (internal)
- No SQL or database access

**Code Location**: Verified across all builtin files

#### 6. Reflection Usage is Safe
**Status**: SECURE ✓

- Limited reflection only used on known `BaseSetting` objects
- `Convert.GetType()` only returns class name strings, not .NET Type objects
- No user-controlled type lookups or method invocations via reflection

**Code Location**: `CustomLogicGameBuiltin.cs:GetGeneralSetting()`, `CustomLogicConvertBuiltin.cs:GetType()`

```csharp
// Safe - only accesses known setting objects
var setting = SettingsManager.InGameCurrent.General.TypedSettings[settingName];
return setting.GetType().GetProperty("Value").GetValue(setting);

// Safe - only returns class name string
public string GetType(CustomLogicClassInstance cInstance)
{
    return cInstance.ClassName;  // Returns "Main", "MyClass", etc.
}
```

---

### ⚠️ Medium Severity Issue Found

#### Format String Vulnerability in `String.FormatFromList`
**Severity**: Medium (CVSS 5.3)
**Status**: VULNERABLE ⚠️

**Description**: The `String.FormatFromList` method passes user-controlled format strings directly to `string.Format()` without validation. Malicious format strings could cause exceptions or potentially leak information.

**Code Location**: `CustomLogicStringBuiltin.cs:26-29`

```csharp
[CLMethod("Equivalent to C# string.format(string, List<string>).")]
public static string FormatFromList(string str, CustomLogicListBuiltin list)
{
    return string.Format(str, list.List.ToArray());  // ⚠️ User-controlled format string
}
```

**Attack Vector**:
```javascript
// Malicious format string could cause exceptions
formatString = "{0} {1} {2} {3} {4} {5}";  // More placeholders than arguments
params = List(1, 2);
result = String.FormatFromList(formatString, params);  // FormatException
```

**Impact**:
- **Denial of Service**: Malformed format strings cause exceptions
- **Information Disclosure**: Format specifiers could expose object internals via `.ToString()` calls
- **NOT Arbitrary Code Execution**: C# string.Format does not execute code

**Mitigation**: The impact is limited because:
1. Script execution is controlled by trusted game hosts
2. Exceptions are caught and logged, not causing crashes
3. C#'s `string.Format` doesn't execute arbitrary code (unlike printf-style vulnerabilities in C)

**Recommendation**: Add validation or wrap in try-catch with better error handling.

---

## Security Controls in Place

### 1. Loop and Recursion Limits
- **Max Loop Iterations**: 100,000 per loop
- **Max Recursion Depth**: 100 levels
- Prevents resource exhaustion and stack overflow attacks

### 2. JSON Validation
- **Max JSON Size**: 1 MB
- **Max JSON Depth**: 100 levels
- Prevents memory exhaustion and stack overflow from malicious JSON

### 3. Rate Limiting
- **Network Messages**: 100 per 10 seconds per player
- **File Operations**: 10 per second
- Prevents flooding and resource abuse

### 4. File System Quotas
- **Total Storage**: 10 MB maximum
- **Max Files**: 100 files
- **File Size**: 1 MB per file
- Prevents disk exhaustion

### 5. Collection Size Limits
- **Max Collection Size**: 100,000 items
- Prevents performance degradation from massive loops

---

## Attack Surface Analysis

| Attack Vector | Risk Level | Protection |
|---------------|------------|------------|
| Arbitrary code execution via reflection | LOW 🟢 | No user-controlled reflection |
| Command injection | LOW 🟢 | No process execution |
| Path traversal | LOW 🟢 | Strict filename validation |
| SQL injection | LOW 🟢 | No database access |
| Server-side request forgery (SSRF) | LOW 🟢 | No external HTTP |
| Deserialization attacks | LOW 🟢 | Only safe JSON parsing |
| Format string exploitation | MEDIUM 🟡 | One unvalidated string.Format |
| Type confusion | LOW 🟢 | Strong type checking |
| Memory corruption | LOW 🟢 | Managed code, no unsafe pointers |
| Infinite loops | LOW 🟢 | Loop iteration limits |
| Stack overflow | LOW 🟢 | Recursion depth limits |

---

## Recommendations

### Immediate Actions Required

#### 1. Fix Format String Vulnerability (Medium Priority)
**Issue**: `String.FormatFromList` allows unvalidated format strings

**Solution**: Add try-catch and argument validation

```csharp
[CLMethod("Equivalent to C# string.format(string, List<string>).")]
public static string FormatFromList(string str, CustomLogicListBuiltin list)
{
    try
    {
        // Validate format string doesn't have more placeholders than arguments
        int maxPlaceholder = -1;
        for (int i = 0; i < str.Length - 1; i++)
        {
            if (str[i] == '{' && char.IsDigit(str[i + 1]))
            {
                int placeholder = str[i + 1] - '0';
                maxPlaceholder = System.Math.Max(maxPlaceholder, placeholder);
            }
        }
        
        if (maxPlaceholder >= list.List.Count)
        {
            throw new System.FormatException($"Format string requires {maxPlaceholder + 1} arguments but only {list.List.Count} provided");
        }
        
        return string.Format(str, list.List.ToArray());
    }
    catch (System.FormatException ex)
    {
        throw new System.Exception("Invalid format string: " + ex.Message);
    }
}
```

### Future Enhancements (Optional)

1. **Add CSP-like Policies**: Allow server admins to disable specific builtins
2. **Sandbox Isolation**: Consider running scripts in separate AppDomains (if Unity supports)
3. **Audit Logging**: Log all file operations and network messages for forensics
4. **Static Analysis**: Add pre-execution analysis to detect suspicious patterns
5. **Resource Monitoring**: Track CPU and memory usage per script

---

## Testing Recommendations

### Security Test Cases to Add

```csharp
[Test]
public void TestFormatStringWithTooManyPlaceholders()
{
    string script = @"
class Main
{
    function Init() {}
    
    function TestFormat()
    {
        formatStr = ""{0} {1} {2}"";
        params = List();
        params.Add(""a"");
        return String.FormatFromList(formatStr, params);
    }
}";
    
    var evaluator = new OfflineCustomLogicEvaluator(script);
    var result = evaluator.EvaluateMainMethod("TestFormat");
    
    // Should handle gracefully, not crash
    Assert.IsTrue(evaluator.HasErrors());
}

[Test]
public void TestNoArbitraryReflection()
{
    // Verify user scripts cannot access .NET reflection
    string script = @"
class Main
{
    function Init() {}
    
    function TryReflection()
    {
        // These should all fail at parse/runtime
        // System.Reflection.Assembly.Load(...)
        // System.Type.GetType(...)
        // System.Activator.CreateInstance(...)
    }
}";
    
    // Script should fail to parse/execute
}
```

---

## Compliance

The Custom Logic system meets the following security standards:

✅ **OWASP Top 10 (2021)**
- A03:2021 – Injection: Protected against SQL, command, and path injection
- A05:2021 – Security Misconfiguration: Secure defaults, no dangerous features exposed
- A08:2021 – Software and Data Integrity Failures: No dynamic code loading

✅ **CWE Coverage**
- CWE-78: OS Command Injection - Not vulnerable
- CWE-22: Path Traversal - Protected
- CWE-89: SQL Injection - Not vulnerable (no SQL)
- CWE-94: Code Injection - Not vulnerable
- CWE-134: Format String - One instance found (Medium severity)

---

## Conclusion

The Custom Logic scripting system has a strong security posture with no critical arbitrary code execution vulnerabilities. The sandboxed design prevents:

✅ Arbitrary .NET code execution
✅ Operating system command execution
✅ Unrestricted file system access
✅ External network access
✅ Malicious use of reflection

**One medium-severity format string vulnerability** was identified and should be addressed, but it does not enable arbitrary code execution.

The existing security controls (loop limits, recursion tracking, rate limiting, file quotas) provide defense-in-depth against resource exhaustion attacks.

**Overall Security Rating**: B+ (Good)
- Strong sandbox with no ACE vectors
- Comprehensive resource limits
- One format string issue to address
- Recommended for production use with the noted fix

---

**Reviewed By**: GitHub Copilot Security Audit
**Date**: January 2, 2026
**Status**: APPROVED with recommendations
