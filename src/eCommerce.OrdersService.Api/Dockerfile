# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/eCommerce.OrdersService.Api/eCommerce.OrdersService.Api.csproj", "src/eCommerce.OrdersService.Api/"]
RUN dotnet restore "./src/eCommerce.OrdersService.Api/eCommerce.OrdersService.Api.csproj"
COPY . .
WORKDIR "/src/src/eCommerce.OrdersService.Api"
RUN dotnet build "./eCommerce.OrdersService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./eCommerce.OrdersService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV MONGODB_HOST=localhost
ENV MONGODB_PORT=27017
ENV MONGODB_DATABASE=eCommerceOrdersServiceDb
ENV UsersMicroserviceName=localhost
ENV UsersMicroservicePort=9090
ENV ProductsMicroserviceName=localhost
ENV ProductsMicroservicePort=8080
ENTRYPOINT ["dotnet", "eCommerce.OrdersService.Api.dll"]