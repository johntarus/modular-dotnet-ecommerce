# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy ALL csproj files, preserving the "src/" directory structure
COPY ["src/Api/Api.csproj", "src/Api/"]
COPY ["src/BuildingBlocks/BuildingBlocks.csproj", "src/BuildingBlocks/"]
COPY ["src/Modules/Catalog/Catalog.csproj", "src/Modules/Catalog/"]
COPY ["src/Modules/Catalog.Contracts/Catalog.Contracts.csproj", "src/Modules/Catalog.Contracts/"]
COPY ["src/Modules/Inventory/Inventory.csproj", "src/Modules/Inventory/"]
COPY ["src/Modules/Inventory.Contracts/Inventory.Contracts.csproj", "src/Modules/Inventory.Contracts/"]
COPY ["src/Modules/Cart/Cart.csproj", "src/Modules/Cart/"]
COPY ["src/Modules/Cart.Contracts/Cart.Contracts.csproj", "src/Modules/Cart.Contracts/"]

# Restore dependencies
RUN dotnet restore "src/Api/Api.csproj"

# Copy everything else
COPY . .

# Build the project
WORKDIR "/src/src/Api"
RUN dotnet build "Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 2: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 3: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Api.dll"]