FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore FleetTrack.slnx
RUN dotnet publish src/FleetTrack.Api/FleetTrack.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Usuario no root (APP_UID viene en la imagen aspnet); la carpeta de evidencias debe ser suya.
RUN mkdir -p /app/evidence && chown $APP_UID /app/evidence
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FleetTrack.Api.dll"]
