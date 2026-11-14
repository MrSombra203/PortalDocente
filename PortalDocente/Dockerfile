# Publica la aplicación en una imagen SDK y copia solo los artefactos publicados a la imagen de ejecución.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PortalDocente.csproj ./
RUN dotnet restore PortalDocente.csproj

COPY . .
RUN dotnet publish PortalDocente.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Render enruta al puerto 10000; los encabezados reenviados permiten reconocer HTTPS detrás del proxy.
ENV ASPNETCORE_HTTP_PORTS=10000
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 10000

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "PortalDocente.dll"]
