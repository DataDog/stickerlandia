# Print Service Design Document

## Overview

This document outlines the design for the complete print service functionality, consisting of:

1. **Backend API** - Server-side service for printer management and print job orchestration
2. **Printer Client** - Client-side application running on local machines with printers

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              BACKEND (Cloud)                                │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────┐  │
│  │   Admin API     │    │  Print Job API  │    │   Printer Poll API      │  │
│  │  (JWT Auth)     │    │   (JWT Auth)    │    │   (API Key Auth)        │  │
│  │                 │    │                 │    │                         │  │
│  │ POST /printer   │    │ POST /print-job │    │ GET  /printer/jobs      │  │
│  │ GET  /printers  │    │                 │    │ POST /printer/jobs/ack  │  │
│  └────────┬────────┘    └────────┬────────┘    └───────────┬─────────────┘  │
│           │                      │                         │                │
│           ▼                      ▼                         ▼                │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                         DOMAIN LAYER                                    ││
│  │  ┌──────────┐  ┌──────────┐  ┌─────────────────────────────────────┐   ││
│  │  │ Printer  │  │ PrintJob │  │ Commands / Queries / Event Handlers │   ││
│  │  └──────────┘  └──────────┘  └─────────────────────────────────────┘   ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│           │                      │                         │                │
│           ▼                      ▼                         ▼                │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                      PERSISTENCE LAYER                                  ││
│  │         Printers Table              │         PrintJobs Table           ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ HTTPS (API Key Auth)
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PRINTER CLIENT (Local Machine)                      │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────┐  │
│  │   Config UI     │    │  Poll Service   │    │   Local Storage         │  │
│  │   (Web App)     │    │  (Background)   │    │   (File System)         │  │
│  │                 │    │                 │    │                         │  │
│  │ - Set API Key   │    │ - Poll backend  │    │ - print-jobs.json       │  │
│  │ - View status   │    │ - Process jobs  │    │ - config.json           │  │
│  │ - View history  │    │ - Ack complete  │    │                         │  │
│  └─────────────────┘    └─────────────────┘    └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Part 1: Backend API

### 1.1 Domain Model: PrintJob

#### Entity Design

```
PrintJob (Aggregate Root)
├── PrintJobId        : Value Object (GUID-based unique identifier)
├── PrinterId         : Value Object (reference to printer)
├── UserId            : string (user who requested the print)
├── StickerId         : string (sticker to be printed)
├── StickerUrl        : string (URL to fetch sticker image)
├── Status            : Enum (Queued, Processing, Completed, Failed)
├── CreatedAt         : DateTimeOffset
├── ProcessedAt       : DateTimeOffset? (when picked up by printer)
├── CompletedAt       : DateTimeOffset? (when acknowledged complete)
└── FailureReason     : string? (if status is Failed)
```

#### PrintJob Status Lifecycle

```
    ┌─────────┐
    │ Queued  │ ◄── Initial state when job submitted
    └────┬────┘
         │
         │ Printer polls and retrieves job
         ▼
    ┌────────────┐
    │ Processing │ ◄── Job has been sent to a printer
    └─────┬──────┘
          │
          ├──────────────────────┐
          │                      │
          ▼                      ▼
    ┌───────────┐          ┌─────────┐
    │ Completed │          │ Failed  │
    └───────────┘          └─────────┘
```

#### Value Objects

**PrintJobId**
- Format: GUID
- Generated on job creation
- Immutable

#### Domain Events

| Event | Trigger | Data |
|-------|---------|------|
| `PrintJobQueuedEvent` | Job submitted | PrintJobId, PrinterId, UserId, StickerId |
| `PrintJobProcessingEvent` | Printer picks up job | PrintJobId, PrinterId |
| `PrintJobCompletedEvent` | Printer acknowledges completion | PrintJobId, PrinterId, CompletedAt |
| `PrintJobFailedEvent` | Job fails | PrintJobId, PrinterId, FailureReason |

---

### 1.2 Commands

#### SubmitPrintJobCommand

**Purpose:** Submit a new print job to a printer's queue

**Input:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| EventName | string | Yes | Event the printer belongs to |
| PrinterName | string | Yes | Target printer name |
| UserId | string | Yes | User requesting the print |
| StickerId | string | Yes | Sticker identifier |
| StickerUrl | string | Yes | URL to fetch sticker image |

**Validation Rules:**
- EventName must not be empty
- PrinterName must not be empty
- UserId must not be empty
- StickerId must not be empty
- StickerUrl must be a valid URL
- Printer must exist for the given EventName/PrinterName combination

**Handler Logic:**
1. Validate command
2. Resolve PrinterId from EventName + PrinterName
3. Verify printer exists (throw `PrinterNotFoundException` if not)
4. Create PrintJob via factory method
5. Store PrintJob in repository
6. Store `PrintJobQueuedEvent` in outbox
7. Return PrintJobId

**Output:**
| Field | Type | Description |
|-------|------|-------------|
| PrintJobId | string | The created job identifier |

---

#### AcknowledgePrintJobCommand

**Purpose:** Mark a print job as completed (or failed) by the printer

**Input:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| PrintJobId | string | Yes | Job to acknowledge |
| Success | bool | Yes | Whether job completed successfully |
| FailureReason | string | No | Reason if Success=false |

**Validation Rules:**
- PrintJobId must not be empty
- PrintJob must exist
- PrintJob must be in Processing status
- If Success=false, FailureReason should be provided

**Handler Logic:**
1. Validate command
2. Load PrintJob from repository
3. Verify current status is Processing
4. Call `PrintJob.Complete()` or `PrintJob.Fail(reason)`
5. Update PrintJob in repository
6. Store appropriate event in outbox

**Output:**
| Field | Type | Description |
|-------|------|-------------|
| Acknowledged | bool | Confirmation of acknowledgment |

---

### 1.3 Queries

#### GetPrintJobsForPrinterQuery

**Purpose:** Retrieve queued print jobs for a specific printer (used by printer polling)

**Input:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| PrinterId | string | Yes | Printer requesting jobs (derived from API key) |
| MaxJobs | int | No | Maximum jobs to return (default: 10) |

**Handler Logic:**
1. Validate PrinterId
2. Query repository for jobs with Status=Queued and matching PrinterId
3. Order by CreatedAt ascending (FIFO)
4. Limit to MaxJobs
5. Mark returned jobs as Processing (atomic operation)
6. Return job list

**Output:**
| Field | Type | Description |
|-------|------|-------------|
| Jobs | List<PrintJobDTO> | List of print jobs to process |

**PrintJobDTO:**
```
{
  "printJobId": "guid",
  "userId": "string",
  "stickerId": "string",
  "stickerUrl": "string",
  "createdAt": "datetime"
}
```

---

### 1.4 API Endpoints

#### Existing Endpoints (No Changes)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/print/v1/event/{eventName}` | JWT (admin) | Register printer |
| GET | `/api/print/v1/event/{eventName}` | JWT (admin, user) | List printers |

#### New Endpoints

##### Submit Print Job

```
POST /api/print/v1/event/{eventName}/printer/{printerName}/jobs
```

| Aspect | Details |
|--------|---------|
| **Auth** | JWT Bearer Token (user or admin role) |
| **Request Body** | `{ "userId": "string", "stickerId": "string", "stickerUrl": "string" }` |
| **Success Response** | `201 Created` with `{ "printJobId": "guid" }` |
| **Error Responses** | `400 Bad Request` - Invalid input |
| | `404 Not Found` - Printer not found |
| | `401 Unauthorized` - Missing/invalid token |

##### Poll Print Jobs (Printer Client)

```
GET /api/print/v1/printer/jobs
```

| Aspect | Details |
|--------|---------|
| **Auth** | API Key via `X-Printer-Key` header |
| **Query Params** | `maxJobs` (optional, default 10) |
| **Success Response** | `200 OK` with `{ "jobs": [...] }` |
| **Error Responses** | `401 Unauthorized` - Invalid API key |
| | `204 No Content` - No jobs available |

**Response Body:**
```json
{
  "jobs": [
    {
      "printJobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userId": "user123",
      "stickerId": "sticker456",
      "stickerUrl": "https://cdn.example.com/stickers/456.png",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

##### Acknowledge Print Job (Printer Client)

```
POST /api/print/v1/printer/jobs/{printJobId}/acknowledge
```

| Aspect | Details |
|--------|---------|
| **Auth** | API Key via `X-Printer-Key` header |
| **Request Body** | `{ "success": true }` or `{ "success": false, "failureReason": "string" }` |
| **Success Response** | `200 OK` with `{ "acknowledged": true }` |
| **Error Responses** | `401 Unauthorized` - Invalid API key |
| | `404 Not Found` - Job not found |
| | `409 Conflict` - Job not in Processing status |
| | `403 Forbidden` - Job belongs to different printer |

---

### 1.5 Authentication

#### Existing: JWT Bearer Authentication

Used for admin/user operations:
- Printer registration
- Printer listing
- Print job submission

No changes required.

#### New: API Key Authentication

Used for printer client operations:
- Polling for jobs
- Acknowledging job completion

**Implementation Design:**

```
┌─────────────────────────────────────────────────────────────┐
│                  API Key Auth Flow                          │
│                                                             │
│  Request                                                    │
│  ┌─────────────────────────────────┐                        │
│  │ GET /api/print/v1/printer/jobs  │                        │
│  │ X-Printer-Key: base64key...     │                        │
│  └───────────────┬─────────────────┘                        │
│                  │                                          │
│                  ▼                                          │
│  ┌─────────────────────────────────┐                        │
│  │   PrinterKeyAuthHandler         │                        │
│  │   1. Extract key from header    │                        │
│  │   2. Lookup printer by key      │                        │
│  │   3. Create ClaimsPrincipal     │                        │
│  │      - PrinterId claim          │                        │
│  │      - EventName claim          │                        │
│  └───────────────┬─────────────────┘                        │
│                  │                                          │
│                  ▼                                          │
│  ┌─────────────────────────────────┐                        │
│  │   Endpoint Handler              │                        │
│  │   - Access PrinterId from       │                        │
│  │     HttpContext.User.Claims     │                        │
│  └─────────────────────────────────┘                        │
└─────────────────────────────────────────────────────────────┘
```

**New Interface:**

```csharp
public interface IPrinterKeyValidator
{
    Task<Printer?> ValidateKeyAsync(string key);
}
```

**Auth Scheme Configuration:**
- Scheme name: `PrinterKey`
- Header: `X-Printer-Key`
- Claims populated: `PrinterId`, `EventName`, `PrinterName`

---

### 1.6 Persistence

#### New Repository Interface

```csharp
public interface IPrintJobRepository
{
    Task<PrintJob> AddAsync(PrintJob printJob);
    Task<PrintJob?> GetByIdAsync(string printJobId);
    Task<List<PrintJob>> GetQueuedJobsForPrinterAsync(string printerId, int maxJobs);
    Task UpdateAsync(PrintJob printJob);
}
```

#### AWS DynamoDB Design

**Table: PrintJobs**

| Attribute | Type | Description |
|-----------|------|-------------|
| PK | String | `PRINTER#{PrinterId}` |
| SK | String | `JOB#{PrintJobId}` |
| GSI1PK | String | `PRINTER#{PrinterId}#STATUS#{Status}` |
| GSI1SK | String | `{CreatedAt}` (ISO8601) |
| PrintJobId | String | Job identifier |
| PrinterId | String | Owning printer |
| UserId | String | Requesting user |
| StickerId | String | Sticker identifier |
| StickerUrl | String | Sticker image URL |
| Status | String | Queued/Processing/Completed/Failed |
| CreatedAt | String | ISO8601 timestamp |
| ProcessedAt | String | ISO8601 timestamp (nullable) |
| CompletedAt | String | ISO8601 timestamp (nullable) |
| FailureReason | String | Error message (nullable) |
| TTL | Number | Unix timestamp for auto-deletion (2 days after completion) |

**TTL Strategy:**
- When a job reaches `Completed` or `Failed` status, set TTL = `CompletedAt + 2 days`
- DynamoDB automatically deletes items when TTL expires
- Queued and Processing jobs do not have TTL set

**Access Patterns:**

| Pattern | Key Condition | Use Case |
|---------|---------------|----------|
| Get job by ID | PK + SK | Fetch specific job |
| Get queued jobs for printer | GSI1PK = `PRINTER#x#STATUS#Queued` | Printer polling |
| Get all jobs for printer | PK = `PRINTER#x` | Admin reporting |

**Index: GSI1 (Jobs by Status)**
- Partition Key: `GSI1PK`
- Sort Key: `GSI1SK`
- Purpose: Efficient querying of jobs by printer+status, ordered by creation time

---

### 1.7 Job Retrieval Atomicity

**Problem:** When a printer polls for jobs, we need to atomically:
1. Query for queued jobs
2. Mark them as Processing
3. Return them to the printer

**Solution: Optimistic Locking with Conditional Updates**

```
1. Query GSI1 for Queued jobs (limit N)
2. For each job:
   a. Attempt conditional update:
      - Condition: Status = Queued
      - Update: Status = Processing, ProcessedAt = now
   b. If condition fails, skip job (another printer got it)
3. Return successfully claimed jobs
```

**Alternative: Lease-based approach**
- Add `LeaseExpiry` field
- Jobs return to Queued if not acknowledged within lease period
- More complex but handles printer crashes

**Recommendation:** Start with optimistic locking. Add lease-based cleanup as enhancement if needed.

---

### 1.8 Printer Status Reporting

Printers report their online/offline status via heartbeat mechanism during polling.

#### Status Model

**Printer Entity Extension:**
```
Printer (Updated)
├── ... existing fields ...
├── Status            : Enum (Online, Offline)
├── LastHeartbeat     : DateTimeOffset?
└── LastJobProcessed  : DateTimeOffset?
```

**Status Enum:**
| Status | Description |
|--------|-------------|
| Online | Printer has sent heartbeat within last 2 minutes |
| Offline | No heartbeat received for > 2 minutes |

#### Heartbeat Mechanism

The polling endpoint (`GET /printer/jobs`) serves dual purpose:
1. Retrieve queued jobs
2. Update printer heartbeat timestamp

**On each poll request:**
1. Validate API key, extract PrinterId
2. Update `Printer.LastHeartbeat = now`
3. If `Printer.Status != Online`, update to `Online`
4. Query and return queued jobs

#### Status Query

**New Endpoint:**
```
GET /api/print/v1/event/{eventName}/printers/status
```

| Aspect | Details |
|--------|---------|
| **Auth** | JWT Bearer Token (admin or user role) |
| **Success Response** | `200 OK` with printer status list |

**Response:**
```json
{
  "printers": [
    {
      "printerId": "EVENT1-PRINTER1",
      "printerName": "Printer1",
      "status": "Online",
      "lastHeartbeat": "2024-01-15T10:30:00Z",
      "lastJobProcessed": "2024-01-15T10:28:00Z"
    },
    {
      "printerId": "EVENT1-PRINTER2",
      "printerName": "Printer2",
      "status": "Offline",
      "lastHeartbeat": "2024-01-15T09:15:00Z",
      "lastJobProcessed": null
    }
  ]
}
```

#### Offline Detection

**Option A: Lazy Evaluation (Recommended)**
- Status computed at query time: `if (LastHeartbeat > now - 2min) → Online else Offline`
- No background process needed
- Simple implementation

**Option B: Background Worker**
- Periodic job scans for stale heartbeats
- Updates status to Offline
- Publishes `PrinterOfflineEvent`
- More complex but enables proactive alerting

**Recommendation:** Start with Option A. Add Option B if alerting is required.

#### Domain Events

| Event | Trigger | Data |
|-------|---------|------|
| `PrinterOnlineEvent` | Printer transitions to Online | PrinterId, EventName |
| `PrinterOfflineEvent` | Printer transitions to Offline | PrinterId, EventName, LastHeartbeat |

#### Persistence Updates

**Printers Table (DynamoDB) - New Attributes:**

| Attribute | Type | Description |
|-----------|------|-------------|
| LastHeartbeat | String | ISO8601 timestamp |
| LastJobProcessed | String | ISO8601 timestamp (nullable) |

**Repository Interface Update:**
```csharp
public interface IPrinterRepository
{
    // ... existing methods ...
    Task UpdateHeartbeatAsync(string printerId, DateTimeOffset heartbeat);
    Task UpdateLastJobProcessedAsync(string printerId, DateTimeOffset timestamp);
    Task<List<PrinterStatusDto>> GetPrinterStatusesForEventAsync(string eventName);
}
```

---

## Part 2: Printer Client Application

### 2.1 Application Overview

The Printer Client is a standalone application that runs on machines with physical printers. It:

1. Provides a web UI for configuration
2. Polls the backend API for print jobs
3. Stores job metadata locally
4. Acknowledges job completion

### 2.2 Project Structure

The client uses **Blazor Server** for the web UI, providing a reactive single-page application experience with server-side rendering.

```
src/
└── Stickerlandia.PrintService.Client/
    ├── Stickerlandia.PrintService.Client.csproj
    ├── Program.cs
    ├── appsettings.json
    │
    ├── Configuration/
    │   ├── PrinterClientConfig.cs
    │   ├── ConfigurationService.cs
    │   └── IConfigurationService.cs
    │
    ├── Services/
    │   ├── PrintJobPollingService.cs      (BackgroundService)
    │   ├── IPrintServiceApiClient.cs
    │   ├── PrintServiceApiClient.cs
    │   ├── ILocalStorageService.cs
    │   ├── LocalStorageService.cs
    │   └── ClientStatusService.cs         (Shared state for UI)
    │
    ├── Models/
    │   ├── PrintJobRecord.cs
    │   └── ClientStatus.cs
    │
    └── Components/
        ├── App.razor
        ├── Routes.razor
        ├── Layout/
        │   ├── MainLayout.razor
        │   └── NavMenu.razor
        └── Pages/
            ├── Home.razor                 (Dashboard/Status)
            ├── Configuration.razor        (API key setup)
            └── History.razor              (Job history)
```

**Why Blazor Server:**
- Single project for both backend services and UI
- Real-time updates via SignalR (status changes, new jobs)
- No separate frontend build process
- Shared C# models between services and UI

### 2.3 Configuration

#### Configuration Model

```csharp
public class PrinterClientConfig
{
    public string? ApiKey { get; set; }
    public string BackendUrl { get; set; } = "https://api.stickerlandia.com";
    public int PollingIntervalSeconds { get; set; } = 5;
    public int MaxJobsPerPoll { get; set; } = 10;
    public string LocalStoragePath { get; set; } = "./print-jobs";
}
```

#### Configuration Storage

- Stored in local JSON file: `~/.stickerlandia/printer-config.json`
- Loaded on startup
- Modifiable via web UI
- Changes trigger polling service restart

### 2.4 Blazor UI Pages

#### Home Page (Dashboard)

**Route:** `/`

**Purpose:** Display real-time status and summary information

**Components:**
- Connection status indicator (Online/Offline/Not Configured)
- Printer info card (PrinterId, EventName, PrinterName)
- Statistics cards:
  - Jobs processed today
  - Total jobs processed
  - Last poll time
  - Last poll result
- Recent activity feed (last 5 jobs)

**Real-time Updates:**
- Uses `ClientStatusService` (singleton) to share state
- Blazor's `StateHasChanged()` triggered on poll completion
- Auto-refresh every 5 seconds for connection status

#### Configuration Page

**Route:** `/configuration`

**Purpose:** Configure the printer client settings

**Form Fields:**
| Field | Type | Validation |
|-------|------|------------|
| API Key | Password input | Required |
| Backend URL | Text input | Required, valid URL |
| Polling Interval | Number input | 1-60 seconds |

**Behavior:**
- On save, validates API key by calling backend
- Shows success/error toast notification
- Restarts polling service with new configuration
- Masks API key after initial entry (show last 4 chars)

**Test Connection Button:**
- Calls `ValidateConnectionAsync()` on API client
- Displays printer info if successful
- Shows error message if failed

#### History Page

**Route:** `/history`

**Purpose:** Browse and search processed print jobs

**Features:**
- Paginated table of jobs (20 per page)
- Columns: Date, Job ID, User ID, Sticker ID, Status
- Filter by date range
- Filter by status (Completed/Failed)
- Click row to expand details (full URLs, timestamps)
- Export to CSV button

**Data Source:**
- Reads from local storage via `ILocalStorageService`
- Cached in memory for fast pagination

#### Shared State Service

```csharp
public class ClientStatusService
{
    public event Action? OnStatusChanged;

    public bool IsConfigured { get; private set; }
    public bool IsConnected { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? LastPollTime { get; private set; }
    public int JobsProcessedToday { get; private set; }
    public int JobsProcessedTotal { get; private set; }
    public PrinterInfoDto? PrinterInfo { get; private set; }

    public void UpdateStatus(/* params */)
    {
        // Update properties
        OnStatusChanged?.Invoke();
    }
}
```

**Registration:** `builder.Services.AddSingleton<ClientStatusService>()`

### 2.5 Polling Service

#### BackgroundService Design

```csharp
public class PrintJobPollingService : BackgroundService
{
    // Dependencies
    private readonly IPrintServiceApiClient _apiClient;
    private readonly ILocalStorageService _localStorage;
    private readonly IConfigurationService _config;
    private readonly ILogger<PrintJobPollingService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_config.IsConfigured)
            {
                // Wait for configuration
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            try
            {
                // 1. Poll for jobs
                var jobs = await _apiClient.PollJobsAsync();

                // 2. Process each job
                foreach (var job in jobs)
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during poll cycle");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_config.Current.PollingIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task ProcessJobAsync(PrintJobDto job, CancellationToken ct)
    {
        // 1. Store metadata locally
        await _localStorage.StoreJobAsync(job);

        // 2. Acknowledge completion
        await _apiClient.AcknowledgeJobAsync(job.PrintJobId, success: true);

        // 3. Update local record
        await _localStorage.MarkCompletedAsync(job.PrintJobId);
    }
}
```

#### Polling Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    Polling Loop                             │
│                                                             │
│  ┌─────────┐                                                │
│  │  Start  │                                                │
│  └────┬────┘                                                │
│       │                                                     │
│       ▼                                                     │
│  ┌─────────────┐  No   ┌─────────────┐                      │
│  │ Configured? │──────►│ Wait 5 sec  │──┐                   │
│  └──────┬──────┘       └─────────────┘  │                   │
│         │ Yes                           │                   │
│         ▼                               │                   │
│  ┌─────────────────┐                    │                   │
│  │ Poll Backend    │◄───────────────────┘                   │
│  │ GET /printer/   │                                        │
│  │     jobs        │                                        │
│  └────────┬────────┘                                        │
│           │                                                 │
│           ▼                                                 │
│  ┌─────────────────┐  No Jobs                               │
│  │  Jobs Found?    │────────────────────┐                   │
│  └────────┬────────┘                    │                   │
│           │ Yes                         │                   │
│           ▼                             │                   │
│  ┌─────────────────┐                    │                   │
│  │ For Each Job:   │                    │                   │
│  │  1. Store local │                    │                   │
│  │  2. Acknowledge │                    │                   │
│  └────────┬────────┘                    │                   │
│           │                             │                   │
│           ▼                             │                   │
│  ┌─────────────────┐                    │                   │
│  │ Wait interval   │◄───────────────────┘                   │
│  └────────┬────────┘                                        │
│           │                                                 │
│           └─────────────────► (loop back to Configured?)    │
└─────────────────────────────────────────────────────────────┘
```

### 2.6 Local Storage

#### Storage Structure

```
~/.stickerlandia/
├── printer-config.json
└── print-jobs/
    ├── index.json              (job index for quick lookup)
    ├── 2024-01-15/
    │   ├── job-guid-1.json
    │   ├── job-guid-2.json
    │   └── ...
    └── 2024-01-16/
        └── ...
```

#### Job Record Format

```json
{
  "printJobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "user123",
  "stickerId": "sticker456",
  "stickerUrl": "https://cdn.example.com/stickers/456.png",
  "receivedAt": "2024-01-15T10:30:00Z",
  "completedAt": "2024-01-15T10:30:05Z",
  "status": "Completed"
}
```

#### Index Format

```json
{
  "totalJobs": 42,
  "lastUpdated": "2024-01-15T10:30:05Z",
  "jobs": [
    {
      "printJobId": "guid",
      "date": "2024-01-15",
      "status": "Completed"
    }
  ]
}
```

### 2.7 API Client

#### Interface

```csharp
public interface IPrintServiceApiClient
{
    Task<List<PrintJobDto>> PollJobsAsync(int maxJobs = 10);
    Task<bool> AcknowledgeJobAsync(string printJobId, bool success, string? failureReason = null);
    Task<PrinterInfoDto?> ValidateConnectionAsync();
}
```

#### Implementation Considerations

- Use `HttpClient` with `IHttpClientFactory`
- Set `X-Printer-Key` header from configuration
- Implement retry logic with exponential backoff
- Handle 204 No Content (no jobs) gracefully
- Log all API interactions

---

## Part 3: Error Handling & Edge Cases

### 3.1 Backend Error Scenarios

| Scenario | Handling |
|----------|----------|
| Printer not found for job submission | Return 404 with problem details |
| Invalid API key | Return 401 Unauthorized |
| Job already acknowledged | Return 409 Conflict |
| Job belongs to different printer | Return 403 Forbidden |
| Database unavailable | Return 503 Service Unavailable |

### 3.2 Client Error Scenarios

| Scenario | Handling |
|----------|----------|
| Backend unreachable | Log error, continue polling after interval |
| Invalid API key | Log error, set status to "Authentication Failed" |
| Local storage full | Log error, pause polling, alert via UI |
| Job acknowledgment fails | Retry with backoff, store in retry queue |

### 3.3 Consistency Considerations

**Scenario: Client crashes after storing locally but before acknowledging**

Options:
1. **Idempotent acknowledgment** - Allow re-acknowledgment of already completed jobs
2. **Client-side tracking** - Track acknowledgment status locally, retry on startup
3. **Lease expiry** - Jobs return to queue after timeout (backend handles)

**Recommendation:** Implement option 2 (client-side tracking) with option 1 as backend safety net.

---

## Part 4: Security Considerations

### 4.1 API Key Security

- Keys are Base64-encoded GUIDs (sufficient entropy)
- Keys should be transmitted only over HTTPS
- Keys displayed once at registration, then masked
- Consider key rotation mechanism for future enhancement

### 4.2 Printer Client Security

- Configuration file should have restricted permissions (600)
- API key stored in local config (acceptable for local service)
- Web UI binds to localhost only by default
- Consider optional password protection for web UI

### 4.3 Job Data Security

- StickerUrl should use HTTPS
- Local job storage should have restricted permissions
- Consider encryption at rest for job metadata (future enhancement)

---

## Part 5: Observability

### 5.1 Backend Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `print_jobs_submitted_total` | Counter | Total jobs submitted |
| `print_jobs_completed_total` | Counter | Total jobs completed |
| `print_jobs_failed_total` | Counter | Total jobs failed |
| `print_job_queue_depth` | Gauge | Jobs waiting per printer |
| `print_job_processing_duration` | Histogram | Time from queued to completed |

### 5.2 Backend Traces

- Span for job submission
- Span for job polling (include printer ID)
- Span for job acknowledgment
- Trace context propagated through events

### 5.3 Client Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `poll_requests_total` | Counter | Total poll requests |
| `poll_errors_total` | Counter | Failed poll requests |
| `jobs_processed_total` | Counter | Jobs processed locally |
| `poll_latency` | Histogram | Poll request duration |

---

## Part 6: Implementation Phases

### Phase 1: Core Backend Functionality
- [ ] PrintJob domain entity and value objects
- [ ] SubmitPrintJobCommand and handler
- [ ] IPrintJobRepository interface
- [ ] DynamoDB implementation of repository
- [ ] POST endpoint for job submission
- [ ] TTL implementation for completed jobs (2 days)

### Phase 2: Printer Polling API
- [ ] API Key authentication scheme
- [ ] IPrinterKeyValidator interface and implementation
- [ ] GetPrintJobsForPrinterQuery and handler
- [ ] GET endpoint for job polling
- [ ] Atomic job status update (Queued → Processing)
- [ ] Heartbeat update on poll (for status tracking)

### Phase 3: Job Acknowledgment & Status
- [ ] AcknowledgePrintJobCommand and handler
- [ ] POST endpoint for acknowledgment
- [ ] Domain events for job lifecycle
- [ ] Printer status fields (LastHeartbeat, LastJobProcessed)
- [ ] GET endpoint for printer status

### Phase 4: Printer Client - Core
- [ ] New project: Stickerlandia.PrintService.Client (Blazor Server)
- [ ] Configuration service and local storage
- [ ] PrintServiceApiClient implementation
- [ ] PrintJobPollingService background service
- [ ] LocalStorageService for job metadata

### Phase 5: Printer Client - Blazor UI
- [ ] ClientStatusService (shared state)
- [ ] Home page (dashboard with status)
- [ ] Configuration page (API key setup)
- [ ] History page (job history browser)
- [ ] Layout and navigation

### Phase 6: Polish & Hardening
- [ ] Error handling and retry logic
- [ ] Observability (metrics, traces, logs)
- [ ] Integration tests for new backend endpoints
- [ ] Client integration tests

---

## Design Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Job TTL | 2 days after completion | Balance storage costs with debugging needs |
| Job Priority | Not implemented | Keep initial version simple |
| Batch Acknowledgment | Not implemented | Single job ack is sufficient for MVP |
| Printer Status | Heartbeat-based (lazy evaluation) | Simple, no background workers needed |
| Client UI Framework | Blazor Server | Single codebase, real-time updates, shared models |

---

## Appendix A: API Summary

### Backend Endpoints (Complete)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/print/v1/event/{eventName}` | JWT (admin) | Register printer |
| GET | `/api/print/v1/event/{eventName}` | JWT (admin, user) | List printers for event |
| GET | `/api/print/v1/event/{eventName}/printers/status` | JWT (admin, user) | Get printer status (online/offline) |
| POST | `/api/print/v1/event/{eventName}/printer/{printerName}/jobs` | JWT (user, admin) | Submit print job |
| GET | `/api/print/v1/printer/jobs` | API Key | Poll for print jobs (also updates heartbeat) |
| POST | `/api/print/v1/printer/jobs/{printJobId}/acknowledge` | API Key | Acknowledge job completion |

### Client UI Pages (Blazor)

| Route | Page | Description |
|-------|------|-------------|
| `/` | Home | Dashboard with status and statistics |
| `/configuration` | Configuration | API key and settings setup |
| `/history` | History | Browse processed job history |
