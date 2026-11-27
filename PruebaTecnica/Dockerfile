FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY PruebaTecnica.csproj ./

RUN dotnet restore PruebaTecnica.csproj

COPY . .

RUN dotnet publish PruebaTecnica.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "PruebaTecnica.dll"]