# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["Mitc_Registration_Portal.csproj", "./"]
RUN dotnet restore "Mitc_Registration_Portal.csproj"

# Copy all source code and publish
COPY . .
RUN dotnet publish "Mitc_Registration_Portal.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mitc_Registration_Portal.dll"]
