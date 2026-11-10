using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Constructs;

namespace CdkSagaOrchestration
{
    public class CdkSagaOrchestrationStack : Stack
    {
        public CdkSagaOrchestrationStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Define .NET 10 runtime (not yet in CDK enum, but supported by Lambda)
            var dotnet10Runtime = new Runtime("dotnet10");

            #region iamroles
            var iamLambdaRole = new Role(this,"LambdaExecutionRole", new RoleProps
            {
                RoleName = "LambdaExecutionRole",
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com")
            });

            // Lambda execution role with least privilege policies
            iamLambdaRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"));
            iamLambdaRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess"));

            var iamStepFunctionRole = new Role(this,"step_functions_basic_execution", new RoleProps
            {
                RoleName = "step_functions_basic_execution",
                AssumedBy = new ServicePrincipal("states.amazonaws.com")
            });

            // Step Functions role with least privilege
            iamStepFunctionRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess"));
            #endregion iamroles
            
            #region DynamoDB tables

            var inventoryTable = new Table(this, "Inventory", new TableProps
            {
                TableName = "Inventory",
                PartitionKey = new Attribute
                {
                    Name = "ItemId",
                    Type = AttributeType.STRING
                },
                RemovalPolicy = RemovalPolicy.DESTROY,
                Encryption = TableEncryption.AWS_MANAGED,
                PointInTimeRecovery = true
            });

            var ordersTable = new Table(this, "Orders", new TableProps
            {
                TableName = "Orders",
                PartitionKey = new Attribute
                {
                    Name = "ItemId",
                    Type = AttributeType.STRING
                },
                SortKey = new Attribute
                {
                    Name = "CustomerId",
                    Type = AttributeType.STRING
                },
                RemovalPolicy = RemovalPolicy.DESTROY,
                Encryption = TableEncryption.AWS_MANAGED,
                PointInTimeRecovery = true
            });

            // Grant least privilege DynamoDB permissions to Lambda role
            inventoryTable.GrantReadWriteData(iamLambdaRole);
            ordersTable.GrantReadWriteData(iamLambdaRole);

            #endregion
            
            //Define Lambda Functions
            var invokeOrchestratorLambda = new Function(this,"InvokeOrchestratorLambda", new FunctionProps
            {
                FunctionName = "InvokeOrchestratorLambda",
                Runtime = dotnet10Runtime,
                Handler = "InvokeOrchestratorLambda::InvokeOrchestratorLambda.InvokeOrchestrator::FunctionHandler",
                Role = iamLambdaRole,
                Code = Code.FromAsset("lambdas/InvokeOrchestratorLambda.zip"),
                Timeout = Duration.Seconds(300),
                Tracing = Tracing.ACTIVE
            });
            
            #region API Gateway
            var api = new RestApi(this, "SagaOrchestratorAPI", new RestApiProps
            {
                RestApiName = "SagaOrchestratorAPI",
                Description = "This service triggers the saga orchestration workflow."
                
            });

            var invokeOrchestratorIntegration =  new LambdaIntegration(invokeOrchestratorLambda, new LambdaIntegrationOptions
            {
                Proxy = false,
                PassthroughBehavior = PassthroughBehavior.WHEN_NO_TEMPLATES,
                //Integration request
                RequestTemplates = new Dictionary<string, string>
                {
                    ["application/json"] = "#set($inputRoot = $input.path(\'$\')) { \"ItemId\" : \"$inputRoot.ItemId\",  \"CustomerId\" : \"$inputRoot.CustomerId\", \"MessageId\" : \"$inputRoot.MessageId\",\"FailAtStage\" : \"$inputRoot.FailAtStage\"}"
                },
                //Integration response
                IntegrationResponses = new IIntegrationResponse[]
                {
                    new IntegrationResponse
                    {
                        StatusCode = "200",
                        ResponseTemplates = new Dictionary<string, string>
                        {
                            { "application/json", "" } 
                        }
                    }
                }
            });

            var anyMethod = api.Root.AddMethod("ANY", invokeOrchestratorIntegration, new MethodOptions
            {
                //Method response
                MethodResponses = new[]
                {
                    new MethodResponse
                    {
                        StatusCode = "200", ResponseModels = new Dictionary<string, IModel>()
                        {
                            ["application/json"] = Model.EMPTY_MODEL
                        }
                    }
                }

            });
            
            var mockIntegration = new MockIntegration(new IntegrationOptions
            {
                //Integration request
                RequestTemplates = new Dictionary<string, string>
                {
                    ["application/json"] = "{ \"statusCode\": \"200\" }"
                },
                //Integration response
                IntegrationResponses = new IIntegrationResponse[]
                {
                    new IntegrationResponse
                    {
                        StatusCode = "200",
                        ResponseTemplates = new Dictionary<string, string>
                        {
                            { "application/json", "" } 
                        }
                    }
                }
            });
            var mockMethod = api.Root.AddMethod("OPTIONS", mockIntegration, new MethodOptions
            {
                //Method response
                MethodResponses = new[]
                {
                    new MethodResponse
                    {
                        StatusCode = "200", ResponseModels = new Dictionary<string, IModel>()
                        {
                            ["application/json"] = Model.EMPTY_MODEL
                        }
                    }
                }
            });
           
            #endregion

            #region Lambda Functions

             var placeOrderLambda = new Function(this,"PlaceOrderLambda", new FunctionProps
             {
                 FunctionName = "PlaceOrderLambda",
                 Runtime = dotnet10Runtime,
                 Handler = "PlaceOrderLambda::PlaceOrderLambda.PlaceOrder::FunctionHandler",
                 Role = iamLambdaRole,
                 Code = Code.FromAsset("lambdas/PlaceOrderLambda.zip"),
                 Timeout = Duration.Seconds(300)
             });

             var updateInventoryLambda = new Function(this,"UpdateInventoryLambda", new FunctionProps
             {
                 FunctionName = "UpdateInventoryLambda",
                 Runtime = dotnet10Runtime,
                 Handler = "UpdateInventoryLambda::UpdateInventoryLambda.UpdateInventory::FunctionHandler",
                 Role = iamLambdaRole,
                 Code = Code.FromAsset("lambdas/UpdateInventoryLambda.zip"),
                 Timeout = Duration.Seconds(300)
             });

             var makePaymentLambda = new Function(this,"MakePaymentLambda", new FunctionProps
             {
                 FunctionName = "MakePaymentLambda",
                 Runtime = dotnet10Runtime,
                 Handler = "MakePaymentLambda::MakePaymentLambda.MakePayment::FunctionHandler",
                 Role = iamLambdaRole,
                 Code = Code.FromAsset("lambdas/MakePaymentLambda.zip"),
                 Timeout = Duration.Seconds(300)
             });

             var revertPaymentLambda = new Function(this,"RevertPaymentLambda", new FunctionProps
             {
                 FunctionName = "RevertPaymentLambda",
                 Runtime = dotnet10Runtime,
                 Handler = "RevertPaymentLambda::RevertPaymentLambda.RevertPayment::FunctionHandler",
                 Role = iamLambdaRole,
                 Code = Code.FromAsset("lambdas/RevertPaymentLambda.zip"),
                 Timeout = Duration.Seconds(300)
             });

             var revertInventoryLambda = new Function(this,"RevertInventoryLambda", new FunctionProps
             {
                 FunctionName = "RevertInventoryLambda",
                 Runtime = dotnet10Runtime,
                 Handler = "RevertInventoryLambda::RevertInventoryLambda.RevertInventory::FunctionHandler",
                 Role = iamLambdaRole,
                 Code = Code.FromAsset("lambdas/RevertInventoryLambda.zip"),
                 Timeout = Duration.Seconds(300)
             });

             var removeOrderLambda = new Function(this,"RemoveOrderLambda", new FunctionProps
             {
                 FunctionName = "RemoveOrderLambda",
                 Runtime = dotnet10Runtime,
                 Handler = "RemoveOrderLambda::RemoveOrderLambda.RemoveOrder::FunctionHandler",
                 Role = iamLambdaRole,
                 Code = Code.FromAsset("lambdas/RemoveOrderLambda.zip"),
                 Timeout = Duration.Seconds(300)
             });

            #endregion
           
             
             
             #region stepfunction
             
             var successState = new Succeed(this,"SuccessState");
             var failState = new Fail(this, "Fail");

             var placeOrderTask = new LambdaInvoke(this, "Place Order", new LambdaInvokeProps
             {
                 LambdaFunction = placeOrderLambda,
                 Comment = "Place Order",
                 RetryOnServiceExceptions = false,
                 PayloadResponseOnly = true
             });
             
             var updateInventoryTask = new LambdaInvoke(this,"Update Inventory", new LambdaInvokeProps
             {
                 LambdaFunction = updateInventoryLambda,
                 Comment = "Update inventory",
                 RetryOnServiceExceptions = false,
                 PayloadResponseOnly = true
             });
             
             var makePaymentTask = new LambdaInvoke(this,"Make Payment", new LambdaInvokeProps
             {
                 LambdaFunction = makePaymentLambda,
                 Comment = "Make Payment",
                 RetryOnServiceExceptions = false,
                 PayloadResponseOnly = true
             });
             
             var removeOrderTask = new LambdaInvoke(this, "Remove Order", new LambdaInvokeProps
             {
                 LambdaFunction = removeOrderLambda,
                 Comment = "Remove Order",
                 RetryOnServiceExceptions = false,
                 PayloadResponseOnly = true
             }).Next(failState);
             
             var revertInventoryTask = new LambdaInvoke(this,"Revert Inventory", new LambdaInvokeProps
             {
                 LambdaFunction = revertInventoryLambda,
                 Comment = "Revert inventory",
                 RetryOnServiceExceptions = false,
                 PayloadResponseOnly = true
             }).Next(removeOrderTask);
             
             var revertPaymentTask = new LambdaInvoke(this,"Revert Payment", new LambdaInvokeProps
             {
                 LambdaFunction = revertPaymentLambda,
                 Comment = "Revert Payment",
                 RetryOnServiceExceptions = false,
                 PayloadResponseOnly = true
             }).Next(revertInventoryTask);

             var waitState = new Wait(this, "Wait state", new WaitProps
             {
                 Time = WaitTime.Duration(Duration.Seconds(30))
             }).Next(revertInventoryTask);
                 
             var stepDefinition = placeOrderTask
                 .Next(new Choice(this, "Is order placed")
                     .When(Condition.StringEquals("$.Status", "ORDER_PLACED"), updateInventoryTask
                         .Next(new Choice(this, "Is inventory updated")
                             .When(Condition.StringEquals("$.Status", "INVENTORY_UPDATED"),
                                 makePaymentTask.Next(new Choice(this, "Is payment success")
                                     .When(Condition.StringEquals("$.Status", "PAYMENT_COMPLETED"), successState)
                                     .When(Condition.StringEquals("$.Status", "ERROR"), revertPaymentTask)))
                             .When(Condition.StringEquals("$.Status", "ERROR"), waitState)))
                     .When(Condition.StringEquals("$.Status", "ERROR"), failState));
            
             var stateMachine = new StateMachine(this, "DistributedTransactionOrchestrator", new StateMachineProps {
                 StateMachineName = "DistributedTransactionOrchestrator",
                 StateMachineType = StateMachineType.STANDARD,
                 Role = iamStepFunctionRole,
                 TracingEnabled = true,
                 Definition = stepDefinition
             });

             // Grant Step Functions permission to invoke Lambda functions
             placeOrderLambda.GrantInvoke(iamStepFunctionRole);
             updateInventoryLambda.GrantInvoke(iamStepFunctionRole);
             makePaymentLambda.GrantInvoke(iamStepFunctionRole);
             revertPaymentLambda.GrantInvoke(iamStepFunctionRole);
             revertInventoryLambda.GrantInvoke(iamStepFunctionRole);
             removeOrderLambda.GrantInvoke(iamStepFunctionRole);

             // Grant InvokeOrchestratorLambda permission to start Step Functions execution
             stateMachine.GrantStartExecution(iamLambdaRole);

             #endregion
        }
    }

}