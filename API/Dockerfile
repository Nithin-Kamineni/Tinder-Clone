# Use the official .NET SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

# Set the working directory inside the container
WORKDIR /app

# Expose the port on which the application will listen
EXPOSE 8080

# Copy the .csproj and restore the dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the entire project and publish it
COPY . ./
RUN dotnet publish -c Release -o out

# Use the official .NET Runtime image for deployment
FROM mcr.microsoft.com/dotnet/aspnet:7.0

# Set the working directory inside the container
WORKDIR /app

# Copy the published files from the build-env stage
COPY --from=build-env /app/out .

# Command to start the .NET application
ENTRYPOINT ["dotnet", "API.dll"]
