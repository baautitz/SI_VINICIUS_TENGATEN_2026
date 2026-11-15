# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# restore: so invalida quando algum .csproj/.slnx muda
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY ["backend.slnx", "Directory.Build.props", "Directory.Build.targets", "./"]
COPY ["Backend.Core/Backend.Core.csproj", "Backend.Core/"]
COPY ["Backend.Infrastructure.PostgreSQL/Backend.Infrastructure.PostgreSQL.csproj", "Backend.Infrastructure.PostgreSQL/"]
COPY ["Backend.Web/Backend.Web.csproj", "Backend.Web/"]
COPY ["Backend.Tests/Backend.Tests.csproj", "Backend.Tests/"]
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "backend.slnx"

# build: roda a cada mudanca de codigo; compila a solution uma unica vez
FROM restore AS build
ARG BUILD_CONFIGURATION=Release
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build "backend.slnx" -c $BUILD_CONFIGURATION --no-restore

# test: CI. docker build --target test ./Backend
FROM build AS test
ARG BUILD_CONFIGURATION=Release
RUN dotnet test "Backend.Tests/Backend.Tests.csproj" -c $BUILD_CONFIGURATION --no-build

# publish: reaproveita o build, sem recompilar
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "Backend.Web/Backend.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-build /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Backend.Web.dll"]
