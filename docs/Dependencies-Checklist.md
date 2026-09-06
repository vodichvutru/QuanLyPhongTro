# Dependencies Checklist

## ✅ Technical Stack (đã xác định trong tài liệu)

| Component | Version | Source | Status |
|-----------|---------|--------|--------|
| .NET SDK | 8.0+ | Microsoft | ✅ |
| ASP.NET Core | 8.0 | Microsoft | ✅ |
| MySQL | 8.0+ | MySQL AB | ✅ |

---

## ✅ NuGet Packages

### Core Packages (Sprint 0)

| Package | Version | Purpose | Sprint | Status |
|---------|---------|---------|--------|--------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.x | JWT Authentication | 0 | ✅ |
| Pomelo.EntityFrameworkCore.MySql | 8.0.x | MySQL Provider for EF Core | 0 | ✅ |
| Swashbuckle.AspNetCore | 6.x | Swagger/OpenAPI | 0 | ✅ |
| BCrypt.Net-Next | 10.x | Password Hashing | 0 | ✅ |
| FluentValidation.AspNetCore | 11.x | Request Validation | 0-4 | ✅ |
| AutoMapper.Extensions.Microsoft.DependencyInjection | 12.x | DTO Mapping | 0-4 | ✅ |
| Microsoft.EntityFrameworkCore.Design | 8.0.x | EF Core CLI Tools | 0 | ✅ |

### Testing Packages

| Package | Version | Purpose | Sprint | Status |
|---------|---------|---------|--------|--------|
| xunit | 2.x | Unit Testing | 0 | ✅ |
| xunit.runner.visualstudio | 2.x | Test Runner | 0 | ✅ |
| Moq | 4.x | Mocking | 0 | ✅ |
| FluentAssertions | 6.x | Test Assertions | 0 | ✅ |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.x | Integration Testing | 4 | ✅ |

### Optional Packages (Sprint 4)

| Package | Version | Purpose | Sprint | Status |
|---------|---------|---------|--------|--------|
| EPPlus | 7.x | Excel Export | 4 | ⚠️ Optional |
| QuestPDF | 2024.x | PDF Generation | 4 | ⚠️ Optional |

---

## ✅ Infrastructure

| Component | Required | Version | Purpose | Status |
|-----------|----------|---------|--------|--------|
| MySQL Server | ✅ | 8.0+ | Database | ✅ |
| Docker Desktop | ⚠️ Recommended | Latest | Containerization | ⚠️ Optional |
| MySQL Workbench | ⚠️ Optional | Latest | DB Visualization | ⚠️ Optional |

---

## ⚠️ MISSING in Original Documentation

Các dependencies sau **CHƯA** được đề cập trong tài liệu nhưng **CẦN THIẾT**:

| Dependency | Reason | Priority |
|------------|--------|----------|
| Pomelo.EntityFrameworkCore.MySql | MySQL provider cho EF Core | Must |
| BCrypt.Net-Next | Password hashing thay vì built-in | Must |
| FluentValidation | Validation thay vì Data Annotations | Should |
| xunit + Moq | Unit testing | Must |
| FluentAssertions | Better test assertions | Should |
| Microsoft.AspNetCore.Mvc.Testing | Integration testing | Must |
| Docker Compose | Dev environment setup | Should |

---

## 📋 Project File (.csproj) Template

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- Authentication -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
    
    <!-- Database -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.*" />
    
    <!-- Validation & Mapping -->
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.*" />
    
    <!-- API Documentation -->
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
    
    <!-- Security -->
    <PackageReference Include="BCrypt.Net-Next" Version="10.*" />
  </ItemGroup>

</Project>
```

## 📋 Test Project (.csproj) Template

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\src\QuanLyPhongTro.Api\QuanLyPhongTro.Api.csproj" />
  </ItemGroup>

</Project>
```

---

## 🔧 Development Environment Setup

### Required Tools

| Tool | Version | Purpose | Status |
|------|---------|---------|--------|
| .NET 8 SDK | 8.0+ | Runtime & CLI | ❌ NEED TO CHECK |
| Visual Studio 2022 / VS Code | Latest | IDE | ❌ NEED TO CHECK |
| Git | Latest | Version Control | ❌ NEED TO CHECK |
| MySQL Server | 8.0+ | Database | ❌ NEED TO CHECK |

### Recommended Tools

| Tool | Purpose | Status |
|------|---------|--------|
| Docker Desktop | Containerization | ⚠️ Optional |
| MySQL Workbench | DB Management | ⚠️ Optional |
| Postman | API Testing | ⚠️ Optional |
| DBeaver | Universal DB Client | ⚠️ Optional |

---

## ✅ Pre-Sprint 0 Checklist

Trước khi bắt đầu Sprint 0, cần verify:

- [ ] .NET 8 SDK installed (`dotnet --version`)
- [ ] MySQL Server installed & running
- [ ] Git installed
- [ ] IDE configured (VS 2022 or VS Code with C# extension)
- [ ] NuGet packages restored
- [ ] Database connection verified

---

## 📝 Update Recommendation

Khuyến nghị cập nhật tài liệu **Quan_Ly_Phong_Tro.md** phần **6.8 Deployment, configuration và operations** để thêm:

```markdown
### Dependencies & NuGet Packages
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.x
- Pomelo.EntityFrameworkCore.MySql 8.0.x
- Swashbuckle.AspNetCore 6.x
- BCrypt.Net-Next 10.x
- FluentValidation.AspNetCore 11.x
- AutoMapper.Extensions.Microsoft.DependencyInjection 12.x
- xunit 2.x + Moq 4.x (testing)
```

---

## Summary

| Category | Total | Complete | Missing |
|----------|-------|----------|---------|
| Technical Stack | 3 | 3 | 0 |
| NuGet Packages | 12 | 12 | 0 |
| Infrastructure | 3 | 2 | 1 |
| Dev Tools | 4 | 0 | 4 |

**Status: ⚠️ Cần verify dev environment trước Sprint 0**
