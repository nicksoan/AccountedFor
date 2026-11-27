namespace Fidos.Core.Training;

public sealed record Objective(string Text, Func<Company, bool> IsMet);

/// <summary>An "Accounted For" training level: a briefing, objectives checked against the sandbox, and points.</summary>
public sealed record Level(
    int Number,
    string Title,
    string Concept,
    IReadOnlyList<string> Briefing,
    IReadOnlyList<Objective> Objectives,
    IReadOnlyList<string> Hints,
    int Points);

public static class Milestones
{
    public const string ChartOfAccounts = "VIEW:COA";
    public const string AccountInquiry = "VIEW:TACCOUNT";
    public const string TrialBalance = "VIEW:TB";
    public const string Reconciliation = "VIEW:RECON";
    public const string ProfitAndLoss = "VIEW:PL";
    public const string BalanceSheet = "VIEW:BS";
    public const string AgedDebtors = "VIEW:AGEDAR";
}

public static class LevelCatalog
{
    public static IReadOnlyList<Level> All { get; } =
    [
        new(1, "FIRST LOGON", "THE CHART OF ACCOUNTS",
            [
                "WELCOME TO NORTHSEA. YOU HAVE JUST JOINED THE ACCOUNTS DEPARTMENT AND BEEN GIVEN ACCESS TO FIDOS, THE COMPANY ERP.",
                "EVERY BUSINESS KEEPS A LIST OF 'LEDGER ACCOUNTS' CALLED THE CHART OF ACCOUNTS. EACH ACCOUNT IS ONE OF FIVE TYPES: ASSETS, LIABILITIES, EQUITY, REVENUE AND EXPENSES.",
                "THE ACCOUNTING EQUATION TIES THEM TOGETHER: ASSETS = LIABILITIES + EQUITY (+ REVENUE - EXPENSES).",
                "EACH ACCOUNT CAN BE PICTURED AS A 'T': DEBITS ON THE LEFT, CREDITS ON THE RIGHT.",
            ],
            [
                new("VIEW THE CHART OF ACCOUNTS (GL MENU OPTION 1, CODE COA)", c => c.HasMilestone(Milestones.ChartOfAccounts)),
                new("OPEN ANY ACCOUNT IN T-ACCOUNT INQUIRY (CODE TACCT)", c => c.HasMilestone(Milestones.AccountInquiry)),
            ],
            ["TYPE COA ON THE COMMAND LINE AND PRESS ENTER.", "FROM THE CHART, SELECT AN ACCOUNT CODE TO OPEN ITS T-ACCOUNT."],
            50),
        new(2, "DEAD CLIC", "DEBITS AND CREDITS",
            [
                "DEBIT (DR) IS THE LEFT SIDE OF AN ACCOUNT; CREDIT (CR) IS THE RIGHT. THEY DO NOT SIMPLY MEAN GOOD OR BAD.",
                "EVERY TRANSACTION HAS EQUAL TOTAL DEBITS AND CREDITS: THIS IS DOUBLE ENTRY.",
                "OPEN THE DRILL FOR GUIDED PRACTICE, ONE SMALL EXAMPLE AT A TIME. THESE EXERCISES DO NOT POST TO YOUR BOOKS.",
                "YOU WILL POST AND INSPECT A REAL PRACTICE JOURNAL IN THE NEXT LEVEL.",
            ],
            GuidedDrillCatalog.All.Select((step, index) => new Objective(step.Objective + " (CODE DRILL)",
                c => c.Drill.GuidedStepsCompleted > index || c.Drill.Correct >= 5 || c.IsLevelComplete(2))).ToArray(),
            ["DEAD CLIC: DEBITS INCREASE DRAWINGS, EXPENSES AND ASSETS; CREDITS INCREASE LIABILITIES, INCOME AND CAPITAL.",
             "TO DECREASE AN ACCOUNT, USE THE OPPOSITE SIDE. DRAWINGS REDUCE EQUITY, SO THEY INCREASE WITH A DEBIT."],
            100),
        new(3, "CAPITAL INJECTION", "YOUR FIRST JOURNAL",
            [
                "NORTHSEA NEEDS STARTING MONEY. THE OWNER IS PAYING THEIR OWN SAVINGS INTO THE BUSINESS BANK ACCOUNT.",
                "A JOURNAL ENTRY RECORDS A TRANSACTION DIRECTLY IN THE GENERAL LEDGER. IT MUST BALANCE: TOTAL DEBITS = TOTAL CREDITS.",
                "THE BANK (AN ASSET) GOES UP, AND THE BUSINESS NOW OWES THE OWNER (CAPITAL, EQUITY) THE SAME AMOUNT.",
            ],
            [
                new("POST A JOURNAL DEBITING 1000 BANK AND CREDITING 3000 OWNER'S CAPITAL", c => c.Journals.Any(j =>
                    j.Source == JournalSource.Manual && j.ReversalOf is null &&
                    j.Lines.Any(l => l.AccountCode == SystemAccounts.Bank && l.Debit > 0) &&
                    j.Lines.Any(l => l.AccountCode == SystemAccounts.Capital && l.Credit > 0))),
            ],
            ["USE JOURNAL ENTRY (CODE JE). LINE 1: 1000 DEBIT 10000. LINE 2: 3000 CREDIT 10000."],
            100),
        new(4, "A LEDGER OF YOUR OWN", "CREATING LEDGER ACCOUNTS",
            [
                "NORTHSEA IS STARTING TO ADVERTISE, BUT THERE IS NO MARKETING ACCOUNT IN THE CHART.",
                "ACCOUNTANTS CREATE NEW LEDGER ACCOUNTS WHENEVER MANAGEMENT NEEDS TO SEE A COST OR INCOME SEPARATELY.",
                "FIDOS NUMBERS ACCOUNTS BY TYPE: 1XXX ASSETS, 2XXX LIABILITIES, 3XXX EQUITY, 4XXX REVENUE, 5XXX-9XXX EXPENSES.",
            ],
            [
                new("CREATE A NEW EXPENSE ACCOUNT (E.G. 6600 MARKETING)", c => c.Accounts.Any(a => a.IsUserDefined && a.Type == AccountType.Expense)),
                new("POST A JOURNAL THAT DEBITS YOUR NEW EXPENSE ACCOUNT", c => c.Journals.Any(j => j.ReversalOf is null &&
                    j.Lines.Any(l => l.Debit > 0 && c.FindAccount(l.AccountCode) is { IsUserDefined: true, Type: AccountType.Expense }))),
            ],
            ["CODE NEWACCT CREATES THE ACCOUNT. THEN PAY FOR SOME ADVERTISING FROM THE BANK WITH A JOURNAL (CODE JE)."],
            100),
        new(5, "MAKING A SALE", "THE SALES LEDGER (ACCOUNTS RECEIVABLE)",
            [
                "WHEN NORTHSEA SELLS ON CREDIT, THE CUSTOMER OWES MONEY. WE TRACK EACH CUSTOMER IN A SUB-LEDGER: THE SALES LEDGER.",
                "THE GENERAL LEDGER ONLY HOLDS ONE TOTAL FOR ALL CUSTOMERS - THE RECEIVABLES CONTROL ACCOUNT (1100).",
                "AN INVOICE WITH VAT POSTS: DR 1100 RECEIVABLES (GROSS), CR 4XXX SALES (NET), CR 2200 VAT OUTPUT (TAX).",
                "THE VAT IS NOT NORTHSEA'S INCOME - IT IS COLLECTED ON BEHALF OF THE TAX OFFICE, SO IT IS A LIABILITY.",
            ],
            [
                new("CREATE A CUSTOMER (CODE NEWCUST)", c => c.Customers.Count > 0),
                new("RAISE A SALES INVOICE THAT INCLUDES VAT (CODE SINV)", c => c.SalesInvoices.Any(i => i.Vat > 0)),
            ],
            ["AFTER POSTING, FIDOS SHOWS YOU THE JOURNAL IT CREATED. STUDY WHICH ACCOUNTS WERE HIT."],
            150),
        new(6, "GETTING PAID", "CUSTOMER RECEIPTS",
            [
                "YOUR CUSTOMER HAS PAID. THE MONEY ARRIVES IN THE BANK AND THE AMOUNT THEY OWE GOES DOWN.",
                "RECEIPT: DR 1000 BANK, CR 1100 RECEIVABLES. NOTE THAT NO REVENUE IS RECORDED - THAT HAPPENED WHEN YOU INVOICED.",
                "THIS IS THE ACCRUALS (MATCHING) PRINCIPLE: INCOME IS RECOGNISED WHEN EARNED, NOT WHEN CASH IS RECEIVED.",
            ],
            [
                new("POST A CUSTOMER RECEIPT (CODE RCPT)", c => c.CustomerReceipts.Count > 0),
                new("FULLY SETTLE A SALES INVOICE", c => c.SalesInvoices.Any(i => i.IsPaid)),
            ],
            ["ENTER THE FULL GROSS AMOUNT OF THE INVOICE, INCLUDING VAT."],
            150),
        new(7, "BUYING ON CREDIT", "THE PURCHASE LEDGER (ACCOUNTS PAYABLE)",
            [
                "SUPPLIERS SEND NORTHSEA INVOICES. UNTIL WE PAY, WE OWE THEM MONEY: A LIABILITY.",
                "EACH SUPPLIER IS TRACKED IN THE PURCHASE LEDGER; THE GL HOLDS THE TOTAL IN PAYABLES CONTROL (2000).",
                "PURCHASE INVOICE: DR EXPENSE OR ASSET (NET), DR 1300 VAT INPUT (RECLAIMABLE), CR 2000 PAYABLES (GROSS).",
            ],
            [
                new("CREATE A SUPPLIER (CODE NEWSUPP)", c => c.Suppliers.Count > 0),
                new("ENTER A PURCHASE INVOICE (CODE PINV)", c => c.PurchaseInvoices.Count > 0),
            ],
            ["TRY BUYING STOCK (1200 INVENTORY) OR PAYING RENT (6000) ON CREDIT."],
            150),
        new(8, "SETTLING UP", "SUPPLIER PAYMENTS",
            [
                "TIME TO PAY THE SUPPLIER. THE LIABILITY GOES DOWN AND SO DOES THE BANK BALANCE.",
                "PAYMENT: DR 2000 PAYABLES, CR 1000 BANK. AGAIN - NO EXPENSE HERE, IT WAS RECORDED WITH THE INVOICE.",
            ],
            [
                new("PAY A SUPPLIER (CODE PAY)", c => c.SupplierPayments.Count > 0),
                new("FULLY SETTLE A PURCHASE INVOICE", c => c.PurchaseInvoices.Any(i => i.IsPaid)),
            ],
            ["CHECK THE SUPPLIER STATEMENT (CODE SUPP, THEN SELECT THE SUPPLIER) TO SEE WHAT IS OWED."],
            150),
        new(9, "PROVE IT", "TRIAL BALANCE AND CONTROL ACCOUNTS",
            [
                "BECAUSE EVERY DEBIT HAS A MATCHING CREDIT, THE SUM OF ALL DEBIT BALANCES ALWAYS EQUALS THE SUM OF ALL CREDIT BALANCES.",
                "THE TRIAL BALANCE LISTS EVERY ACCOUNT TO PROVE THIS. IT WILL NOT CATCH EVERYTHING - A BALANCED ENTRY TO THE WRONG ACCOUNT STILL BALANCES!",
                "THE CONTROL ACCOUNT RECONCILIATION PROVES THAT THE SUB-LEDGERS AGREE WITH THEIR GL CONTROL ACCOUNTS.",
                "THAT IS WHY FIDOS BLOCKS MANUAL JOURNALS TO CONTROL ACCOUNTS: THEY WOULD BREAK THE RECONCILIATION.",
            ],
            [
                new("RUN THE TRIAL BALANCE (CODE TB)", c => c.HasMilestone(Milestones.TrialBalance)),
                new("RUN THE CONTROL ACCOUNT RECONCILIATION (CODE RECON)", c => c.HasMilestone(Milestones.Reconciliation)),
            ],
            ["TRY POSTING A JOURNAL TO 1100 AND READ THE ERROR MESSAGE."],
            100),
        new(10, "OOPS!", "CORRECTING ERRORS",
            [
                "IN A REAL LEDGER YOU NEVER DELETE OR EDIT A POSTED ENTRY - THE AUDIT TRAIL MUST STAY INTACT.",
                "INSTEAD YOU POST A REVERSAL: THE SAME LINES WITH DEBITS AND CREDITS SWAPPED, CANCELLING THE ORIGINAL.",
                "THEN YOU POST THE CORRECT ENTRY.",
            ],
            [
                new("REVERSE A MANUAL JOURNAL (OPEN IT FROM CODE JNLS AND USE REVERSE)", c => c.Journals.Any(j => j.ReversalOf is not null)),
            ],
            ["POST A DELIBERATE MISTAKE - E.G. 50 OF STATIONERY CODED TO RENT - THEN REVERSE IT."],
            150),
        new(11, "ADJUSTMENTS", "ACCRUALS AND DEPRECIATION",
            [
                "AT MONTH END ACCOUNTANTS POST ADJUSTMENTS SO THE PROFIT REFLECTS THE PERIOD CORRECTLY.",
                "DEPRECIATION SPREADS THE COST OF A LONG-LIVED ASSET OVER ITS LIFE: DR 6400 DEPRECIATION, CR 1510 ACCUMULATED DEPRECIATION.",
                "AN ACCRUAL RECORDS A COST ALREADY INCURRED BUT NOT YET BILLED: DR THE EXPENSE, CR 2100 ACCRUED EXPENSES.",
            ],
            [
                new("POST DEPRECIATION: DR 6400, CR 1510", c => c.Journals.Any(j => j.ReversalOf is null &&
                    j.Lines.Any(l => l.AccountCode == "6400" && l.Debit > 0) && j.Lines.Any(l => l.AccountCode == "1510" && l.Credit > 0))),
                new("POST AN ACCRUAL: DR ANY EXPENSE, CR 2100", c => c.Journals.Any(j => j.ReversalOf is null &&
                    j.Lines.Any(l => l.Debit > 0 && c.FindAccount(l.AccountCode)?.Type == AccountType.Expense) &&
                    j.Lines.Any(l => l.AccountCode == SystemAccounts.Accruals && l.Credit > 0))),
            ],
            ["BOTH ARE MANUAL JOURNALS (CODE JE)."],
            200),
        new(12, "THE STORY IN NUMBERS", "FINANCIAL STATEMENTS",
            [
                "THE PROFIT AND LOSS (INCOME STATEMENT) SHOWS REVENUE LESS EXPENSES FOR A PERIOD.",
                "THE BALANCE SHEET SHOWS WHAT THE BUSINESS OWNS AND OWES AT A POINT IN TIME. IT MUST BALANCE.",
                "THE AGED DEBTORS REPORT SHOWS WHO OWES MONEY AND HOW OVERDUE IT IS - VITAL FOR CASH FLOW.",
            ],
            [
                new("RUN THE PROFIT AND LOSS (CODE PL)", c => c.HasMilestone(Milestones.ProfitAndLoss)),
                new("RUN THE BALANCE SHEET (CODE BS)", c => c.HasMilestone(Milestones.BalanceSheet)),
                new("RUN THE AGED DEBTORS REPORT (CODE AGEDAR)", c => c.HasMilestone(Milestones.AgedDebtors)),
            ],
            ["NOTICE HOW THE PROFIT ON THE P&L APPEARS IN EQUITY ON THE BALANCE SHEET."],
            150),
        new(13, "YEAR END", "CLOSING THE BOOKS",
            [
                "REVENUE AND EXPENSE ACCOUNTS MEASURE ONE YEAR'S PERFORMANCE, SO AT YEAR END THEY ARE RESET TO ZERO.",
                "THE CLOSING JOURNAL MOVES THEIR BALANCES INTO RETAINED EARNINGS (3200), PART OF EQUITY.",
                "ASSET, LIABILITY AND EQUITY BALANCES CARRY FORWARD INTO THE NEW YEAR.",
            ],
            [
                new("RUN THE YEAR-END CLOSE (CODE YEC)", c => c.Journals.Any(j => j.Source == JournalSource.YearEnd)),
            ],
            ["COMPARE THE TRIAL BALANCE BEFORE AND AFTER THE CLOSE."],
            250),
    ];

    public static Level? Get(int number) => All.FirstOrDefault(l => l.Number == number);

    public static int MaxScore => All.Sum(l => l.Points);
}
