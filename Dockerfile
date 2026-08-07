# Employee360 HRMS API — production image for Render (Docker web service).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Employee360.sln ./
COPY src/Employee360.Domain/Employee360.Domain.csproj src/Employee360.Domain/
COPY src/Employee360.Application/Employee360.Application.csproj src/Employee360.Application/
COPY src/Employee360.Infrastructure/Employee360.Infrastructure.csproj src/Employee360.Infrastructure/
COPY src/Employee360.API/Employee360.API.csproj src/Employee360.API/

RUN dotnet restore src/Employee360.API/Employee360.API.csproj

COPY src/ src/

RUN dotnet tool install --global dotnet-ef --version 8.0.*
ENV PATH="${PATH}:/root/.dotnet/tools"

RUN dotnet ef migrations bundle \
    --project src/Employee360.Infrastructure/Employee360.Infrastructure.csproj \
    --startup-project src/Employee360.API/Employee360.API.csproj \
    --output /app/efbundle \
    --configuration Release

RUN dotnet publish src/Employee360.API/Employee360.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build /app/publish .
COPY --from=build /app/efbundle ./efbundle
COPY docker-entrypoint.sh .
RUN chmod +x docker-entrypoint.sh efbundle

EXPOSE 8080

ENTRYPOINT ["./docker-entrypoint.sh"]
