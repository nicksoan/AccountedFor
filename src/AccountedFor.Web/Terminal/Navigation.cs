using Fidos.Core.Training;

namespace AccountedFor.Web.Terminal;

public sealed record MenuOption(string Key, string Code, string Label, string Description, string Url);

public sealed record MenuDefinition(string Id, string ScreenId, string Title, string Url, string Parent, IReadOnlyList<MenuOption> Options);

public sealed record PfKey(string Key, string Label, string Url);

/// <summary>The FIDOS menu tree and command-line resolution (option numbers, "=2.3" jumps and transaction codes).</summary>
public static class Navigation
{
    public const string MainMenu = "/Menu";

    public static IReadOnlyList<MenuDefinition> Menus { get; } =
    [
        new("MAIN", "FID000", "PRIMARY OPTION MENU", MainMenu, MainMenu,
        [
            new("1", "GL", "GENERAL LEDGER", "CHART OF ACCOUNTS, JOURNALS, T-ACCOUNTS, YEAR END", "/Menu/GL"),
            new("2", "AR", "ACCOUNTS RECEIVABLE", "SALES LEDGER: CUSTOMERS, INVOICES, RECEIPTS", "/Menu/AR"),
            new("3", "AP", "ACCOUNTS PAYABLE", "PURCHASE LEDGER: SUPPLIERS, INVOICES, PAYMENTS", "/Menu/AP"),
            new("4", "RPT", "REPORTS", "TRIAL BALANCE, P&L, BALANCE SHEET, RECONCILIATION", "/Menu/RPT"),
            new("5", "TRN", "TUTORIAL", "TRAINING LEVELS AND DEBIT/CREDIT DRILL", "/Menu/TRN"),
            new("6", "HELP", "ACCOUNTING FUNDAMENTALS", "DOUBLE-ENTRY BOOKKEEPING EXPLAINED", "/Help"),
            new("8", "OPT", "DISPLAY OPTIONS", "CRT FILTER AND FONT COLOURS", "/Options"),
            new("9", "SYS", "SANDBOX UTILITIES", "OPEN, START OR RESET A SANDBOX", "/System"),
            new("X", "LOGOFF", "LOG OFF", "END THIS FIDOS SESSION", "/Logoff"),
        ]),
        new("GL", "GL000", "GENERAL LEDGER", "/Menu/GL", MainMenu,
        [
            new("1", "COA", "CHART OF ACCOUNTS", "LIST ALL LEDGER ACCOUNTS AND BALANCES", "/Gl/Accounts"),
            new("2", "NEWACCT", "CREATE LEDGER ACCOUNT", "ADD A NEW ACCOUNT TO THE CHART", "/Gl/NewAccount"),
            new("3", "JE", "JOURNAL ENTRY", "POST A MANUAL DOUBLE-ENTRY JOURNAL", "/Gl/Journal"),
            new("4", "JNLS", "JOURNAL LIST / DAY BOOK", "BROWSE, VIEW AND REVERSE JOURNALS", "/Gl/Journals"),
            new("5", "TACCT", "ACCOUNT INQUIRY", "SEE ANY ACCOUNT AS A T-ACCOUNT", "/Gl/Inquiry"),
            new("6", "YEC", "YEAR-END CLOSE", "CLOSE INCOME AND EXPENSES TO RETAINED EARNINGS", "/Gl/YearEnd"),
        ]),
        new("AR", "AR000", "ACCOUNTS RECEIVABLE - SALES LEDGER", "/Menu/AR", MainMenu,
        [
            new("1", "CUST", "CUSTOMER LIST", "CUSTOMERS, BALANCES AND STATEMENTS", "/ar/parties"),
            new("2", "NEWCUST", "NEW CUSTOMER", "OPEN A CUSTOMER ACCOUNT", "/ar/parties/new"),
            new("3", "SINV", "SALES INVOICE", "BILL A CUSTOMER (DR RECEIVABLES, CR SALES)", "/ar/invoice"),
            new("4", "RCPT", "CUSTOMER RECEIPT", "RECORD MONEY RECEIVED (DR BANK, CR RECEIVABLES)", "/ar/settle"),
            new("5", "SINVS", "SALES INVOICE LIST", "ALL INVOICES AND WHAT IS STILL OWED", "/ar/invoices"),
            new("6", "AGEDAR", "AGED DEBTORS", "WHO OWES US, AND HOW OVERDUE", "/ar/aged"),
        ]),
        new("AP", "AP000", "ACCOUNTS PAYABLE - PURCHASE LEDGER", "/Menu/AP", MainMenu,
        [
            new("1", "SUPP", "SUPPLIER LIST", "SUPPLIERS, BALANCES AND STATEMENTS", "/ap/parties"),
            new("2", "NEWSUPP", "NEW SUPPLIER", "OPEN A SUPPLIER ACCOUNT", "/ap/parties/new"),
            new("3", "PINV", "PURCHASE INVOICE", "RECORD A BILL (DR EXPENSE, CR PAYABLES)", "/ap/invoice"),
            new("4", "PAY", "SUPPLIER PAYMENT", "PAY A SUPPLIER (DR PAYABLES, CR BANK)", "/ap/settle"),
            new("5", "PINVS", "PURCHASE INVOICE LIST", "ALL BILLS AND WHAT WE STILL OWE", "/ap/invoices"),
            new("6", "AGEDAP", "AGED CREDITORS", "WHO WE OWE, AND WHEN IT IS DUE", "/ap/aged"),
        ]),
        new("RPT", "RP000", "REPORTS AND INQUIRIES", "/Menu/RPT", MainMenu,
        [
            new("1", "TB", "TRIAL BALANCE", "PROVE TOTAL DEBITS = TOTAL CREDITS", "/Reports/TrialBalance"),
            new("2", "PL", "PROFIT AND LOSS", "INCOME STATEMENT FOR A PERIOD", "/Reports/ProfitLoss"),
            new("3", "BS", "BALANCE SHEET", "ASSETS = LIABILITIES + EQUITY", "/Reports/BalanceSheet"),
            new("4", "RECON", "CONTROL ACCOUNT RECONCILIATION", "SUB-LEDGERS VS GENERAL LEDGER", "/Reports/Reconcile"),
            new("5", "DAYBOOK", "DAY BOOK", "EVERY JOURNAL IN DATE ORDER", "/Gl/Journals"),
            new("6", "AGED", "AGED DEBTORS", "OUTSTANDING CUSTOMER BALANCES BY AGE", "/ar/aged"),
        ]),
        new("TRN", "TR000", "TUTORIAL", "/Menu/TRN", MainMenu,
        [
            new("1", "LEVELS", "TRAINING LEVELS", "MISSIONS THAT TEACH BOOKKEEPING STEP BY STEP", "/Training"),
            new("2", "DRILL", "DEBIT OR CREDIT?", "QUICK-FIRE DOUBLE-ENTRY PRACTICE", "/Training/Drill"),
            new("3", "TUTOR", "ACCOUNTING FUNDAMENTALS", "THE THEORY BEHIND THE SCREENS", "/Help"),
        ]),
    ];

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MAIN"] = MainMenu,
        ["MENU"] = MainMenu,
        ["HOME"] = MainMenu,
        ["LOGON"] = "/",
        ["RESET"] = "/System",
        ["CUSTOMERS"] = "/ar/parties",
        ["SUPPLIERS"] = "/ap/parties",
        ["STATUS"] = "/Training",
    };

    public static MenuDefinition? FindMenu(string id) =>
        Menus.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Resolves what the operator typed on the command line to a local URL, or null when not recognised.</summary>
    public static string? Resolve(string command, string? fromPath)
    {
        var cmd = command.Trim().ToUpperInvariant();
        if (cmd.Length == 0)
        {
            return null;
        }

        // ISPF-style jump: "=2.3" or "2.3" walks the menu tree from the primary menu.
        if (cmd.StartsWith('=') || cmd.Contains('.'))
        {
            return Jump(cmd.TrimStart('=').Split('.', StringSplitOptions.RemoveEmptyEntries));
        }

        if (string.Equals(fromPath?.TrimEnd('/'), "/Training", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(cmd, out var levelNumber)
            && LevelCatalog.All.Any(level => level.Number == levelNumber))
        {
            return $"/Training/Level/{levelNumber}";
        }

        var current = Menus.FirstOrDefault(m => string.Equals(m.Url, fromPath, StringComparison.OrdinalIgnoreCase));
        var option = current?.Options.FirstOrDefault(o => o.Key == cmd);
        if (option is not null)
        {
            return option.Url;
        }

        var menu = FindMenu(cmd);
        if (menu is not null)
        {
            return menu.Url;
        }

        var byCode = Menus.SelectMany(m => m.Options).FirstOrDefault(o => o.Code == cmd);
        if (byCode is not null)
        {
            return byCode.Url;
        }

        if (Aliases.TryGetValue(cmd, out var alias))
        {
            return alias;
        }

        return cmd == "X" ? "/Logoff" : null;
    }

    private static string? Jump(IReadOnlyList<string> keys)
    {
        var menu = FindMenu("MAIN")!;
        string? url = menu.Url;
        foreach (var key in keys)
        {
            if (menu is null)
            {
                return null;
            }

            var option = menu.Options.FirstOrDefault(o => o.Key == key);
            if (option is null)
            {
                return null;
            }

            url = option.Url;
            menu = Menus.FirstOrDefault(m => m.Url == option.Url);
        }

        return url;
    }
}
