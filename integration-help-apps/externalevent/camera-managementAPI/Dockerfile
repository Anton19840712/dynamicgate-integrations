FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 7080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CameraManagementAPI.csproj", "."]
RUN dotnet restore "CameraManagementAPI.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "CameraManagementAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CameraManagementAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CameraManagementAPI.dll"]