FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CompareWorksheets.sln", "./"]
COPY ["MesWEB/CompareWorksheets.csproj", "MesWEB/"]
COPY ["MesWEB.Shared/CompareWorksheets.Shared.csproj", "MesWEB.Shared/"]
COPY ["MesWEB.ExcelCompare/CompareWorksheets.ExcelCompare.csproj", "MesWEB.ExcelCompare/"]
COPY ["MesWEB.GrowthNote/MesWEB.GrowthNote.csproj", "MesWEB.GrowthNote/"]

RUN dotnet restore "CompareWorksheets.sln"

COPY ["MesWEB/appsettings.json", "MesWEB/"]
COPY . .

WORKDIR /src/MesWEB
RUN dotnet publish "CompareWorksheets.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:6100
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 6100

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MesWEB.dll"]

