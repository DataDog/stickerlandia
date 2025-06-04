FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /App

# Deps
COPY src/Stickerlandia.UserManagement.Api/Stickerlandia.UserManagement.Api.csproj src/Stickerlandia.UserManagement.Api/
RUN dotnet restore src/Stickerlandia.UserManagement.Api/Stickerlandia.UserManagement.Api.csproj

# Now copy the rest of the source code
COPY . ./

# Publish the application
RUN dotnet publish src/Stickerlandia.UserManagement.Api/Stickerlandia.UserManagement.Api.csproj -o out -c Release

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /App
COPY --from=build /App/out .
EXPOSE 80
ENTRYPOINT [ "dotnet", "Stickerlandia.UserManagement.Api.dll" ]
