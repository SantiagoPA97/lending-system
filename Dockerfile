FROM node:22-alpine AS frontend
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src
COPY backend/src/Lending.Domain/Lending.Domain.csproj src/Lending.Domain/
COPY backend/src/Lending.Infrastructure/Lending.Infrastructure.csproj src/Lending.Infrastructure/
COPY backend/src/Lending.Api/Lending.Api.csproj src/Lending.Api/
RUN dotnet restore src/Lending.Api/Lending.Api.csproj
COPY backend/src/ src/
RUN dotnet publish src/Lending.Api/Lending.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=backend /app ./
COPY --from=frontend /src/dist ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Lending.Api.dll"]
