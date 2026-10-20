FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Api/Api.csproj", "Api/"]
COPY ["src/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/"]
COPY ["src/Modules/Catalog/Catalog.csproj", "Modules/Catalog/"]
COPY ["src/Modules/Catalog.Contracts/Catalog.Contracts.csproj", "Modules/Catalog.Contracts/"]
COPY ["src/Modules/Inventory/Inventory.csproj", "Modules/Inventory/"]
COPY ["src/Modules/Inventory.Contracts/Inventory.Contracts.csproj", "Modules/Inventory.Contracts/"]
RUN dotnet restore "src/Api/Api.csproj"
COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
