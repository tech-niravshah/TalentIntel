Talent Intelligence & Ranking Engine (MVP)

###1. Overview

This system replaces a legacy keyword-based recruitment engine with a semantic, AI-driven matching system that ranks candidates against job descriptions.

The MVP focuses on:

End-to-end pipeline (ingestion → parsing → embedding → ranking)
Semantic similarity using embeddings
Scalable architecture with minimal complexity
Cloud-native implementation using .NET + Azure

###2. Architecture
**High-Level Architecture (Project-Based)**
[Client / Recruiter]
        ↓
[TalentIntel.API]
        ↓
[Azure Blob Storage] ← (Raw Files)
        ↓
[Queue (Azure Service Bus / Queue)]
        ↓
[TalentIntel.Functions]
        ↓
[TalentIntel.Infrastructure → Cosmos DB]
        ↓
[TalentIntel.API → Matching Endpoint]
        ↓
[Top 10 Candidates Response]

| Project                    | Responsibility                                   |
| -------------------------- | ------------------------------------------------ |
| TalentIntel.API            | API endpoints (upload, match)                    |
| TalentIntel.Application    | Core business logic (ranking, parsing, skills)   |
| TalentIntel.Domain         | Entities/models                                  |
| TalentIntel.Infrastructure | Azure integrations (Cosmos, Blob, Queue, OpenAI) |
| TalentIntel.Functions      | Background processing (parsing, embedding)       |
| TalentIntel.Tests		     | Xunit Test Cases       |

**Components**
1. API Layer (ASP.NET Core)
Upload resumes and job descriptions
Fetch Top 10 candidates
Stateless, horizontally scalable

2. Azure Blob Storage
Stores raw documents (PDF, DOCX)
Acts as durable file storage

3. Queue (Azure Queue / Service Bus)
Decouples ingestion from processing
Enables retry and reliability

4. Azure Function (Processing Worker)
Handles:
Document parsing
Skill extraction (lightweight)
Embedding generation (via Azure OpenAI)

5. Cosmos DB
The system uses two containers:
Candidates container
Stores candidate profiles
Skills, embeddings, metadata
Jobs container
Stores job descriptions
Extracted skills and embeddings

6. Matching Service (.NET API)
Receives Job ID
Fetches job data from Cosmos DB
Fetches filtered candidates
Computes similarity (cosine similarity)
Ranks and returns Top 10

###3. Data Flow
**A. Ingestion Pipeline**

1. User uploads Resume / JD
2. API stores file in Blob Storage
3. API publishes message to Queue
4. Function processes message:
   - Parse document → extract text
   - Extract skills (keyword-based)
   - Generate embedding
5. Store structured data in Cosmos DB
   - Candidates → Candidates container
   - Jobs → Jobs container
   
**B. Matching Pipeline**
1. Receive Job ID
2. Fetch job (skills + embedding) from Jobs container
3. Query Candidates container (filtered by skills/metadata)
4. Compute cosine similarity in application layer
5. Rank candidates
6. Return Top 10

**C. Data Model (Cosmos DB)**
Candidate Document
{
  "id": "candidate-123",
  "type": "candidate",
  "name": "John Doe",
  "skills": ["c#", "azure", "sql"],
  "experienceYears": 5,
  "embedding": [0.21, 0.77, ...],
  "updatedAt": "2026-04-05",
  "embeddingVersion": "v1"
}
Job Document
{
  "id": "job-456",
  "type": "job",
  "title": "Senior .NET Developer",
  "skills": ["c#", ".net", "azure"],
  "embedding": [0.45, 0.12, ...],
  "createdAt": "2026-04-05",
  "embeddingVersion": "v1"
}

###4. Ranking Logic
Scoring Function
Score =
  0.7 * semantic_similarity +
  0.3 * skill_overlap
Matching Steps
1. Retrieve job embedding
2. Filter candidates
3. Compute cosine similarity
4. Rank and return Top 10

###5. Scaling Approach
A. Horizontal Scaling
API and Function Apps are stateless → scale out easily
Azure handles auto-scaling
B. Asynchronous Processing
Queue decouples ingestion from processing
Prevents API bottlenecks
C. Efficient Querying
Use Cosmos DB filtering:
Skills
Reduce dataset before similarity computation
D. Controlled Candidate Set
Limit candidates for ranking:
Fetch top 500–1000 candidates only
E. Parallel Processing
Use parallel computation for similarity scoring
F. Future Scaling Path
Introduce vector search engine (Azure Cognitive Search)
Move similarity computation out of application layer

###6. Failure Modes & Mitigations
| Failure Mode                 | Impact             | Mitigation                         |
| ---------------------------- | ------------------ | ---------------------------------- |
| Parsing failure              | Missing data       | Store raw text, retry parsing      |
| Queue message loss           | Data inconsistency | Use retry + dead-letter queue      |
| Function failure             | Processing delay   | Automatic retries                  |
| Embedding API failure        | Missing embeddings | Retry with exponential backoff     |
| Duplicate processing         | Data duplication   | Use idempotent upsert in Cosmos DB |
| High latency (large dataset) | Slow ranking       | Pre-filter candidates              |
| High RU consumption          | Increased cost     | Optimize queries + projections     |
| Cold start (Function App)    | Delayed processing | Use warm instances / premium plan  |

###7. Design Decisions
Semantic-first ranking using embeddings
Cosmos DB as unified storage
Separate containers for:
Candidates
Jobs
Queue-based async pipeline for ingestion
Application-layer ranking to simplify MVP
Skill extraction as supporting signal (not primary)

###8. Trade-offs
| Decision                 | Trade-off                                        |
| ------------------------ | ------------------------------------------------ |
| No vector DB             | Simpler MVP, but less efficient at scale         |
| App-based similarity     | More control, but higher compute cost            |
| Keyword skill extraction | Fast, but less accurate                          |
| Cosmos DB only           | Fewer components, but weaker search capabilities |

###9. Limitations
No native vector indexing (manual similarity computation)
Limited NLP capabilities (basic skill extraction)
Potential filtering bias (missing relevant candidates)
No learning/feedback loop
Latency increases as dataset grows
Cosmos DB RU cost can rise with inefficient queries

###10. Future Improvements
Networking implementation to Azure resources (VNet integration, private endpoints)
Use of Log Analytics Workspace with DCR (Data Collection Rules) and DCE (Data Collection Endpoints)
Use of Managed Identities instead of connection string keys
Use of Azure Key Vault for secrets and configuration management
Authentication and Authorization (Azure AD / Entra ID)
Semantic ranking based on multiple factors (experience, seniority, role context, domain)
Integrate Azure Cognitive Search for vector search
Replace keyword extraction with NLP/LLM-based parsing
Introduce ML-based ranking (learning-to-rank)
Add feedback loop (clicks, hires)
Add caching layer (Redis)
Support multi-tenancy and access isolation
Implement compliance features (data privacy, deletion)

###11. Conclusion

This MVP delivers a semantic, scalable candidate ranking system with:

Clean separation of ingestion and processing
Efficient filtering + ranking pipeline
Minimal infrastructure complexity
Clear upgrade path to a vector-native architecture

It balances speed of implementation (4-hour constraint) with production-aligned design principles.