FROM node:22-alpine AS web-build
WORKDIR /web
COPY src/web/package.json src/web/package-lock.json* ./
RUN npm ci
COPY src/web/ .
ENV VITE_API_BASE_URL=""
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY src/api/WatchTracker.Api.csproj .
RUN dotnet restore
COPY src/api/ .
RUN dotnet publish -c Release -o /app/publish

# Build rembg in a separate stage to keep the final image clean
FROM python:3.12-slim AS rembg-build
RUN pip install --no-cache-dir --target=/opt/rembg "rembg[cpu,cli]" && \
    PYTHONPATH=/opt/rembg python -c "from rembg.sessions import new_session; new_session('u2net')" && \
    find /opt/rembg -type d -name '__pycache__' -exec rm -rf {} + 2>/dev/null; \
    find /opt/rembg -type d -name 'tests' -exec rm -rf {} + 2>/dev/null; \
    find /opt/rembg -type d -name '*.dist-info' -exec rm -rf {} + 2>/dev/null; \
    true

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends python3 && \
    apt-get clean && rm -rf /var/lib/apt/lists/*
COPY --from=rembg-build /opt/rembg /opt/rembg
COPY --from=rembg-build /root/.u2net /root/.u2net
ENV PYTHONPATH=/opt/rembg \
    PATH="/opt/rembg/bin:$PATH"
WORKDIR /app
COPY --from=api-build /app/publish .
COPY --from=web-build /web/dist ./wwwroot
RUN mkdir -p /app/uploads /app/data
VOLUME ["/app/uploads", "/app/data"]
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WatchTracker.Api.dll"]
