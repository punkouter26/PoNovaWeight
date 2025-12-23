# Build stage - Using official .NET 10.0 SDK (Ubuntu 24.04 Noble)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY PoNovaWeight.sln .
COPY global.json .
COPY src/PoNovaWeight.Api/PoNovaWeight.Api.csproj src/PoNovaWeight.Api/
COPY src/PoNovaWeight.Client/PoNovaWeight.Client.csproj src/PoNovaWeight.Client/
COPY src/PoNovaWeight.Shared/PoNovaWeight.Shared.csproj src/PoNovaWeight.Shared/
COPY src/PoNovaWeight.AppHost/PoNovaWeight.AppHost.csproj src/PoNovaWeight.AppHost/
COPY src/PoNovaWeight.ServiceDefaults/PoNovaWeight.ServiceDefaults.csproj src/PoNovaWeight.ServiceDefaults/
COPY tests/PoNovaWeight.Api.Tests/PoNovaWeight.Api.Tests.csproj tests/PoNovaWeight.Api.Tests/
COPY tests/PoNovaWeight.Client.Tests/PoNovaWeight.Client.Tests.csproj tests/PoNovaWeight.Client.Tests/
COPY tests/PoNovaWeight.E2E/PoNovaWeight.E2E.csproj tests/PoNovaWeight.E2E/
COPY Directory.Build.props .
COPY Directory.Packages.props .

# Restore dependencies
RUN dotnet restore src/PoNovaWeight.Api/PoNovaWeight.Api.csproj

# Copy all source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/PoNovaWeight.Api/PoNovaWeight.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage - Using official .NET 10.0 ASP.NET runtime (Ubuntu 24.04 Noble Chiseled for smaller image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Note: Health check handled by Azure Container Apps, not in container
# Chiseled images don't include curl

# Run the app
ENTRYPOINT ["dotnet", "PoNovaWeight.Api.dll"]
