using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using RemoveOrderLambda;

namespace RemoveOrderLambda.Tests
{
    public class RemoveOrderLambdaShould
    {
        [Fact]
        public void RemovePlacedOrder()
        {
            var function = new RemoveOrder();
            var context = new TestLambdaContext();
            var orderDetails = new OrderDetails {ItemId = "ABC/001", FailAtStage = "RevertInventory", Status = "ERROR"};
            var outputObj = function.FunctionHandler(orderDetails, context);

            Assert.Equal("ORDER_REMOVED", outputObj);
        }
    }
}