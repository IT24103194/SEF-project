FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/SmartGym.Api/SmartGym.Api.csproj", "backend/SmartGym.Api/"]
RUN dotnet restore "backend/SmartGym.Api/SmartGym.Api.csproj"

COPY backend/SmartGym.Api/ backend/SmartGym.Api/
WORKDIR "/src/backend/SmartGym.Api"
RUN dotnet build "SmartGym.Api.csproj" -c Release -o /app/build
RUN dotnet publish "SmartGym.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartGym.Api.dll"]
