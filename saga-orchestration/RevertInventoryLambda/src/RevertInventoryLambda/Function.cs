using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RevertInventoryLambda
{
    public class RevertInventory
    {
        private static string tableName = "Inventory";
        private static AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        public OrderDetails FunctionHandler(OrderDetails orderDetails, ILambdaContext context)
        {
            try
            {
                UpdateStock(orderDetails.ItemId, context);
                orderDetails.Status = "INVENTORY_REVERTED";
            }
            catch (Exception ex)
            {
                orderDetails.Status = "ERROR";
                context.Logger.Log("Error while updating inventory");
                context.Logger.Log(ex.Message);
                context.Logger.Log(ex.StackTrace);
            }

            return orderDetails;
        }
        
        private void UpdateStock(string ItemId, ILambdaContext context)
        {
            var updateRequest = new UpdateItemRequest()
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "ItemId", new AttributeValue { S =ItemId } }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":inc", new AttributeValue { N = "1" } }
                },
                UpdateExpression = "SET ItemsInStock = ItemsInStock + :inc",
                ReturnValues = "ALL_NEW"
            };
            var response = client.UpdateItemAsync(updateRequest);

            // Check the response.
            var attributeList = response.Result.Attributes; // attribute list in the response.
            // print attributeList.
            Console.WriteLine("\nPrinting item after incrementing stock  ............");
            PrintItem(attributeList,context);
        }
        
        private void PrintItem(Dictionary<string, AttributeValue> attributeList,ILambdaContext context)
        {
            foreach (KeyValuePair<string, AttributeValue> kvp in attributeList)
            {
                string attributeName = kvp.Key;
                AttributeValue value = kvp.Value;

                context.Logger.Log(
                    attributeName + " " +
                    (value.S == null ? "" : value.S + " ") +
                    (value.N == null ? "" : value.N + " ") +
                    (value.SS == null ? "" : string.Join(",", value.SS.ToArray()) + " ") +
                    (value.NS == null ? "" : string.Join(",", value.NS.ToArray()) + " ")
                );
            }
        }
    }
    
    public class OrderDetails
    {
        public string ItemId { get; set; }
        public string CustomerId { get; set; }
        public string MessageId { get; set; }
        public string FailAtStage { get; set; }
        public string Status { get; set; }
    }
}