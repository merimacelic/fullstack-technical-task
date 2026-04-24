# syntax=docker/dockerfile:1.7

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

# Cache-friendly layer order: copy only the files that influence `dotnet restore` first.
COPY Directory.Packages.props ./
COPY Directory.Build.props ./
COPY global.json ./
COPY .editorconfig ./
COPY TaskManagement.sln ./
COPY src/TaskManagement.Domain/TaskManagement.Domain.csproj         src/TaskManagement.Domain/
COPY src/TaskManagement.Application/TaskManagement.Application.csproj src/TaskManagement.Application/
COPY src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj src/TaskManagement.Infrastructure/
COPY src/TaskManagement.Api/TaskManagement.Api.csproj               src/TaskManagement.Api/

RUN dotnet restore src/TaskManagement.Api/TaskManagement.Api.csproj

# Copy the rest and publish.
COPY src/ src/
RUN dotnet publish src/TaskManagement.Api/TaskManagement.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# ---- Runtime stage ----
# jammy-chiseled-extra: minimal Ubuntu base, no shell, non-root (UID 1654) by default.
# The "-extra" variant bundles ICU + tzdata which Microsoft.Data.SqlClient requires
# (the base chiseled image runs in invariant globalization mode, incompatible with SqlClient).
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskManagement.Api.dll"]
