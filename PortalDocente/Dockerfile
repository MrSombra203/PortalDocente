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
ENV ASPNETCORE_HTTP_PORTS=10000
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 10000

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "PortalDocente.dll"]
