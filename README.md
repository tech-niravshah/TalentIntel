# TalentIntel

AI-powered talent matching platform that ranks candidates against job descriptions
using semantic embeddings and skill overlap scoring.

---

## What it does

1. **Upload** a resume or job description (PDF or DOCX) via the REST API
2. **Process** the document asynchronously — text extraction, skill detection, embedding generation
3. **Match** candidates to a job opening using two-stage retrieval:
   - Skill overlap filter (keyword matching)
   - Semantic similarity ranking (cosine similarity on 1536-d vectors)

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8 Web API |
| Background Worker | Azure Functions v4 Isolated |
| Document Storage | Azure Blob Storage |
| Message Queue | Azure Service Bus |
| Database | Azure Cosmos DB (NoSQL) |
| AI / Embeddings | Azure OpenAI — text-embedding-3-small |
| PDF Parsing | UglyToad.PdfPig |
| DOCX Parsing | DocumentFormat.OpenXml |
| Tests | xUnit + Moq |

---

## Project Structure
TalentIntel/
├── TalentIntel.Domain/          # Entities, enums, exceptions — no dependencies
├── TalentIntel.Application/     # Interfaces, services, DTOs — no infrastructure
├── TalentIntel.Infrastructure/  # Cosmos, Blob, ServiceBus, OpenAI, Parsing
├── TalentIntel.API/             # HTTP endpoints
├── TalentIntel.Functions/       # Service Bus queue worker
└── TalentIntel.Tests/           # Unit tests

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local Storage emulator)
- [Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/local-emulator) (local Cosmos)
- An Azure Service Bus namespace (no local emulator available)
- An Azure OpenAI resource with `text-embedding-3-small` deployed

---

### 1. Clone the repo
```bash
git clone https://github.com/your-org/talentintel.git
cd talentintel
```

---

### 2. Configure secrets locally

Never commit real secrets. Use `dotnet user-secrets` for local development.

**For TalentIntel.API:**
```bash
cd TalentIntel.API
dotnet user-secrets set "Azure:BlobStorage:ConnectionString" "your-value"
dotnet user-secrets set "Azure:CosmosDb:ConnectionString" "your-value"
dotnet user-secrets set "Azure:ServiceBus:ConnectionString" "your-value"
dotnet user-secrets set "Azure:OpenAI:ApiKey" "your-value"
```

**For TalentIntel.Functions:**
```bash
cd TalentIntel.Functions
dotnet user-secrets set "Azure:BlobStorage:ConnectionString" "your-value"
dotnet user-secrets set "Azure:CosmosDb:ConnectionString" "your-value"
dotnet user-secrets set "Azure:OpenAI:ApiKey" "your-value"
```

Update `local.settings.json` with your real Service Bus connection string:
```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "ServiceBusConnection": "your-service-bus-connection-string"
  }
}
```

---

### 3. Set up Cosmos DB

Create two containers in your Cosmos DB database (`TalentIntel`):

| Container | Partition Key |
|---|---|
| `Candidates` | `/id` |
| `Jobs` | `/id` |

> ⚠️ Partition key must be lowercase `/id` — the Cosmos SDK CamelCase serializer
> maps the C# `Id` property to `id` automatically.

---

### 4. Run the API
```bash
cd TalentIntel.API
dotnet run
```
Swagger UI available at: `https://localhost:{port}/swagger`

---

### 5. Run the Function worker
```bash
cd TalentIntel.Functions
func start
```

---

### 6. Run tests
```bash
dotnet test
```

---

## API Reference

### Upload a document

---

## Configuration Reference

All configuration keys used across the solution:

| Key | Used by | Description |
|---|---|---|
| `Azure:CosmosDb:ConnectionString` | API, Functions | Cosmos DB connection string |
| `Azure:CosmosDb:DatabaseName` | API, Functions | Database name (e.g. `TalentIntel`) |
| `Azure:CosmosDb:CandidatesContainer` | API, Functions | Container name (e.g. `Candidates`) |
| `Azure:CosmosDb:JobsContainer` | API, Functions | Container name (e.g. `Jobs`) |
| `Azure:BlobStorage:ConnectionString` | API, Functions | Blob Storage connection string |
| `Azure:BlobStorage:ContainerName` | API, Functions | Blob container (e.g. `documents`) |
| `Azure:ServiceBus:ConnectionString` | API | Service Bus connection string |
| `Azure:ServiceBus:QueueName` | API | Queue name (e.g. `talentintel-docprocessing-queue`) |
| `ServiceBusConnection` | Functions | Flat key for ServiceBusTrigger Connection attribute |
| `Azure:OpenAI:Endpoint` | API, Functions | Azure OpenAI endpoint URI |
| `Azure:OpenAI:ApiKey` | API, Functions | Azure OpenAI API key |
| `Azure:OpenAI:DeploymentName` | API, Functions | Deployment name (e.g. `text-embedding-3-small`) |
| `Azure:ApplicationInsights:ConnectionString` | API, Functions | App Insights connection string |

---

## Known Limitations

- Skill extraction uses a fixed keyword list — no ML-based NER
- Candidate retrieval is a full Cosmos container scan — suitable up to ~50K candidates
- No authentication on API endpoints — add Azure AD / API key before production
- Service Bus has no local emulator — requires a real Azure namespace for local dev

---

## Roadmap

- [ ] Add Azure AD authentication to API
- [ ] Replace keyword skill extraction with NER model
- [ ] Add Cosmos vector index for large-scale candidate retrieval
- [ ] Add Key Vault integration for secret management
- [ ] Add CI/CD pipeline (GitHub Actions)
- [ ] Add integration tests