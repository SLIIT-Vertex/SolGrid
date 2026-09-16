FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SolGrid.Domain/SolGrid.Domain.csproj src/SolGrid.Domain/
COPY src/SolGrid.Application/SolGrid.Application.csproj src/SolGrid.Application/
COPY src/SolGrid.Infrastructure/SolGrid.Infrastructure.csproj src/SolGrid.Infrastructure/
COPY src/SolGrid.Api/SolGrid.Api.csproj src/SolGrid.Api/

RUN dotnet restore src/SolGrid.Api/SolGrid.Api.csproj

COPY src/ src/
RUN dotnet publish src/SolGrid.Api/SolGrid.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

RUN groupadd --system solgrid \
    && useradd --system --gid solgrid --create-home solgrid

COPY --from=build /app/publish .

USER solgrid
EXPOSE 8080

ENTRYPOINT ["dotnet", "SolGrid.Api.dll"]
