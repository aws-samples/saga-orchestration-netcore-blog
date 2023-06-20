## Build a serverless distributed application in .NET using Saga orchestration pattern 

Saga design pattern can be used to preserve the data integrity with distributed transactions across microservices. The source code in this repo provids sample code for the implementation of the saga orchestration pattern using .NET 6.0 on AWS.  

Blog reference: [https://aws.amazon.com/blogs/compute/building-a-serverless-distributed-application-using-a-saga-orchestration-pattern/](https://aws.amazon.com/blogs/compute/building-a-serverless-distributed-application-using-a-saga-orchestration-pattern/)

## Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

## License

This library is licensed under the MIT-0 License. See the LICENSE file.

## Prerequisites

For this walkthrough, you need:
- An [AWS](https://signin.aws.amazon.com/signin?redirect_uri=https%3A%2F%2Fportal.aws.amazon.com%2Fbilling%2Fsignup%2Fresume&client_id=signup) account
- An AWS user with AdministratorAccess (see the [instructions](https://console.aws.amazon.com/iam/home#/roles%24new?step=review&commonUseCase=EC2%2BEC2&selectedUseCase=EC2&policies=arn:aws:iam::aws:policy%2FAdministratorAccess) on the [AWS Identity and Access Management](http://aws.amazon.com/iam) (IAM) console)
- Access to the following AWS services: Amazon API Gateway, AWS Lambda, AWS Step Functions, and Amazon DynamoDB.
- [Node.js](https://nodejs.org/en/download/) installed
- .NET 6.0 SDK installed
- JetBrains Rider or Microsoft Visual Studio 2017 or later (or Visual Studio Code)
- [Postman](https://www.postman.com/downloads/) to make the API call

## Setting up the environment

For this walkthrough, use the AWS CDK code in the GitHub Repository to create the AWS resources. These include IAM roles, REST API using API Gateway, DynamoDB tables, the Step Functions workflow and Lambda functions.

1. You need an AWS access key ID and secret access key for configuring the AWS Command Line Interface (AWS CLI). To learn more about configuring the AWS CLI, follow these instructions.
2. Clone the repo:
```bash
git clone https://github.com/aws-samples/saga-orchestration-netcore-blog
```
3. The Lambda functions in the saga-orchestration directory must be packaged and copied to the cdk-saga-orchestration\lambdas directory before deployment. Run these commands to process the PlaceOrderLambda function:
```bash
cd PlaceOrderLambda/src/PlaceOrderLambda 
dotnet lambda package
cp bin/Release/netcoreapp3.1/PlaceOrderLambda.zip ../../../../cdk-saga-orchestration/lambdas
```

4. Repeat the same commands for all the Lambda functions in the saga-orchestration directory.

5. Build the CDK code before deploying to the console:
```bash
cd cdk-saga-orchestration/src/CdkSagaOrchestration
dotnet build
```

6. Install the aws-cdk package:
```bash
npm install -g aws-cdk 
```

7. The cdk synth command causes the resources defined in the application to be translated into an AWS CloudFormation template. The cdk deploy command deploys the stacks into your AWS account. Run:
```bash
cd cdk-saga-orchestration
cdk synth 
cdk deploy
```
8. CDK deploys the environment to AWS. You can monitor the progress using the CloudFormation console. The stack name is CdkSagaOrchestrationStack

Please refer to the [blog](https://aws.amazon.com/blogs/compute/building-a-serverless-distributed-application-using-a-saga-orchestration-pattern/) for further instructions on testing the pattern.