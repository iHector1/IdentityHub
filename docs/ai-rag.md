# AI y RAG

AIService responde preguntas usando la documentación interna indexada en Qdrant. El flujo es: embedding, búsqueda vectorial, construcción de contexto y generación con OpenAI.

## Configuración

- Embeddings: `text-embedding-3-small`.
- Generación: `gpt-5.6-luna`.
- API key: `OPENAI_API_KEY` o `OpenAI__ApiKey`, fuera del repositorio.
- Costos opcionales: `OpenAI__InputCostPerMillionTokens` y `OpenAI__OutputCostPerMillionTokens`.

Si los precios no están configurados, se registran los tokens sin inventar un costo. Cuando están configurados, AIService estima el costo de entrada y salida con esos valores por millón de tokens.

## Métricas y calidad

AIService registra latencia de embeddings, búsqueda vectorial, generación y latencia total, además de los chunks recuperados, tokens de entrada, tokens de salida, tokens totales y costo estimado cuando aplica.

La validación de calidad es básica y se basa únicamente en el contexto recuperado: `Grounded` cuando hay chunks y respuesta, y `NoContext` cuando la búsqueda no devuelve contexto. No se usa otro LLM para evaluar la respuesta.

## Endpoints

Los endpoints requieren el JWT de los demás servicios protegidos:

- `POST /api/ai/index`: indexa `src/AIService/Knowledge` en Qdrant.
- `POST /api/ai/ask`: recibe `{ "question": "How does role assignment work?" }`.

La respuesta de `/api/ai/ask` incluye `answer`, `sources`, `latencyMs`, `quality` y `usage` con tokens y `estimatedCost` (`null` si no hay precios configurados).

AIService también se integra directamente con UserService mediante REST interno para generar resúmenes de usuarios:

- `GET /api/ai/users/{userId}/summary`: requiere JWT, consulta los datos públicos del usuario y devuelve un resumen junto con `usage` y `latencyMs`.

La integración solo envía a OpenAI `UserId`, `Email` e `IsActive`; nunca envía contraseñas, hashes ni tokens.
