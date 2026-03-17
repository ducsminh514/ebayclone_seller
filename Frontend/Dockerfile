# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["Frontend/EbayClone.Frontend/EbayClone.Frontend.csproj", "Frontend/EbayClone.Frontend/"]
COPY ["Shared/EbayClone.Shared/EbayClone.Shared.csproj", "Shared/EbayClone.Shared/"]
RUN dotnet restore "Frontend/EbayClone.Frontend/EbayClone.Frontend.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/Frontend/EbayClone.Frontend"
RUN dotnet publish "EbayClone.Frontend.csproj" -c Release -o /app/publish

# Stage 2: Runtime (Nginx)
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
# Copy published WASM files to Nginx html directory
COPY --from=build /app/publish/wwwroot .
# Copy custom Nginx config for Blazor routing
COPY Frontend/EbayClone.Frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

#docker-compose up --build