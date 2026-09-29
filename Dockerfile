FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish servers/Azure.Mcp.Server/src/Azure.Mcp.Server.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o /app/publish

# Stage 2: Minimal runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /mcp-server
COPY --from=build /app/publish .

RUN adduser -D -s /sbin/nologin mcp && \
    chown -R mcp:mcp /mcp-server
USER mcp

EXPOSE 8089
ENTRYPOINT ["dotnet", "azmcp.dll", "server", "start"]
