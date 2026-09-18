FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Domain/Domain.csproj           Domain/
COPY Controller/Controller.csproj   Controller/
COPY Service/Service.csproj         Service/
COPY Repository/Repository.csproj   Repository/
COPY Backend/Backend.csproj         Backend/

RUN dotnet restore Backend/Backend.csproj

COPY . .

RUN dotnet publish Backend/Backend.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p Logs

EXPOSE 5000

ENTRYPOINT ["dotnet", "Backend.dll"]
