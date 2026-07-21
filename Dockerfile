# Stage 1: Base runtime for the final API
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Run as root temporarily to set up the database folder permissions
USER root
WORKDIR /app
RUN mkdir /app/data && chown app:app /app/data

# Switch back to the secure app user
USER app
EXPOSE 8080

# Stage 2: SDK image for building and restoring
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything into the container first to avoid pathing errors
COPY . .

# Restore the solution directly
RUN dotnet restore "Accessories.Api/Accessories.Api.csproj"
RUN dotnet restore "Accessories.Api.Tests/Accessories.Api.Tests.csproj"

# Stage 3: The Test Runner
# We branch off the 'build' stage so docker-compose can target this specifically
FROM build AS test
WORKDIR /src/Accessories.Api.Tests
ENTRYPOINT ["dotnet", "test", "--logger:console;verbosity=detailed"]

# Stage 4: Publish the compiled API
FROM build AS publish
WORKDIR /src/Accessories.Api
RUN dotnet publish "Accessories.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 5: The Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Accessories.Api.dll"]