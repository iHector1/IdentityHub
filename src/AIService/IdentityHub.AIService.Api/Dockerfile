FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/AIService/IdentityHub.AIService.Api/IdentityHub.AIService.Api.csproj", "src/AIService/IdentityHub.AIService.Api/"]
COPY ["src/AIService/IdentityHub.AIService.Infrastructure/IdentityHub.AIService.Infrastructure.csproj", "src/AIService/IdentityHub.AIService.Infrastructure/"]
COPY ["src/AIService/IdentityHub.AIService.Application/IdentityHub.AIService.Application.csproj", "src/AIService/IdentityHub.AIService.Application/"]
COPY ["src/AIService/IdentityHub.AIService.Domain/IdentityHub.AIService.Domain.csproj", "src/AIService/IdentityHub.AIService.Domain/"]
RUN dotnet restore "src/AIService/IdentityHub.AIService.Api/IdentityHub.AIService.Api.csproj"

COPY src/AIService src/AIService
RUN dotnet publish "src/AIService/IdentityHub.AIService.Api/IdentityHub.AIService.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityHub.AIService.Api.dll"]
