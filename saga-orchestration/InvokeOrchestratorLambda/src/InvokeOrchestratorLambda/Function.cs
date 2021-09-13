using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace InvokeOrchestratorLambda
{
    public class InvokeOrchestrator
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler(OrderDetails orderDetails, ILambdaContext context)
        {
            var input = HttpStatusCode.Accepted;
            var amazonStepFunctionsConfig = new AmazonStepFunctionsConfig {RegionEndpoint = RegionEndpoint.USEast1};
            using (var amazonStepFunctionsClient =
                new AmazonStepFunctionsClient(amazonStepFunctionsConfig))
            {
                context.Logger.Log(orderDetails.ItemId);
                
                var jsonInput = JsonSerializer.Serialize(orderDetails);
                context.Logger.Log(jsonInput);

                var functionArn = context.InvokedFunctionArn;
                var splitArn = functionArn.Split(":");
                var accountId = splitArn[4];
                var awsRegion = System.Environment.GetEnvironmentVariable("AWS_REGION");

                var startExecutionRequest = new StartExecutionRequest
                {
                    Input = jsonInput,
                    StateMachineArn = "arn:aws:states:" + awsRegion + ":" + accountId + ":stateMachine:DistributedTransactionOrchestrator"
                };
                context.Logger.Log("before StartExecutionAsync");
                var taskStartExecutionResponse =
                    await amazonStepFunctionsClient.StartExecutionAsync(startExecutionRequest);
                input = taskStartExecutionResponse.HttpStatusCode;
                context.Logger.Log(input.ToString());
            }

            var response = CreateResponse(input);
            return response;
        }
        
        
        private APIGatewayProxyResponse CreateResponse(HttpStatusCode httpStatusCode)
        {
            int statusCode = (int)httpStatusCode;
            
            var response = new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Body = "Processed",
                Headers = new Dictionary<string, string>
                { 
                    { "Content-Type", "application/json" }, 
                    { "Access-Control-Allow-Origin", "*" } 
                }
            };
    
            return response;
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