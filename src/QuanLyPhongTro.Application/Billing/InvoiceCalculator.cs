using QuanLyPhongTro.Application.Dtos;

namespace QuanLyPhongTro.Application.Billing;

/// <summary>
/// Thuần tính toán hóa đơn (không phụ thuộc DB) — tách riêng để viết unit test.
/// </summary>
public static class InvoiceCalculator
{
    /// <summary>Tính điện (kWh) và nước (m³) tiêu thụ = chỉ số cuối − chỉ số đầu.</summary>
    public static (decimal ElectricKwh, decimal WaterM3) ComputeUsage(
        decimal electricOpening, decimal electricClosing, decimal waterOpening, decimal waterClosing)
    {
        var kwh = electricClosing - electricOpening;
        var m3 = waterClosing - waterOpening;
        if (kwh < 0) throw new ArgumentException("Chỉ số điện cuối nhỏ hơn chỉ số đầu.", nameof(electricClosing));
        if (m3 < 0) throw new ArgumentException("Chỉ số nước cuối nhỏ hơn chỉ số đầu.", nameof(waterClosing));
        return (kwh, m3);
    }

    /// <summary>
    /// Dựng các dòng hóa đơn theo thứ tự: tiền phòng, tiền điện, tiền nước, rồi các khoản khác.
    /// </summary>
    public static IReadOnlyList<InvoiceItemSpec> BuildItems(
        string roomName,
        decimal monthlyRent,
        decimal electricKwh, decimal electricPrice,
        decimal waterM3, decimal waterPrice,
        IReadOnlyList<ExtraFeeLine>? extras)
    {
        var items = new List<InvoiceItemSpec>
        {
            new("Tiền phòng " + roomName, 1, null, monthlyRent, monthlyRent)
        };

        if (electricKwh > 0)
        {
            var amount = Round2(electricKwh * electricPrice);
            items.Add(new InvoiceItemSpec("Tiền điện", electricKwh, "kWh", electricPrice, amount));
        }
        if (waterM3 > 0)
        {
            var amount = Round2(waterM3 * waterPrice);
            items.Add(new InvoiceItemSpec("Tiền nước", waterM3, "m³", waterPrice, amount));
        }
        if (extras is not null)
        {
            foreach (var extra in extras)
            {
                if (string.IsNullOrWhiteSpace(extra.Name) || extra.Amount <= 0) continue;
                items.Add(new InvoiceItemSpec(extra.Name.Trim(), 1, null, extra.Amount, extra.Amount));
            }
        }

        return items;
    }

    public static decimal SumItems(IEnumerable<InvoiceItemSpec> items) =>
        Round2(items.Sum(i => i.Amount));

    public static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Đặc tả một dòng hóa đơn trước khi lưu DB.</summary>
public record InvoiceItemSpec(string Name, decimal Quantity, string? Unit, decimal UnitPrice, decimal Amount);
