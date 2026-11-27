# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY FlyzoneMockup.csproj ./
RUN dotnet restore

# Copy all source files (including Migrations folder)
COPY . ./
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# The Compose healthcheck probes over HTTP; curl is not in the base image.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && apt-get clean
WORKDIR /app

# .NET 10 aspnet images listen on 8080 by default.
ENV ASPNETCORE_HTTP_PORTS=8080

# Copy published output from build stage
COPY --from=build /app/publish .

# SQLite database and Data Protection keys live on the mounted data volume.
RUN mkdir -p /app/Data /app/Data/keys /app/Data/uploads

EXPOSE 8080

ENTRYPOINT ["dotnet", "FlyzoneMockup.dll"]
