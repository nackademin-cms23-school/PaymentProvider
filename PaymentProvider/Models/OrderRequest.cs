namespace PaymentProvider.Models;

public class OrderRequest
{
    public List<LineItemModel> LineItems { get; set; } = [];
}
