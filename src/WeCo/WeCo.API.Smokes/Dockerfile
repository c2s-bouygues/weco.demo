#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WeCo.API.Smokes/WeCo.API.Smokes.csproj", "WeCo.API.Smokes/"]
RUN dotnet restore "WeCo.API.Smokes/WeCo.API.Smokes.csproj"
COPY . .
WORKDIR "/src/WeCo.API.Smokes"
RUN dotnet build "WeCo.API.Smokes.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WeCo.API.Smokes.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeCo.API.Smokes.dll"]