# Copilot Instructions

## Project Guidelines
- User requires: XML doc comments on every public class/method, inline comments for non-obvious logic, async methods accept/pass CancellationToken, skills normalized to lowercase at storage/comparison, no hardcoded secrets (read from IConfiguration), no business logic in controllers/functions, use ILogger<T> in every class with structured logging for significant ops, and use VectorMath.CosineSimilarity for semantic scoring.