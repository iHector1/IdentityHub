# Diagnóstico de producción

Este documento resume una ruta práctica para revisar errores de APIs, registros que no se guardan y problemas de latencia en AIService.

## Error 500

Primero se revisan el `CorrelationId`, los logs estructurados, la excepción registrada, el microservicio afectado y el health check correspondiente. Después se valida la conexión a la base de datos y cualquier servicio externo involucrado.

El `CorrelationId` se toma de la respuesta o de los logs de la solicitud. Con ese valor se siguen los registros del servicio de entrada, las llamadas internas y el evento publicado, hasta encontrar dónde cambia el flujo o aparece la excepción.

## Registros que no se guardan

Revisar en este orden:

- Datos enviados por el cliente.
- Validaciones de la API.
- Logs de Entity Framework.
- Connection string y disponibilidad de SQL Server.
- Constraints, schema y migrations.
- Errores de transacción.

También se debe comprobar si el evento relacionado se publicó correctamente cuando el flujo depende de RabbitMQ.

## Latencia de AIService

Separar la medición en estos puntos:

- Tiempo de creación del embedding.
- Tiempo de búsqueda en Qdrant.
- Tiempo de respuesta de OpenAI.
- Latencia total.
- Cantidad de chunks recuperados.
- Tamaño del prompt y cantidad de tokens.
- Rate limits, timeouts y reintentos.

El servicio ya registra mediciones de embeddings, búsqueda vectorial, generación, latencia total y chunks usados. Las mejoras posibles son reducir los chunks, limitar el contexto, configurar timeouts, usar retry con backoff, revisar el modelo y aplicar caché cuando tenga sentido.

## Comunicación del incidente

La comunicación debe indicar:

- Qué está afectado.
- Cuál es el impacto actual.
- Qué mitigación se aplicó.
- En qué estado está la investigación.
- Cuándo será la siguiente actualización.

No se debe afirmar una causa raíz hasta contar con evidencia en logs, métricas o reproducciones controladas.

## Observabilidad

Los servicios usan Serilog con logs estructurados en JSON, request logging, `CorrelationId` y health checks. El `CorrelationId` permite relacionar los logs de una misma solicitud entre varios servicios.

Ejemplo de la estructura de un request log:

```json
{
  "@t": "2026-09-23T16:59:11.1924145Z",
  "@m": "HTTP GET \"/health\" responded 200 in 67.2520 ms",
  "SourceContext": "Serilog.AspNetCore.RequestLoggingMiddleware",
  "RequestPath": "/health",
  "StatusCode": 200,
  "CorrelationId": "<correlation-id>"
}
```

Los logs no deben incluir passwords, JWT, API keys ni connection strings.

## Seguridad durante el diagnóstico

- BCrypt para contraseñas.
- JWT para autenticación.
- Validación distribuida del JWT.
- API key interna para comunicación entre servicios cuando aplica.
- Secrets mediante variables de entorno.
- Endpoints protegidos.
- Validación de inputs.
- No registrar passwords ni tokens.
