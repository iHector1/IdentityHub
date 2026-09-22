FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/UserService/IdentityHub.UserService.Api/IdentityHub.UserService.Api.csproj", "src/UserService/IdentityHub.UserService.Api/"]
COPY ["src/UserService/IdentityHub.UserService.Infrastructure/IdentityHub.UserService.Infrastructure.csproj", "src/UserService/IdentityHub.UserService.Infrastructure/"]
COPY ["src/UserService/IdentityHub.UserService.Application/IdentityHub.UserService.Application.csproj", "src/UserService/IdentityHub.UserService.Application/"]
COPY ["src/UserService/IdentityHub.UserService.Domain/IdentityHub.UserService.Domain.csproj", "src/UserService/IdentityHub.UserService.Domain/"]
RUN dotnet restore "src/UserService/IdentityHub.UserService.Api/IdentityHub.UserService.Api.csproj"

COPY src/UserService src/UserService
RUN dotnet publish "src/UserService/IdentityHub.UserService.Api/IdentityHub.UserService.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityHub.UserService.Api.dll"]
