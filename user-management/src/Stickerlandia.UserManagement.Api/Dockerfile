FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG TARGETPLATFORM

WORKDIR /App

COPY ./scripts ./scripts

# Download the latest version of the tracer but don't install yet
RUN . scripts/target.sh && curl -Lo /tmp/datadog-dotnet-apm.deb https://github.com/DataDog/dd-trace-dotnet/releases/download/v3.34.0/datadog-dotnet-apm_3.34.0_${TRACER_ARCHITECTURE}.deb

# Deps
COPY src/Stickerlandia.UserManagement.Api/Stickerlandia.UserManagement.Api.csproj src/Stickerlandia.UserManagement.Api/
RUN dotnet restore src/Stickerlandia.UserManagement.Api/Stickerlandia.UserManagement.Api.csproj

# Now copy the rest of the source code
COPY . ./

# Publish the application
RUN dotnet publish src/Stickerlandia.UserManagement.Api/Stickerlandia.UserManagement.Api.csproj -o out -c Release

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Copy the tracer from build target
COPY --from=build /tmp/datadog-dotnet-apm.deb /tmp/datadog-dotnet-apm.deb
# Install curl and the tracer
RUN apt-get update \
    && apt-get install -y curl \
    && mkdir -p /opt/datadog \
    && mkdir -p /var/log/datadog \
    && dpkg -i /tmp/datadog-dotnet-apm.deb \
    && rm /tmp/datadog-dotnet-apm.deb \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

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
COPY --from=build /App/out .
EXPOSE 8080
ENTRYPOINT [ "dotnet", "Stickerlandia.UserManagement.Api.dll" ]
