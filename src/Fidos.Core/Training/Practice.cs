namespace Fidos.Core.Training;

public sealed record PracticeMission(
    string Title, string Situation, string DebitCode, string DebitReason,
    string CreditCode, string CreditReason, decimal Amount);

public static class PracticeCatalog
{
    private static readonly DateOnly PracticeDate = new(2026, 1, 1);

    public static IReadOnlyList<PracticeMission> All { get; } =
    [
        new("Owner investment", "The owner puts £100 of their own money into the business bank account.",
            SystemAccounts.Bank, "Bank is an asset (something the business owns). Money coming in increases that asset, so debit Bank.",
            SystemAccounts.Capital, "Owner's capital is the owner's stake in the business. The investment increases that stake, so credit Capital.",
            100),
        new("First sale", "A customer buys goods for £25 and pays by bank transfer right away. Ignore VAT in this example.",
            SystemAccounts.Bank, "The money arrives in the bank. Bank is an asset, and a debit increases an asset.",
            "4000", "The business has earned sales income. A credit increases income, so credit Sales.",
            25),
        new("Buy office supplies", "The business spends £10 from the bank on office supplies it uses straight away.",
            "6300", "Office supplies are a cost to the business. A debit increases an expense.",
            SystemAccounts.Bank, "Money leaves the bank. A credit decreases an asset, so credit Bank.",
            10),
    ];

    // Rebuild these example books from completed missions; never post into the learner's main books.
    public static Company OpenBooks(int completedMissions)
    {
        var books = Company.CreateNew("PRACTICE");
        foreach (var mission in All.Take(completedMissions))
        {
            Post(books, mission, mission.DebitCode, mission.Amount, mission.CreditCode, mission.Amount);
        }

        return books;
    }

    internal static JournalEntry PostEntry(int missionIndex, string? debitCode, decimal debit, string? creditCode, decimal credit)
    {
        var mission = All[missionIndex];
        if (debitCode != mission.DebitCode)
        {
            throw new BookkeepingException($"Check the debit (left) account. {mission.DebitReason}");
        }

        if (creditCode != mission.CreditCode)
        {
            throw new BookkeepingException($"Check the credit (right) account. {mission.CreditReason}");
        }

        if (debit != credit)
        {
            throw new BookkeepingException("The entry doesn't balance yet. Use the same amount on both sides.");
        }

        if (debit != mission.Amount)
        {
            throw new BookkeepingException($"The entry balances, but this transaction is for £{mission.Amount:0.00}. Check both amounts.");
        }

        return Post(OpenBooks(missionIndex), mission, debitCode, debit, creditCode, credit);
    }

    private static JournalEntry Post(Company books, PracticeMission mission, string? debitCode, decimal debit, string? creditCode, decimal credit) =>
        books.PostJournal(PracticeDate, mission.Title, [new(debitCode, debit, 0), new(creditCode, 0, credit)]);
}
