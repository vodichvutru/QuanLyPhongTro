using System.Net;
using System.Text.Json;
using QuanLyPhongTro.Application.Common;

namespace QuanLyPhongTro.Api.Middleware;

/// <summary>
/// Bắt <see cref="AppException"/> -> trả JSON lỗi nghiệp vụ đúng HTTP status;
/// các lỗi khác -> 500 chung (tránh lộ chi tiết).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = ex.Message, statusCode = ex.StatusCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "Đã xảy ra lỗi máy chủ. Vui lòng thử lại.", statusCode = 500 });
        }
    }
}
