## Build a serverless distributed application in .NET using Saga orchestration pattern 

Saga design pattern can be used to preserve the data integrity with distributed transactions across microservices. The source code in this repo provides sample code for the implementation of the saga orchestration pattern using .NET 6.0 on AWS.  

Blog reference: [https://aws.amazon.com/blogs/compute/building-a-serverless-distributed-application-using-a-saga-orchestration-pattern/](https://aws.amazon.com/blogs/compute/building-a-serverless-distributed-application-using-a-saga-orchestration-pattern/)

## Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.
dotnet
## License

This library is licensed under the MIT-0 License. See the LICENSE file.

## Prerequisites

For this walkthrough, you need:
- An [AWS](https://signin.aws.amazon.com/signin?redirect_uri=https%3A%2F%2Fportal.aws.amazon.com%2Fbilling%2Fsignup%2Fresume&client_id=signup) account
- An AWS user with AdministratorAccess (see the [instructions](https://console.aws.amazon.com/iam/home#/roles%24new?step=review&commonUseCase=EC2%2BEC2&selectedUseCase=EC2&policies=arn:aws:iam::aws:policy%2FAdministratorAccess) on the [AWS Identity and Access Management](http://aws.amazon.com/iam) (IAM) console)
- Access to the following AWS services: Amazon API Gateway, AWS Lambda, AWS Step Functions, and Amazon DynamoDB.
- [Node.js](https://nodejs.org/en/download/) installed
- .NET 6.0 SDK installed
- To install the global tool provided by AWS for managing .NET Core Applications with Lambda
```bash
dotnet tool install -g Amazon.Lambda.Tools
```
- JetBrains Rider or Microsoft Visual Studio 2017 or later (or Visual Studio Code)
- [Postman](https://www.postman.com/downloads/) to make the API call

## Setting up the environment

For this walkthrough, use the AWS CDK code in the GitHub Repository to create the AWS resources. These include IAM roles, REST API using API Gateway, DynamoDB tables, the Step Functions workflow and Lambda functions.

1. You need an AWS access key ID and secret access key for configuring the AWS Command Line Interface (AWS CLI). To learn more about configuring the AWS CLI, follow these instructions.
2. Clone the repo:

```bash
git clone https://github.com/aws-samples/saga-orchestration-netcore-blog
```

3.  Navigate to cdk-saga-orchestration and create the folder lambdas

``` bash
cd cdk-saga-orchestration
```

``` bash
mkdir lambdas
```

4.  Now that we have created the lambdas folder, we will navigate to the saga-orchestration directory.  Once there we want to do a ls command to view the files inside the directory

```bash
ls
```

 We should see the following inside the saga-orchestration directory:

```bash
InvokeOrchestratorLambda
MakePaymentLambda		
README.md
RevertInventoryLambda		
SendNotificationLambda
LICENSE				
PlaceOrderLambda
RemoveOrderLambda		
RevertPaymentLambda
UpdateInventoryLambda
```

5.  All of the Lambda functions above must be packaged and copied from the saga-orchestration directory to the cdk-saga-orchestration/lambdas directory before deployment.

Run these commands to package the first function: In this case "PlaceOrderLambda"

```bash
cd PlaceOrderLambda/src/PlaceOrderLambda
```
```bash
dotnet lambda package
```

Once it is packaged you will see something like this: 

"Lambda project successfully packaged: 

/xxxx/saga-orchestration-netcore-blog/saga-orchestration/RevertPaymentLambda/src/RevertPaymentLambda/bin/Release/net6.0/RevertPaymentLambda.zip"

We will then copy this zip. to our cdk-saga-orchestration/lambdas directory:

For example

```bash
cp xxxx/watemc/saga-orchestration-netcore-blog/saga-orchestration/RevertPaymentLambda/src/RevertPaymentLambda/bin/Release/net6.0/RevertPaymentLambda.zip xxxx/saga-orchestration-netcore-blog/cdk-saga-orchestration/lambdas
```

6. Repeat the above for the remaining Lambda functions until all the functions have been packaged and copied to the cdk-saga-orchestration/lambdas directory

7. Build the CDK code before deploying to the console:
```bash
cd cdk-saga-orchestration/src/CdkSagaOrchestration
```

```bash
dotnet build
```

8. Install the aws-cdk package:
```bash
npm install -g aws-cdk 
```

9. The cdk synth command causes the resources defined in the application to be translated into an AWS CloudFormation template.  In essence, "cdk synth" is the step that translates the higher-level CDK code into a format (CloudFormation) that AWS can directly understand and act upon.

The cdk deploy command deploys the stacks into your AWS account.

Run:
```bash
cd cdk-saga-orchestration
```
```bash
cdk bootstrap
```
```bash
cdk synth
```
```bash
cdk deploy
```
10. CDK deploys the environment to AWS. You can monitor the progress using the CloudFormation console. The stack name will be CdkSagaOrchestrationStack

[CloudFormation Events](/Users/watemc/desktop/github/CloudFormationEventsraw=true)

Please refer to the [blog](https://aws.amazon.com/blogs/compute/building-a-serverless-distributed-application-using-a-saga-orchestration-pattern/) for further instructions on testing the pattern.
