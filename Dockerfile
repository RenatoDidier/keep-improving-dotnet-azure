FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["KeepImproving.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Build.Reference.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/core/KeepImproving.Domain/KeepImproving.Domain.csproj", "src/core/KeepImproving.Domain/"]
COPY ["src/core/KeepImproving.Application/KeepImproving.Application.csproj", "src/core/KeepImproving.Application/"]
COPY ["src/external/KeepImproving.Infra/KeepImproving.Infra.csproj", "src/external/KeepImproving.Infra/"]
COPY ["src/external/private/KeepImproving.API/KeepImproving.API.csproj", "src/external/private/KeepImproving.API/"]

COPY ["src/tests/core/KeepImproving.Domain.Test/KeepImproving.Domain.Test.csproj", "src/tests/core/KeepImproving.Domain.Test/"]
COPY ["src/tests/core/KeepImproving.Application.Test/KeepImproving.Application.Test.csproj", "src/tests/core/KeepImproving.Application.Test/"]
COPY ["src/tests/external/KeepImproving.Infra.Test/KeepImproving.Infra.Test.csproj", "src/tests/external/KeepImproving.Infra.Test/"]
COPY ["src/tests/external/private/KeepImproving.API.Test/KeepImproving.API.Test.csproj", "src/tests/external/private/KeepImproving.API.Test/"]

RUN dotnet restore "KeepImproving.sln"
WORKDIR "/src"
COPY . .

RUN dotnet build "src/external/private/KeepImproving.API/KeepImproving.API.csproj" -c Release -o /app/build

FROM build AS publish

RUN dotnet publish "src/external/private/KeepImproving.API/KeepImproving.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "KeepImproving.API.dll"]
