namespace BillingService.Models;

public class Invoice
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Status { get; set; } = "Aberta";
    public List<InvoiceItem> Items { get; set; } = [];
}