using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SendNotificationLambda
{
    public class SendNotification
    {
        public SendNotificationResult FunctionHandler(ILambdaContext context)
        {
            context.Logger.Log("In SendNotificationLambda");
            var result = new SendNotificationResult {Status = "SUCCESS"};
            context.Logger.Log("After setting result to success");
            return result;
        }

        public class SendNotificationResult
        {
            public string Status { get; set; }
        }
    }
}