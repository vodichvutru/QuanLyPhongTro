using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuanLyPhongTro.Api.Middleware;
using QuanLyPhongTro.Api.Security;
using QuanLyPhongTro.Application.Options;
using QuanLyPhongTro.Application.Security;
using QuanLyPhongTro.Application.Services;
using QuanLyPhongTro.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- Cấu hình từ appsettings ----
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
          ?? new JwtOptions { Secret = "QuanLyPhongTro_Secret_Key_0123456789" };
if (string.IsNullOrEmpty(jwt.Secret) || jwt.Secret.Length < 32)
    throw new InvalidOperationException("Jwt:Secret phải có ít nhất 32 ký tự.");

// ---- Services ----
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.AddSingleton<ITokenService, AuthTokenService>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<MeterReadingService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<MeService>();
builder.Services.AddScoped<RepairRequestService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ---- JWT Authentication ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ---- Swagger/OpenAPI ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Quản Lý Phòng Trọ API",
        Version = "v1",
        Description = "Hệ thống quản lý phòng trọ và thanh toán tiền thuê (BTL Web nâng cao)."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán token lấy từ POST /api/auth/login. Dạng: <token>"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---- Tự tạo database + seed dữ liệu mẫu (Sprint 0) ----
try
{
    await app.Services.InitializeDatabaseAsync();
    app.Logger.LogInformation("Database sẵn sàng và đã seed dữ liệu mẫu.");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Không kết nối được MySQL. Kiểm tra connection string trong appsettings.json (Default).");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Giao diện web (SPA) nằm trong wwwroot, phục vụ tại /
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
