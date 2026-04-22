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

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends python3 python3-pip && \
    pip3 install --break-system-packages rembg[cli] && \
    rembg d && \
    apt-get clean && rm -rf /var/lib/apt/lists/* /tmp/* /root/.cache/pip
WORKDIR /app
COPY --from=api-build /app/publish .
COPY --from=web-build /web/dist ./wwwroot
RUN mkdir -p /app/uploads /app/data
VOLUME ["/app/uploads", "/app/data"]
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WatchTracker.Api.dll"]
