#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WoG.Accounts/WoG.Accounts.Api/WoG.Accounts.Api.csproj", "WoG.Accounts/WoG.Accounts.Api/"]
RUN dotnet restore "./WoG.Accounts/WoG.Accounts.Api/WoG.Accounts.Api.csproj"
COPY . .
WORKDIR "/src/WoG.Accounts/WoG.Accounts.Api"
RUN dotnet build "./WoG.Accounts.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WoG.Accounts.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WoG.Accounts.Api.dll"]