FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5029

ENV ASPNETCORE_URLS=http://+:5029

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["ScoringAPI/ScoringAPI.csproj", "ScoringAPI/"]
RUN dotnet restore "ScoringAPI/ScoringAPI.csproj"
COPY . .
WORKDIR "/src/ScoringAPI"
RUN dotnet build "ScoringAPI.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "ScoringAPI.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ScoringAPI.dll"]
