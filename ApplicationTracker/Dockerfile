FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ApplicationTracker/ ApplicationTracker/
RUN dotnet publish ApplicationTracker/src/Api/ApplicationTracker.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApplicationTracker.Api.dll"]
