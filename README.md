# IdentityHub

IdentityHub es una plataforma de identidad basada en microservicios. Gestiona usuarios, credenciales, roles, auditoría y consultas sobre la documentación interna mediante AIService.

El proyecto usa .NET 9 para las APIs, Angular para el frontend y Docker Compose para ejecutar los servicios y sus dependencias.

## Arquitectura

La solución separa cada responsabilidad en un microservicio independiente. Las APIs se comunican por REST, los eventos de auditoría viajan por RabbitMQ y cada servicio usa el almacenamiento que corresponde a su responsabilidad.

La arquitectura y los flujos principales están documentados en [docs/architecture.md](docs/architecture.md).

## Servicios

- **UserService:** gestiona usuarios y sus datos principales en SQL Server. Usa Redis como caché distribuida para consultas por ID.
- **AuthService:** gestiona credenciales, login y generación de JWT. Las contraseñas se almacenan con BCrypt.
- **RoleService:** gestiona roles y sus asignaciones. Valida usuarios consultando UserService.
- **AuditService:** consume eventos por RabbitMQ y los almacena en MongoDB.
- **AIService:** implementa RAG con embeddings, Qdrant y OpenAI, con métricas de latencia, tokens, costo configurable y calidad básica del contexto.
- **Frontend:** aplicación Angular que consume las APIs por HTTP.

## Tecnologías

- .NET 9 y Entity Framework Core
- Angular y Node.js
- SQL Server
- MongoDB
- RabbitMQ
- Qdrant
- OpenAI
- Docker y Docker Compose
- Serilog
- xUnit
- Redis para caché distribuida en UserService.

## Requisitos

- Docker
- Docker Compose
- .NET 9
- Node.js
- npm

## Configuración

La API key de OpenAI se configura fuera del código. Copia `.env.example` como `.env` y coloca tu clave local:

```powershell
Copy-Item .env.example .env
notepad .env
```

También se puede definir temporalmente en PowerShell:

```powershell
$env:OPENAI_API_KEY = "..."
```

No se debe subir `.env` al repositorio. El archivo `.env.example` no contiene una clave real.

Los valores de passwords, `Jwt__Key` e `InternalServices__ApiKey` definidos en `docker-compose.yml` son solo para desarrollo. En producción deben configurarse mediante variables de entorno o un gestor de secretos.

## Ejecución

Desde la raíz del repositorio:

```powershell
docker compose up -d --build
docker compose ps
```

El frontend queda disponible en `http://localhost:4200`. Para detener los servicios:

```powershell
docker compose down
```

> `docker compose down -v` también elimina los volúmenes y los datos persistidos.

## Tests

```powershell
dotnet test IdentityHub.slnx
```

Para validar el frontend:

```powershell
Set-Location src/Frontend
npm install
npm run build
npm test -- --watch=false
```

El frontend usa Angular Signals para el estado de autenticación. El detalle de cobertura backend está en [docs/test-coverage.md](docs/test-coverage.md).

## Endpoints principales

Todos los endpoints protegidos requieren `Authorization: Bearer <token>`.

### AuthService

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`

### UserService

- `GET /api/users`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `PUT /api/users/{id}/status` para activar o desactivar un usuario

No existe un `DELETE` de usuario en la implementación actual.

### RoleService

- `GET /api/roles`
- `GET /api/roles/{id}`
- `POST /api/roles`
- `PUT /api/roles/{id}`
- `DELETE /api/roles/{id}`
- `POST /api/roles/{roleId}/users/{userId}`
- `DELETE /api/roles/{roleId}/users/{userId}`
- `GET /api/roles/users/{userId}`

### AuditService

- `GET /api/audit`
- `GET /api/audit/{id}`
- `GET /api/audit/user/{userId}`
- `GET /api/audit/type/{eventType}`

### AIService

- `POST /api/ai/index`
- `POST /api/ai/ask`

## Frontend

Rutas disponibles:

- `/login`
- `/users`
- `/roles`
- `/audit`
- `/ai`

El `JWT interceptor` agrega automáticamente `Authorization: Bearer <token>` a las solicitudes protegidas. `AuthGuard` evita acceder a las rutas protegidas sin sesión.

## Documentación adicional

- [Arquitectura](docs/architecture.md)
- [Diagnóstico de producción](docs/production-diagnostics.md)
- [AI y RAG](docs/ai-rag.md)
