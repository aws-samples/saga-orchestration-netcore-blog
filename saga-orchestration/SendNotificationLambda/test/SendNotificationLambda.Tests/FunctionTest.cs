using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using SendNotificationLambda;

namespace SendNotificationLambda.Tests
{
    public class FunctionTest
    {
        [Fact]
        public void TestToUpperFunction()
        {
            // Invoke the lambda function and confirm the string was upper cased.
            var function = new SendNotification();
            var context = new TestLambdaContext();
            var result = function.FunctionHandler(context);

            Assert.Equal("SUCCESS", result.Status);
        }
    }
}