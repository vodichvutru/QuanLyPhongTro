namespace QuanLyPhongTro.Application.Common;

/// <summary>Lỗi nghiệp vụ — được middleware chuyển thành HTTP response JSON.</summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
