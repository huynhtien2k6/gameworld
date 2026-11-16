FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY GameWorld.Domain/GameWorld.Domain.csproj GameWorld.Domain/
COPY GameWorld.Infrastructure/GameWorld.Infrastructure.csproj GameWorld.Infrastructure/
COPY GameWorld.Api/GameWorld.Api.csproj GameWorld.Api/
RUN dotnet restore GameWorld.Api/GameWorld.Api.csproj
COPY GameWorld.Domain/ GameWorld.Domain/
COPY GameWorld.Infrastructure/ GameWorld.Infrastructure/
COPY GameWorld.Api/ GameWorld.Api/
RUN dotnet publish GameWorld.Api/GameWorld.Api.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/ ./
RUN mkdir -p /app/App_Data && chown -R app:app /app/App_Data
USER app
ENV ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://127.0.0.1:8080
ENTRYPOINT ["dotnet", "GameWorld.Api.dll"]
