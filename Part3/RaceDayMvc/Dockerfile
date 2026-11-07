FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY Part3/RaceDayMvc/RaceDayMvc.csproj Part3/RaceDayMvc/
RUN dotnet restore Part3/RaceDayMvc/RaceDayMvc.csproj
COPY Part3/RaceDayMvc/ Part3/RaceDayMvc/
RUN dotnet publish Part3/RaceDayMvc/RaceDayMvc.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
RUN groupadd --gid 10001 raceday && useradd --uid 10001 --gid raceday --create-home raceday
COPY --from=build --chown=raceday:raceday /app .
USER raceday
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDayMvc.dll"]
