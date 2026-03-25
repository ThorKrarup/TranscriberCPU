FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TranscriberCPU/TranscriberCPU.csproj", "TranscriberCPU/"]
RUN dotnet restore "TranscriberCPU/TranscriberCPU.csproj"
COPY . .
WORKDIR "/src/TranscriberCPU"
RUN dotnet build "./TranscriberCPU.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TranscriberCPU.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TranscriberCPU.dll"]
