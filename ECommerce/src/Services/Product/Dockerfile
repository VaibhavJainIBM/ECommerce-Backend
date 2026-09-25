FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["ECommerce.Product.Api/ECommerce.Product.Api.csproj", "ECommerce.Product.Api/"]
COPY ["ECommerce.Product.Application/ECommerce.Product.Application.csproj", "ECommerce.Product.Application/"]
COPY ["ECommerce.Product.Domain/ECommerce.Product.Domain.csproj", "ECommerce.Product.Domain/"]
COPY ["ECommerce.Product.Infrastructure/ECommerce.Product.Infrastructure.csproj", "ECommerce.Product.Infrastructure/"]

RUN dotnet restore "ECommerce.Product.Api/ECommerce.Product.Api.csproj"

COPY . .

WORKDIR "/src/ECommerce.Product.Api"

RUN dotnet publish "ECommerce.Product.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "ECommerce.Product.Api.dll"]