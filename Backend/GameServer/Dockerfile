ARG BUILD_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0
FROM ${BUILD_IMAGE} AS build
WORKDIR /src
COPY GameServer/GameServer.csproj GameServer/
RUN dotnet restore GameServer/GameServer.csproj -r linux-x64 -p:PublishReadyToRun=true
COPY GameServer/ GameServer/
COPY Shared/ Shared/
# Precompile first-use server paths so gameplay does not pay their JIT cost during a tick.
RUN dotnet publish GameServer/GameServer.csproj -c Release -r linux-x64 --self-contained false -p:PublishReadyToRun=true -o /out --no-restore

FROM ${RUNTIME_IMAGE}
WORKDIR /app
COPY --from=build /out .
EXPOSE 7777/udp
ENTRYPOINT ["dotnet", "GameServer.dll"]
