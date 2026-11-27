using Fidos.Core.Training;

namespace Fidos.Core;

/// <summary>
/// A trainee's sandbox company: the complete set of books (general ledger, sub-ledgers and training progress).
/// All state is plain data so the whole company can be serialised to JSON.
/// </summary>
public sealed partial class Company
{
    public const decimal MaxAmount = 999_999_999.99m;
    public const int MaxTextLength = 40;

    public string Name { get; set; } = "NORTHSEA";
    public string OperatorId { get; set; } = "TRAINEE";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<Account> Accounts { get; set; } = [];
    public List<JournalEntry> Journals { get; set; } = [];
    public List<Customer> Customers { get; set; } = [];
    public List<Supplier> Suppliers { get; set; } = [];
    public List<SalesInvoice> SalesInvoices { get; set; } = [];
    public List<CustomerReceipt> CustomerReceipts { get; set; } = [];
    public List<PurchaseInvoice> PurchaseInvoices { get; set; } = [];
    public List<SupplierPayment> SupplierPayments { get; set; } = [];
    public Dictionary<string, int> Sequences { get; set; } = [];

    /// <summary>Screens visited / actions performed, used by the training levels (e.g. "VIEW:TB").</summary>
    public HashSet<string> Milestones { get; set; } = [];

    /// <summary>Completed training levels and when they were completed.</summary>
    public Dictionary<int, DateTime> CompletedLevels { get; set; } = [];

    public int? SelectedTrainingLevel { get; set; }

    public DrillState Drill { get; set; } = new();

    public static Company CreateNew(string? operatorId = null)
    {
        var company = new Company();
        if (!string.IsNullOrWhiteSpace(operatorId))
        {
            company.OperatorId = Clean(operatorId, 8);
        }

        company.Accounts.AddRange(SystemAccounts.DefaultChart());
        company.Drill.CurrentQuestion = DrillCatalog.All[0].Id;
        return company;
    }

    public void Mark(string milestone) => Milestones.Add(milestone.ToUpperInvariant());

    public bool HasMilestone(string milestone) => Milestones.Contains(milestone.ToUpperInvariant());

    // ---------------------------------------------------------------- Chart of accounts

    public Account? FindAccount(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : Accounts.FirstOrDefault(a => a.Code == code.Trim());

    public Account GetAccount(string? code) =>
        FindAccount(code) ?? throw new BookkeepingException($"ACCOUNT '{code?.Trim()}' NOT FOUND IN CHART OF ACCOUNTS");

    public Account AddAccount(string? code, string? name, AccountType type, string? description = null)
    {
        code = code?.Trim() ?? "";
        if (code.Length != 4 || !code.All(char.IsAsciiDigit))
        {
            throw new BookkeepingException("ACCOUNT CODE MUST BE EXACTLY 4 DIGITS, E.G. 6600");
        }

        if (!type.CodeMatches(code))
        {
            throw new BookkeepingException($"{type.ToString().ToUpperInvariant()} ACCOUNTS MUST USE CODES {type.CodeRange()}");
        }

        if (FindAccount(code) is { } existing)
        {
            throw new BookkeepingException($"ACCOUNT {code} ALREADY EXISTS ({existing.Name})");
        }

        var account = new Account
        {
            Code = code,
            Name = Required(name, "ACCOUNT NAME"),
            Type = type,
            Description = string.IsNullOrWhiteSpace(description) ? null : Clean(description, 60),
            IsUserDefined = true,
        };
        Accounts.Add(account);
        Accounts.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
        return account;
    }

    // ---------------------------------------------------------------- Journals

    public JournalEntry? FindJournal(int number) => Journals.FirstOrDefault(j => j.Number == number);

    public JournalEntry GetJournal(int number) =>
        FindJournal(number) ?? throw new BookkeepingException($"JOURNAL {number:000000} NOT FOUND");

    /// <summary>Posts a manual general ledger journal. Control accounts are blocked: they belong to the sub-ledgers.</summary>
    public JournalEntry PostJournal(DateOnly date, string? description, IEnumerable<JournalLineInput> lines, string? reference = null) =>
        Post(JournalSource.Manual, date, description, reference, lines, allowControlAccounts: false);

    /// <summary>Reverses a manual journal by posting the same lines with debits and credits swapped.</summary>
    public JournalEntry ReverseJournal(int number, DateOnly date)
    {
        var original = GetJournal(number);
        if (original.Source != JournalSource.Manual)
        {
            throw new BookkeepingException($"JOURNAL {number:000000} CAME FROM THE {JournalEntry.SourceCode(original.Source)} LEDGER - CORRECT IT THERE, NOT IN THE GL");
        }

        if (original.ReversedBy is { } by)
        {
            throw new BookkeepingException($"JOURNAL {number:000000} WAS ALREADY REVERSED BY JOURNAL {by:000000}");
        }

        if (original.ReversalOf is not null)
        {
            throw new BookkeepingException($"JOURNAL {number:000000} IS ITSELF A REVERSAL - POST A NEW JOURNAL INSTEAD");
        }

        if (date < original.Date)
        {
            throw new BookkeepingException($"REVERSAL DATE CANNOT BE BEFORE THE ORIGINAL DATE {original.Date:yyyy-MM-dd}");
        }

        var reversal = Post(
            JournalSource.Manual,
            date,
            $"REV {number:000000} {original.Description}",
            original.Reference,
            original.Lines.Select(l => new JournalLineInput(l.AccountCode, l.Credit, l.Debit, l.Memo, l.PartyId)),
            allowControlAccounts: false);
        reversal.ReversalOf = original.Number;
        original.ReversedBy = reversal.Number;
        return reversal;
    }

    private JournalEntry Post(
        JournalSource source,
        DateOnly date,
        string? description,
        string? reference,
        IEnumerable<JournalLineInput> input,
        bool allowControlAccounts)
    {
        var text = Required(description, "JOURNAL DESCRIPTION");
        var lines = input
            .Where(l => !string.IsNullOrWhiteSpace(l.AccountCode) || l.Debit != 0 || l.Credit != 0 || !string.IsNullOrWhiteSpace(l.Memo))
            .ToList();
        if (lines.Count < 2)
        {
            throw new BookkeepingException("A JOURNAL NEEDS AT LEAST TWO LINES - EVERY DEBIT NEEDS A CREDIT");
        }

        var posted = new List<JournalLine>();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var label = $"LINE {i + 1}:";
            if (string.IsNullOrWhiteSpace(line.AccountCode))
            {
                throw new BookkeepingException($"{label} ACCOUNT CODE IS REQUIRED");
            }

            var account = FindAccount(line.AccountCode)
                ?? throw new BookkeepingException($"{label} ACCOUNT '{line.AccountCode.Trim()}' NOT FOUND IN CHART OF ACCOUNTS");
            if (line.Debit < 0 || line.Credit < 0)
            {
                throw new BookkeepingException($"{label} AMOUNTS CANNOT BE NEGATIVE - USE THE OTHER COLUMN INSTEAD");
            }

            if (line.Debit != 0 && line.Credit != 0)
            {
                throw new BookkeepingException($"{label} ENTER A DEBIT OR A CREDIT, NOT BOTH");
            }

            if (line.Debit == 0 && line.Credit == 0)
            {
                throw new BookkeepingException($"{label} ENTER A DEBIT OR A CREDIT AMOUNT");
            }

            CheckAmount(line.Debit + line.Credit, label);
            if (!allowControlAccounts && account.Control != ControlType.None)
            {
                var ledger = account.Control == ControlType.Receivables ? "AR (SALES)" : "AP (PURCHASE)";
                throw new BookkeepingException($"{label} {account.Code} IS A CONTROL ACCOUNT - POST VIA THE {ledger} LEDGER");
            }

            posted.Add(new JournalLine
            {
                AccountCode = account.Code,
                Debit = line.Debit,
                Credit = line.Credit,
                Memo = string.IsNullOrWhiteSpace(line.Memo) ? null : Clean(line.Memo, MaxTextLength),
                PartyId = line.PartyId,
            });
        }

        var debits = posted.Sum(l => l.Debit);
        var credits = posted.Sum(l => l.Credit);
        if (debits != credits)
        {
            throw new BookkeepingException(
                $"OUT OF BALANCE: DEBITS {debits:N2} CREDITS {credits:N2} DIFFERENCE {Math.Abs(debits - credits):N2}");
        }

        var entry = new JournalEntry
        {
            Number = NextSequence("JNL"),
            Date = date,
            Description = text,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : Clean(reference, 20),
            Source = source,
            Lines = posted,
            PostedBy = OperatorId,
            PostedUtc = DateTime.UtcNow,
        };
        Journals.Add(entry);
        return entry;
    }

    // ---------------------------------------------------------------- Year end

    /// <summary>
    /// Closes every revenue and expense balance (up to and including <paramref name="yearEnd"/>) into retained earnings,
    /// so the next year's profit and loss starts from zero.
    /// </summary>
    public JournalEntry CloseYear(DateOnly yearEnd)
    {
        var lastClose = Journals.Where(j => j.Source == JournalSource.YearEnd).Select(j => (DateOnly?)j.Date).Max();
        if (lastClose is { } closed && yearEnd <= closed)
        {
            throw new BookkeepingException($"YEAR ALREADY CLOSED AT {closed:yyyy-MM-dd} - CHOOSE A LATER YEAR-END DATE");
        }

        var balances = Accounts
            .Where(a => a.Type is AccountType.Revenue or AccountType.Expense)
            .Select(a => (Account: a, Balance: Balance(a.Code, yearEnd)))
            .Where(b => b.Balance != 0)
            .ToList();
        if (balances.Count == 0)
        {
            throw new BookkeepingException($"NOTHING TO CLOSE - NO INCOME OR EXPENSE BALANCES AT {yearEnd:yyyy-MM-dd}");
        }

        var lines = balances
            .Select(b => b.Balance > 0
                ? new JournalLineInput(b.Account.Code, 0, b.Balance, "CLOSE TO RETAINED EARNINGS")
                : new JournalLineInput(b.Account.Code, -b.Balance, 0, "CLOSE TO RETAINED EARNINGS"))
            .ToList();
        var net = balances.Sum(b => b.Balance);
        var memo = net < 0 ? "NET PROFIT FOR THE YEAR" : "NET LOSS FOR THE YEAR";
        lines.Add(net < 0
            ? new JournalLineInput(SystemAccounts.RetainedEarnings, 0, -net, memo)
            : new JournalLineInput(SystemAccounts.RetainedEarnings, net, 0, memo));

        return Post(JournalSource.YearEnd, yearEnd, $"YEAR-END CLOSE {yearEnd:yyyy-MM-dd}", "YEAREND", lines, allowControlAccounts: false);
    }

    // ---------------------------------------------------------------- Helpers

    private int NextSequence(string key)
    {
        Sequences.TryGetValue(key, out var current);
        Sequences[key] = current + 1;
        return current + 1;
    }

    private static void CheckAmount(decimal amount, string label)
    {
        if (decimal.Round(amount, 2) != amount)
        {
            throw new BookkeepingException($"{label} AMOUNTS CAN HAVE AT MOST 2 DECIMAL PLACES");
        }

        if (amount > MaxAmount)
        {
            throw new BookkeepingException($"{label} AMOUNT TOO LARGE FOR THIS TRAINING SYSTEM");
        }
    }

    private static string Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BookkeepingException($"{field} IS REQUIRED");
        }

        return Clean(value, MaxTextLength);
    }

    internal static string Clean(string value, int maxLength)
    {
        var text = string.Join(' ', value.Trim().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        text = new string(text.Where(c => !char.IsControl(c)).ToArray());
        return text.Length > maxLength ? text[..maxLength] : text;
    }
}
