# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Backend/EbayClone.API/EbayClone.API.csproj", "Backend/EbayClone.API/"]
COPY ["Backend/EbayClone.Application/EbayClone.Application.csproj", "Backend/EbayClone.Application/"]
COPY ["Backend/EbayClone.Domain/EbayClone.Domain.csproj", "Backend/EbayClone.Domain/"]
COPY ["Backend/EbayClone.Infrastructure/EbayClone.Infrastructure.csproj", "Backend/EbayClone.Infrastructure/"]
COPY ["Shared/EbayClone.Shared/EbayClone.Shared.csproj", "Shared/EbayClone.Shared/"]

RUN dotnet restore "Backend/EbayClone.API/EbayClone.API.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/Backend/EbayClone.API"
RUN dotnet build "EbayClone.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "EbayClone.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EbayClone.API.dll"]
#docker-compose up --build