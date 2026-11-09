FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDayMVC/RaceDayMVC.csproj RaceDayMVC/
RUN dotnet restore RaceDayMVC/RaceDayMVC.csproj
COPY . .
RUN dotnet publish RaceDayMVC/RaceDayMVC.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=final-build /app/publish .
ENTRYPOINT ["dotnet", "RaceDayMVC.dll"]