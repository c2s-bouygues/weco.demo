#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WeCo.IsAliveAPI/WeCo.IsAliveAPI.csproj", "WeCo.IsAliveAPI/"]
RUN dotnet restore "WeCo.IsAliveAPI/WeCo.IsAliveAPI.csproj"
COPY . .
WORKDIR "/src/WeCo.IsAliveAPI"
RUN dotnet build "WeCo.IsAliveAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WeCo.IsAliveAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeCo.IsAliveAPI.dll"]