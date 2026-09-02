FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SportReplay.slnx ./
COPY src/backend ./src/backend
COPY src/workers ./src/workers
RUN dotnet restore src/backend/SportReplay.Api/SportReplay.Api.csproj
RUN dotnet publish src/backend/SportReplay.Api/SportReplay.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s CMD sh -c "curl -f http://127.0.0.1:${PORT:-8080}/health || exit 1"
ENTRYPOINT ["sh", "-c", "dotnet SportReplay.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
