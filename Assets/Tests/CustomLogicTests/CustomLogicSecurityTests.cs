using NUnit.Framework;
using CustomLogic;
using CustomLogic.OfflineEvaluator;
using System;

namespace Tests.CustomLogic
{
    /// <summary>
    /// Security tests for Custom Logic to verify protection against DoS attacks,
    /// stack overflow, memory exhaustion, and resource abuse.
    /// </summary>
    [TestFixture]
    public class CustomLogicSecurityTests
    {
        #region Infinite Loop Protection Tests

        [Test]
        public void TestInfiniteWhileLoopProtection()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function InfiniteLoop()
    {
        x = 0;
        while (true)
        {
            x = x + 1;
        }
        return x;
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("InfiniteLoop");
            
            // Should terminate and return null due to loop limit exceeded
            Assert.IsNull(result);
            Assert.IsTrue(evaluator.HasErrors());
            Assert.IsTrue(evaluator.GetCapturedErrors()[0].Message.Contains("Maximum loop iterations"));
        }

        [Test]
        public void TestWhileLoopWithinLimit()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function SafeLoop()
    {
        x = 0;
        while (x < 100)
        {
            x = x + 1;
        }
        return x;
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("SafeLoop");
            
            Assert.AreEqual(100, result);
            Assert.IsFalse(evaluator.HasErrors());
        }

        [Test]
        public void TestLargeForLoopCollection()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function ProcessLargeList()
    {
        # Create a list with more than MaxCollectionSize items
        largeList = List();
        i = 0;
        while (i < 200000)
        {
            largeList.Add(i);
            i = i + 1;
        }
        
        # Try to iterate - should fail
        sum = 0;
        for (item in largeList)
        {
            sum = sum + item;
        }
        return sum;
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("ProcessLargeList");
            
            // Should fail due to collection size limit
            Assert.IsNull(result);
            Assert.IsTrue(evaluator.HasErrors());
        }

        #endregion

        #region Recursion Depth Protection Tests

        [Test]
        public void TestExcessiveRecursionProtection()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function RecursiveFunction(depth)
    {
        if (depth > 0)
        {
            return self.RecursiveFunction(depth - 1);
        }
        return depth;
    }
    
    function TestRecursion()
    {
        # Try to recurse 200 times (exceeds limit of 100)
        return self.RecursiveFunction(200);
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("TestRecursion");
            
            // Should terminate and return null due to recursion depth exceeded
            Assert.IsNull(result);
            Assert.IsTrue(evaluator.HasErrors());
            Assert.IsTrue(evaluator.GetCapturedErrors()[0].Message.Contains("recursion depth"));
        }

        [Test]
        public void TestRecursionWithinLimit()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function RecursiveFunction(depth)
    {
        if (depth > 0)
        {
            return self.RecursiveFunction(depth - 1) + 1;
        }
        return 0;
    }
    
    function TestRecursion()
    {
        # Recurse 50 times (within limit)
        return self.RecursiveFunction(50);
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("TestRecursion");
            
            Assert.AreEqual(50, result);
            Assert.IsFalse(evaluator.HasErrors());
        }

        #endregion

        #region JSON Security Tests

        [Test]
        public void TestJsonSizeLimit()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function TestLargeJson()
    {
        # Create a string larger than 1MB
        largeString = """";
        i = 0;
        while (i < 150000)
        {
            largeString = largeString + ""0123456789"";
            i = i + 1;
        }
        
        # Try to parse it as JSON
        result = Json.LoadFromString(largeString);
        return result;
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("TestLargeJson");
            
            // Should fail due to JSON size limit
            Assert.IsNull(result);
            Assert.IsTrue(evaluator.HasErrors());
        }

        [Test]
        public void TestJsonValidSize()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function TestValidJson()
    {
        jsonString = ""{""key"": ""string:value""}"";
        result = Json.LoadFromString(jsonString);
        return result.Get(""key"");
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("TestValidJson");
            
            Assert.AreEqual("value", result);
            Assert.IsFalse(evaluator.HasErrors());
        }

        [Test]
        public void TestJsonDepthLimit()
        {
            // Note: This test creates deeply nested JSON which would exceed the depth limit
            // In a real implementation, we would generate JSON with 101+ levels of nesting
            // For now, we just verify the limit exists
            Assert.AreEqual(100, CustomLogicSecurityLimits.MaxJsonDepth);
        }

        #endregion

        #region Security Constants Tests

        [Test]
        public void TestSecurityConstantsExist()
        {
            // Verify all security limits are defined and reasonable
            Assert.AreEqual(100000, CustomLogicSecurityLimits.MaxLoopIterations);
            Assert.AreEqual(100, CustomLogicSecurityLimits.MaxRecursionDepth);
            Assert.AreEqual(1000000, CustomLogicSecurityLimits.MaxJsonSize);
            Assert.AreEqual(100, CustomLogicSecurityLimits.MaxJsonDepth);
            Assert.AreEqual(100000, CustomLogicSecurityLimits.MaxCollectionSize);
            Assert.AreEqual(100, CustomLogicSecurityLimits.MaxNetworkMessagesPerWindow);
            Assert.AreEqual(10f, CustomLogicSecurityLimits.NetworkMessageWindowSeconds);
            Assert.AreEqual(10, CustomLogicSecurityLimits.MaxFileOperationsPerWindow);
            Assert.AreEqual(1f, CustomLogicSecurityLimits.FileOperationWindowSeconds);
            Assert.AreEqual(10 * 1024 * 1024, CustomLogicSecurityLimits.MaxTotalStorageBytes);
            Assert.AreEqual(100, CustomLogicSecurityLimits.MaxPersistentDataFiles);
        }

        #endregion

        #region Nested Loop Tests

        [Test]
        public void TestNestedLoopsWithinLimit()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function NestedLoops()
    {
        sum = 0;
        i = 0;
        while (i < 100)
        {
            j = 0;
            while (j < 100)
            {
                sum = sum + 1;
                j = j + 1;
            }
            i = i + 1;
        }
        return sum;
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("NestedLoops");
            
            Assert.AreEqual(10000, result);
            Assert.IsFalse(evaluator.HasErrors());
        }

        [Test]
        public void TestNestedLoopsExceedingLimit()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function ExcessiveNestedLoops()
    {
        sum = 0;
        i = 0;
        # Outer loop 400 times * inner loop 400 times = 160,000 iterations total
        # This exceeds MaxLoopIterations of 100,000
        while (i < 400)
        {
            j = 0;
            while (j < 400)
            {
                sum = sum + 1;
                j = j + 1;
            }
            i = i + 1;
        }
        return sum;
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("ExcessiveNestedLoops");
            
            // Inner loop should hit the limit first
            Assert.IsNull(result);
            Assert.IsTrue(evaluator.HasErrors());
        }

        #endregion

        #region Mixed Attack Scenarios

        [Test]
        public void TestCombinedRecursionAndLoops()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function RecursiveWithLoop(depth)
    {
        if (depth > 0)
        {
            sum = 0;
            i = 0;
            while (i < 10)
            {
                sum = sum + 1;
                i = i + 1;
            }
            return sum + self.RecursiveWithLoop(depth - 1);
        }
        return 0;
    }
    
    function TestMixed()
    {
        # Each recursive call does 10 iterations
        # 50 recursive calls = 500 total iterations (well within limit)
        return self.RecursiveWithLoop(50);
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("TestMixed");
            
            // Should succeed as both recursion and iterations are within limits
            Assert.AreEqual(500, result);
            Assert.IsFalse(evaluator.HasErrors());
        }

        #endregion

        #region Format String Vulnerability Tests

        [Test]
        public void TestFormatStringWithTooManyPlaceholders()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
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
            
            // Should handle gracefully with error, not crash
            Assert.IsNull(result);
            Assert.IsTrue(evaluator.HasErrors());
            Assert.IsTrue(evaluator.GetCapturedErrors()[0].Message.Contains("format string") || 
                         evaluator.GetCapturedErrors()[0].Message.Contains("Format string"));
        }

        [Test]
        public void TestFormatStringValidUsage()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function TestFormat()
    {
        formatStr = ""Hello {0}, you are {1} years old"";
        params = List();
        params.Add(""Alice"");
        params.Add(""25"");
        return String.FormatFromList(formatStr, params);
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("TestFormat");
            
            Assert.AreEqual("Hello Alice, you are 25 years old", result);
            Assert.IsFalse(evaluator.HasErrors());
        }

        [Test]
        public void TestFormatStringEdgeCase()
        {
            string script = @"
class Main
{
    function Init()
    {
    }
    
    function TestFormat()
    {
        formatStr = ""{0}"";
        params = List();
        params.Add(""test"");
        return String.FormatFromList(formatStr, params);
    }
}";

            var evaluator = new OfflineCustomLogicEvaluator(script);
            var result = evaluator.EvaluateMainMethod("TestFormat");
            
            Assert.AreEqual("test", result);
            Assert.IsFalse(evaluator.HasErrors());
        }

        #endregion
    }
}
