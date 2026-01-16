# Code Review Report: Stickerlandia Print Service

**Review Date:** January 16, 2026
**Reviewer:** Senior Fullstack Code Reviewer
**Target Framework:** .NET 10
**Architecture:** Ports and Adapters (Hexagonal) with CQRS

---

## Executive Summary

The Stickerlandia Print Service is a well-architected .NET 10 application implementing the ports and adapters (hexagonal) pattern with platform adaptability. The codebase demonstrates strong adherence to SOLID principles, proper separation of concerns, and modern .NET best practices including nullable reference types, primary constructors, and records.

**Overall Quality Score: B+ (Good)**

The application shows excellent architectural decisions and clean code organization. However, there are several areas requiring attention, particularly around security hardening, incomplete implementations, and some performance considerations.

### Key Strengths
- Clean hexagonal architecture with clear separation between Core, Adapters, and Driving layers
- Proper CQRS implementation with commands and queries
- Comprehensive observability with OpenTelemetry instrumentation
- Well-structured unit tests with good coverage
- Modern .NET 10 features utilized effectively

### Key Concerns
- Hardcoded default JWT signing key in production code
- AWS Outbox implementation is a stub (non-functional)
- DynamoDB Scan operations used where Query could work
- Some synchronous blocking in async contexts
- CORS configured to allow all origins

---

## Critical Findings

### SEC-001: Hardcoded Default JWT Signing Key [CRITICAL]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Api/Configurations/AuthenticationExtensions.cs:127`

```csharp
var signingKey = configuration["Jwt:SigningKey"]
    ?? "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=";
```

**Issue:** A hardcoded default JWT signing key is present in the source code. If configuration is not properly set in production, this publicly visible key would be used to sign and validate tokens, allowing anyone to forge valid JWT tokens.

**Recommendation:**
- Remove the default fallback value entirely
- Throw an `InvalidOperationException` if the signing key is not configured
- Use Azure Key Vault, AWS Secrets Manager, or similar secret management

---

### IMPL-001: AWS Outbox Implementation is Non-Functional [CRITICAL]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.AWS/AwsOutboxImplementation.cs:11-28`

```csharp
public class AwsOutboxImplementation : IOutbox
{
    public Task StoreEventFor(DomainEvent domainEvent)
    {
        // Dummy implementation
        return Task.CompletedTask;
    }
    // ... other methods also return empty/no-op
}
```

**Issue:** The AWS outbox implementation is a stub that does nothing. Events stored via the outbox pattern are silently discarded, meaning domain events (PrinterRegisteredEvent, PrintJobQueuedEvent) are never actually persisted or published in AWS deployments.

**Impact:** Complete data loss for event-driven workflows in AWS environment.

**Recommendation:**
- Implement using DynamoDB or SQS for event storage
- Add integration tests to verify outbox behavior
- Consider using AWS EventBridge or SNS for event publishing

---

## High Severity Findings

### SEC-002: Overly Permissive CORS Configuration [HIGH]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Api/Program.cs:65-72`

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});
```

**Issue:** CORS is configured to allow all origins, methods, and headers. This could expose the API to cross-site request attacks and data exfiltration.

**Recommendation:**
- Define specific allowed origins based on deployment environment
- Use environment-specific CORS policies
- At minimum, restrict methods to only those needed (GET, POST)

---

### PERF-001: DynamoDB Scan Operations Used Instead of Query [HIGH]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.AWS/DynamoDbPrinterRepository.cs:47-68` and `:167-188`

```csharp
public async Task<Printer?> GetPrinterByIdAsync(Guid printerId)
{
    var request = new ScanRequest
    {
        TableName = _tableName,
        FilterExpression = "SK = :printerId",
        // ...
    };
    var response = await dynamoDbClient.ScanAsync(request);
    // ...
}
```

**Issue:** Multiple methods use `ScanAsync` to find items by SK (sort key), which scans the entire table. This is O(n) and will become increasingly expensive as data grows.

**Recommendation:**
- Create a GSI on the sort key if direct lookups are needed
- Restructure the data model to support query patterns
- Consider using a composite key pattern to avoid scans

---

### SEC-003: Certificate Validation Disabled in Non-HTTPS Mode [HIGH]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Api/Configurations/AuthenticationExtensions.cs:86-89`

```csharp
httpHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
```

**Issue:** When `RequireHttpsMetadata` is false, certificate validation is completely bypassed. While intended for development, this code path could accidentally be enabled in production.

**Recommendation:**
- Add warning logs when this code path is taken
- Add compile-time or startup checks for production environment
- Consider using `#if DEBUG` guards

---

### ARCH-001: Inconsistent Naming Between Service Name and Project [HIGH]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Core/ServiceExtensions.cs:22`

```csharp
public static IServiceCollection AddStickerlandiaUserManagement(this IServiceCollection services)
```

**Issue:** The method name references "UserManagement" but this is the PrintService project. This naming inconsistency appears in multiple places (e.g., log messages referencing "UserManagement API").

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Api/Program.cs:216`

```csharp
logger.Information("UserManagement API started on {Urls}", urls);
```

**Recommendation:**
- Rename to `AddStickerlandiaPrintService`
- Update all log messages to reference PrintService
- Search codebase for "UserManagement" references

---

## Medium Severity Findings

### PERF-002: Synchronous Blocking in Async Context [MEDIUM]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Agnostic/ServiceExtensions.cs:87`

```csharp
Thread.Sleep(retryDelay);
```

**Issue:** Using `Thread.Sleep` in code that will run on the ASP.NET thread pool blocks threads unnecessarily. While this is in startup code, it sets a poor pattern.

**Recommendation:**
- Move Kafka connection validation to a health check
- Use `await Task.Delay()` if synchronous blocking is truly needed during startup
- Consider using `IHostedService` for background initialization

---

### PERF-003: GetByIdAsync Uses Scan in PrintJobRepository [MEDIUM]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.AWS/DynamoDbPrintJobRepository.cs:45-68`

```csharp
public async Task<PrintJob?> GetByIdAsync(string printJobId)
{
    // We need to scan since we don't know the PrinterId
    var request = new ScanRequest
    {
        TableName = _tableName,
        FilterExpression = "PrintJobId = :printJobId",
        // ...
    };
}
```

**Issue:** Getting a print job by ID requires a full table scan because the PrinterId is not known.

**Recommendation:**
- Add a GSI with PrintJobId as the partition key
- Or include PrinterId in the API request
- Consider caching frequently accessed jobs

---

### DESIGN-001: Missing Input Validation at Endpoint Level [MEDIUM]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Api/RegisterPrinterEndpoint.cs:16-25`

```csharp
public static async Task<IResult> HandleAsync(
    string eventName,
    HttpContext context,
    [FromServices] RegisterPrinterCommandHandler updateHandler,
    [FromBody] RegisterPrinterCommand request)
{
    var response = await updateHandler.Handle(request);
    return Results.Created($"/api/print/v1/event/{eventName}", ...);
}
```

**Issue:** The `eventName` route parameter is not used - the command contains its own event name. This could lead to confusion and URL mismatch issues.

**Recommendation:**
- Either use the route parameter `eventName` in the command
- Or validate that `request.EventName` matches the route parameter
- Document the intended behavior

---

### SEC-004: API Key Generation Uses Guid [MEDIUM]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Core/Printer.cs:62`

```csharp
Key = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
```

**Issue:** Using `Guid.NewGuid()` for API key generation provides only 122 bits of randomness and uses a predictable algorithm. While adequate for most purposes, cryptographically secure random bytes are preferred for API keys.

**Recommendation:**
- Use `RandomNumberGenerator.GetBytes()` for cryptographic randomness
- Consider using a longer key (256 bits minimum)
- Example: `Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`

---

### ERR-001: Silent Failure in UpdateAsync [MEDIUM]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Agnostic/Repositories/PostgresPrinterRepository.cs:104-125`

```csharp
public async Task UpdateAsync(Printer printer)
{
    var entity = await dbContext.Printers
        .FirstOrDefaultAsync(p => p.PrinterId == printer.Id.Value);

    if (entity is null)
    {
        return; // Silent failure
    }
    // ... update logic
}
```

**Issue:** If the printer doesn't exist, the update silently does nothing. This could mask bugs where updates are attempted on non-existent entities.

**Recommendation:**
- Throw `PrinterNotFoundException` if entity is not found
- Or return a boolean indicating success
- Add logging for the not-found case

---

### TEST-001: Missing Tests for API Key Authentication [MEDIUM]

**Directory:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/tests/`

**Issue:** While JWT authentication is tested via integration tests, there are no dedicated tests for the `PrinterKeyAuthenticationHandler` custom authentication scheme.

**Recommendation:**
- Add unit tests for `PrinterKeyAuthenticationHandler`
- Test cases: valid key, invalid key, missing header, empty header
- Test claim extraction

---

## Low Severity Findings

### CODE-001: Duplicate License Headers [LOW]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Core/Printer.cs:1-9`

```csharp
/*
 * Unless explicitly stated otherwise all files in this repository...
 */

// Unless explicitly stated otherwise all files in this repository...
```

**Issue:** Multiple files have duplicate license headers (both block comment and line comment).

**Recommendation:**
- Standardize on one license header format
- Use EditorConfig or similar to enforce consistency

---

### CODE-002: Inconsistent ConfigureAwait Usage [LOW]

**Issue:** Some async calls use `.ConfigureAwait(false)` while others don't. The AWS implementations consistently use it, but the Postgres implementations are inconsistent.

**Recommendation:**
- For library code, use `.ConfigureAwait(false)` consistently
- Consider using `await using` patterns where appropriate

---

### CODE-003: Magic Strings for Event Types [LOW]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Core/Outbox/OutboxProcessor.cs:62`

```csharp
case "users.userRegistered.v1":
```

**Issue:** Event type strings are hardcoded. This is also inconsistent with the PrintService naming.

**Recommendation:**
- Use constants or the `EventName` property from the event class
- Example: `case nameof(PrinterRegisteredEvent):`
- Rename to `printers.printerRegistered.v1`

---

### DOC-001: Missing XML Documentation [LOW]

**Issue:** Several public interfaces and classes lack XML documentation, particularly in the repository interfaces.

**Files affected:**
- `IPrinterRepository.cs` - partial documentation
- `IPrintJobRepository.cs` - no documentation
- `IPrinterKeyValidator.cs` - no documentation

**Recommendation:**
- Add XML documentation to all public APIs
- Enable documentation generation in build

---

### PERF-004: HttpClient Disposal in IssuerSigningKeyResolver [LOW]

**File:** `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Api/Configurations/AuthenticationExtensions.cs:85-101`

```csharp
using var httpHandler = new HttpClientHandler();
// ...
using var httpClient = new HttpClient(httpHandler);
// ...
options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
{
    var jwksResponse = httpClient.GetStringAsync(new Uri(internalJwksUrl)).GetAwaiter().GetResult();
    // ...
};
```

**Issue:** The `httpClient` is disposed after the lambda is created, but the lambda captures it and uses it later. This will cause `ObjectDisposedException` at runtime.

**Recommendation:**
- Remove the `using` statement for the HttpClient
- Or use `IHttpClientFactory` pattern
- Consider caching the JWKS response

---

## Positive Observations

### Well-Implemented Patterns

1. **Domain-Driven Design Elements**: The `Printer` and `PrintJob` entities properly encapsulate domain logic with state transitions and domain event generation.

2. **Outbox Pattern**: The implementation of the transactional outbox pattern ensures reliable event publishing (when properly implemented).

3. **CQRS Separation**: Commands (`RegisterPrinterCommand`, `SubmitPrintJobCommand`) and Queries (`GetPrintersForEventQuery`) are properly separated with dedicated handlers.

4. **Observability**: Comprehensive instrumentation with OpenTelemetry metrics and Datadog tracing integration:
   - `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Core/Observability/PrintJobInstrumentation.cs`

5. **Test Quality**: Unit tests use FakeItEasy for mocking and follow arrange-act-assert pattern with descriptive test names:
   - `/Users/james.eastham/source/datadog/stickerlandia/print-service/tests/Stickerlandia.PrintService.UnitTest/PrintJobTests/SubmitPrintJobCommandHandlerTests.cs`

6. **Platform Adaptability**: Clean separation allows easy swapping between AWS (DynamoDB, SNS), PostgreSQL, and Kafka implementations.

7. **Optimistic Concurrency**: Print job claiming uses optimistic concurrency control to prevent duplicate processing:
   - `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.AWS/DynamoDbPrintJobRepository.cs:126-161`

8. **Rate Limiting**: API includes rate limiting configured at 60 requests per minute:
   - `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Api/Program.cs:42-56`

9. **Global Exception Handling**: Comprehensive exception handling middleware with proper HTTP status code mapping:
   - `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Api/Middlewares/GlobalExceptionHandler.cs`

10. **Retry Policies**: Client application uses Polly for resilient HTTP calls with exponential backoff:
    - `/Users/james.eastham/source/datadog/stickerlandia/print-service/src/Stickerlandia.PrintService.Client/Program.cs:38-48`

---

## Recommendations Summary

### Immediate Actions (Before Production)

1. **Remove hardcoded JWT signing key** and require configuration
2. **Implement AWS Outbox** properly or disable AWS adapter
3. **Fix CORS configuration** to restrict allowed origins
4. **Fix HttpClient disposal** in IssuerSigningKeyResolver

### Short-Term Improvements

5. Add GSI for PrintJobId lookups in DynamoDB
6. Replace DynamoDB Scan operations with Query
7. Rename UserManagement references to PrintService
8. Add validation for route parameters vs request body

### Long-Term Enhancements

9. Implement comprehensive API key authentication tests
10. Add XML documentation to all public APIs
11. Standardize ConfigureAwait usage
12. Consider moving to asymmetric JWT signing for better security

---

## Appendix: Files Reviewed

### Core Domain Layer
- `src/Stickerlandia.PrintService.Core/Printer.cs`
- `src/Stickerlandia.PrintService.Core/PrintJobs/PrintJob.cs`
- `src/Stickerlandia.PrintService.Core/PrintJobs/SubmitPrintJobCommand.cs`
- `src/Stickerlandia.PrintService.Core/PrintJobs/SubmitPrintJobCommandHandler.cs`
- `src/Stickerlandia.PrintService.Core/RegisterPrinter/RegisterPrinterCommand.cs`
- `src/Stickerlandia.PrintService.Core/RegisterPrinter/RegisterPrinterCommandHandler.cs`
- `src/Stickerlandia.PrintService.Core/Outbox/OutboxProcessor.cs`
- `src/Stickerlandia.PrintService.Core/Observability/PrintJobInstrumentation.cs`

### API Layer
- `src/Stickerlandia.PrintService.Api/Program.cs`
- `src/Stickerlandia.PrintService.Api/Configurations/AuthenticationExtensions.cs`
- `src/Stickerlandia.PrintService.Api/Configurations/PrinterKeyAuthenticationHandler.cs`
- `src/Stickerlandia.PrintService.Api/Middlewares/GlobalExceptionHandler.cs`
- `src/Stickerlandia.PrintService.Api/RegisterPrinterEndpoint.cs`
- `src/Stickerlandia.PrintService.Api/SubmitPrintJobEndpoint.cs`
- `src/Stickerlandia.PrintService.Api/PollPrintJobsEndpoint.cs`
- `src/Stickerlandia.PrintService.Api/AcknowledgePrintJobEndpoint.cs`

### AWS Implementations
- `src/Stickerlandia.PrintService.AWS/DynamoDbPrinterRepository.cs`
- `src/Stickerlandia.PrintService.AWS/DynamoDbPrintJobRepository.cs`
- `src/Stickerlandia.PrintService.AWS/SnsEventPublisher.cs`
- `src/Stickerlandia.PrintService.AWS/AwsOutboxImplementation.cs`
- `src/Stickerlandia.PrintService.AWS/ServiceExtensions.cs`

### Agnostic Implementations
- `src/Stickerlandia.PrintService.Agnostic/Repositories/PostgresPrinterRepository.cs`
- `src/Stickerlandia.PrintService.Agnostic/Repositories/PostgresPrintJobRepository.cs`
- `src/Stickerlandia.PrintService.Agnostic/Data/PrintServiceDbContext.cs`
- `src/Stickerlandia.PrintService.Agnostic/ServiceExtensions.cs`

### Client Application
- `src/Stickerlandia.PrintService.Client/Program.cs`
- `src/Stickerlandia.PrintService.Client/Services/PrintServiceApiClient.cs`
- `src/Stickerlandia.PrintService.Client/Services/PrintJobPollingService.cs`
- `src/Stickerlandia.PrintService.Client/Configuration/ConfigurationService.cs`

### Tests
- `tests/Stickerlandia.PrintService.UnitTest/PrintJobTests/SubmitPrintJobCommandHandlerTests.cs`
- `tests/Stickerlandia.PrintService.UnitTest/PrinterTests/RegisterPrinterCommandHandlerTests.cs`
- `tests/Stickerlandia.PrintService.IntegrationTest/PrinterTests.cs`
- `tests/Stickerlandia.PrintService.IntegrationTest/PrintJobTests.cs`
- `tests/Stickerlandia.PrintService.IntegrationTest/Hooks/TestSetupFixture.cs`
