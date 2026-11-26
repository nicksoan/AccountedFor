using System.Text.Json.Serialization;

namespace Fidos.Core;

/// <summary>Where a journal originated. Sub-ledgers post their own journals to the general ledger.</summary>
public enum JournalSource
{
    Manual,
    SalesLedger,
    PurchaseLedger,
    YearEnd,
}

public sealed class JournalEntry
{
    public int Number { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = "";
    public string? Reference { get; set; }
    public JournalSource Source { get; set; }
    public List<JournalLine> Lines { get; set; } = [];
    public int? ReversalOf { get; set; }
    public int? ReversedBy { get; set; }
    public string PostedBy { get; set; } = "";
    public DateTime PostedUtc { get; set; }

    [JsonIgnore]
    public decimal TotalDebits => Lines.Sum(l => l.Debit);
    [JsonIgnore]
    public decimal TotalCredits => Lines.Sum(l => l.Credit);

    public static string SourceCode(JournalSource source) => source switch
    {
        JournalSource.Manual => "GL",
        JournalSource.SalesLedger => "AR",
        JournalSource.PurchaseLedger => "AP",
        JournalSource.YearEnd => "YE",
        _ => "??",
    };
}

public sealed class JournalLine
{
    public string AccountCode { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }

    /// <summary>Customer or supplier id when the line hits a control account.</summary>
    public string? PartyId { get; set; }
}

public sealed record JournalLineInput(string? AccountCode, decimal Debit, decimal Credit, string? Memo = null, string? PartyId = null);

/// <summary>A line on a sales or purchase invoice: the account to analyse to and the net (pre-tax) amount.</summary>
public sealed record DocumentLineInput(string? AccountCode, string? Description, decimal Net);
