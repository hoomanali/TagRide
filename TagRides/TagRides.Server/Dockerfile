FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY TagRides.Server/TagRides.Server.csproj TagRides.Server/
COPY TagRides.Shared/TagRides.Shared.csproj TagRides.Shared/
RUN dotnet restore TagRides.Server/TagRides.Server.csproj
COPY . .
WORKDIR /src/TagRides.Server
RUN dotnet build TagRides.Server.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish TagRides.Server.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TagRides.Server.dll"]
