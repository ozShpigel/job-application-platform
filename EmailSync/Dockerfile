FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY EmailSync/ EmailSync/
RUN dotnet publish EmailSync/ApplicationTracker.EmailSync.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
RUN apk add --no-cache dcron \
    && mkdir -p /etc/crontabs
WORKDIR /app
COPY --from=build /app .
COPY EmailSync/crontab /etc/crontabs/root

# Use full path for crond to avoid PATH issues in restricted environments
ENTRYPOINT ["/usr/sbin/crond", "-f", "-l", "8"]
