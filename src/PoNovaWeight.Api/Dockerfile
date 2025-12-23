# Build stage - Using .NET 10.0 preview
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY PoNovaWeight.sln .
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

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the app
ENTRYPOINT ["dotnet", "PoNovaWeight.Api.dll"]
