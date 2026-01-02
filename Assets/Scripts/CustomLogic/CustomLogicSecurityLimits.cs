namespace CustomLogic
{
    /// <summary>
    /// Security limits for Custom Logic execution to prevent denial of service attacks.
    /// These limits protect against infinite loops, excessive recursion, memory exhaustion,
    /// and other resource abuse attacks from malicious or poorly written scripts.
    /// </summary>
    public static class CustomLogicSecurityLimits
    {
        /// <summary>
        /// Maximum number of iterations allowed in a single while loop before termination.
        /// Prevents infinite loop attacks that can freeze the game.
        /// Default: 100,000 iterations (roughly 2 seconds at 50 ticks/sec if in OnTick)
        /// </summary>
        public const int MaxLoopIterations = 100000;

        /// <summary>
        /// Maximum recursion depth for method calls.
        /// Prevents stack overflow attacks from excessive recursion.
        /// Default: 100 levels
        /// </summary>
        public const int MaxRecursionDepth = 100;

        /// <summary>
        /// Maximum size in characters for JSON deserialization.
        /// Prevents memory exhaustion from extremely large JSON payloads.
        /// Default: 1 MB (1,000,000 characters)
        /// </summary>
        public const int MaxJsonSize = 1000000;

        /// <summary>
        /// Maximum nesting depth for JSON objects/arrays.
        /// Prevents stack overflow from deeply nested JSON structures.
        /// Default: 100 levels
        /// </summary>
        public const int MaxJsonDepth = 100;

        /// <summary>
        /// Maximum number of items in a collection for for-in loops.
        /// Prevents performance degradation from iterating over massive collections.
        /// Default: 100,000 items
        /// </summary>
        public const int MaxCollectionSize = 100000;

        /// <summary>
        /// Maximum number of network messages per player per time window.
        /// Prevents network flooding attacks.
        /// Default: 100 messages
        /// </summary>
        public const int MaxNetworkMessagesPerWindow = 100;

        /// <summary>
        /// Time window in seconds for network message rate limiting.
        /// Default: 10 seconds
        /// </summary>
        public const float NetworkMessageWindowSeconds = 10f;

        /// <summary>
        /// Maximum number of file operations per time window.
        /// Prevents file system abuse through excessive read/write operations.
        /// Default: 10 operations
        /// </summary>
        public const int MaxFileOperationsPerWindow = 10;

        /// <summary>
        /// Time window in seconds for file operation rate limiting.
        /// Default: 1 second
        /// </summary>
        public const float FileOperationWindowSeconds = 1f;

        /// <summary>
        /// Maximum total storage in bytes across all persistent data files.
        /// Prevents disk space exhaustion.
        /// Default: 10 MB
        /// </summary>
        public const long MaxTotalStorageBytes = 10 * 1024 * 1024;

        /// <summary>
        /// Maximum number of persistent data files to keep.
        /// Prevents inode exhaustion and directory bloat.
        /// Default: 100 files
        /// </summary>
        public const int MaxPersistentDataFiles = 100;
    }
}
