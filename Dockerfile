FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["../TripOrganization_TAQUET/TripOrganization_TAQUET.csproj", "../TripOrganization_TAQUET/"]
RUN dotnet restore "../TripOrganization_TAQUET/TripOrganization_TAQUET.csproj"
COPY . .
WORKDIR "/src/../TripOrganization_TAQUET"
RUN dotnet build "TripOrganization_TAQUET.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TripOrganization_TAQUET.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TripOrganization_TAQUET.dll"]
