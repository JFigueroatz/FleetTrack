FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore FleetTrack.slnx
RUN dotnet publish src/FleetTrack.Consumer/FleetTrack.Consumer.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetTrack.Consumer.dll"]
