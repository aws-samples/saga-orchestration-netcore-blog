using System;
using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PlaceOrderLambda
{
    public class PlaceOrder
    {
        private StageNameEnum CurrentStage = StageNameEnum.PlaceOrder;
        private static string tableName = "Orders";
        private static AmazonDynamoDBClient client = new AmazonDynamoDBClient();

        public OrderDetails FunctionHandler(OrderDetails orderDetails, ILambdaContext context)
        {
            orderDetails.Status = "ORDER_PLACED";
            CreateOrder(orderDetails,context);
            
            
            Enum.TryParse(orderDetails.FailAtStage, out StageNameEnum stage);
            if (stage.Equals(CurrentStage))
                orderDetails.Status = "ERROR";
            
            return orderDetails;
        }
        
        
        private void CreateOrder(OrderDetails orderDetails, ILambdaContext context)
        {
            // Define item attributes
            var attributes = new Dictionary<string, AttributeValue>();
            attributes["ItemId"] = new AttributeValue { S = orderDetails.ItemId };
            attributes["CustomerId"] = new AttributeValue { S = orderDetails.CustomerId };
            attributes["Status"] = new AttributeValue { S = orderDetails.Status };

            // Create PutItem request
            var request = new PutItemRequest
            {
                TableName = tableName,
                Item = attributes
            };
 
            // Issue PutItem request
            var response = client.PutItemAsync(request);

            // Check the response.
            var attributeList = response.Result.Attributes; // attribute list in the response.
           
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

    public enum StageNameEnum
    {
        PlaceOrder = 0,
        MakePayment = 1,
        UpdateInventory = 2,
        SendNotification = 3,
        None
    }
}