using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MakePaymentLambda
{
    public class MakePayment
    {
        private StageNameEnum CurrentStage = StageNameEnum.MakePayment;
        public OrderDetails FunctionHandler(OrderDetails orderDetails, ILambdaContext context)
        {
            var status = "PAYMENT_COMPLETED";
            Enum.TryParse(orderDetails.FailAtStage, out StageNameEnum stage);
            if (stage.Equals(CurrentStage))
                status = "ERROR";
            
            orderDetails.Status = status;
            return orderDetails;
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
        None = 4
    }
}