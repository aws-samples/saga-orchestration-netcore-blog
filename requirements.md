# Requirements Document: Saga Orchestration for .NET on AWS

## Table of Contents
- [1. Executive Summary](#1-executive-summary)
- [2. System Overview](#2-system-overview)
- [3. Architecture Diagrams](#3-architecture-diagrams)
- [4. Functional Requirements](#4-functional-requirements)
- [5. Key Non-Functional Requirements](#5-key-non-functional-requirements)
- [6. Business Rules and Constraints](#6-business-rules-and-constraints)
- [7. Integration Requirements](#7-integration-requirements)
- [8. Assumptions and Constraints](#8-assumptions-and-constraints)
- [9. Glossary](#9-glossary)

---

## 1. Executive Summary

This document describes the functional and non-functional requirements for a **serverless distributed application** that implements the **Saga Orchestration Pattern** using .NET 6.0 on AWS. The system provides a mechanism to maintain data integrity across distributed transactions spanning multiple services (order management, inventory management, and payment processing) by using AWS Step Functions as a central orchestrator.

The application exposes a REST API via Amazon API Gateway, which triggers a Step Functions state machine. The state machine coordinates a series of Lambda functions that execute forward transactions (place order, update inventory, make payment) and, upon failure, automatically executes compensating transactions (revert payment, revert inventory, remove order) to restore data consistency.

**Key Business Value:**
- Ensures data consistency across distributed services without relying on two-phase commit (2PC)
- Provides automatic failure recovery through compensating transactions
- Supports fault injection testing for resilience validation
- Fully serverless architecture requiring no infrastructure management

---

## 2. System Overview

### 2.1 Purpose
The system implements a saga orchestration pattern to coordinate distributed transactions across order, inventory, and payment services. It demonstrates how to preserve data integrity in distributed systems using AWS Step Functions as the central coordinator.

### 2.2 Scope
The application encompasses:
- **Order Management**: Creating and removing orders in DynamoDB
- **Inventory Management**: Decrementing and incrementing stock levels in DynamoDB
- **Payment Processing**: Simulating payment completion and reversal
- **Saga Orchestration**: Coordinating the entire transaction workflow with forward and compensating transactions
- **Notification**: Sending notifications upon process completion
- **Fault Injection**: Testing failure scenarios at specific stages

### 2.3 Users and Actors
| Actor | Description |
|-------|-------------|
| **API Client** | External client (e.g., Postman, web application) that submits order requests via REST API |
| **API Gateway** | AWS service that receives and routes HTTP requests |
| **Step Functions** | AWS service that orchestrates the saga workflow |
| **Lambda Functions** | Serverless compute units that execute individual saga steps |
| **DynamoDB** | NoSQL database storing order and inventory data |

### 2.4 Technology Stack
| Component | Technology | Version |
|-----------|------------|---------|
| Programming Language | C# | .NET 6.0 |
| Runtime | AWS Lambda | dotnet6 |
| API Layer | Amazon API Gateway | REST API |
| Orchestration | AWS Step Functions | Standard workflow |
| Database | Amazon DynamoDB | On-demand |
| Infrastructure as Code | AWS CDK | 2.69.0 (C#) |
| Observability | AWS X-Ray | Active tracing |
| CI/CD | AWS CodeBuild | buildspec.yml |

---

## 3. Architecture Diagrams

### 3.1 System Architecture Diagram (C4 Context Level)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        AWS Cloud                                     │
│                                                                      │
│  ┌──────────┐    ┌──────────────────┐    ┌─────────────────────┐    │
│  │          │    │   API Gateway     │    │   AWS Step          │    │
│  │  Client  │───▶│  SagaOrchestrator│───▶│   Functions          │    │
│  │ (Postman)│    │  API (REST)       │    │  (Distributed       │    │
│  │          │    │                   │    │   Transaction       │    │
│  └──────────┘    │  ANY /            │    │   Orchestrator)     │    │
│                  └────────┬──────────┘    └──────────┬──────────┘    │
│                           │                          │               │
│                           ▼                          ▼               │
│                  ┌──────────────────┐    ┌─────────────────────┐    │
│                  │ InvokeOrchestrator│    │  Saga Step Lambdas  │    │
│                  │ Lambda            │    │  ┌─PlaceOrder      │    │
│                  │ (Triggers State   │    │  ├─UpdateInventory  │    │
│                  │  Machine)         │    │  ├─MakePayment      │    │
│                  └──────────────────┘    │  ├─RevertPayment    │    │
│                                          │  ├─RevertInventory  │    │
│                                          │  ├─RemoveOrder      │    │
│                                          │  └─SendNotification │    │
│                                          └──────────┬──────────┘    │
│                                                     │               │
│                                                     ▼               │
│                                          ┌─────────────────────┐    │
│                                          │  Amazon DynamoDB     │    │
│                                          │  ┌─Orders Table     │    │
│                                          │  └─Inventory Table  │    │
│                                          └─────────────────────┘    │
│                                                                      │
│  ┌─────────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  AWS CloudWatch  │  │  AWS X-Ray   │  │  AWS IAM             │   │
│  │  (Logging)       │  │  (Tracing)   │  │  (Security)          │   │
│  └─────────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.2 Deployment Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                     AWS Account / Region                              │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                    Amazon API Gateway                            │ │
│  │  ┌──────────────────────────────────────────────────────────┐   │ │
│  │  │  REST API: SagaOrchestratorAPI                            │   │ │
│  │  │  Methods: ANY / (Lambda Integration), OPTIONS / (Mock)    │   │ │
│  │  └──────────────────────────────────────────────────────────┘   │ │
│  └──────────────────────────────────┬──────────────────────────────┘ │
│                                     │                                 │
│  ┌──────────────────────────────────▼──────────────────────────────┐ │
│  │                    AWS Lambda Functions                          │ │
│  │  Runtime: .NET 6.0 (dotnet6) | Memory: 256 MB | Timeout: 300s  │ │
│  │                                                                  │ │
│  │  ┌────────────────────┐  ┌────────────────────┐                 │ │
│  │  │InvokeOrchestrator  │  │  PlaceOrderLambda  │                 │ │
│  │  │Lambda (X-Ray ON)   │  │  (DynamoDB PutItem)│                 │ │
│  │  └────────────────────┘  └────────────────────┘                 │ │
│  │  ┌────────────────────┐  ┌────────────────────┐                 │ │
│  │  │UpdateInventory     │  │  MakePaymentLambda │                 │ │
│  │  │Lambda (DynamoDB    │  │  (Simulated)       │                 │ │
│  │  │ UpdateItem)        │  └────────────────────┘                 │ │
│  │  └────────────────────┘  ┌────────────────────┐                 │ │
│  │  ┌────────────────────┐  │RevertPaymentLambda │                 │ │
│  │  │RevertInventory     │  │  (Status update)   │                 │ │
│  │  │Lambda (DynamoDB    │  └────────────────────┘                 │ │
│  │  │ UpdateItem)        │  ┌────────────────────┐                 │ │
│  │  └────────────────────┘  │RemoveOrderLambda   │                 │ │
│  │  ┌────────────────────┐  │(DynamoDB DeleteItem)│                │ │
│  │  │SendNotification    │  └────────────────────┘                 │ │
│  │  │Lambda              │                                          │ │
│  │  └────────────────────┘                                          │ │
│  └──────────────────────────────────┬──────────────────────────────┘ │
│                                     │                                 │
│  ┌──────────────────────────────────▼──────────────────────────────┐ │
│  │              AWS Step Functions (Standard)                       │ │
│  │  State Machine: DistributedTransactionOrchestrator               │ │
│  │  X-Ray Tracing: Enabled                                          │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                    Amazon DynamoDB                                │ │
│  │  ┌─────────────────────┐  ┌─────────────────────────┐          │ │
│  │  │ Orders Table         │  │ Inventory Table          │          │ │
│  │  │ PK: ItemId (String)  │  │ PK: ItemId (String)      │          │ │
│  │  │ SK: CustomerId (Str) │  │ Attrs: ItemName,          │          │ │
│  │  │ Attrs: Status         │  │   ItemsInStock, ItemStatus│          │ │
│  │  └─────────────────────┘  └─────────────────────────┘          │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│  ┌──────────────────┐ ┌──────────────┐ ┌────────────────────────┐   │
│  │ IAM Roles         │ │ CloudWatch   │ │ AWS X-Ray              │   │
│  │ -LambdaExecution  │ │ Logs         │ │ (Distributed Tracing)  │   │
│  │ -StepFunctionRole │ │              │ │                        │   │
│  └──────────────────┘ └──────────────┘ └────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

### 3.3 Component Interaction Diagram (Saga Workflow Sequence)

```
Client          API GW       InvokeOrch    StepFunctions   PlaceOrder   UpdateInv    MakePayment   RevertPay    RevertInv    RemoveOrder   DynamoDB
  │                │              │              │              │            │             │            │            │             │           │
  │  POST /        │              │              │              │            │             │            │            │             │           │
  │  {OrderDetails}│              │              │              │            │             │            │            │             │           │
  ├───────────────▶│              │              │              │            │             │            │            │             │           │
  │                │  VTL mapping │              │              │            │             │            │            │             │           │
  │                ├─────────────▶│              │              │            │             │            │            │             │           │
  │                │              │ StartExecution              │            │             │            │            │             │           │
  │                │              ├─────────────▶│              │            │             │            │            │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │   202 Accept │              │              │            │             │            │            │             │           │
  │◀───────────────┤◀─────────────┤              │              │            │             │            │            │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ ═══════════ FORWARD TRANSACTIONS (Happy Path) ════════════════   │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ LambdaInvoke │            │             │            │            │             │           │
  │                │              │              ├─────────────▶│            │             │            │            │             │           │
  │                │              │              │              │ PutItem    │             │            │            │             │           │
  │                │              │              │              ├────────────┼─────────────┼────────────┼────────────┼─────────────┼──────────▶│
  │                │              │              │ {Status:     │            │             │            │            │             │           │
  │                │              │              │ ORDER_PLACED}│            │             │            │            │             │           │
  │                │              │              │◀─────────────┤            │             │            │            │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ [Choice: ORDER_PLACED?]   │             │            │            │             │           │
  │                │              │              │ LambdaInvoke │            │             │            │            │             │           │
  │                │              │              ├──────────────┼───────────▶│             │            │            │             │           │
  │                │              │              │              │            │ UpdateItem  │            │            │             │           │
  │                │              │              │              │            │ (decrement) │            │            │             │           │
  │                │              │              │              │            ├─────────────┼────────────┼────────────┼─────────────┼──────────▶│
  │                │              │              │ {Status:     │            │             │            │            │             │           │
  │                │              │              │ INVENTORY_UPDATED}        │             │            │            │             │           │
  │                │              │              │◀─────────────┼────────────┤             │            │            │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ [Choice: INVENTORY_UPDATED?]            │            │            │             │           │
  │                │              │              │ LambdaInvoke │            │             │            │            │             │           │
  │                │              │              ├──────────────┼────────────┼────────────▶│            │            │             │           │
  │                │              │              │ {Status:     │            │             │            │            │             │           │
  │                │              │              │ PAYMENT_COMPLETED}        │             │            │            │             │           │
  │                │              │              │◀─────────────┼────────────┼─────────────┤            │            │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ [Choice: PAYMENT_COMPLETED?] ──▶ SuccessState        │            │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ ═══════════ COMPENSATING TRANSACTIONS (Error Path) ═════════════ │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ [If MakePayment returns ERROR]          │            │            │             │           │
  │                │              │              │ LambdaInvoke │            │             │            │            │             │           │
  │                │              │              ├──────────────┼────────────┼─────────────┼───────────▶│            │             │           │
  │                │              │              │ {Status: PAYMENT_REVERTED}│             │            │            │             │           │
  │                │              │              │◀─────────────┼────────────┼─────────────┼────────────┤            │             │           │
  │                │              │              │ LambdaInvoke │            │             │            │            │             │           │
  │                │              │              ├──────────────┼────────────┼─────────────┼────────────┼───────────▶│             │           │
  │                │              │              │              │            │             │            │            │ UpdateItem  │           │
  │                │              │              │              │            │             │            │            │ (increment) │           │
  │                │              │              │              │            │             │            │            ├─────────────┼──────────▶│
  │                │              │              │ {Status: INVENTORY_REVERTED}            │            │            │             │           │
  │                │              │              │◀─────────────┼────────────┼─────────────┼────────────┼────────────┤             │           │
  │                │              │              │ LambdaInvoke │            │             │            │            │             │           │
  │                │              │              ├──────────────┼────────────┼─────────────┼────────────┼────────────┼────────────▶│           │
  │                │              │              │              │            │             │            │            │             │ DeleteItem│
  │                │              │              │              │            │             │            │            │             ├──────────▶│
  │                │              │              │ {Status: ORDER_REMOVED}   │             │            │            │             │           │
  │                │              │              │◀─────────────┼────────────┼─────────────┼────────────┼────────────┼─────────────┤           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ ──▶ FailState│            │             │            │            │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ [If UpdateInventory returns ERROR]      │            │            │             │           │
  │                │              │              │ Wait(30s) ──▶ RevertInventory ──▶ RemoveOrder ──▶ FailState      │             │           │
  │                │              │              │              │            │             │            │            │             │           │
  │                │              │              │ [If PlaceOrder returns ERROR]           │            │            │             │           │
  │                │              │              │ ──▶ FailState│            │             │            │            │             │           │
```

---

## 4. Functional Requirements

### 4.1 Order Management (FR-ORD)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-ORD-01 | The system shall accept order requests containing ItemId, CustomerId, MessageId, and FailAtStage fields via REST API | High |
| FR-ORD-02 | The system shall create a new order record in the DynamoDB Orders table with ItemId (partition key), CustomerId (sort key), and Status attributes | High |
| FR-ORD-03 | The system shall set the order status to "ORDER_PLACED" upon successful order creation | High |
| FR-ORD-04 | The system shall support order removal (compensating transaction) by deleting the order record from the Orders table using ItemId and CustomerId | High |
| FR-ORD-05 | The system shall set the status to "ORDER_REMOVED" after successful order deletion | High |
| FR-ORD-06 | The system shall return all old attributes when deleting an order (ReturnValues: ALL_OLD) for audit logging | Medium |

### 4.2 Inventory Management (FR-INV)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-INV-01 | The system shall decrement the ItemsInStock attribute by 1 for the specified ItemId when updating inventory | High |
| FR-INV-02 | The system shall set the status to "INVENTORY_UPDATED" upon successful inventory decrement | High |
| FR-INV-03 | The system shall support inventory reversal (compensating transaction) by incrementing ItemsInStock by 1 | High |
| FR-INV-04 | The system shall set the status to "INVENTORY_REVERTED" upon successful inventory increment | High |
| FR-INV-05 | The system shall set the status to "ERROR" if inventory update or reversal operations fail with an exception | High |
| FR-INV-06 | The system shall log errors including message and stack trace when inventory operations fail | Medium |
| FR-INV-07 | The Inventory table shall store items with attributes: ItemId, ItemName, ItemsInStock (numeric), ItemStatus | High |

### 4.3 Payment Processing (FR-PAY)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-PAY-01 | The system shall simulate payment processing and set the status to "PAYMENT_COMPLETED" upon success | High |
| FR-PAY-02 | The system shall support payment reversal (compensating transaction) by setting status to "PAYMENT_REVERTED" | High |
| FR-PAY-03 | Payment processing shall evaluate the FailAtStage field to support fault injection testing | Medium |

### 4.4 Saga Orchestration (FR-SAGA)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-SAGA-01 | The system shall orchestrate the distributed transaction using AWS Step Functions as the central coordinator | High |
| FR-SAGA-02 | The forward transaction flow shall execute in order: PlaceOrder → UpdateInventory → MakePayment | High |
| FR-SAGA-03 | Upon successful completion of all forward transactions, the state machine shall transition to SuccessState | High |
| FR-SAGA-04 | The system shall use Choice states to evaluate the Status field after each saga step | High |
| FR-SAGA-05 | Each saga step Lambda shall be invoked with PayloadResponseOnly mode (no metadata wrapping) | Medium |
| FR-SAGA-06 | Retry on service exceptions shall be disabled for all saga step invocations | Medium |

### 4.5 Error Handling and Compensation (FR-ERR)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-ERR-01 | If PlaceOrder fails (Status=ERROR), the state machine shall transition directly to FailState | High |
| FR-ERR-02 | If UpdateInventory fails (Status=ERROR), the state machine shall wait 30 seconds, then execute RevertInventory → RemoveOrder → FailState | High |
| FR-ERR-03 | If MakePayment fails (Status=ERROR), the state machine shall execute RevertPayment → RevertInventory → RemoveOrder → FailState | High |
| FR-ERR-04 | Compensating transactions shall execute in reverse order to ensure data consistency | High |
| FR-ERR-05 | The 30-second wait state shall be applied before compensating inventory updates to allow for eventual consistency | Medium |

### 4.6 Fault Injection Testing (FR-FIT)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-FIT-01 | The system shall support fault injection through the FailAtStage parameter in the request | Medium |
| FR-FIT-02 | Valid FailAtStage values shall be: PlaceOrder, MakePayment, UpdateInventory, SendNotification, None | Medium |
| FR-FIT-03 | When FailAtStage matches the current stage, the Lambda shall set Status to "ERROR" to trigger compensating transactions | Medium |

### 4.7 Notification (FR-NOT)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-NOT-01 | The system shall support sending notifications via the SendNotificationLambda | Low |
| FR-NOT-02 | SendNotificationLambda shall return a SendNotificationResult with Status="SUCCESS" | Low |
| FR-NOT-03 | Note: SendNotificationLambda is not currently wired into the Step Functions workflow but is deployed as a standalone function | Low |

---

## 5. Key Non-Functional Requirements

### 5.1 Scalability (NFR-SCA)

| ID | Requirement |
|----|-------------|
| NFR-SCA-01 | The system shall be fully serverless, leveraging AWS Lambda auto-scaling capabilities |
| NFR-SCA-02 | DynamoDB tables shall use on-demand capacity mode (no provisioned throughput limits) |
| NFR-SCA-03 | Each Lambda function shall be allocated 256 MB memory with 300-second timeout (CDK-configured) |

### 5.2 Data Consistency (NFR-CON)

| ID | Requirement |
|----|-------------|
| NFR-CON-01 | The system shall maintain data consistency across Orders and Inventory tables through the saga orchestration pattern |
| NFR-CON-02 | Compensating transactions shall restore data to a consistent state upon any step failure |
| NFR-CON-03 | DynamoDB UpdateItem operations shall use expression-based updates for atomic modifications |

### 5.3 Observability (NFR-OBS)

| ID | Requirement |
|----|-------------|
| NFR-OBS-01 | AWS X-Ray distributed tracing shall be enabled for InvokeOrchestratorLambda and the Step Functions state machine |
| NFR-OBS-02 | All Lambda functions shall log execution details through ILambdaContext.Logger (CloudWatch Logs) |
| NFR-OBS-03 | CloudWatch Logs full access shall be granted to all Lambda and Step Functions roles |

### 5.4 Availability (NFR-AVA)

| ID | Requirement |
|----|-------------|
| NFR-AVA-01 | The system shall leverage AWS managed services (Lambda, Step Functions, DynamoDB, API Gateway) for high availability |
| NFR-AVA-02 | Step Functions shall use Standard workflow type for exactly-once execution semantics |

### 5.5 Deployment (NFR-DEP)

| ID | Requirement |
|----|-------------|
| NFR-DEP-01 | Infrastructure shall be defined and deployable using AWS CDK (C#) |
| NFR-DEP-02 | Lambda functions shall be packaged as ZIP files and deployed via CDK |
| NFR-DEP-03 | All resources shall be configured with RemovalPolicy.DESTROY for clean teardown |

---

## 6. Business Rules and Constraints

### 6.1 Business Rules

| ID | Rule |
|----|------|
| BR-01 | An order must be placed before inventory can be updated |
| BR-02 | Inventory must be updated before payment can be processed |
| BR-03 | If payment fails, inventory must be reverted and order must be removed |
| BR-04 | If inventory update fails, a 30-second wait is imposed before compensating, then inventory is reverted and order is removed |
| BR-05 | If order placement fails, no compensating transactions are needed (immediate fail) |
| BR-06 | Inventory decrement and increment operations use atomic DynamoDB expressions (ItemsInStock = ItemsInStock ± 1) |
| BR-07 | Each order is uniquely identified by the combination of ItemId and CustomerId |
| BR-08 | The FailAtStage mechanism allows testing specific failure scenarios without code changes |

### 6.2 Status State Machine

```
  ┌──────────┐     ┌──────────────────┐     ┌─────────────────────┐     ┌──────────┐
  │          │     │                  │     │                     │     │          │
  │  START   │────▶│  ORDER_PLACED    │────▶│  INVENTORY_UPDATED  │────▶│ PAYMENT_ │
  │          │     │                  │     │                     │     │COMPLETED │
  └──────────┘     └───────┬──────────┘     └──────────┬──────────┘     └────┬─────┘
                           │                           │                     │
                     [ERROR]│                     [ERROR]│               [ERROR]│
                           │                           │                     │
                           ▼                           ▼                     ▼
                   ┌──────────┐              ┌──────────────┐      ┌──────────────┐
                   │          │              │  WAIT 30s    │      │  PAYMENT_    │
                   │   FAIL   │              └──────┬───────┘      │  REVERTED    │
                   │          │                     │              └──────┬───────┘
                   └──────────┘                     ▼                     │
                                            ┌──────────────┐             │
                                            │  INVENTORY_  │◀────────────┘
                                            │  REVERTED    │
                                            └──────┬───────┘
                                                   │
                                                   ▼
                                            ┌──────────────┐
                                            │  ORDER_      │
                                            │  REMOVED     │
                                            └──────┬───────┘
                                                   │
                                                   ▼
                                            ┌──────────────┐
                                            │    FAIL      │
                                            └──────────────┘
```

---

## 7. Integration Requirements

### 7.1 API Gateway Integration

| ID | Requirement |
|----|-------------|
| IR-API-01 | REST API shall accept ANY method on the root resource (/) |
| IR-API-02 | Request body shall be mapped using VTL template extracting: ItemId, CustomerId, MessageId, FailAtStage |
| IR-API-03 | OPTIONS method shall be configured as mock integration for CORS preflight support |
| IR-API-04 | Method response shall return HTTP 200 with empty model |

### 7.2 Step Functions Integration

| ID | Requirement |
|----|-------------|
| IR-SF-01 | InvokeOrchestratorLambda shall construct the Step Functions state machine ARN dynamically from the Lambda function ARN (extracting account ID and region) |
| IR-SF-02 | The state machine shall be of type STANDARD |
| IR-SF-03 | State machine input shall be the serialized OrderDetails JSON |

### 7.3 DynamoDB Integration

| ID | Requirement |
|----|-------------|
| IR-DDB-01 | Lambda functions shall use the AWS SDK for .NET (AWSSDK.DynamoDBv2) to interact with DynamoDB |
| IR-DDB-02 | AmazonDynamoDBClient shall be instantiated as a static field for connection reuse across invocations |
| IR-DDB-03 | Operations used: PutItem (create order), UpdateItem with expressions (update inventory), DeleteItem (remove order) |

### 7.4 Request/Response Contract

**Request Body:**
```json
{
  "ItemId": "string (required)",
  "CustomerId": "string (required)",
  "MessageId": "string (optional)",
  "FailAtStage": "string (optional, values: None|PlaceOrder|MakePayment|UpdateInventory|SendNotification)"
}
```

**API Response:**
```json
{
  "statusCode": 200,
  "body": "Processed",
  "headers": {
    "Content-Type": "application/json",
    "Access-Control-Allow-Origin": "*"
  }
}
```

---

## 8. Assumptions and Constraints

### 8.1 Assumptions

| ID | Assumption |
|----|------------|
| AS-01 | The Inventory DynamoDB table is pre-populated with item records before the workflow is triggered |
| AS-02 | ItemId values in the request correspond to existing items in the Inventory table |
| AS-03 | Payment processing is simulated (no actual payment gateway integration) |
| AS-04 | A single item is ordered per transaction (stock decremented/incremented by 1) |
| AS-05 | The AWS account has sufficient Lambda concurrency limits for parallel executions |
| AS-06 | Network connectivity to DynamoDB and Step Functions is available from the Lambda execution environment |

### 8.2 Constraints

| ID | Constraint |
|----|------------|
| CO-01 | .NET 6.0 runtime is required (reaching end of support consideration) |
| CO-02 | All Lambda functions share the same IAM role (LambdaExecutionRole) with broad permissions |
| CO-03 | DynamoDB tables are created with RemovalPolicy.DESTROY and will be deleted on stack deletion |
| CO-04 | No authentication/authorization is configured on the API Gateway endpoint |
| CO-05 | CORS is configured with wildcard origin (`Access-Control-Allow-Origin: *`) |
| CO-06 | The buildspec.yml references .NET 3.1 runtime for CodeBuild, which is inconsistent with the .NET 6.0 target framework |
| CO-07 | MySql.Data dependency exists in RemoveOrderLambda but is not actively used in the code |

---

## 9. Glossary

| Term | Definition |
|------|------------|
| **Saga Pattern** | A design pattern for managing distributed transactions across multiple services through a sequence of local transactions, each with a compensating transaction for rollback |
| **Saga Orchestration** | A saga implementation where a central coordinator (orchestrator) manages the sequence of transactions and compensations |
| **Compensating Transaction** | A transaction that reverses the effects of a previous transaction to restore data consistency upon failure |
| **Forward Transaction** | A transaction in the saga's happy path that progresses toward the final goal (PlaceOrder, UpdateInventory, MakePayment) |
| **Step Functions** | AWS service for building serverless workflows using state machines to coordinate distributed components |
| **State Machine** | A computational model defining states, transitions, and conditions for orchestrating workflows |
| **Choice State** | A Step Functions state type that evaluates conditions and branches the workflow accordingly |
| **Wait State** | A Step Functions state type that pauses execution for a specified duration |
| **Fault Injection** | A testing technique where failures are deliberately introduced to validate system resilience and error handling |
| **FailAtStage** | A request parameter that enables fault injection by specifying which stage should simulate an error |
| **OrderDetails** | The data transfer object (DTO) containing ItemId, CustomerId, MessageId, FailAtStage, and Status fields |
| **DynamoDB** | Amazon's fully managed NoSQL database service used for storing orders and inventory |
| **Lambda** | AWS serverless compute service that runs code in response to events without provisioning servers |
| **API Gateway** | AWS service for creating, publishing, and managing REST APIs |
| **CDK (Cloud Development Kit)** | AWS framework for defining cloud infrastructure using familiar programming languages |
| **VTL (Velocity Template Language)** | A template language used in API Gateway for request/response transformation |
| **2PC (Two-Phase Commit)** | A distributed transaction protocol that the saga pattern replaces for better scalability |
| **ACID** | Atomicity, Consistency, Isolation, Durability - properties that the saga pattern preserves at the eventual consistency level |
| **X-Ray** | AWS distributed tracing service for analyzing and debugging distributed applications |
| **CORS** | Cross-Origin Resource Sharing - a security feature allowing controlled access from different origins |

---

*Document generated by reverse engineering the saga-orchestration-netcore-blog application codebase.*
