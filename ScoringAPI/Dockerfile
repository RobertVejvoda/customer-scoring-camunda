FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ScoringAPI/ScoringAPI.csproj", "ScoringAPI/"]
RUN dotnet restore "ScoringAPI/ScoringAPI.csproj"
COPY . .
WORKDIR "/src/ScoringAPI"
RUN dotnet build "ScoringAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ScoringAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER $APP_UID
ENTRYPOINT ["./ScoringAPI.dll"]
