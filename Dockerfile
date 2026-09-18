FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY case-reviews-server/ ./
RUN dotnet restore --locked-mode && dotnet publish -c Release --no-restore -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "case-reviews-server.dll"]
