using QuanLyPhongTro.Application.Billing;
using QuanLyPhongTro.Application.Dtos;

namespace QuanLyPhongTro.Tests;

public class InvoiceCalculatorTests
{
    [Fact]
    public void ComputeUsage_SubtractsOpeningFromClosing()
    {
        var (kwh, m3) = InvoiceCalculator.ComputeUsage(120, 160, 5, 12);
        Assert.Equal(40, kwh);
        Assert.Equal(7, m3);
    }

    [Theory]
    [InlineData(120, 160)] // điện giảm (cuối < đầu)
    [InlineData(5, 10)]    // nước giảm
    public void ComputeUsage_NegativeConsumption_Throws(decimal electricClose, decimal electricOpen)
    {
        Assert.Throws<ArgumentException>(() => InvoiceCalculator.ComputeUsage(electricOpen, electricClose, 0, 5));
    }

    [Fact]
    public void BuildItems_RentAlwaysIncluded_UtilitiesWhenUsed()
    {
        var items = InvoiceCalculator.BuildItems("P101", 2_500_000, 40, 3500, 7, 25_000, null);

        var names = items.Select(i => i.Name).ToList();
        Assert.Contains("Tiền phòng P101", names);
        Assert.Contains("Tiền điện", names);
        Assert.Contains("Tiền nước", names);

        var rent = items.Single(i => i.Name == "Tiền phòng P101");
        Assert.Equal(2_500_000, rent.Amount);
        Assert.Equal(40, items.Single(i => i.Name == "Tiền điện").Quantity);
    }

    [Fact]
    public void BuildItems_SkipsZeroUsageAndBadExtras_AddsGoodExtras()
    {
        var items = InvoiceCalculator.BuildItems("P101", 2_500_000, 0, 3500, 0, 25_000,
            new List<ExtraFeeLine>
            {
                new("Phí vệ sinh", 50_000),
                new("", 30_000),      // tên trống → bỏ qua
                new("Tên", -5)        // âm → bỏ qua
            });

        Assert.DoesNotContain(items, i => i.Name == "Tiền điện");
        Assert.DoesNotContain(items, i => i.Name == "Tiền nước");
        Assert.Contains(items, i => i.Name == "Phí vệ sinh" && i.Amount == 50_000);
        Assert.Single(items, i => i.Name != "Tiền phòng P101");
    }

    [Fact]
    public void SumItems_AddsAllLineAmounts()
    {
        var items = InvoiceCalculator.BuildItems("P101", 2_500_000, 40, 3500, 7, 25_000,
            new List<ExtraFeeLine> { new("Phí vệ sinh", 50_000) });

        var expected = 2_500_000 + 40 * 3500 + 7 * 25_000 + 50_000;
        Assert.Equal(expected, InvoiceCalculator.SumItems(items));
    }
}
