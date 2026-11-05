ARG BUILD_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0
FROM ${BUILD_IMAGE} AS build
WORKDIR /src
COPY Api/Api.csproj Api/
RUN dotnet restore Api/Api.csproj
COPY Api/ Api/
COPY Shared/ Shared/
RUN dotnet publish Api/Api.csproj -c Release -o /out --no-restore

FROM ${RUNTIME_IMAGE}
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Api.dll"]
