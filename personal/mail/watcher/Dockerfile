FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Wolfe.Lab.Mail -c Release -o /app

# aspnet, not runtime: the watcher serves a health endpoint (compose.yaml).
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# curl, for the container healthcheck alone.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Runs as the host user so the state bind mount is writable without making
# the directory world-writable. That user has no
# passwd entry here and therefore no home, and .NET wants somewhere to put
# its own scratch — so HOME is the scratch directory.
ENV HOME=/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER 501:20

ENTRYPOINT ["dotnet", "Wolfe.Lab.Mail.dll"]
