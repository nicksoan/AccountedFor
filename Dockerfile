# Build: docker build -t accountedfor .
# Run:   docker run -p 8080:8080 -v fidos-data:/app/App_Data accountedfor
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json AccountedFor.sln ./
COPY src/Fidos.Core/Fidos.Core.csproj src/Fidos.Core/
COPY src/AccountedFor.Web/AccountedFor.Web.csproj src/AccountedFor.Web/
RUN dotnet restore src/AccountedFor.Web/AccountedFor.Web.csproj
COPY src/ src/
RUN dotnet publish src/AccountedFor.Web/AccountedFor.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/App_Data/sandboxes && chown -R app:app /app/App_Data
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
VOLUME ["/app/App_Data"]
ENTRYPOINT ["dotnet", "AccountedFor.Web.dll"]
