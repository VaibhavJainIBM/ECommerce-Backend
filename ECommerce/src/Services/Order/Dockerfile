FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ECommerce.Order.Api/ECommerce.Order.Api.csproj", "ECommerce.Order.Api/"]
COPY ["ECommerce.Order.Application/ECommerce.Order.Application.csproj", "ECommerce.Order.Application/"]
COPY ["ECommerce.Order.Domain/ECommerce.Order.Domain.csproj", "ECommerce.Order.Domain/"]
COPY ["ECommerce.Order.Infrastructure/ECommerce.Order.Infrastructure.csproj", "ECommerce.Order.Infrastructure/"]
RUN dotnet restore "ECommerce.Order.Api/ECommerce.Order.Api.csproj"

COPY . .
WORKDIR /src/ECommerce.Order.Api
RUN dotnet publish "ECommerce.Order.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Order.Api.dll"]
