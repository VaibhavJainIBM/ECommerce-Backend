FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ECommerce.Payment.Api/ECommerce.Payment.Api.csproj", "ECommerce.Payment.Api/"]
COPY ["ECommerce.Payment.Application/ECommerce.Payment.Application.csproj", "ECommerce.Payment.Application/"]
COPY ["ECommerce.Payment.Domain/ECommerce.Payment.Domain.csproj", "ECommerce.Payment.Domain/"]
COPY ["ECommerce.Payment.Infrastructure/ECommerce.Payment.Infrastructure.csproj", "ECommerce.Payment.Infrastructure/"]
RUN dotnet restore "ECommerce.Payment.Api/ECommerce.Payment.Api.csproj"

COPY . .
WORKDIR /src/ECommerce.Payment.Api
RUN dotnet publish "ECommerce.Payment.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Payment.Api.dll"]
