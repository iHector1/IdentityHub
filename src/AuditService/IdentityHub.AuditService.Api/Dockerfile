FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/AuditService/IdentityHub.AuditService.Api/IdentityHub.AuditService.Api.csproj", "src/AuditService/IdentityHub.AuditService.Api/"]
COPY ["src/AuditService/IdentityHub.AuditService.Infrastructure/IdentityHub.AuditService.Infrastructure.csproj", "src/AuditService/IdentityHub.AuditService.Infrastructure/"]
COPY ["src/AuditService/IdentityHub.AuditService.Application/IdentityHub.AuditService.Application.csproj", "src/AuditService/IdentityHub.AuditService.Application/"]
COPY ["src/AuditService/IdentityHub.AuditService.Domain/IdentityHub.AuditService.Domain.csproj", "src/AuditService/IdentityHub.AuditService.Domain/"]
RUN dotnet restore "src/AuditService/IdentityHub.AuditService.Api/IdentityHub.AuditService.Api.csproj"

COPY src/AuditService src/AuditService
RUN dotnet publish "src/AuditService/IdentityHub.AuditService.Api/IdentityHub.AuditService.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityHub.AuditService.Api.dll"]
