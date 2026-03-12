FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY EmailSync/ EmailSync/
RUN dotnet publish EmailSync/ApplicationTracker.EmailSync.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
WORKDIR /app
COPY --from=build /app .

# Run the EmailSync worker directly; use Render's built-in scheduler
ENTRYPOINT ["dotnet", "ApplicationTracker.EmailSync.dll"]
