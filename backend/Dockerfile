# Backend image: the published API plus an EF Core migrations bundle
# (efbundle), which docker-compose.yml runs as the one-shot "migrate" service
# before the API starts. Build context: backend/

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, so the package layer stays cached until a project file changes.
COPY GameHub.Domain/GameHub.Domain.csproj GameHub.Domain/
COPY GameHub.Application/GameHub.Application.csproj GameHub.Application/
COPY GameHub.Infrastructure/GameHub.Infrastructure.csproj GameHub.Infrastructure/
COPY GameHub.Api/GameHub.Api.csproj GameHub.Api/
RUN dotnet restore GameHub.Api/GameHub.Api.csproj

COPY . .
RUN dotnet publish GameHub.Api/GameHub.Api.csproj -c Release -o /app/publish --no-restore

# Same dotnet-ef version as the EF Core packages. Build stage only: the tool
# is not part of the runtime image.
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="${PATH}:/root/.dotnet/tools"

# Building the bundle runs Program.cs, whose startup checks require a
# connection string, JWT settings and AllowedHosts. These placeholders only
# satisfy those checks for this command: nothing connects to a database or
# signs a token, and they are not kept in the image. At runtime the bundle and
# the API read the real values from the environment (docker-compose.yml / .env).
RUN ConnectionStrings__DefaultConnection="Host=design-time-only" \
    Jwt__Key="design-time-placeholder-not-a-secret-000000" \
    Jwt__Issuer="design-time-only" \
    Jwt__Audience="design-time-only" \
    AllowedHosts="localhost" \
    dotnet ef migrations bundle \
    --project GameHub.Infrastructure \
    --startup-project GameHub.Api \
    --configuration Release \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/efbundle .

# Non-root user provided by the .NET images; Kestrel listens on 8080.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "GameHub.Api.dll"]
