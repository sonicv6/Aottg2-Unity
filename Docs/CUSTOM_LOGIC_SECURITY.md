# Custom Logic Security Features

## Overview

The Custom Logic system has been hardened against various security threats including denial of service (DoS) attacks, resource exhaustion, and file system abuse. This document describes the security measures implemented and their configuration.

## Security Vulnerabilities Fixed

### 1. Infinite Loop Protection

**Vulnerability**: User scripts could contain infinite loops that freeze the game indefinitely.

**Impact**: Game becomes unresponsive, affecting all players in multiplayer sessions.

**Solution**: Added iteration counter to all while loops that terminates execution after `MaxLoopIterations` (default: 100,000) iterations.

**Configuration**: `CustomLogicSecurityLimits.MaxLoopIterations`

**Example Protected Code**:
```csharp
while (condition)
{
    loopIterations++;
    if (loopIterations > MaxLoopIterations)
    {
        LogError("Maximum loop iterations exceeded");
        break;
    }
    // ... loop body
}
```

### 2. Recursion Depth Limit

**Vulnerability**: Excessive recursion could cause stack overflow and crash the application.

**Impact**: Application crash affecting the entire game session.

**Solution**: Added recursion depth tracking that prevents method calls beyond `MaxRecursionDepth` (default: 100) levels.

**Configuration**: `CustomLogicSecurityLimits.MaxRecursionDepth`

**Example Protected Code**:
```csharp
_recursionDepth++;
if (_recursionDepth > MaxRecursionDepth)
{
    LogError("Maximum recursion depth exceeded");
    return null;
}
try
{
    // ... method execution
}
finally
{
    _recursionDepth--;
}
```

### 3. JSON Deserialization Protection

**Vulnerability**: Large or deeply nested JSON could cause memory exhaustion or stack overflow.

**Impact**: Out of memory errors or stack overflow crashes.

**Solution**: 
- Added size limit on JSON strings (`MaxJsonSize`: 1 MB)
- Added depth limit on JSON nesting (`MaxJsonDepth`: 100 levels)

**Configuration**: 
- `CustomLogicSecurityLimits.MaxJsonSize`
- `CustomLogicSecurityLimits.MaxJsonDepth`

**Example Protected Code**:
```csharp
if (json.Length > MaxJsonSize)
{
    throw new Exception("JSON string size exceeds maximum allowed");
}

protected static object LoadJSON(JSONNode json, int depth)
{
    if (depth > MaxJsonDepth)
    {
        throw new Exception("JSON nesting depth exceeds maximum allowed");
    }
    // ... recursive processing with depth tracking
}
```

### 4. Collection Size Limits

**Vulnerability**: For-in loops over massive collections could cause severe performance degradation.

**Impact**: Game freezes or becomes unresponsive during loop execution.

**Solution**: Added collection size check before for-in loop execution (`MaxCollectionSize`: 100,000 items).

**Configuration**: `CustomLogicSecurityLimits.MaxCollectionSize`

**Example Protected Code**:
```csharp
var iterable = EvaluateExpression(...).List;
if (iterable.Count > MaxCollectionSize)
{
    LogError("Collection size exceeds maximum allowed");
    return;
}
foreach (object variable in iterable)
{
    // ... loop body
}
```

### 5. Network Message Rate Limiting

**Vulnerability**: Scripts could flood the network with messages, causing lag or disconnections.

**Impact**: Network congestion, increased latency, potential disconnections.

**Solution**: Added per-player rate limiting for network messages (default: 100 messages per 10 seconds).

**Configuration**: 
- `CustomLogicSecurityLimits.MaxNetworkMessagesPerWindow`
- `CustomLogicSecurityLimits.NetworkMessageWindowSeconds`

**Example Protected Code**:
```csharp
private static void CheckNetworkMessageRateLimit()
{
    // Track messages per player with sliding time window
    if (messageTimes.Count >= MaxNetworkMessagesPerWindow)
    {
        throw new Exception("Network message rate limit exceeded");
    }
    messageTimes.Enqueue(currentTime);
}
```

### 6. File Operation Rate Limiting

**Vulnerability**: Excessive file operations could degrade system performance or fill disk space.

**Impact**: Disk I/O bottleneck, potential disk space exhaustion.

**Solution**: 
- Added rate limiting (10 operations per second)
- Added total storage size limit (10 MB)
- Limited number of persistent data files (100 files)

**Configuration**: 
- `CustomLogicSecurityLimits.MaxFileOperationsPerWindow`
- `CustomLogicSecurityLimits.FileOperationWindowSeconds`
- `CustomLogicSecurityLimits.MaxTotalStorageBytes`
- `CustomLogicSecurityLimits.MaxPersistentDataFiles`

**Example Protected Code**:
```csharp
private static void CheckFileOperationRateLimit()
{
    if (_fileOperationTimes.Count >= MaxFileOperationsPerWindow)
    {
        throw new Exception("File operation rate limit exceeded");
    }
}

private static void CheckTotalStorageSize(string directory, long newFileSize)
{
    if (totalSize + newFileSize > MaxTotalStorageBytes)
    {
        throw new Exception("Total persistent data storage would exceed maximum");
    }
}
```

## Security Configuration

All security limits are defined in `CustomLogicSecurityLimits.cs` as constants:

| Limit | Default Value | Purpose |
|-------|---------------|---------|
| `MaxLoopIterations` | 100,000 | Maximum iterations in a single while loop |
| `MaxRecursionDepth` | 100 | Maximum method call stack depth |
| `MaxJsonSize` | 1,000,000 chars | Maximum JSON string size for deserialization |
| `MaxJsonDepth` | 100 levels | Maximum JSON nesting depth |
| `MaxCollectionSize` | 100,000 items | Maximum items in for-in loop collection |
| `MaxNetworkMessagesPerWindow` | 100 messages | Maximum network messages per time window |
| `NetworkMessageWindowSeconds` | 10 seconds | Time window for network message rate limiting |
| `MaxFileOperationsPerWindow` | 10 operations | Maximum file operations per time window |
| `FileOperationWindowSeconds` | 1 second | Time window for file operation rate limiting |
| `MaxTotalStorageBytes` | 10 MB | Maximum total persistent data storage |
| `MaxPersistentDataFiles` | 100 files | Maximum number of persistent data files |

## Error Handling

All security violations are handled gracefully:

1. **Error Logging**: Security violations are logged with descriptive messages
2. **Execution Termination**: Malicious or runaway code is safely terminated
3. **Error Capture**: In test mode, errors are captured for inspection
4. **User Feedback**: Clear error messages indicate what limit was exceeded

## Testing

Comprehensive security tests are available in `CustomLogicSecurityTests.cs`:

- `TestInfiniteWhileLoopProtection`: Verifies infinite loop detection
- `TestExcessiveRecursionProtection`: Verifies recursion depth limiting
- `TestJsonSizeLimit`: Verifies JSON size validation
- `TestLargeForLoopCollection`: Verifies collection size limiting
- And more...

Run tests with:
```bash
# Run all security tests
dotnet test --filter "FullyQualifiedName~CustomLogicSecurityTests"
```

## Best Practices for Custom Logic Authors

1. **Avoid Infinite Loops**: Always ensure loop conditions can eventually become false
2. **Limit Recursion**: Use iteration instead of recursion where possible
3. **Validate Input Sizes**: Check data sizes before processing
4. **Batch Operations**: Combine multiple network/file operations when possible
5. **Test Extensively**: Test your scripts with edge cases and large inputs

## Performance Impact

The security measures have minimal performance impact:

- Loop counters: Negligible (single integer increment per iteration)
- Recursion tracking: Minimal (stack push/pop operations)
- JSON validation: One-time check at parse time
- Rate limiting: Constant-time queue operations

## Future Enhancements

Potential future security improvements:

1. Configurable limits per game mode
2. Dynamic limit adjustment based on system resources
3. Detailed security audit logging
4. Admin interface for monitoring resource usage
5. Automatic script complexity analysis before execution

## Security Contact

For security concerns or to report vulnerabilities, please contact the development team.

## Version History

- **v1.0** (2026-01-02): Initial security implementation
  - Added loop iteration limits
  - Added recursion depth tracking
  - Added JSON validation
  - Added rate limiting for network and file operations
  - Added collection size limits
