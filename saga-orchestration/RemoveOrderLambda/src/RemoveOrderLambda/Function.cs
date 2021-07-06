using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using MySql.Data.MySqlClient;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RemoveOrderLambda
{
    public class RemoveOrder
    {
        private static string tableName = "Orders";
        private static AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        
        public string FunctionHandler(OrderDetails orderDetails,ILambdaContext context)
        {
            DeleteOrder(orderDetails,context);
            orderDetails.Status = "ORDER_REMOVED";
            return orderDetails.Status;
        }
        
        private void DeleteOrder(OrderDetails orderDetails, ILambdaContext context)
        {
            // Define item attributes
            var attributes = new Dictionary<string, AttributeValue>
            {
                {"ItemId", new AttributeValue {S = orderDetails.ItemId}},
                {"CustomerId", new AttributeValue {S = orderDetails.CustomerId}},
            };
            

            // Create DeleteItem request
            var request = new DeleteItemRequest()
            {
                TableName = tableName,
                Key = attributes,
                ReturnValues = "ALL_OLD"
            };
 
            var response = client.DeleteItemAsync(request);
           
               
           var attributeList = response.Result.Attributes; // attribute list in the response.
           context.Logger.Log("\nPrinting item after retrieving it ............");
           PrintItem(attributeList, context);
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