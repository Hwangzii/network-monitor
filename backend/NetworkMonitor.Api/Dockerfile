# Stage 1: Build (runtime)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["NetworkMonitor.Api.csproj", "."]
RUN dotnet restore "NetworkMonitor.Api.csproj"
COPY . .
RUN dotnet publish "NetworkMonitor.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime (nhỏ hơn, production-ready)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080 
ENTRYPOINT ["dotnet", "NetworkMonitor.Api.dll"]