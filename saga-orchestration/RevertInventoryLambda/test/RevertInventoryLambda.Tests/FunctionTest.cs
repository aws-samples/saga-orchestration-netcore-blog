using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using RevertInventoryLambda;

namespace RevertInventoryLambda.Tests
{
    public class RevertInventoryLambdaShould
    {
        [Fact]
        public void RevertInventory()
        {
            var function = new RevertInventory();
            var context = new TestLambdaContext();
            var orderDetails = new OrderDetails {ItemId = "ABC/001", FailAtStage = "RevertInventory", Status = "ERROR"};
            var outputObj = function.FunctionHandler(orderDetails, context);

            Assert.Equal("INVENTORY_REVERTED", outputObj.Status);
        }
    }
}