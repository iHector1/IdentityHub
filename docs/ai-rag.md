# AI y RAG

AIService responde preguntas usando documentación interna del proyecto. La respuesta se basa en el contexto encontrado en Qdrant; si no hay información suficiente, el servicio lo indica.

## Flujo

```text
Pregunta
    -> embedding
    -> búsqueda en Qdrant
    -> contexto relevante
    -> prompt con la pregunta y el contexto
    -> OpenAI
    -> respuesta y fuentes
```

La indexación lee los archivos Markdown de `src/AIService/Knowledge`, crea fragmentos con metadata, genera sus embeddings y los guarda en la colección `identityhub_knowledge` de Qdrant. La indexación se ejecuta automáticamente después de un login exitoso desde el frontend y también está disponible mediante el endpoint de indexación.

## Modelos y configuración

- Embeddings: `text-embedding-3-small`.
- Generación de respuesta: `gpt-5.6-luna`.
- Qdrant dentro de Docker: `http://qdrant:6333`.
- Colección: `identityhub_knowledge`.
- API key: variable `OPENAI_API_KEY` o configuración `OpenAI__ApiKey`.

La clave se configura en `.env`, que está excluido de Git. No debe escribirse en `docker-compose.yml`, en el código ni en los logs.

## Endpoints

Los dos endpoints requieren el mismo JWT que los demás servicios protegidos:

- `POST /api/ai/index`: lee la documentación, genera embeddings y actualiza los puntos en Qdrant. Repetirlo no duplica los puntos porque sus IDs son deterministas.
- `POST /api/ai/ask`: recibe `{ "question": "How does role assignment work?" }` y devuelve `answer`, `sources` y `latencyMs`.

## Fuentes y latencia

La respuesta devuelve los archivos usados como fuentes para que el usuario pueda identificar de dónde salió el contexto. AIService registra por separado el tiempo de embeddings, la búsqueda vectorial, la generación, la latencia total y la cantidad de chunks recuperados.

Cada indexación genera embeddings para los documentos. Cada pregunta usa un embedding y una solicitud de generación; por eso conviene mantener el contexto limitado y actualizar la documentación solo cuando sea necesario.
