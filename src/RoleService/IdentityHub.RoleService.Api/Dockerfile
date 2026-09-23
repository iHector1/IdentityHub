FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/RoleService/IdentityHub.RoleService.Api/IdentityHub.RoleService.Api.csproj", "src/RoleService/IdentityHub.RoleService.Api/"]
COPY ["src/RoleService/IdentityHub.RoleService.Infrastructure/IdentityHub.RoleService.Infrastructure.csproj", "src/RoleService/IdentityHub.RoleService.Infrastructure/"]
COPY ["src/RoleService/IdentityHub.RoleService.Application/IdentityHub.RoleService.Application.csproj", "src/RoleService/IdentityHub.RoleService.Application/"]
COPY ["src/RoleService/IdentityHub.RoleService.Domain/IdentityHub.RoleService.Domain.csproj", "src/RoleService/IdentityHub.RoleService.Domain/"]
RUN dotnet restore "src/RoleService/IdentityHub.RoleService.Api/IdentityHub.RoleService.Api.csproj"

COPY src/RoleService src/RoleService
RUN dotnet publish "src/RoleService/IdentityHub.RoleService.Api/IdentityHub.RoleService.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityHub.RoleService.Api.dll"]
