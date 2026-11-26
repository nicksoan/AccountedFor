namespace Fidos.Core;

public sealed record TrialBalanceRow(Account Account, decimal Debit, decimal Credit);

public sealed record StatementRow(Account Account, decimal Amount);

public sealed record IncomeStatement(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<StatementRow> Revenue,
    IReadOnlyList<StatementRow> Expenses)
{
    public decimal TotalRevenue => Revenue.Sum(r => r.Amount);
    public decimal TotalExpenses => Expenses.Sum(r => r.Amount);
    public decimal NetProfit => TotalRevenue - TotalExpenses;
}

public sealed record BalanceSheet(
    DateOnly AsOf,
    IReadOnlyList<StatementRow> Assets,
    IReadOnlyList<StatementRow> Liabilities,
    IReadOnlyList<StatementRow> Equity,
    decimal CurrentEarnings)
{
    public decimal TotalAssets => Assets.Sum(r => r.Amount);
    public decimal TotalLiabilities => Liabilities.Sum(r => r.Amount);
    public decimal TotalEquity => Equity.Sum(r => r.Amount) + CurrentEarnings;
    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public bool Balances => TotalAssets == TotalLiabilitiesAndEquity;
}

public sealed record LedgerActivityRow(JournalEntry Journal, JournalLine Line, decimal RunningBalance);

public sealed record ReconciliationRow(string Ledger, Account ControlAccount, decimal ControlBalance, decimal SubLedgerBalance)
{
    public decimal Difference => ControlBalance - SubLedgerBalance;
    public bool Agrees => Difference == 0;
}

public sealed record AgedRow(string PartyId, string PartyName, decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Over90)
{
    public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + Over90;
}

public sealed record PartyStatementRow(DateOnly Date, string Document, string Description, decimal Debit, decimal Credit, decimal Balance);

public sealed partial class Company
{
    /// <summary>Account balance as debits minus credits (positive = debit balance).</summary>
    public decimal Balance(string code, DateOnly? asOf = null) =>
        Journals.Where(j => asOf is null || j.Date <= asOf)
            .SelectMany(j => j.Lines)
            .Where(l => l.AccountCode == code)
            .Sum(l => l.Debit - l.Credit);

    public IReadOnlyList<TrialBalanceRow> TrialBalance(DateOnly? asOf = null) =>
        Accounts
            .Select(a => (Account: a, Balance: Balance(a.Code, asOf)))
            .Where(b => b.Balance != 0)
            .Select(b => new TrialBalanceRow(b.Account, Math.Max(b.Balance, 0), Math.Max(-b.Balance, 0)))
            .ToList();

    /// <summary>Profit and loss for a period. Year-end closing journals are excluded so closed years still report.</summary>
    public IncomeStatement ProfitAndLoss(DateOnly from, DateOnly to)
    {
        var movements = Journals
            .Where(j => j.Source != JournalSource.YearEnd && j.Date >= from && j.Date <= to)
            .SelectMany(j => j.Lines)
            .GroupBy(l => l.AccountCode)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Debit - l.Credit));

        List<StatementRow> Rows(AccountType type, int sign) => Accounts
            .Where(a => a.Type == type && movements.TryGetValue(a.Code, out var m) && m != 0)
            .Select(a => new StatementRow(a, sign * movements[a.Code]))
            .ToList();

        return new IncomeStatement(from, to, Rows(AccountType.Revenue, -1), Rows(AccountType.Expense, 1));
    }

    public BalanceSheet BalanceSheet(DateOnly asOf)
    {
        List<StatementRow> Rows(AccountType type, int sign) => Accounts
            .Where(a => a.Type == type)
            .Select(a => new StatementRow(a, sign * Balance(a.Code, asOf)))
            .Where(r => r.Amount != 0)
            .ToList();

        var earnings = -Accounts
            .Where(a => a.Type is AccountType.Revenue or AccountType.Expense)
            .Sum(a => Balance(a.Code, asOf));

        return new BalanceSheet(asOf, Rows(AccountType.Asset, 1), Rows(AccountType.Liability, -1), Rows(AccountType.Equity, -1), earnings);
    }

    public IReadOnlyList<LedgerActivityRow> AccountActivity(string code)
    {
        var running = 0m;
        var rows = new List<LedgerActivityRow>();
        foreach (var journal in Journals.OrderBy(j => j.Date).ThenBy(j => j.Number))
        {
            foreach (var line in journal.Lines.Where(l => l.AccountCode == code))
            {
                running += line.Debit - line.Credit;
                rows.Add(new LedgerActivityRow(journal, line, running));
            }
        }

        return rows;
    }

    /// <summary>Compares each control account in the general ledger with the total of its sub-ledger.</summary>
    public IReadOnlyList<ReconciliationRow> Reconcile() =>
    [
        new("SALES LEDGER (AR)", GetAccount(SystemAccounts.Receivables), Balance(SystemAccounts.Receivables),
            Customers.Sum(c => CustomerBalance(c.Id))),
        new("PURCHASE LEDGER (AP)", GetAccount(SystemAccounts.Payables), -Balance(SystemAccounts.Payables),
            Suppliers.Sum(s => SupplierBalance(s.Id))),
    ];

    public IReadOnlyList<AgedRow> AgedReceivables(DateOnly asOf) =>
        Aged(SalesInvoices, Customers.ToDictionary(c => c.Id, c => c.Name), asOf);

    public IReadOnlyList<AgedRow> AgedPayables(DateOnly asOf) =>
        Aged(PurchaseInvoices, Suppliers.ToDictionary(s => s.Id, s => s.Name), asOf);

    public IReadOnlyList<PartyStatementRow> CustomerStatement(string customerId) =>
        PartyStatement(
            SalesInvoices.Where(i => i.PartyId == customerId),
            CustomerReceipts.Where(r => r.PartyId == customerId),
            "INVOICE",
            "RECEIPT - THANK YOU",
            invoicesIncrease: true);

    public IReadOnlyList<PartyStatementRow> SupplierStatement(string supplierId) =>
        PartyStatement(
            PurchaseInvoices.Where(i => i.PartyId == supplierId),
            SupplierPayments.Where(p => p.PartyId == supplierId),
            "INVOICE",
            "PAYMENT",
            invoicesIncrease: false);

    private static List<AgedRow> Aged(IEnumerable<Invoice> invoices, Dictionary<string, string> names, DateOnly asOf) =>
        invoices
            .Where(i => i.Outstanding != 0 && i.Date <= asOf)
            .GroupBy(i => i.PartyId)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                decimal Bucket(int min, int max) => g
                    .Where(i => asOf.DayNumber - i.DueDate.DayNumber is var d && d >= min && d <= max)
                    .Sum(i => i.Outstanding);
                return new AgedRow(
                    g.Key,
                    names.GetValueOrDefault(g.Key, "?"),
                    Bucket(int.MinValue, 0),
                    Bucket(1, 30),
                    Bucket(31, 60),
                    Bucket(61, 90),
                    Bucket(91, int.MaxValue));
            })
            .ToList();

    /// <summary>Chronological statement. Customer statements show invoices as debits; supplier statements as credits.</summary>
    private static List<PartyStatementRow> PartyStatement(
        IEnumerable<Invoice> invoices,
        IEnumerable<Settlement> settlements,
        string invoiceText,
        string settlementText,
        bool invoicesIncrease)
    {
        var items = invoices
            .Select(i => (i.Date, Doc: i.Number, Text: $"{invoiceText} {i.Reference}".Trim(), Amount: i.Gross, IsInvoice: true))
            .Concat(settlements.Select(s => (s.Date, Doc: s.Number, Text: $"{settlementText} {s.Reference}".Trim(), s.Amount, IsInvoice: false)))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.IsInvoice ? 0 : 1)
            .ThenBy(x => x.Doc, StringComparer.Ordinal);

        var balance = 0m;
        var rows = new List<PartyStatementRow>();
        foreach (var item in items)
        {
            balance += item.IsInvoice ? item.Amount : -item.Amount;
            var debit = item.IsInvoice == invoicesIncrease ? item.Amount : 0;
            var credit = item.IsInvoice == invoicesIncrease ? 0 : item.Amount;
            rows.Add(new PartyStatementRow(item.Date, item.Doc, item.Text, debit, credit, balance));
        }

        return rows;
    }
}
