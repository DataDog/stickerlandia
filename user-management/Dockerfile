FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG TARGETPLATFORM

WORKDIR /App

COPY ./scripts ./scripts

# Download the latest version of the tracer but don't install yet
RUN . scripts/target.sh && curl -Lo /tmp/datadog-dotnet-apm.deb https://github.com/DataDog/dd-trace-dotnet/releases/download/v3.19.0/datadog-dotnet-apm_3.19.0_${TRACER_ARCHITECTURE}.deb

# Copy all project files for restore (include all dependencies)
COPY src/Stickerlandia.UserManagement.Api/Stickerlandia.UserManagement.Api.csproj src/Stickerlandia.UserManagement.Api/
COPY src/Stickerlandia.UserManagement.Worker/Stickerlandia.UserManagement.Worker.csproj src/Stickerlandia.UserManagement.Worker/
COPY src/Stickerlandia.UserManagement.MigrationService/Stickerlandia.UserManagement.MigrationService.csproj src/Stickerlandia.UserManagement.MigrationService/
COPY src/Stickerlandia.UserManagement.Core/Stickerlandia.UserManagement.Core.csproj src/Stickerlandia.UserManagement.Core/
COPY src/Stickerlandia.UserManagement.Agnostic/Stickerlandia.UserManagement.Agnostic.csproj src/Stickerlandia.UserManagement.Agnostic/
COPY src/Stickerlandia.UserManagement.Auth/Stickerlandia.UserManagement.Auth.csproj src/Stickerlandia.UserManagement.Auth/
COPY src/Stickerlandia.UserManagement.Azure/Stickerlandia.UserManagement.Azure.csproj src/Stickerlandia.UserManagement.Azure/
COPY src/Stickerlandia.UserManagement.ServiceDefaults/Stickerlandia.UserManagement.ServiceDefaults.csproj src/Stickerlandia.UserManagement.ServiceDefaults/
COPY src/Stickerlandia.UserManagement.AWS/Stickerlandia.UserManagement.AWS.csproj src/Stickerlandia.UserManagement.AWS/

# Restore all projects
RUN dotnet restore src/Stickerlandia.UserManagement.Api/Stickerlandia.UserManagement.Api.csproj
RUN dotnet restore src/Stickerlandia.UserManagement.Worker/Stickerlandia.UserManagement.Worker.csproj
RUN dotnet restore src/Stickerlandia.UserManagement.MigrationService/Stickerlandia.UserManagement.MigrationService.csproj

# Copy the rest of the source code
COPY . ./

# Publish all applications
RUN dotnet publish src/Stickerlandia.UserManagement.Api/Stickerlandia.UserManagement.Api.csproj -o out/api -c Release
RUN dotnet publish src/Stickerlandia.UserManagement.Worker/Stickerlandia.UserManagement.Worker.csproj -o out/worker -c Release
RUN dotnet publish src/Stickerlandia.UserManagement.MigrationService/Stickerlandia.UserManagement.MigrationService.csproj -o out/migrations -c Release

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install curl
RUN apt update && apt install -y curl && apt clean

# Copy the tracer from build target
COPY --from=build /tmp/datadog-dotnet-apm.deb /tmp/datadog-dotnet-apm.deb
# Install the tracer
RUN mkdir -p /opt/datadog \
    && mkdir -p /var/log/datadog \
    && dpkg -i /tmp/datadog-dotnet-apm.deb \
    && rm /tmp/datadog-dotnet-apm.deb

ARG DD_GIT_REPOSITORY_URL
ARG DD_GIT_COMMIT_SHA

ENV DD_GIT_REPOSITORY_URL=${DD_GIT_REPOSITORY_URL} 
ENV DD_GIT_COMMIT_SHA=${DD_GIT_COMMIT_SHA}

# These environment variables configure the Datadog .NET Tracer
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
ENV DD_INTEGRATIONS=/opt/datadog/integrations.json
ENV DD_LOGS_INJECTION=true
ENV DD_RUNTIME_METRICS_ENABLED=true
ENV DD_PROFILING_ENABLED=1
LABEL com.datadoghq.tags.service="user-service"
LABEL com.datadoghq.tags.version=${DD_GIT_COMMIT_SHA}

WORKDIR /App
# Copy all published applications
COPY --from=build /App/out/api ./api/
COPY --from=build /App/out/worker ./worker/
COPY --from=build /App/out/migrations ./migrations/

EXPOSE 8080

# Default entry point (can be overridden in docker-compose)
ENTRYPOINT ["dotnet"]
CMD ["api/Stickerlandia.UserManagement.Api.dll"]