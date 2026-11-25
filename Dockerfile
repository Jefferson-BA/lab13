FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT=GatewayApi
WORKDIR /src
COPY . .
RUN dotnet publish "${PROJECT}/${PROJECT}.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG PROJECT=GatewayApi
ENV APP_PROJECT=${PROJECT}
ENV ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_PROJECT.dll\""]
