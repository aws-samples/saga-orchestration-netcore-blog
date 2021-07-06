using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using MakePaymentLambda;

namespace MakePaymentLambda.Tests
{
    public class FunctionTest
    {
        [Fact]
        public void TestSuccessScenario()
        {
            var function = new MakePayment();
            var context = new TestLambdaContext();
            var inputObj = new OrderDetails {ItemId = "ABC/001", FailAtStage = "None"};
            var retValue = function.FunctionHandler(inputObj,context);

            Assert.Equal("PAYMENT_COMPLETED", retValue.Status);
        }
        
         
        [Fact]
        public void TestFailureScenario()
        {
            var function = new MakePayment();
            var context = new TestLambdaContext();
            var inputObj = new OrderDetails {ItemId = "ABC/001", CustomerId  = "SGP/010", FailAtStage = "MakePayment"};
            
            var retValue = function.FunctionHandler(inputObj,context);
            Assert.Equal("ERROR", retValue.Status);
        }
    }
}