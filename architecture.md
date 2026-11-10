# Architecture Design Document: Saga Orchestration for .NET on AWS

## Table of Contents
- [1. System Overview](#1-system-overview)
- [2. Architectural Style and Patterns](#2-architectural-style-and-patterns)
- [3. Technology Stack](#3-technology-stack)
- [4. System Architecture](#4-system-architecture)
- [5. Deployment Architecture](#5-deployment-architecture)
- [6. Component Interactions](#6-component-interactions)
- [7. Data Models and Key Entities](#7-data-models-and-key-entities)
- [8. API Design and Integration Architecture](#8-api-design-and-integration-architecture)
- [9. Security Architecture](#9-security-architecture)
- [10. Technical Decisions and Rationale](#10-technical-decisions-and-rationale)

---

## 1. System Overview

### 1.1 Purpose

This application is a **serverless distributed system** that implements the **Saga Orchestration Pattern** to maintain data integrity across distributed transactions. It coordinates order placement, inventory management, and payment processing using AWS Step Functions as the central orchestrator, with automatic compensating transactions when failures occur.

### 1.2 Business Context

In distributed systems where multiple services store data in different data stores, maintaining data consistency is challenging. Traditional two-phase commit (2PC) protocols are complex and don't scale well, especially with NoSQL databases that don't support ACID transactions natively. This system addresses this challenge by implementing the saga orchestration pattern, which breaks a distributed transaction into a series of local transactions, each with a corresponding compensating transaction for rollback.

### 1.3 Key Capabilities

| Capability | Description |
|------------|-------------|
| **Order Processing** | Creates and manages order records in DynamoDB |
| **Inventory Management** | Atomically updates stock levels using DynamoDB expressions |
| **Payment Processing** | Simulates payment gateway integration |
| **Saga Orchestration** | Coordinates distributed transactions with automatic compensation |
| **Fault Injection** | Enables controlled failure testing at any saga step |
| **Observability** | Provides distributed tracing via AWS X-Ray and logging via CloudWatch |

### 1.4 Repository Structure

```
saga-orchestration-netcore-blog/
├── README.md
├── saga-orchestration/                  # Lambda function source code
│   ├── InvokeOrchestratorLambda/       # API Gateway -> Step Functions bridge
│   │   ├── InvokeOrchestratorLambda.sln
│   │   ├── buildspec.yml
│   │   ├── global.json
│   │   ├── src/InvokeOrchestratorLambda/
│   │   │   ├── Function.cs             # Entry point
│   │   │   ├── InvokeOrchestratorLambda.csproj
│   │   │   └── aws-lambda-tools-defaults.json
│   │   └── test/InvokeOrchestratorLambda.Tests/
│   ├── PlaceOrderLambda/               # Forward: Create order
│   ├── UpdateInventoryLambda/          # Forward: Decrement stock
│   ├── MakePaymentLambda/             # Forward: Process payment
│   ├── RevertPaymentLambda/           # Compensating: Revert payment
│   ├── RevertInventoryLambda/         # Compensating: Increment stock
│   ├── RemoveOrderLambda/             # Compensating: Delete order
│   └── SendNotificationLambda/        # Standalone: Notification
├── cdk-saga-orchestration/             # AWS CDK infrastructure
│   ├── cdk.json
│   ├── package.json
│   ├── lambdas/                        # Packaged Lambda ZIP files
│   └── src/CdkSagaOrchestration/
│       ├── CdkSagaOrchestration.csproj
│       ├── CdkSagaOrchestrationStack.cs
│       ├── Program.cs
│       └── GlobalSuppressions.cs
└── images/
```

---

## 2. Architectural Style and Patterns

### 2.1 Primary Architectural Style: Serverless Event-Driven Architecture

The application follows a **serverless architecture** pattern where all compute is handled by AWS Lambda functions, orchestrated by AWS Step Functions, with Amazon DynamoDB as the data store.

### 2.2 Core Design Patterns

#### 2.2.1 Saga Orchestration Pattern

AWS Step Functions acts as the orchestrator coordinating a distributed transaction across multiple services.

```
┌─────────────────────────────────────────────────────┐
│              Saga Orchestrator                        │
│         (AWS Step Functions State Machine)            │
│                                                       │
│  Forward Transactions:                                │
│  ┌────────────┐  ┌────────────────┐  ┌────────────┐ │
│  │ PlaceOrder │─>│UpdateInventory │─>│MakePayment │ │
│  └────────────┘  └────────────────┘  └────────────┘ │
│                                                       │
│  Compensating Transactions (on failure):              │
│  ┌──────────────┐  ┌────────────────┐  ┌───────────┐│
│  │RevertPayment │─>│RevertInventory │─>│RemoveOrder││
│  └──────────────┘  └────────────────┘  └───────────┘│
└─────────────────────────────────────────────────────┘
```

**Key characteristics:**
- **Central coordinator**: Step Functions manages the transaction lifecycle
- **Idempotent steps**: Each step produces a deterministic status
- **Compensating transactions**: Each forward step has a corresponding reversal
- **Status-based branching**: Choice states evaluate `$.Status` to determine flow

#### 2.2.2 Compensating Transaction Pattern

| Forward Transaction | Status on Success | Compensating Transaction | Status on Completion |
|---------------------|-------------------|-------------------------|---------------------|
| PlaceOrder | ORDER_PLACED | RemoveOrder | ORDER_REMOVED |
| UpdateInventory | INVENTORY_UPDATED | RevertInventory | INVENTORY_REVERTED |
| MakePayment | PAYMENT_COMPLETED | RevertPayment | PAYMENT_REVERTED |

#### 2.2.3 Single Responsibility Pattern

Each Lambda function handles exactly one saga step, promoting independent deployment, isolated failure domains, and simple testing.

#### 2.2.4 Gateway Pattern

Amazon API Gateway serves as the single entry point for request routing, VTL transformation, and CORS management.

---

## 3. Technology Stack

### 3.1 Core Technologies

| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| **Language** | C# | .NET 6.0 | Application logic |
| **Runtime** | AWS Lambda (dotnet6) | .NET 6.0 | Serverless compute |
| **API Layer** | Amazon API Gateway | REST API v1 | HTTP endpoint |
| **Orchestration** | AWS Step Functions | Standard workflow | Saga orchestrator |
| **Database** | Amazon DynamoDB | On-demand | NoSQL data store |
| **IaC** | AWS CDK | 2.69.0 (C#) | Infrastructure definition |
| **CDK CLI** | aws-cdk (Node.js) | ^2.98.0 | CDK command-line tool |
| **CI/CD** | AWS CodeBuild | buildspec v0.2 | Build automation |
| **Tracing** | AWS X-Ray | Active | Distributed tracing |
| **Logging** | Amazon CloudWatch Logs | Managed | Centralized logging |

### 3.2 NuGet Dependencies by Lambda Function

| Lambda Function | Package | Version |
|----------------|---------|---------|
| **InvokeOrchestratorLambda** | Amazon.Lambda.Core | 2.1.0 |
| | Amazon.Lambda.Serialization.SystemTextJson | 2.3.1 |
| | Amazon.Lambda.APIGatewayEvents | 2.6.0 |
| | AWSSDK.APIGateway | 3.7.100.94 |
| | AWSSDK.ApiGatewayManagementApi | 3.7.100.94 |
| | AWSSDK.StepFunctions | 3.7.102.67 |
| **PlaceOrderLambda** | Amazon.Lambda.Core | 2.1.0 |
| | Amazon.Lambda.Serialization.SystemTextJson | 2.3.1 |
| | AWSSDK.DynamoDBv2 | 3.7.102.8 |
| **UpdateInventoryLambda** | Amazon.Lambda.Serialization.SystemTextJson | 2.3.1 |
| | AWSSDK.DynamoDBv2 | 3.7.102.2 |
| **MakePaymentLambda** | Amazon.Lambda.Core | 2.1.0 |
| | Amazon.Lambda.Serialization.SystemTextJson | 2.3.1 |
| **RevertPaymentLambda** | Amazon.Lambda.Core | 2.1.0 |
| | Amazon.Lambda.Serialization.SystemTextJson | 2.3.1 |
| **RevertInventoryLambda** | Amazon.Lambda.Core | 2.1.0 |
| | Amazon.Lambda.Serialization.SystemTextJson | 2.3.1 |
| | AWSSDK.DynamoDBv2 | 3.7.102.8 |
| **RemoveOrderLambda** | Amazon.Lambda.Core | 2.1.0 |
| | Amazon.Lambda.Serialization.SystemTextJson | 2.3.1 |
| | AWSSDK.DynamoDBv2 | 3.7.102.8 |
| | MySql.Data | 8.0.32.1 |
| **SendNotificationLambda** | Amazon.Lambda.Core | 2.1.0 |
| | Amazon.Lambda.Serialization.SystemTextJson | 2.3.1 |
| **CDK Project** | Amazon.CDK.Lib | 2.69.0 |

### 3.3 Test Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Amazon.Lambda.TestUtilities | 2.0.0 | Lambda test context mocking |
| Microsoft.NET.Test.Sdk | 17.5.0 | Test platform |
| xunit | 2.4.2 | Test framework |
| xunit.runner.visualstudio | 2.4.5 | Test runner |

### 3.4 SDK Configuration

All Lambda projects use:
- **Target Framework**: `net6.0`
- **SDK Version**: `6.0.0` (with `rollForward: latestMajor`)
- **Lambda Runtime**: `dotnet6`
- **Memory**: 256 MB
- **Timeout**: 30-90s (aws-lambda-tools-defaults.json), overridden to 300s in CDK

### 3.5 Notable Observations

- **MySql.Data dependency**: Present in RemoveOrderLambda but not actively used in code
- **Inconsistent DynamoDB SDK versions**: UpdateInventoryLambda uses 3.7.102.2 while others use 3.7.102.8
- **buildspec.yml runtime mismatch**: References `dotnet: 3.1` but projects target `net6.0`

---

## 4. System Architecture

### 4.1 C4 Component Diagram

```
+-----------------------------------------------------------------------+
|                              AWS Cloud                                 |
|                                                                        |
|  +----------+                                                          |
|  |  Client  |  HTTP POST                                               |
|  | (Postman)|--------+                                                 |
|  +----------+        |                                                 |
|                      v                                                 |
|           +------------------------+                                   |
|           |  Amazon API Gateway    |                                   |
|           |  SagaOrchestratorAPI   |                                   |
|           |  ANY / -> Lambda       |                                   |
|           |  OPTIONS / -> Mock     |                                   |
|           +----------+-------------+                                   |
|                      | VTL mapping                                     |
|                      v                                                 |
|           +------------------------+    +---------------------------+  |
|           | InvokeOrchestrator     |    |  AWS Step Functions       |  |
|           | Lambda                 |--->|  DistributedTransaction   |  |
|           | (Bridge Function)      |    |  Orchestrator             |  |
|           | X-Ray: Active          |    |  Type: STANDARD           |  |
|           +------------------------+    |  X-Ray: Enabled           |  |
|                                         +----------+----------------+  |
|                                                    |                   |
|                                                    | Orchestrates      |
|                                                    v                   |
|   +----------------------------------------------------------------+  |
|   |              Saga Step Lambda Functions                         |  |
|   |                                                                 |  |
|   |  Forward:                                                       |  |
|   |  [PlaceOrder] -> [UpdateInventory] -> [MakePayment]            |  |
|   |                                                                 |  |
|   |  Compensating:                                                  |  |
|   |  [RevertPayment] -> [RevertInventory] -> [RemoveOrder]         |  |
|   |                                                                 |  |
|   |  Standalone: [SendNotification]                                 |  |
|   +----------------------------+------------------------------------+  |
|                                |                                       |
|                                v                                       |
|           +--------------------------------------------+               |
|           |          Amazon DynamoDB                    |               |
|           |  +----------------+  +------------------+  |               |
|           |  | Orders Table   |  | Inventory Table  |  |               |
|           |  | PK: ItemId     |  | PK: ItemId       |  |               |
|           |  | SK: CustomerId |  | ItemName         |  |               |
|           |  | Status         |  | ItemsInStock     |  |               |
|           |  +----------------+  | ItemStatus       |  |               |
|           |                      +------------------+  |               |
|           +--------------------------------------------+               |
|                                                                        |
|  Cross-Cutting: [IAM] [CloudWatch Logs] [AWS X-Ray]                  |
+-----------------------------------------------------------------------+
```

### 4.2 Component Catalog

| Component | Type | Responsibility | Data Store Access |
|-----------|------|---------------|-------------------|
| SagaOrchestratorAPI | API Gateway REST API | HTTP entry point | None |
| InvokeOrchestratorLambda | Lambda Function | Bridge to Step Functions | None |
| DistributedTransactionOrchestrator | Step Functions | Saga workflow coordinator | None |
| PlaceOrderLambda | Lambda Function | Creates order record | Orders (PutItem) |
| UpdateInventoryLambda | Lambda Function | Decrements stock | Inventory (UpdateItem) |
| MakePaymentLambda | Lambda Function | Simulates payment | None |
| RevertPaymentLambda | Lambda Function | Reverts payment status | None |
| RevertInventoryLambda | Lambda Function | Increments stock back | Inventory (UpdateItem) |
| RemoveOrderLambda | Lambda Function | Deletes order record | Orders (DeleteItem) |
| SendNotificationLambda | Lambda Function | Sends notification | None |

---

## 5. Deployment Architecture

### 5.1 Deployment Diagram

```
+----------------------------------------------------------------------+
|                    AWS Account / Region                                |
|  +------------------------------------------------------------------+|
|  |  CloudFormation Stack: CdkSagaOrchestrationStack                  ||
|  |  Deployed via: AWS CDK (cdk deploy)                               ||
|  |                                                                    ||
|  |  API Layer:                                                        ||
|  |    API Gateway: SagaOrchestratorAPI                                ||
|  |    Endpoint: https://{id}.execute-api.{region}.amazonaws.com/prod/ ||
|  |                                                                    ||
|  |  Compute Layer (8 Lambda Functions):                               ||
|  |    Runtime: .NET 6.0 | Memory: 256MB | Timeout: 300s              ||
|  |    IAM Role: LambdaExecutionRole                                   ||
|  |    [InvokeOrchestrator] [PlaceOrder] [UpdateInventory]            ||
|  |    [MakePayment] [RevertPayment] [RevertInventory]                ||
|  |    [RemoveOrder] [SendNotification]                                ||
|  |                                                                    ||
|  |  Orchestration Layer:                                              ||
|  |    Step Functions: DistributedTransactionOrchestrator              ||
|  |    Type: STANDARD | Tracing: Enabled                              ||
|  |    IAM Role: step_functions_basic_execution                        ||
|  |                                                                    ||
|  |  Data Layer:                                                       ||
|  |    DynamoDB: Orders (PK:ItemId, SK:CustomerId)                    ||
|  |    DynamoDB: Inventory (PK:ItemId)                                ||
|  |    RemovalPolicy: DESTROY                                          ||
|  |                                                                    ||
|  |  Observability: CloudWatch Logs | X-Ray Tracing                   ||
|  +------------------------------------------------------------------+|
+----------------------------------------------------------------------+
```

### 5.2 Deployment Process

1. **Package Lambda Functions**: `dotnet lambda package` per Lambda
2. **Copy ZIP files**: Packaged ZIPs to `cdk-saga-orchestration/lambdas/`
3. **Build CDK**: `dotnet build` the CDK project
4. **Synthesize**: `cdk synth` generates CloudFormation template
5. **Deploy**: `cdk deploy` creates/updates the stack

### 5.3 Lambda Function Configuration

| Setting | aws-lambda-tools-defaults.json | CDK Override |
|---------|-------------------------------|-------------|
| Memory | 256 MB | Not overridden |
| Timeout | 30-90s (varies) | 300s (all functions) |
| Runtime | dotnet6 | DOTNET_6 |
| Tracing | Not set | ACTIVE (InvokeOrchestrator only) |

---

## 6. Component Interactions

### 6.1 Step Functions State Machine Definition

```
                    +-------------------+
                    |   Place Order     |
                    |   (LambdaInvoke)  |
                    +---------+---------+
                              |
                    +---------v---------+
                    | Is order placed?  | (Choice)
                    +---+----------+----+
            ORDER_PLACED|          |ERROR
                        |          |
              +---------v---+  +---v--------+
              |Update       |  |  Fail      |
              |Inventory    |  |  State     |
              |(LambdaInvoke|  +------------+
              +------+------+
                     |
            +--------v----------+
            |Is inventory       | (Choice)
            |updated?           |
            +---+-----------+---+
  INVENTORY_    |           |ERROR
  UPDATED       |           |
      +---------v---+  +----v---------+
      |Make Payment |  |Wait State    |
      |(LambdaInvoke|  |(30 seconds)  |
      +------+------+  +----+---------+
             |               |
      +------v--------+     |
      |Is payment     |     |
      |success?(Choice|     |
      +--+--------+---+     |
  PAYMENT |        |ERROR    |
  COMPLETED        |         |
   +-----v---+ +---v-------+|
   | Success  | |Revert     ||
   | State    | |Payment    ||
   +----------+ |(Lambda)   ||
                +----+------+|
                     |        |
              +------v--------v--+
              |Revert Inventory  |
              |(LambdaInvoke)    |
              +--------+---------+
                       |
              +--------v---------+
              |Remove Order      |
              |(LambdaInvoke)    |
              +--------+---------+
                       |
              +--------v---------+
              |Fail State        |
              +------------------+
```

### 6.2 Happy Path Sequence

```
StepFunctions    PlaceOrder    UpdateInventory  MakePayment    DynamoDB
     |               |              |                |            |
     | Invoke         |              |                |            |
     |-------------->|              |                |            |
     |               | PutItem(Orders)               |            |
     |               |-------------------------------------------->|
     | ORDER_PLACED  |              |                |            |
     |<--------------|              |                |            |
     |               |              |                |            |
     | Choice: OK    |              |                |            |
     | Invoke         |              |                |            |
     |------------------------------>|                |            |
     |               |              | UpdateItem(Inv)|            |
     |               |              | (decrement)    |            |
     |               |              |------------------------------>|
     | INVENTORY_UPDATED            |                |            |
     |<------------------------------|                |            |
     |               |              |                |            |
     | Choice: OK    |              |                |            |
     | Invoke         |              |                |            |
     |---------------------------------------------->|            |
     |               |              |  PAYMENT_COMPLETED          |
     |<----------------------------------------------|            |
     |               |              |                |            |
     | -> SuccessState              |                |            |
```

### 6.3 Compensation Path (Payment Fails)

```
StepFunctions    RevertPayment  RevertInventory  RemoveOrder    DynamoDB
     |               |              |                |            |
     | MakePayment returned ERROR   |                |            |
     |               |              |                |            |
     | Invoke         |              |                |            |
     |-------------->|              |                |            |
     | PAYMENT_REVERTED             |                |            |
     |<--------------|              |                |            |
     |               |              |                |            |
     | Invoke         |              |                |            |
     |------------------------------>|                |            |
     |               |              | UpdateItem(Inv)|            |
     |               |              | (increment)    |            |
     |               |              |------------------------------>|
     | INVENTORY_REVERTED           |                |            |
     |<------------------------------|                |            |
     |               |              |                |            |
     | Invoke         |              |                |            |
     |---------------------------------------------->|            |
     |               |              |                | DeleteItem |
     |               |              |                |----------->|
     | ORDER_REMOVED  |              |                |            |
     |<----------------------------------------------|            |
     |               |              |                |            |
     | -> FailState   |              |                |            |
```

### 6.4 State Machine Step Configurations

| Step | Type | Lambda | RetryOnServiceExceptions | PayloadResponseOnly |
|------|------|--------|--------------------------|---------------------|
| Place Order | LambdaInvoke | PlaceOrderLambda | false | true |
| Update Inventory | LambdaInvoke | UpdateInventoryLambda | false | true |
| Make Payment | LambdaInvoke | MakePaymentLambda | false | true |
| Revert Payment | LambdaInvoke | RevertPaymentLambda | false | true |
| Revert Inventory | LambdaInvoke | RevertInventoryLambda | false | true |
| Remove Order | LambdaInvoke | RemoveOrderLambda | false | true |
| Wait state | Wait (30s) | N/A | N/A | N/A |

---

## 7. Data Models and Key Entities

### 7.1 OrderDetails Data Transfer Object (DTO)

The `OrderDetails` class is the primary DTO used across all saga step Lambda functions. It is defined independently in each Lambda's namespace (no shared library).

```csharp
public class OrderDetails
{
    public string ItemId { get; set; }       // Partition key for both DynamoDB tables
    public string CustomerId { get; set; }   // Sort key for Orders table
    public string MessageId { get; set; }    // Message correlation identifier
    public string FailAtStage { get; set; }  // Fault injection control
    public string Status { get; set; }       // Current saga step status
}
```

### 7.2 StageNameEnum (Fault Injection)

```csharp
public enum StageNameEnum
{
    PlaceOrder = 0,
    MakePayment = 1,
    UpdateInventory = 2,
    SendNotification = 3,
    None = 4
}
```

When `FailAtStage` matches the current step's `CurrentStage`, the Lambda sets `Status = "ERROR"` to trigger compensation.

### 7.3 DynamoDB Table Schemas

#### 7.3.1 Orders Table

| Attribute | Type | Key | Description |
|-----------|------|-----|-------------|
| **ItemId** | String (S) | Partition Key | Item identifier |
| **CustomerId** | String (S) | Sort Key | Customer identifier |
| **Status** | String (S) | Attribute | Order status |

**Operations:**
- **PutItem** (PlaceOrderLambda): Creates `{ItemId, CustomerId, Status: "ORDER_PLACED"}`
- **DeleteItem** (RemoveOrderLambda): Deletes by `{ItemId, CustomerId}` with `ReturnValues: ALL_OLD`

#### 7.3.2 Inventory Table

| Attribute | Type | Key | Description |
|-----------|------|-----|-------------|
| **ItemId** | String (S) | Partition Key | Item identifier |
| **ItemName** | String (S) | Attribute | Product name |
| **ItemsInStock** | Number (N) | Attribute | Current stock quantity |
| **ItemStatus** | String (S) | Attribute | Item status |

**Operations:**
- **UpdateItem** (UpdateInventoryLambda): `SET ItemsInStock = ItemsInStock - :inc` (`:inc = 1`)
- **UpdateItem** (RevertInventoryLambda): `SET ItemsInStock = ItemsInStock + :inc` (`:inc = 1`)

**Sample Data:**
```json
[
  {"ItemId": "ITEM001", "ItemName": "Soap", "ItemsInStock": 1000, "ItemStatus": ""},
  {"ItemId": "ITEM002", "ItemName": "Shampoo", "ItemsInStock": 500, "ItemStatus": ""},
  {"ItemId": "ITEM003", "ItemName": "Toothpaste", "ItemsInStock": 2000, "ItemStatus": ""}
]
```

### 7.4 Entity Relationship

```
+------------------------+         +--------------------------+
|      Orders Table       |         |     Inventory Table       |
|                         |         |                           |
|  PK: ItemId (String)   |-------->|  PK: ItemId (String)     |
|  SK: CustomerId (String)|         |                           |
|  Status (String)        |         |  ItemName (String)        |
|                         |         |  ItemsInStock (Number)    |
|  Created: PlaceOrder    |         |  ItemStatus (String)      |
|  Deleted: RemoveOrder   |         |  Updated: UpdateInventory |
+------------------------+         |  Reverted: RevertInventory|
                                    +--------------------------+

Relationship: Orders.ItemId -> Inventory.ItemId (logical, not enforced)
One Inventory item can have many Orders (1:N via different CustomerIds)
```

### 7.5 Status Values and Transitions

| Status Value | Set By | Meaning | Triggers |
|-------------|--------|---------|----------|
| `ORDER_PLACED` | PlaceOrderLambda | Order created in DynamoDB | Continue to UpdateInventory |
| `INVENTORY_UPDATED` | UpdateInventoryLambda | Stock decremented | Continue to MakePayment |
| `PAYMENT_COMPLETED` | MakePaymentLambda | Payment processed | SuccessState |
| `ERROR` | Any forward Lambda | Step failed | Compensating transactions |
| `PAYMENT_REVERTED` | RevertPaymentLambda | Payment undone | Continue to RevertInventory |
| `INVENTORY_REVERTED` | RevertInventoryLambda | Stock restored | Continue to RemoveOrder |
| `ORDER_REMOVED` | RemoveOrderLambda | Order deleted | FailState |

### 7.6 SendNotificationResult

```csharp
public class SendNotificationResult
{
    public string Status { get; set; }  // "SUCCESS"
}
```

---

## 8. API Design and Integration Architecture

### 8.1 REST API Contract

| Property | Value |
|----------|-------|
| **API Name** | SagaOrchestratorAPI |
| **Type** | REST API (v1) |
| **Description** | "This service triggers the saga orchestration workflow." |
| **Endpoint** | `https://{api-id}.execute-api.{region}.amazonaws.com/prod/` |

### 8.2 Endpoints

**ANY /** - Trigger Saga Workflow
- **Integration Type**: Lambda (non-proxy)
- **Integration Target**: InvokeOrchestratorLambda
- **Passthrough Behavior**: WHEN_NO_TEMPLATES
- **VTL Request Template**: Maps ItemId, CustomerId, MessageId, FailAtStage from request body

**Request Body:**
```json
{
  "ItemId": "ITEM001",
  "CustomerId": "ABC/002",
  "MessageId": "",
  "FailAtStage": "None"
}
```

**Response:** HTTP 200, Body: "Processed", Headers: Content-Type: application/json, Access-Control-Allow-Origin: *

**OPTIONS /** - CORS Preflight (Mock integration, HTTP 200)

### 8.3 Lambda Handler Signatures

| Lambda Function | Class.Method | Input | Output |
|----------------|-------------|-------|--------|
| InvokeOrchestratorLambda | `InvokeOrchestrator.FunctionHandler` | `OrderDetails, ILambdaContext` | `Task<APIGatewayProxyResponse>` |
| PlaceOrderLambda | `PlaceOrder.FunctionHandler` | `OrderDetails, ILambdaContext` | `OrderDetails` |
| UpdateInventoryLambda | `UpdateInventory.FunctionHandler` | `OrderDetails, ILambdaContext` | `OrderDetails` |
| MakePaymentLambda | `MakePayment.FunctionHandler` | `OrderDetails, ILambdaContext` | `OrderDetails` |
| RevertPaymentLambda | `RevertPayment.FunctionHandler` | `OrderDetails, ILambdaContext` | `OrderDetails` |
| RevertInventoryLambda | `RevertInventory.FunctionHandler` | `OrderDetails, ILambdaContext` | `OrderDetails` |
| RemoveOrderLambda | `RemoveOrder.FunctionHandler` | `OrderDetails, ILambdaContext` | `string` |
| SendNotificationLambda | `SendNotification.FunctionHandler` | `ILambdaContext` | `SendNotificationResult` |

### 8.4 Integration Patterns

#### API Gateway to InvokeOrchestratorLambda
- Non-proxy integration with VTL request mapping
- Lambda returns APIGatewayProxyResponse with HTTP status and CORS headers

#### InvokeOrchestratorLambda to Step Functions
- Constructs state machine ARN dynamically from Lambda function ARN (extracting accountId and region)
- Serializes OrderDetails to JSON and calls `StartExecutionAsync`
- State machine: `arn:aws:states:{region}:{accountId}:stateMachine:DistributedTransactionOrchestrator`

#### Lambda Functions to DynamoDB

| Lambda | Operation | Table | Key | Expression |
|--------|-----------|-------|-----|------------|
| PlaceOrderLambda | PutItem | Orders | {ItemId, CustomerId} | Creates item with Status |
| UpdateInventoryLambda | UpdateItem | Inventory | {ItemId} | `SET ItemsInStock = ItemsInStock - :inc` |
| RevertInventoryLambda | UpdateItem | Inventory | {ItemId} | `SET ItemsInStock = ItemsInStock + :inc` |
| RemoveOrderLambda | DeleteItem | Orders | {ItemId, CustomerId} | ReturnValues: ALL_OLD |

**Best Practice**: All DynamoDB-interacting Lambdas use a `static AmazonDynamoDBClient` for connection reuse across warm invocations.

---

## 9. Security Architecture

### 9.1 IAM Roles and Policies

#### LambdaExecutionRole
- **Assumed By**: lambda.amazonaws.com
- **Used By**: All 8 Lambda functions

| Managed Policy | Purpose |
|---------------|---------|
| AmazonDynamoDBFullAccess | DynamoDB table access |
| SecretsManagerReadWrite | Secrets Manager access |
| CloudWatchLogsFullAccess | Logging |
| AWSXrayFullAccess | X-Ray tracing |
| AWSLambdaVPCAccessExecutionRole | VPC network interface management |
| AWSStepFunctionsFullAccess | Step Functions access (InvokeOrchestrator) |

#### step_functions_basic_execution
- **Assumed By**: states.amazonaws.com
- **Used By**: DistributedTransactionOrchestrator

| Managed Policy | Purpose |
|---------------|---------|
| CloudWatchLogsFullAccess | Logging |
| AWSLambdaRole | Invoke Lambda functions |
| AWSXrayFullAccess | X-Ray tracing |

### 9.2 Observability and Tracing

| Component | X-Ray | CloudWatch Logs |
|-----------|-------|-----------------|
| InvokeOrchestratorLambda | **ACTIVE** | Yes |
| All other Lambdas | PassThrough | Yes |
| Step Functions State Machine | **Enabled** | Yes |

### 9.3 CORS Configuration

| Header | Value |
|--------|-------|
| Access-Control-Allow-Origin | `*` (wildcard) |
| Content-Type | application/json |

CORS headers are set in InvokeOrchestratorLambda response. OPTIONS method uses mock integration.

### 9.4 Security Observations and Recommendations

| Finding | Severity | Description |
|---------|----------|-------------|
| Overly broad IAM policies | Medium | Uses `*FullAccess` managed policies instead of least-privilege |
| Shared IAM role | Low | All Lambdas share one role; best practice is per-function roles |
| No API authentication | High | No auth/authz on API Gateway; add Cognito, API keys, or IAM auth |
| Wildcard CORS | Medium | `*` origin allows all; restrict to specific domains |
| SecretsManagerReadWrite unused | Low | Policy included but no Secrets Manager usage in code |
| VPC access role unused | Low | VPC policy included but no VPC configuration |

### 9.5 Secrets Management

- buildspec.yml references Secrets Manager for CodeBuild credentials
- Lambda functions use IAM role-based authentication (no hardcoded credentials)
- DynamoDB clients use default AWS credential chain

---

## 10. Technical Decisions and Rationale

### 10.1 Architectural Decisions

#### AD-01: Saga Orchestration over Choreography
**Decision**: Central coordinator (Step Functions) instead of event-driven choreography.
**Rationale**: Explicit control flow, visual monitoring, centralized error handling. Choreography would create tighter service coupling.
**Trade-off**: Vendor lock-in to AWS Step Functions, cost per state transition.

#### AD-02: AWS Step Functions as Orchestrator
**Decision**: Standard workflow for exactly-once execution semantics.
**Rationale**: Built-in state management, Choice/Wait states, visual debugging, no custom orchestration code needed.

#### AD-03: DynamoDB for Data Storage
**Decision**: NoSQL for Orders and Inventory data.
**Rationale**: Single-digit ms latency, serverless scaling, Lambda integration. Key-value access patterns are ideal. Lack of cross-table ACID transactions is exactly why saga is needed.

#### AD-04: Separate Lambda per Saga Step
**Decision**: Independent deployment unit per step.
**Rationale**: Single responsibility, independent scaling, isolated failures, targeted dependency sets.
**Trade-off**: More functions to manage, duplicated code (OrderDetails DTO).

#### AD-05: Fault Injection via FailAtStage
**Decision**: Request parameter for controlled failure testing.
**Rationale**: Test compensating flows without code changes. Easy demonstration.
**Trade-off**: Must be disabled in production to prevent abuse.

#### AD-06: Non-Proxy Lambda Integration
**Decision**: VTL templates for request/response transformation.
**Rationale**: Decouples API contract from Lambda input format.
**Trade-off**: VTL harder to test and debug than proxy integration.

#### AD-07: AWS CDK (C#) for Infrastructure
**Decision**: Same language for app and infra code.
**Rationale**: Type safety, IDE support, higher-level abstractions, single stack.

### 10.2 Design Considerations

- **DC-01: Duplicated DTO** - OrderDetails defined in each Lambda namespace rather than shared NuGet package. Simple but creates maintenance overhead.
- **DC-02: Blocking async calls** - Some Lambdas use `.Result` instead of `await`, blocking threads unnecessarily.
- **DC-03: Static DynamoDB client** - Best practice for Lambda, enables connection reuse across warm invocations.
- **DC-04: 30-second Wait state** - Before RevertInventory on UpdateInventory failure; provides eventual consistency grace period.
- **DC-05: SendNotificationLambda not in workflow** - Deployed but not wired into Step Functions; placeholder for future integration.

### 10.3 Areas for Improvement

| Area | Current State | Recommendation |
|------|--------------|----------------|
| Shared Library | DTO duplicated across 7 projects | Create shared NuGet package |
| IAM Least Privilege | Broad managed policies | Custom resource-scoped policies |
| API Authentication | No auth configured | Add Cognito/API keys/IAM auth |
| Error Handling | Basic try-catch | Structured errors, DLQ for failures |
| .NET Version | .NET 6.0 (LTS ended) | Migrate to .NET 8.0 |
| CORS | Wildcard origin | Restrict to specific origins |
| Async Pattern | .Result blocking calls | Convert to async/await |
| Testing | Basic xUnit structure | Integration tests, SF local testing |
| MySql.Data | Unused dependency | Remove from RemoveOrderLambda |
| buildspec.yml | References .NET 3.1 | Update to .NET 6.0+ |

---

*Architecture Design Document generated by reverse engineering the saga-orchestration-netcore-blog application codebase. All findings are based on source code analysis and may require validation with the development team.*
