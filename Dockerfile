FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MesWEB.sln", "./"]
COPY ["MesWEB/MesWEB.csproj", "MesWEB/"]
COPY ["MesWEB.Shared/MesWEB.Shared.csproj", "MesWEB.Shared/"]
COPY ["MesWEB.ExcelCompare/MesWEB.ExcelCompare.csproj", "MesWEB.ExcelCompare/"]
COPY ["MesWEB.GrowthNote/MesWEB.GrowthNote.csproj", "MesWEB.GrowthNote/"]

RUN dotnet restore "MesWEB.sln"

COPY ["MesWEB/appsettings.json", "MesWEB/"]
COPY . .

WORKDIR /src/MesWEB
RUN dotnet publish "MesWEB.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:6100
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 6100

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MesWEB.dll"]
