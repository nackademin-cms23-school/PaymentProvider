namespace PaymentProvider.Models;

public class LineItemModel
{
    public string priceId { get; set; } = null!;
    public int quantity { get; set; }
}
