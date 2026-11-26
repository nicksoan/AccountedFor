using System.Text.Json.Serialization;

namespace Fidos.Core;

public sealed class Customer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Contact { get; set; }
    public int TermsDays { get; set; } = 30;
}

public sealed class Supplier
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Contact { get; set; }
    public int TermsDays { get; set; } = 30;
}

public sealed class DocumentLine
{
    public string AccountCode { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Net { get; set; }
}

/// <summary>Common shape of sales and purchase invoices (an "open item" in the sub-ledger).</summary>
public abstract class Invoice
{
    public string Number { get; set; } = "";
    public string PartyId { get; set; } = "";
    public DateOnly Date { get; set; }
    public DateOnly DueDate { get; set; }
    public string? Reference { get; set; }
    public List<DocumentLine> Lines { get; set; } = [];
    public decimal VatRate { get; set; }
    public decimal Net { get; set; }
    public decimal Vat { get; set; }
    public decimal Gross { get; set; }
    public decimal AmountPaid { get; set; }
    public int JournalNumber { get; set; }

    [JsonIgnore]
    public decimal Outstanding => Gross - AmountPaid;

    [JsonIgnore]
    public bool IsPaid => Outstanding == 0m;
}

public sealed class SalesInvoice : Invoice;

public sealed class PurchaseInvoice : Invoice;

public sealed class Allocation
{
    public string InvoiceNumber { get; set; } = "";
    public decimal Amount { get; set; }
}

/// <summary>Common shape of customer receipts and supplier payments.</summary>
public abstract class Settlement
{
    public string Number { get; set; } = "";
    public string PartyId { get; set; } = "";
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string BankAccount { get; set; } = SystemAccounts.Bank;
    public string? Reference { get; set; }
    public List<Allocation> Allocations { get; set; } = [];
    public int JournalNumber { get; set; }
}

public sealed class CustomerReceipt : Settlement;

public sealed class SupplierPayment : Settlement;

public sealed class BookkeepingException(string message) : Exception(message);
