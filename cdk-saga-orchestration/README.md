# Welcome to your CDK C# project!

The `cdk.json` file tells the CDK Toolkit how to execute your app.

It uses the [.NET Core CLI](https://docs.microsoft.com/dotnet/articles/core/) to compile and execute your project.

## Useful commands

* `dotnet build src` compile this app
* `cdk deploy`       deploy this stack to your default AWS account/region
* `cdk diff`         compare deployed stack with current state
* `cdk synth`        emits the synthesized CloudFormation template

The saga-orchestration project contains the code for the individual lambda functions. The lambda functions should be compiled and packaged using the following commands.

* `dotnet build src`
* `dotnet lambda package`

The packaged lambda function zip file will be present in the bin/Release/netcoreapp3.1 folder.

Once packaged, create a folder called "lambdas" in the directory where cdk.json file is present. Copy the packaged lambda functions to this folder. The packaged lambda functions (zip files) should be present here for CDK to deploy to the AWS environment.
