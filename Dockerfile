FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY ["src/AwesomeGithubStats.Api/AwesomeGithubStats.Api.csproj", "src/AwesomeGithubStats.Api/"]
COPY ["src/AwesomeGithubStats.Core/AwesomeGithubStats.Core.csproj", "src/AwesomeGithubStats.Core/"]
RUN dotnet restore "src/AwesomeGithubStats.Api/AwesomeGithubStats.Api.csproj"
COPY . .
WORKDIR "/repo/src/AwesomeGithubStats.Api"
RUN dotnet build "AwesomeGithubStats.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AwesomeGithubStats.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AwesomeGithubStats.Api.dll"]
