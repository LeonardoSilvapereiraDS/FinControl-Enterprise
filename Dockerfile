FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY FinControl.Domain/ FinControl.Domain/
COPY FinControl.Application/ FinControl.Application/
COPY FinControl.Infrastructure/ FinControl.Infrastructure/
COPY FinControl.Web/ FinControl.Web/
RUN dotnet publish FinControl.Web/FinControl.Web.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0
USER root
RUN mkdir -p /data && chown app:app /data
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 FINCONTROL_DATA_DIR=/data
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinControl.Web.dll"]
