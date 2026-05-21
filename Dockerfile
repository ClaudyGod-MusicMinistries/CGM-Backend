# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY ClaudyGod.sln .
COPY src/ClaudyGod.Domain/ClaudyGod.Domain.csproj src/ClaudyGod.Domain/
COPY src/ClaudyGod.Application/ClaudyGod.Application.csproj src/ClaudyGod.Application/
COPY src/ClaudyGod.Infrastructure/ClaudyGod.Infrastructure.csproj src/ClaudyGod.Infrastructure/
COPY src/ClaudyGod.API/ClaudyGod.API.csproj src/ClaudyGod.API/

RUN dotnet restore src/ClaudyGod.API/ClaudyGod.API.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish src/ClaudyGod.API/ClaudyGod.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd --system appgroup && useradd --system --gid appgroup appuser

# Copy published output
COPY --from=build /app/publish .

# Create uploads directory and set permissions
RUN mkdir -p /app/uploads /app/logs && \
    chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ClaudyGod.API.dll"]
