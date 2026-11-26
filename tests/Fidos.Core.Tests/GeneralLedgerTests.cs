using Fidos.Core;

namespace Fidos.Core.Tests;

public class GeneralLedgerTests
{
    private static readonly DateOnly Day = new(2026, 1, 15);

    [Fact]
    public void NewCompany_HasSeededChartAndNoBalances()
    {
        var company = Company.CreateNew("alice");

        Assert.Equal("ALICE", company.OperatorId);
        Assert.Equal("NORTHSEA", company.Name);
        Assert.NotNull(company.FindAccount(SystemAccounts.Bank));
        Assert.Equal(ControlType.Receivables, company.GetAccount(SystemAccounts.Receivables).Control);
        Assert.Empty(company.TrialBalance());
    }

    [Fact]
    public void PostJournal_BalancedEntry_UpdatesBalances()
    {
        var company = Company.CreateNew();

        var journal = company.PostJournal(Day, "Owner invests", [new("1000", 10_000, 0), new("3000", 0, 10_000)]);

        Assert.Equal(1, journal.Number);
        Assert.Equal("OWNER INVESTS", journal.Description);
        Assert.Equal(10_000, company.Balance("1000"));
        Assert.Equal(-10_000, company.Balance("3000"));
    }

    [Fact]
    public void PostJournal_Unbalanced_IsRejected()
    {
        var company = Company.CreateNew();

        var ex = Assert.Throws<BookkeepingException>(() =>
            company.PostJournal(Day, "Bad", [new("1000", 100, 0), new("3000", 0, 90)]));

        Assert.Contains("OUT OF BALANCE", ex.Message);
        Assert.Empty(company.Journals);
    }

    [Theory]
    [InlineData("1100")]
    [InlineData("2000")]
    public void PostJournal_ToControlAccount_IsRejected(string control)
    {
        var company = Company.CreateNew();

        var ex = Assert.Throws<BookkeepingException>(() =>
            company.PostJournal(Day, "Sneaky", [new(control, 100, 0), new("4000", 0, 100)]));

        Assert.Contains("CONTROL ACCOUNT", ex.Message);
    }

    [Fact]
    public void PostJournal_ValidatesLines()
    {
        var company = Company.CreateNew();

        Assert.Throws<BookkeepingException>(() => company.PostJournal(Day, "One line", [new("1000", 1, 0)]));
        Assert.Throws<BookkeepingException>(() => company.PostJournal(Day, "Both", [new("1000", 1, 1), new("3000", 0, 0)]));
        Assert.Throws<BookkeepingException>(() => company.PostJournal(Day, "Neg", [new("1000", -1, 0), new("3000", 0, -1)]));
        Assert.Throws<BookkeepingException>(() => company.PostJournal(Day, "Unknown", [new("9999", 1, 0), new("3000", 0, 1)]));
        Assert.Throws<BookkeepingException>(() => company.PostJournal(Day, "Decimals", [new("1000", 1.001m, 0), new("3000", 0, 1.001m)]));
        Assert.Throws<BookkeepingException>(() => company.PostJournal(Day, " ", [new("1000", 1, 0), new("3000", 0, 1)]));
    }

    [Fact]
    public void ReverseJournal_SwapsDebitsAndCredits_AndCanOnlyHappenOnce()
    {
        var company = Company.CreateNew();
        var original = company.PostJournal(Day, "Rent miscoded", [new("6000", 50, 0), new("1000", 0, 50)]);

        var reversal = company.ReverseJournal(original.Number, Day);

        Assert.Equal(original.Number, reversal.ReversalOf);
        Assert.Equal(reversal.Number, original.ReversedBy);
        Assert.Equal(0, company.Balance("6000"));
        Assert.Equal(0, company.Balance("1000"));
        Assert.Throws<BookkeepingException>(() => company.ReverseJournal(original.Number, Day));
        Assert.Throws<BookkeepingException>(() => company.ReverseJournal(reversal.Number, Day));
    }

    [Fact]
    public void AddAccount_EnforcesNumberingConvention()
    {
        var company = Company.CreateNew();

        var account = company.AddAccount("6600", "Marketing", AccountType.Expense);

        Assert.True(account.IsUserDefined);
        Assert.Equal("MARKETING", account.Name);
        Assert.Throws<BookkeepingException>(() => company.AddAccount("6600", "Dup", AccountType.Expense));
        Assert.Throws<BookkeepingException>(() => company.AddAccount("1600", "Wrong range", AccountType.Expense));
        Assert.Throws<BookkeepingException>(() => company.AddAccount("66", "Short", AccountType.Expense));
        Assert.Throws<BookkeepingException>(() => company.AddAccount("6700", "", AccountType.Expense));
    }

    [Fact]
    public void TrialBalance_AlwaysBalances()
    {
        var company = Company.CreateNew();
        company.PostJournal(Day, "Capital", [new("1000", 5_000, 0), new("3000", 0, 5_000)]);
        company.PostJournal(Day, "Rent", [new("6000", 1_200, 0), new("1000", 0, 1_200)]);

        var tb = company.TrialBalance();

        Assert.Equal(tb.Sum(r => r.Debit), tb.Sum(r => r.Credit));
        Assert.Equal(3, tb.Count);
    }

    [Fact]
    public void FinancialStatements_TieTogether()
    {
        var company = Company.CreateNew();
        company.PostJournal(Day, "Capital", [new("1000", 5_000, 0), new("3000", 0, 5_000)]);
        company.PostJournal(Day, "Cash sale", [new("1000", 800, 0), new("4000", 0, 800)]);
        company.PostJournal(Day, "Rent", [new("6000", 300, 0), new("1000", 0, 300)]);

        var pl = company.ProfitAndLoss(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var bs = company.BalanceSheet(new DateOnly(2026, 12, 31));

        Assert.Equal(500, pl.NetProfit);
        Assert.Equal(500, bs.CurrentEarnings);
        Assert.Equal(5_500, bs.TotalAssets);
        Assert.True(bs.Balances);
    }

    [Fact]
    public void CloseYear_MovesProfitToRetainedEarnings()
    {
        var company = Company.CreateNew();
        company.PostJournal(Day, "Cash sale", [new("1000", 800, 0), new("4000", 0, 800)]);
        company.PostJournal(Day, "Rent", [new("6000", 300, 0), new("1000", 0, 300)]);
        var yearEnd = new DateOnly(2026, 12, 31);

        var close = company.CloseYear(yearEnd);

        Assert.Equal(JournalSource.YearEnd, close.Source);
        Assert.Equal(0, company.Balance("4000"));
        Assert.Equal(0, company.Balance("6000"));
        Assert.Equal(-500, company.Balance(SystemAccounts.RetainedEarnings));
        Assert.Equal(500, company.ProfitAndLoss(new DateOnly(2026, 1, 1), yearEnd).NetProfit);
        Assert.True(company.BalanceSheet(yearEnd).Balances);
        Assert.Throws<BookkeepingException>(() => company.CloseYear(yearEnd));
    }

    [Fact]
    public void CloseYear_RejectsDateOnOrBeforeAnEarlierClose()
    {
        var company = Company.CreateNew();
        company.PostJournal(Day, "Cash sale", [new("1000", 800, 0), new("4000", 0, 800)]);
        company.CloseYear(new DateOnly(2026, 12, 31));

        var ex = Assert.Throws<BookkeepingException>(() => company.CloseYear(Day));

        Assert.Contains("ALREADY CLOSED", ex.Message);
        Assert.Equal(-800, company.Balance(SystemAccounts.RetainedEarnings));
    }

    [Fact]
    public void AccountActivity_HasRunningBalance()
    {
        var company = Company.CreateNew();
        company.PostJournal(Day, "In", [new("1000", 100, 0), new("3000", 0, 100)]);
        company.PostJournal(Day.AddDays(1), "Out", [new("6300", 40, 0), new("1000", 0, 40)]);

        var rows = company.AccountActivity("1000");

        Assert.Equal([100m, 60m], rows.Select(r => r.RunningBalance));
    }
}
