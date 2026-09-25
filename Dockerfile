# ==============================================================
# Multi-stage Dockerfile for CaseShop.Web (.NET 10 + Tailwind CSS)
# ==============================================================

# -------------------------------------------------------------
# Stage 1: Build CSS with Node.js
# -------------------------------------------------------------
FROM node:20-alpine AS css-builder
WORKDIR /app

COPY CaseShop.Web/package*.json ./
RUN npm install

COPY CaseShop.Web/tailwind.config.js ./
COPY CaseShop.Web/wwwroot/app.css ./wwwroot/
COPY CaseShop.Web/Components ./Components
COPY CaseShop.Web/wwwroot ./wwwroot

RUN npm run build:css

# -------------------------------------------------------------
# Stage 2: Build & Publish .NET Web App
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CaseShop.Web/CaseShop.Web.csproj", "CaseShop.Web/"]
RUN dotnet restore "CaseShop.Web/CaseShop.Web.csproj"

COPY . .
# Copy compiled CSS from css-builder to avoid needing Node in .NET SDK container
COPY --from=css-builder /app/wwwroot/app.min.css CaseShop.Web/wwwroot/app.min.css

WORKDIR "/src/CaseShop.Web"
RUN dotnet publish "CaseShop.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:SkipTailwindBuild=true

# -------------------------------------------------------------
# Stage 3: Minimal ASP.NET Core Runtime
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CaseShop.Web.dll"]
