# IdentityHub AI/RAG

AIService is a small retrieval-augmented generation service. It does not use agents or tool calling.

```text
Markdown documents
    -> chunks with source metadata
    -> OpenAI text-embedding-3-small
    -> Qdrant identityhub_knowledge
    -> top 5 similar chunks
    -> grounded prompt
    -> OpenAI gpt-5.6-luna
    -> answer and retrieved sources
```

## Endpoints

Both endpoints require the same bearer JWT as the other protected services:

- `POST /api/ai/index` reads `src/AIService/Knowledge`, creates chunks and upserts their vectors in Qdrant. Point IDs are deterministic, so repeated indexing updates the same points instead of duplicating them.
- `POST /api/ai/ask` accepts `{ "question": "How does role assignment work?" }` and returns `answer`, `sources` and `latencyMs`.

## Configuration

Set the OpenAI key outside Git using either environment variable:

```powershell
$env:OPENAI_API_KEY = "your-key"
docker compose up -d --build aiservice qdrant
```

The equivalent .NET configuration key is `OpenAI__ApiKey`. Docker uses `OpenAI__ApiKey=${OPENAI_API_KEY}` without storing the value in `docker-compose.yml`.

Defaults are:

- Generation model: `gpt-5.6-luna`
- Embedding model: `text-embedding-3-small`
- Qdrant URL in Docker: `http://qdrant:6333`
- Qdrant collection: `identityhub_knowledge`

If the key is missing, the service still starts, but indexing and questions fail with a configuration error rather than exposing a secret or using ungrounded model output.

## Latency, cost and quality

AIService logs structured measurements for embedding, vector search, generation, total latency and the number of retrieved chunks. It does not log API keys, Authorization headers or full prompts.

Embedding every document during indexing costs tokens once per index run. Each question costs one embedding request plus one generation request. Smaller chunks and top-5 retrieval reduce prompt size and cost, while overlap helps preserve context across chunk boundaries. Retrieval quality depends on the accuracy and freshness of the Markdown knowledge files; the assistant is instructed to say when the indexed context is insufficient.
