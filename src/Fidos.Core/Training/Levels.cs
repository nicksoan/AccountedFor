namespace Fidos.Core.Training;

public sealed record Objective(string Text, Func<Company, bool> IsMet);

public sealed record GuidedScenario(
    string Situation,
    string Teaching,
    IReadOnlyList<string> Instructions,
    int ObjectiveIndex);

/// <summary>An "Accounted For" training level: a briefing, objectives checked against the sandbox, and points.</summary>
public sealed record Level(
    int Number,
    string Title,
    string Concept,
    IReadOnlyList<string> Briefing,
    IReadOnlyList<Objective> Objectives,
    IReadOnlyList<string> Hints,
    int Points)
{
    public IReadOnlyList<GuidedScenario> Scenarios { get; init; } = [];
}

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
        new Level(1, "FIRST LOGON", "THE CHART OF ACCOUNTS",
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
            50)
        {
            Scenarios =
            [
                new("It's your first day at NorthSea. Find where the business keeps track of money in the bank.",
                    "The chart of accounts is the list of places the business records its money, what it owes, what it earns and what it spends.",
                    ["Open the chart of accounts and find 1000 BANK. Look for its account type: asset."], 0),
                new("Open the Bank ledger before recording your first transaction.",
                    "The T-account shows entries on two sides: debits on the left and credits on the right. Empty books have no entries yet.",
                    ["Select account 1000 in the chart to open its T-account. Find its debit and credit columns."], 1),
            ],
        },
        new(2, "DEAD CLIC", "DEBITS AND CREDITS",
            [
                "DEBIT (DR) IS THE LEFT SIDE OF AN ACCOUNT; CREDIT (CR) IS THE RIGHT. THEY DO NOT SIMPLY MEAN GOOD OR BAD.",
                "EVERY TRANSACTION HAS EQUAL TOTAL DEBITS AND CREDITS: THIS IS DOUBLE ENTRY.",
                "OPEN THE PRACTICE BOOKS AND POST BOTH SIDES OF EACH TRANSACTION, ONE SMALL EXAMPLE AT A TIME.",
                "THE EXAMPLE JOURNALS AND BALANCES STAY SEPARATE FROM YOUR MAIN SANDBOX BOOKS.",
            ],
            PracticeCatalog.All.Select((mission, index) => new Objective($"POST: {mission.Title.ToUpperInvariant()}",
                c => c.PracticeMissionsCompleted > index || c.IsLevelComplete(2))).ToArray(),
            ["DEAD CLIC: DEBITS INCREASE DRAWINGS, EXPENSES AND ASSETS; CREDITS INCREASE LIABILITIES, INCOME AND CAPITAL.",
             "TO DECREASE AN ACCOUNT, USE THE OPPOSITE SIDE. DRAWINGS REDUCE EQUITY, SO THEY INCREASE WITH A DEBIT."],
            100),
        new Level(3, "CAPITAL INJECTION", "YOUR FIRST JOURNAL",
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
            100)
        {
            Scenarios =
            [
                new("The owner puts £10,000 of personal savings into NorthSea's bank account.",
                    "Bank is an asset, and an increase in an asset is a debit. The owner's capital is equity, and an increase in equity is a credit. The two sides balance because the business receives exactly what the owner invests.",
                    ["Open Journal Entry. Enter a description such as OWNER INVESTMENT.",
                     "On line 1 enter account 1000, debit 10000, and no credit.",
                     "On line 2 enter account 3000, credit 10000, and no debit. Check that both totals are 10000, then post."],
                    0),
            ],
        },
        new Level(4, "A LEDGER OF YOUR OWN", "CREATING LEDGER ACCOUNTS",
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
            100)
        {
            Scenarios =
            [
                new("NorthSea wants to see its advertising costs separately.",
                    "A separate general-ledger account makes this cost easy to find in reports. Expense accounts use codes from 5000 to 9999, so 6600 is a suitable code for Marketing.",
                    ["Open Create Ledger Account. Enter code 6600, name MARKETING, and type EXPENSE, then create it."],
                    0),
                new("NorthSea pays £250 for its first marketing campaign from the bank.",
                    "The campaign is an expense as soon as it is used, so debit 6600. Money leaving the bank decreases an asset, so credit 1000. This journal records both effects without changing the balance.",
                    ["Open Journal Entry and describe the payment as MARKETING CAMPAIGN.",
                     "Enter line 1 as account 6600, debit 250; enter line 2 as account 1000, credit 250.",
                     "Post the journal, then inspect the new account from the chart if you want to see its entry."],
                    1),
            ],
        },
        new Level(5, "MAKING A SALE", "THE SALES LEDGER (ACCOUNTS RECEIVABLE)",
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
            150)
        {
            Scenarios =
            [
                new("A new customer, Harbour Books, orders goods from NorthSea on credit.",
                    "Create a customer first so the invoice is tracked in that customer's sales-ledger account, not just as a general-ledger total. Use 30-day payment terms.",
                    ["Open New Customer. Enter HARBOUR BOOKS, leave the optional contact blank, enter 30 days, and create the customer."],
                    0),
                new("Invoice Harbour Books £100 for goods, plus 20% VAT.",
                    "The customer owes £120 in total: £100 net sales plus £20 VAT. FIDOS debits the gross £120 to receivables (1100), credits £100 sales (4000), and credits £20 VAT output (2200). VAT is owed to the tax authority, not earned as income.",
                    ["Open Sales Invoice and select Harbour Books.",
                     "Enter a reference such as HB-001, VAT rate 20, and a line for account 4000 SALES - GOODS, description GOODS, net amount 100.",
                     "Post the invoice. Review the journal FIDOS creates and identify the £120 debit and the two credits."],
                    1),
            ],
        },
        new Level(6, "GETTING PAID", "CUSTOMER RECEIPTS",
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
            150)
        {
            Scenarios =
            [
                new("Harbour Books makes a part-payment against its invoice.",
                    "The invoice already recorded the sale and VAT. Receiving cash is a different event: debit bank (1000) and credit receivables (1100). Do not record sales revenue a second time.",
                    ["Open Customer Receipt and select Harbour Books.",
                     "Enter 40 as a part-payment (or a smaller amount than the outstanding balance), select the invoice under ALLOCATE TO INVOICE, and use bank account 1000.",
                     "Post the receipt. The invoice should still have an outstanding balance."],
                    0),
                new("Check that the customer balance has cleared.",
                    "A second receipt clears the remaining balance. The customer sub-ledger and 1100 receivables control account are updated together, without recording the sale again.",
                    ["Open Customer Receipt again and select Harbour Books.",
                     "Enter the remaining amount shown for the invoice, select that invoice, and use bank account 1000.",
                     "Post the receipt. The invoice should now be fully settled."],
                    1),
            ],
        },
        new Level(7, "BUYING ON CREDIT", "THE PURCHASE LEDGER (ACCOUNTS PAYABLE)",
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
            150)
        {
            Scenarios =
            [
                new("NorthSea receives a bill from North Star Supplies for office supplies bought on credit.",
                    "The purchase ledger keeps the amount owed to this supplier. For a £50 purchase plus £10 reclaimable VAT, debit the expense 6300 by £50 and VAT input 1300 by £10; FIDOS credits payables 2000 by the £60 gross total.",
                    ["Open New Supplier. Enter NORTH STAR SUPPLIES, leave the optional contact blank, enter 30-day terms, and create the supplier."],
                    0),
                new("Enter the supplier's £50 office-supplies invoice plus 20% VAT.",
                    "Unlike a manual journal, the purchase-invoice screen calculates the VAT and posts to the supplier's sub-ledger and the 2000 control account together. The supplier is owed £60 in total.",
                    ["Open Purchase Invoice and select North Star Supplies.",
                     "Enter supplier reference NS-001, VAT rate 20, and a line for account 6300 OFFICE SUPPLIES, description OFFICE SUPPLIES, net amount 50.",
                     "Post the invoice and inspect the resulting journal: expense debit £50, VAT input debit £10, and payables credit £60."],
                    1),
            ],
        },
        new Level(8, "SETTLING UP", "SUPPLIER PAYMENTS",
            [
                "TIME TO PAY THE SUPPLIER. THE LIABILITY GOES DOWN AND SO DOES THE BANK BALANCE.",
                "PAYMENT: DR 2000 PAYABLES, CR 1000 BANK. AGAIN - NO EXPENSE HERE, IT WAS RECORDED WITH THE INVOICE.",
            ],
            [
                new("PAY A SUPPLIER (CODE PAY)", c => c.SupplierPayments.Count > 0),
                new("FULLY SETTLE A PURCHASE INVOICE", c => c.PurchaseInvoices.Any(i => i.IsPaid)),
            ],
            ["CHECK THE SUPPLIER STATEMENT (CODE SUPP, THEN SELECT THE SUPPLIER) TO SEE WHAT IS OWED."],
            150)
        {
            Scenarios =
            [
                new("NorthSea makes a part-payment to North Star Supplies.",
                    "Paying a bill reduces both the liability and the bank asset: debit payables (2000) and credit bank (1000). The office-supplies expense and reclaimable VAT were already recorded when the invoice arrived.",
                    ["Open Supplier Payment and select North Star Supplies.",
                     "Enter 20 as a part-payment (or a smaller amount than the outstanding balance), select the invoice under ALLOCATE TO INVOICE, and use bank account 1000.",
                     "Post the payment. The invoice should still have an outstanding balance."],
                    0),
                new("Verify that the supplier balance is clear.",
                    "The payment allocation updates the supplier's sub-ledger and the 2000 payables control account together. Check the supplier statement to confirm nothing remains owed for this invoice.",
                    ["Open Supplier Payment again and select North Star Supplies.",
                     "Enter the remaining amount shown for the invoice, select that invoice, and use bank account 1000.",
                     "Post the payment, then open the supplier statement and confirm the invoice is fully settled."],
                    1),
            ],
        },
        new Level(9, "PROVE IT", "TRIAL BALANCE AND CONTROL ACCOUNTS",
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
            ["EQUAL TOTALS DO NOT PROVE THAT EVERY ENTRY USED THE RIGHT ACCOUNTS."],
            100)
        {
            Scenarios =
            [
                new("Check whether NorthSea's books balance after the recent postings.",
                    "The trial balance adds up every account balance on its debit or credit side.",
                    ["Open Trial Balance and compare the debit and credit grand totals."], 0),
                new("Check whether the customer and supplier ledgers agree with the general ledger.",
                    "A control account summarises a whole sub-ledger. Its balance should match the total of the individual customer or supplier balances.",
                    ["Open Control Account Reconciliation. Compare the general-ledger and sub-ledger totals and look for a difference of zero."], 1),
            ],
        },
        new Level(10, "OOPS!", "CORRECTING ERRORS",
            [
                "IN A REAL LEDGER YOU NEVER DELETE OR EDIT A POSTED ENTRY - THE AUDIT TRAIL MUST STAY INTACT.",
                "INSTEAD YOU POST A REVERSAL: THE SAME LINES WITH DEBITS AND CREDITS SWAPPED, CANCELLING THE ORIGINAL.",
                "THEN YOU POST THE CORRECT ENTRY.",
            ],
            [
                new("REVERSE A MANUAL JOURNAL (OPEN IT FROM CODE JNLS AND USE REVERSE)", c => c.Journals.Any(j => j.ReversalOf is not null)),
            ],
            ["POST A DELIBERATE MISTAKE - E.G. 50 OF STATIONERY CODED TO RENT - THEN REVERSE IT."],
            150)
        {
            Scenarios =
            [
                new("You discover a £50 stationery payment was recorded as rent.",
                    "A reversal cancels the amounts without erasing the original entry. Both entries stay visible in the audit trail.",
                    ["If you don't already have a practice mistake to reverse, use Journal Entry (JE) to post SAMPLE ERROR: debit 6000 RENT £50 and credit 1000 BANK £50.",
                     "Open the journal list, select your practice mistake, choose Reverse and confirm.",
                     "Check that the reversal credits Rent and debits Bank. To record the correct payment, post debit 6300 OFFICE SUPPLIES and credit 1000 BANK for £50."], 0),
            ],
        },
        new Level(11, "ADJUSTMENTS", "ACCRUALS AND DEPRECIATION",
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
            200)
        {
            Scenarios =
            [
                new("NorthSea uses £20 worth of its office equipment's value this month.",
                    "Depreciation records the cost of using equipment. The matching credit tracks the total value written off so far.",
                    ["Open Journal Entry. Enter MONTHLY DEPRECIATION, debit 6400 £20 and credit 1510 £20, then post."], 0),
                new("NorthSea used £30 of electricity this month, but the bill has not arrived.",
                    "Record the cost now. The credit goes to Accrued Expenses to show the business owes money it hasn't paid yet.",
                    ["Open Journal Entry. Enter ELECTRICITY ACCRUAL, debit 6200 UTILITIES £30 and credit 2100 £30, then post."], 1),
            ],
        },
        new Level(12, "THE STORY IN NUMBERS", "FINANCIAL STATEMENTS",
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
            150)
        {
            Scenarios =
            [
                new("The owner asks whether NorthSea made a profit.",
                    "Profit is income minus expenses over a period, not the amount of cash in the bank.",
                    ["Open Profit and Loss. Find sales, expenses and the net profit or loss for the period."], 0),
                new("The owner asks what the business owns and owes today.",
                    "The balance sheet is a snapshot: assets equal liabilities plus equity.",
                    ["Open Balance Sheet. Find Bank under assets and compare total assets with total liabilities and equity."], 1),
                new("Find out whether any customers still owe NorthSea money.",
                    "The aged debtors report groups unpaid invoices by how overdue they are. If earlier invoices were all paid, an empty report is expected.",
                    ["Open Aged Debtors. Look for unpaid balances and find the column that shows the oldest debts."], 2),
            ],
        },
        new Level(13, "YEAR END", "CLOSING THE BOOKS",
            [
                "REVENUE AND EXPENSE ACCOUNTS MEASURE ONE YEAR'S PERFORMANCE, SO AT YEAR END THEY ARE RESET TO ZERO.",
                "THE CLOSING JOURNAL MOVES THEIR BALANCES INTO RETAINED EARNINGS (3200), PART OF EQUITY.",
                "ASSET, LIABILITY AND EQUITY BALANCES CARRY FORWARD INTO THE NEW YEAR.",
            ],
            [
                new("RUN THE YEAR-END CLOSE (CODE YEC)", c => c.Journals.Any(j => j.Source == JournalSource.YearEnd)),
            ],
            ["COMPARE THE TRIAL BALANCE BEFORE AND AFTER THE CLOSE."],
            250)
        {
            Scenarios =
            [
                new("Your practice year is over. Carry the year's profit or loss into retained earnings.",
                    "Closing moves income and expense balances into equity so the next year starts with a clean income and expense record.",
                    ["Open Year-End Close. Enter a date on or after your practice income and expense postings, then preview.",
                     "Find the offset to 3200 RETAINED EARNINGS. Confirm the close only in your practice sandbox.",
                     "Open Trial Balance and compare it with the preview. Bank carries forward; the closed income and expense balances do not."], 0),
            ],
        },
    ];

    public static Level? Get(int number) => All.FirstOrDefault(l => l.Number == number);

    public static int MaxScore => All.Sum(l => l.Points);
}
