FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/AutoSale.Vehicles.Api/AutoSale.Vehicles.Api.csproj", "src/AutoSale.Vehicles.Api/"]
COPY ["src/AutoSale.Vehicles.Application/AutoSale.Vehicles.Application.csproj", "src/AutoSale.Vehicles.Application/"]
COPY ["src/AutoSale.Vehicles.Domain/AutoSale.Vehicles.Domain.csproj", "src/AutoSale.Vehicles.Domain/"]
COPY ["src/AutoSale.Vehicles.Infrastructure/AutoSale.Vehicles.Infrastructure.csproj", "src/AutoSale.Vehicles.Infrastructure/"]
COPY ["src/BuildingBlocks/AutoSale.SharedKernel/AutoSale.SharedKernel.csproj", "src/BuildingBlocks/AutoSale.SharedKernel/"]
RUN dotnet restore "src/AutoSale.Vehicles.Api/AutoSale.Vehicles.Api.csproj"

COPY . .
RUN dotnet publish "src/AutoSale.Vehicles.Api/AutoSale.Vehicles.Api.csproj" --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "AutoSale.Vehicles.Api.dll"]
