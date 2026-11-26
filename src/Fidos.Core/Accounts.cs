using System.Text.Json.Serialization;

namespace Fidos.Core;

/// <summary>The five elements of the accounting equation.</summary>
public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
}

/// <summary>Marks a general ledger account as the control account for a sub-ledger.</summary>
public enum ControlType
{
    None,
    Receivables,
    Payables,
}

public enum NormalBalance
{
    Debit,
    Credit,
}

public sealed class Account
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public AccountType Type { get; set; }
    public ControlType Control { get; set; }
    public string? Description { get; set; }

    /// <summary>True when the trainee created the account (as opposed to the seeded chart).</summary>
    public bool IsUserDefined { get; set; }

    [JsonIgnore]
    public NormalBalance NormalBalance => Type.NormalBalance();
}

public static class AccountTypeExtensions
{
    public static NormalBalance NormalBalance(this AccountType type) =>
        type is AccountType.Asset or AccountType.Expense ? Core.NormalBalance.Debit : Core.NormalBalance.Credit;

    public static string ShortCode(this AccountType type) => type switch
    {
        AccountType.Asset => "AST",
        AccountType.Liability => "LIA",
        AccountType.Equity => "EQU",
        AccountType.Revenue => "REV",
        AccountType.Expense => "EXP",
        _ => "???",
    };

    /// <summary>FIDOS numbering convention: 1xxx assets, 2xxx liabilities, 3xxx equity, 4xxx revenue, 5xxx-9xxx expenses.</summary>
    public static string CodeRange(this AccountType type) => type switch
    {
        AccountType.Asset => "1000-1999",
        AccountType.Liability => "2000-2999",
        AccountType.Equity => "3000-3999",
        AccountType.Revenue => "4000-4999",
        _ => "5000-9999",
    };

    public static bool CodeMatches(this AccountType type, string code) =>
        code.Length == 4 && char.IsAsciiDigit(code[0]) && type switch
        {
            AccountType.Asset => code[0] == '1',
            AccountType.Liability => code[0] == '2',
            AccountType.Equity => code[0] == '3',
            AccountType.Revenue => code[0] == '4',
            _ => code[0] >= '5',
        };

    public static bool TryParse(string? value, out AccountType type)
    {
        switch (value?.Trim().ToUpperInvariant())
        {
            case "A" or "AST" or "ASSET": type = AccountType.Asset; return true;
            case "L" or "LIA" or "LIABILITY": type = AccountType.Liability; return true;
            case "E" or "EQU" or "EQUITY": type = AccountType.Equity; return true;
            case "R" or "REV" or "REVENUE" or "INCOME": type = AccountType.Revenue; return true;
            case "X" or "EXP" or "EXPENSE": type = AccountType.Expense; return true;
            default: type = default; return false;
        }
    }
}

/// <summary>Well-known accounts in the seeded FIDOS chart of accounts.</summary>
public static class SystemAccounts
{
    public const string Bank = "1000";
    public const string Receivables = "1100";
    public const string VatInput = "1300";
    public const string Payables = "2000";
    public const string Accruals = "2100";
    public const string VatOutput = "2200";
    public const string Capital = "3000";
    public const string RetainedEarnings = "3200";

    public static IEnumerable<Account> DefaultChart()
    {
        yield return New("1000", "BANK - CURRENT ACCOUNT", AccountType.Asset, "MONEY HELD AT THE BANK");
        yield return New("1050", "PETTY CASH", AccountType.Asset, "NOTES AND COINS KEPT IN THE OFFICE");
        yield return New("1100", "ACCOUNTS RECEIVABLE CONTROL", AccountType.Asset, "TOTAL OWED BY CUSTOMERS (SALES LEDGER)", ControlType.Receivables);
        yield return New("1200", "INVENTORY", AccountType.Asset, "GOODS HELD FOR RESALE");
        yield return New("1300", "VAT INPUT - RECOVERABLE", AccountType.Asset, "VAT PAID ON PURCHASES, RECLAIMABLE");
        yield return New("1500", "EQUIPMENT - COST", AccountType.Asset, "LONG-TERM (FIXED) ASSETS AT COST");
        yield return New("1510", "EQUIPMENT - ACCUM DEPRECIATION", AccountType.Asset, "CONTRA ASSET: WEAR AND TEAR TO DATE");
        yield return New("2000", "ACCOUNTS PAYABLE CONTROL", AccountType.Liability, "TOTAL OWED TO SUPPLIERS (PURCHASE LEDGER)", ControlType.Payables);
        yield return New("2100", "ACCRUED EXPENSES", AccountType.Liability, "COSTS INCURRED BUT NOT YET INVOICED");
        yield return New("2200", "VAT OUTPUT - PAYABLE", AccountType.Liability, "VAT CHARGED ON SALES, OWED TO TAX OFFICE");
        yield return New("2300", "BANK LOAN", AccountType.Liability, "MONEY BORROWED FROM THE BANK");
        yield return New("3000", "OWNER'S CAPITAL", AccountType.Equity, "MONEY THE OWNER PUT INTO THE BUSINESS");
        yield return New("3100", "OWNER'S DRAWINGS", AccountType.Equity, "CONTRA EQUITY: MONEY THE OWNER TOOK OUT");
        yield return New("3200", "RETAINED EARNINGS", AccountType.Equity, "PROFITS KEPT IN THE BUSINESS (YEAR-END CLOSE)");
        yield return New("4000", "SALES - GOODS", AccountType.Revenue, "INCOME FROM SELLING PRODUCTS");
        yield return New("4100", "SALES - SERVICES", AccountType.Revenue, "INCOME FROM SERVICES / CONSULTING");
        yield return New("4900", "OTHER INCOME", AccountType.Revenue, "INTEREST AND SUNDRY INCOME");
        yield return New("5000", "COST OF GOODS SOLD", AccountType.Expense, "COST OF THE PRODUCTS SOLD");
        yield return New("6000", "RENT", AccountType.Expense, "OFFICE AND WAREHOUSE RENT");
        yield return New("6100", "WAGES AND SALARIES", AccountType.Expense, "STAFF COSTS");
        yield return New("6200", "UTILITIES", AccountType.Expense, "POWER, WATER, HEATING");
        yield return New("6300", "OFFICE SUPPLIES", AccountType.Expense, "STATIONERY AND CONSUMABLES");
        yield return New("6400", "DEPRECIATION", AccountType.Expense, "COST OF FIXED ASSETS USED UP");
        yield return New("6500", "BANK CHARGES", AccountType.Expense, "FEES CHARGED BY THE BANK");
    }

    private static Account New(string code, string name, AccountType type, string description, ControlType control = ControlType.None) =>
        new() { Code = code, Name = name, Type = type, Description = description, Control = control };
}
