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
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Create Data directory for SQLite database persistence
RUN mkdir -p /app/Data

EXPOSE 80

ENTRYPOINT ["dotnet", "FlyzoneMockup.dll"]