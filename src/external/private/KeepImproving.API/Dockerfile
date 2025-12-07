FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["KeepImproving.API.csproj", "KeepImprovingDocker/"]
RUN dotnet restore "KeepImprovingDocker/KeepImproving.API.csproj"
WORKDIR "/src/KeepImprovingDocker"
COPY . .

RUN dotnet build "KeepImproving.API.csproj" -c Release -o /app/build

FROM build AS publish

RUN dotnet publish "KeepImproving.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "KeepImproving.API.dll"]
