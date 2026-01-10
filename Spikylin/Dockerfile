# Runtime base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install Node.js (for asset build)
RUN apt-get update && apt-get install -y curl \
    && curl -sL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# 1) Restore NuGet
COPY ["Spikylin.csproj", "./"]
RUN dotnet restore "Spikylin.csproj"

# 2) Install NPM deps (use npm ci for reproducible installs when lockfile exists)
COPY ["package.json","package-lock.json*", "./"]
RUN if [ -f package-lock.json ]; then npm ci --prefer-offline --no-audit --progress=false; else npm install --no-audit --progress=false; fi

# 3) Copy source and build
COPY . .
RUN dotnet build "Spikylin.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Spikylin.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# Final image (create non-root user)
FROM base AS final
ARG APP_UID=1000
RUN adduser --disabled-password --gecos "" --uid $APP_UID appuser \
    && chown -R appuser:appuser /app
WORKDIR /app
COPY --from=publish /app/publish .
USER appuser
ENTRYPOINT ["dotnet", "Spikylin.dll"]