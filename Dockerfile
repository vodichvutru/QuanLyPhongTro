# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj trước để tận dụng layer cache của Docker
COPY src/QuanLyPhongTro.Api/QuanLyPhongTro.Api.csproj            src/QuanLyPhongTro.Api/
COPY src/QuanLyPhongTro.Application/QuanLyPhongTro.Application.csproj src/QuanLyPhongTro.Application/
COPY src/QuanLyPhongTro.Core/QuanLyPhongTro.Core.csproj          src/QuanLyPhongTro.Core/
COPY src/QuanLyPhongTro.Infrastructure/QuanLyPhongTro.Infrastructure.csproj src/QuanLyPhongTro.Infrastructure/

RUN dotnet restore src/QuanLyPhongTro.Api/QuanLyPhongTro.Api.csproj

COPY . .
RUN dotnet publish src/QuanLyPhongTro.Api/QuanLyPhongTro.Api.csproj \
    -c Release -o /app/publish --no-restore

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "QuanLyPhongTro.Api.dll"]
