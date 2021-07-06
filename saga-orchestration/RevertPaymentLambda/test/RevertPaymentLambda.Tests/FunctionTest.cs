using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using RevertPaymentLambda;

namespace RevertPaymentLambda.Tests
{
    public class RevertPaymentShould
    {
        [Fact]
        public void ReturnRevertedOnSuccess()
        {
            // Invoke the lambda function and confirm the string was upper cased.
            var function = new RevertPayment();
            var context = new TestLambdaContext();
            var inputObj = new OrderDetails {ItemId = "ABC/001", CustomerId  = "SGP/010", FailAtStage = "None"};
            
            var retValue = function.FunctionHandler(inputObj,context);
            Assert.Equal("PAYMENT_REVERTED", retValue.Status);
        }
    }
}