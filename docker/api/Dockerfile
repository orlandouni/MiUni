# API .NET de MiUni. Se construye desde la raíz del repo:
#   docker build -f docker/api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MiUni.Api/MiUni.Api.csproj MiUni.Api/
RUN dotnet restore MiUni.Api/MiUni.Api.csproj
COPY MiUni.Api/ MiUni.Api/
RUN dotnet publish MiUni.Api/MiUni.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Llaves de Data Protection (enlaces de verificación de correo) en un volumen.
RUN mkdir -p /data/llaves && chown -R $APP_UID /data
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DataProtection__KeysPath=/data/llaves
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "MiUni.Api.dll"]
