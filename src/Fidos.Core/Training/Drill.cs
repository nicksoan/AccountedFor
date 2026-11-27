namespace Fidos.Core.Training;

public sealed record DrillQuestion(int Id, string Scenario, string DebitCode, string CreditCode, string Explanation);

public sealed class DrillState
{
    public int CurrentQuestion { get; set; } = 1;
    public int Attempts { get; set; }
    public int Correct { get; set; }
    public int Streak { get; set; }
    public int BestStreak { get; set; }
}

public sealed record DrillResult(DrillQuestion Question, bool IsCorrect, bool DebitCorrect, bool CreditCorrect);

/// <summary>"Debit or credit?" practice questions using the seeded FIDOS chart of accounts.</summary>
public static class DrillCatalog
{
    public static IReadOnlyList<DrillQuestion> All { get; } =
    [
        new(1, "THE OWNER PAYS 10,000 OF THEIR OWN MONEY INTO THE BUSINESS BANK ACCOUNT.", "1000", "3000",
            "BANK (ASSET) GOES UP = DEBIT. THE BUSINESS NOW OWES THE OWNER, SO CAPITAL (EQUITY) GOES UP = CREDIT."),
        new(2, "BUY OFFICE STATIONERY FOR 45, PAID STRAIGHT FROM THE BANK.", "6300", "1000",
            "OFFICE SUPPLIES (EXPENSE) GOES UP = DEBIT. BANK (ASSET) GOES DOWN = CREDIT."),
        new(3, "PAY THIS MONTH'S OFFICE RENT OF 1,200 BY BANK TRANSFER.", "6000", "1000",
            "RENT (EXPENSE) GOES UP = DEBIT. BANK (ASSET) GOES DOWN = CREDIT."),
        new(4, "INVOICE A CUSTOMER 500 FOR CONSULTING SERVICES ON CREDIT (IGNORE VAT).", "1100", "4100",
            "THE CUSTOMER NOW OWES YOU: RECEIVABLES (ASSET) UP = DEBIT. SERVICE SALES (REVENUE) UP = CREDIT."),
        new(5, "A CUSTOMER PAYS THE 500 THEY OWED INTO THE BANK.", "1000", "1100",
            "BANK (ASSET) UP = DEBIT. RECEIVABLES (ASSET) DOWN = CREDIT. NO REVENUE - THAT WAS RECORDED WHEN YOU INVOICED."),
        new(6, "RECEIVE A SUPPLIER INVOICE FOR 300 OF STOCK BOUGHT ON CREDIT (IGNORE VAT).", "1200", "2000",
            "INVENTORY (ASSET) UP = DEBIT. YOU NOW OWE THE SUPPLIER: PAYABLES (LIABILITY) UP = CREDIT."),
        new(7, "PAY A SUPPLIER THE 300 YOU OWE THEM.", "2000", "1000",
            "PAYABLES (LIABILITY) DOWN = DEBIT. BANK (ASSET) DOWN = CREDIT."),
        new(8, "THE OWNER TAKES 200 OUT OF THE BUSINESS FOR PERSONAL USE.", "3100", "1000",
            "DRAWINGS REDUCE EQUITY, SO DRAWINGS GOES UP = DEBIT. BANK (ASSET) DOWN = CREDIT."),
        new(9, "RECORD 150 OF DEPRECIATION ON OFFICE EQUIPMENT.", "6400", "1510",
            "DEPRECIATION (EXPENSE) UP = DEBIT. ACCUMULATED DEPRECIATION (CONTRA ASSET) UP = CREDIT, REDUCING THE ASSET."),
        new(10, "THE BANK LENDS THE BUSINESS 5,000, PAID INTO THE CURRENT ACCOUNT.", "1000", "2300",
            "BANK (ASSET) UP = DEBIT. BANK LOAN (LIABILITY) UP = CREDIT."),
        new(11, "THE BANK DEDUCTS 12 OF CHARGES FROM THE ACCOUNT.", "6500", "1000",
            "BANK CHARGES (EXPENSE) UP = DEBIT. BANK (ASSET) DOWN = CREDIT."),
        new(12, "BUY A NEW SERVER FOR 2,000, PAID FROM THE BANK. IT WILL LAST YEARS.", "1500", "1000",
            "A LONG-LIVED ITEM IS CAPITALISED: EQUIPMENT (ASSET) UP = DEBIT. BANK (ASSET) DOWN = CREDIT."),
        new(13, "YOU HAVE USED 90 OF ELECTRICITY THIS MONTH BUT HAVE NO BILL YET.", "6200", "2100",
            "ACCRUAL: UTILITIES (EXPENSE) UP = DEBIT. ACCRUED EXPENSES (LIABILITY) UP = CREDIT."),
        new(14, "TOP UP THE PETTY CASH TIN WITH 100 WITHDRAWN FROM THE BANK.", "1050", "1000",
            "ONE ASSET UP, ANOTHER DOWN: PETTY CASH = DEBIT, BANK = CREDIT."),
        new(15, "SELL GOODS FOR 80 CASH, PAID INTO THE BANK IMMEDIATELY (IGNORE VAT).", "1000", "4000",
            "BANK (ASSET) UP = DEBIT. SALES - GOODS (REVENUE) UP = CREDIT."),
        new(16, "PAY STAFF WAGES OF 2,400 FROM THE BANK.", "6100", "1000",
            "WAGES (EXPENSE) UP = DEBIT. BANK (ASSET) DOWN = CREDIT."),
    ];

    public static DrillQuestion Get(int id) => All.FirstOrDefault(q => q.Id == id) ?? All[0];
}
