FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /App

# Deps
COPY src/Stickerlandia.PrintService.Worker/Stickerlandia.PrintService.Worker.csproj src/Stickerlandia.PrintService.Worker/
RUN dotnet restore src/Stickerlandia.PrintService.Worker/Stickerlandia.PrintService.Worker.csproj

# Now copy the rest of the source code
COPY . ./

# Publish the application
RUN dotnet publish src/Stickerlandia.PrintService.Worker/Stickerlandia.PrintService.Worker.csproj -o out -c Release -r linux-arm64

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled AS runtime
WORKDIR /App
COPY --from=build /App/out .
EXPOSE 80
ENTRYPOINT [ "dotnet", "Stickerlandia.PrintService.Worker.dll" ]
