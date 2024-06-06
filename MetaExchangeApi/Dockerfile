FROM mcr.Microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app
EXPOSE 5057

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final-env
WORKDIR /app

# Unzip the order_books_data files
RUN apt-get update && apt-get install -y p7zip-full
COPY ./Data/order_books_data.7z /app/Data/
RUN 7z x /app/Data/order_books_data.7z -o/app/Data/

COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "MetaExchangeApi.dll"]