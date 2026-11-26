using Fidos.Core;

namespace AccountedFor.Web.Terminal;

public sealed record PartyInfo(string Id, string Name, string? Contact, int TermsDays, decimal Balance);

/// <summary>
/// Describes one of the two sub-ledgers. The sales (AR) and purchase (AP) ledgers mirror each other,
/// so the same screens serve both.
/// </summary>
public sealed record LedgerKind(
    string Key,
    string Code,
    string Title,
    string LedgerName,
    string Party,
    string Parties,
    string InvoiceTitle,
    string Settlement,
    string ControlAccount,
    string DefaultLineAccount,
    string LineAccountRule,
    string InvoiceDoubleEntry,
    string SettlementDoubleEntry,
    string AgedTitle,
    string HelpTopic)
{
    public static readonly LedgerKind Receivables = new(
        "ar", "AR", "ACCOUNTS RECEIVABLE", "SALES LEDGER", "CUSTOMER", "CUSTOMERS", "SALES INVOICE", "RECEIPT",
        SystemAccounts.Receivables, "4000", "REVENUE ACCOUNTS 4XXX",
        "DR 1100 RECEIVABLES (GROSS)  CR 4XXX SALES (NET)  CR 2200 VAT OUTPUT (VAT)",
        "DR 1000 BANK  CR 1100 RECEIVABLES",
        "AGED DEBTORS", "SALES");

    public static readonly LedgerKind Payables = new(
        "ap", "AP", "ACCOUNTS PAYABLE", "PURCHASE LEDGER", "SUPPLIER", "SUPPLIERS", "PURCHASE INVOICE", "PAYMENT",
        SystemAccounts.Payables, "6300", "EXPENSE OR ASSET ACCOUNTS",
        "DR EXPENSE/ASSET (NET)  DR 1300 VAT INPUT (VAT)  CR 2000 PAYABLES (GROSS)",
        "DR 2000 PAYABLES  CR 1000 BANK",
        "AGED CREDITORS", "PURCHASES");

    public static LedgerKind? From(string? key) => key?.ToLowerInvariant() switch
    {
        "ar" => Receivables,
        "ap" => Payables,
        _ => null,
    };

    public bool IsReceivables => Key == "ar";

    public string Url(string path = "") => $"/{Key}/{path}".TrimEnd('/');

    public IReadOnlyList<PartyInfo> PartyList(Company c) => IsReceivables
        ? c.Customers.Select(x => new PartyInfo(x.Id, x.Name, x.Contact, x.TermsDays, c.CustomerBalance(x.Id))).ToList()
        : c.Suppliers.Select(x => new PartyInfo(x.Id, x.Name, x.Contact, x.TermsDays, c.SupplierBalance(x.Id))).ToList();

    public PartyInfo? FindParty(Company c, string? id) =>
        PartyList(c).FirstOrDefault(p => string.Equals(p.Id, id?.Trim(), StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Invoice> Invoices(Company c) =>
        IsReceivables ? c.SalesInvoices : c.PurchaseInvoices;

    public IReadOnlyList<Settlement> Settlements(Company c) =>
        IsReceivables ? c.CustomerReceipts : c.SupplierPayments;

    public string AddParty(Company c, string? name, string? contact, int terms) =>
        IsReceivables ? c.AddCustomer(name, contact, terms).Id : c.AddSupplier(name, contact, terms).Id;

    public Invoice PostInvoice(Company c, string? partyId, DateOnly date, IEnumerable<DocumentLineInput> lines, decimal vatRate, string? reference) =>
        IsReceivables
            ? c.PostSalesInvoice(partyId, date, lines, vatRate, reference)
            : c.PostPurchaseInvoice(partyId, date, lines, vatRate, reference);

    public Settlement PostSettlement(Company c, string? partyId, DateOnly date, decimal amount, string? bank, string? invoice, string? reference) =>
        IsReceivables
            ? c.PostCustomerReceipt(partyId, date, amount, bank, invoice, reference)
            : c.PostSupplierPayment(partyId, date, amount, bank, invoice, reference);

    public IReadOnlyList<PartyStatementRow> Statement(Company c, string partyId) =>
        IsReceivables ? c.CustomerStatement(partyId) : c.SupplierStatement(partyId);

    public IReadOnlyList<AgedRow> Aged(Company c, DateOnly asOf) =>
        IsReceivables ? c.AgedReceivables(asOf) : c.AgedPayables(asOf);

    public bool AllowsLineAccount(Account a) => IsReceivables
        ? a.Type == AccountType.Revenue
        : a.Type is AccountType.Expense or AccountType.Asset && a.Control == ControlType.None && a.Code != SystemAccounts.VatInput;
}

/// <summary>Base for the shared AR/AP screens, routed as /ar/... and /ap/...</summary>
public abstract class LedgerPageModel(SandboxStore store) : TerminalPageModel(store)
{
    public LedgerKind Kind { get; private set; } = LedgerKind.Receivables;

    public override void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context)
    {
        var kind = LedgerKind.From(RouteData.Values["ledger"] as string);
        if (kind is null)
        {
            context.Result = NotFound();
            return;
        }

        Kind = kind;
        base.OnPageHandlerExecuting(context);
        ViewData["Exit"] = $"/Menu/{kind.Code}";
        ViewData["HelpTopic"] = kind.HelpTopic;
    }
}
