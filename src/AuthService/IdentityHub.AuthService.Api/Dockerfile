FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/AuthService/IdentityHub.AuthService.Api/IdentityHub.AuthService.Api.csproj", "src/AuthService/IdentityHub.AuthService.Api/"]
COPY ["src/AuthService/IdentityHub.AuthService.Infrastructure/IdentityHub.AuthService.Infrastructure.csproj", "src/AuthService/IdentityHub.AuthService.Infrastructure/"]
COPY ["src/AuthService/IdentityHub.AuthService.Application/IdentityHub.AuthService.Application.csproj", "src/AuthService/IdentityHub.AuthService.Application/"]
COPY ["src/AuthService/IdentityHub.AuthService.Domain/IdentityHub.AuthService.Domain.csproj", "src/AuthService/IdentityHub.AuthService.Domain/"]
RUN dotnet restore "src/AuthService/IdentityHub.AuthService.Api/IdentityHub.AuthService.Api.csproj"

COPY src/AuthService src/AuthService
RUN dotnet publish "src/AuthService/IdentityHub.AuthService.Api/IdentityHub.AuthService.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityHub.AuthService.Api.dll"]
