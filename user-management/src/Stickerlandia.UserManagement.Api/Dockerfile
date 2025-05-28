FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /App

# Deps
COPY src/Stickerlandia.UserManagement.AspNet/Stickerlandia.UserManagement.AspNet.csproj src/Stickerlandia.UserManagement.AspNet/
RUN dotnet restore src/Stickerlandia.UserManagement.AspNet/Stickerlandia.UserManagement.AspNet.csproj

# Now copy the rest of the source code
COPY . ./

# Publish the application
RUN dotnet publish src/Stickerlandia.UserManagement.AspNet/Stickerlandia.UserManagement.AspNet.csproj -o out -c Release -r linux-arm64

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled AS runtime
WORKDIR /App
COPY --from=build /App/out .
EXPOSE 80
ENTRYPOINT [ "dotnet", "Stickerlandia.UserManagement.AspNet.dll" ]
