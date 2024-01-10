FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /source

# copy csproj and restore as distinct layers
COPY backend/*.sln .
COPY backend/src/*/*.csproj src/
RUN for from in src/*.csproj; do \
      to=$(echo "$from" | sed 's/\/\([^/]*\)\.csproj$/\/\1&/') \
      && mkdir -p "$(dirname "$to")" && mv "$from" "$to"; \
    done
RUN dotnet restore src/CryptoBank.WebApi/CryptoBank.WebApi.csproj

# copy everything else and build app
COPY backend/ .
RUN dotnet publish -c Release -o /app/publish src/CryptoBank.WebApi/CryptoBank.WebApi.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "CryptoBank.WebApi.dll"]
