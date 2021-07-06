using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Newtonsoft.Json;
using PlaceOrderLambda;

namespace PlaceOrderLambda.Tests
{
    public class FunctionTest
    {
        [Fact]
        public void TestSuccessScenario()
        {
            // Invoke the lambda function and confirm the string was upper cased.
            var function = new PlaceOrder();
            var context = new TestLambdaContext();
            var inputObj = new OrderDetails {ItemId = "ABC/001", CustomerId  = "SGP/010", FailAtStage = "None"};
            
            var retValue = function.FunctionHandler(inputObj,context);
            Assert.Equal("ORDER_PLACED", retValue.Status);
        }
        
        
        [Fact]
        public void TestFailureScenario()
        {
            // Invoke the lambda function and confirm the string was upper cased.
            var function = new PlaceOrder();
            var context = new TestLambdaContext();
            var inputObj = new OrderDetails {ItemId = "ABC/001", CustomerId  = "SGP/010", FailAtStage = "PlaceOrder"};
            
            var retValue = function.FunctionHandler(inputObj,context);
            Assert.Equal("ERROR", retValue.Status);
        }
    }
}