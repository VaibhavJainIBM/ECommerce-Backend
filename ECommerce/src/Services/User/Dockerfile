FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ECommerce.User.Api/ECommerce.User.Api.csproj", "ECommerce.User.Api/"]
COPY ["ECommerce.User.Application/ECommerce.User.Application.csproj", "ECommerce.User.Application/"]
COPY ["ECommerce.User.Domain/ECommerce.User.Domain.csproj", "ECommerce.User.Domain/"]
COPY ["ECommerce.User.Infrastructure/ECommerce.User.Infrastructure.csproj", "ECommerce.User.Infrastructure/"]
RUN dotnet restore "ECommerce.User.Api/ECommerce.User.Api.csproj"

COPY . .
WORKDIR /src/ECommerce.User.Api
RUN dotnet publish "ECommerce.User.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.User.Api.dll"]
