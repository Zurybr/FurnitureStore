#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["FurnitureStore.API/FurnitureStore.API.csproj", "FurnitureStore.API/"]
RUN dotnet restore "FurnitureStore.API/FurnitureStore.API.csproj"
COPY . .
WORKDIR "/src/FurnitureStore.API"
RUN dotnet build "FurnitureStore.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FurnitureStore.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FurnitureStore.API.dll"]