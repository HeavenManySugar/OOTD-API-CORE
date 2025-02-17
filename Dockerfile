# Use the official .NET 8 SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.20 AS build-env

WORKDIR /app

# Copy the project files
COPY . ./

# Restore the dependencies
RUN dotnet restore

# Build the project
RUN dotnet publish -c Release -o out

# Use the official .NET 8 runtime image as the runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the build output from the build environment
COPY --from=build-env /app/out .

# Expose the port the app runs on
# EXPOSE 5000
EXPOSE 8080

# Set the entry point for the container
ENTRYPOINT ["dotnet", "OOTD-API-ASP.NET-CORE.dll"]