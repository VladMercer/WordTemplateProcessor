# Базовый образ с runtime + LibreOffice
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Установка LibreOffice под root
USER root
RUN apt-get update && \
    apt-get install -y libreoffice && \
    apt-get clean

# Создание непривилегированного пользователя
RUN useradd -m appuser
USER appuser

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Слой сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WordTemplateProcessor.web/WordTemplateProcessor.web.csproj", "WordTemplateProcessor.web/"]
RUN dotnet restore "WordTemplateProcessor.web/WordTemplateProcessor.web.csproj"
COPY . .
WORKDIR "/src/WordTemplateProcessor.web"
RUN dotnet build "WordTemplateProcessor.web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Публикация
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WordTemplateProcessor.web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальный слой
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "WordTemplateProcessor.web.dll"]