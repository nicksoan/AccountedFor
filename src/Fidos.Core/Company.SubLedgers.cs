namespace Fidos.Core;

public sealed partial class Company
{
    // ---------------------------------------------------------------- Customers / Accounts Receivable (sales ledger)

    public Customer? FindCustomer(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : Customers.FirstOrDefault(c => string.Equals(c.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));

    public Customer GetCustomer(string? id) =>
        FindCustomer(id) ?? throw new BookkeepingException($"CUSTOMER '{id?.Trim()}' NOT FOUND");

    public Customer AddCustomer(string? name, string? contact = null, int termsDays = 30)
    {
        var customer = new Customer
        {
            Name = Required(name, "CUSTOMER NAME"),
            Contact = string.IsNullOrWhiteSpace(contact) ? null : Clean(contact, MaxTextLength),
            TermsDays = CheckTerms(termsDays),
        };
        customer.Id = $"C{NextSequence("CUST"):0000}";
        Customers.Add(customer);
        return customer;
    }

    /// <summary>Raises a sales invoice: DR receivables control (gross), CR revenue (net), CR VAT output (tax).</summary>
    public SalesInvoice PostSalesInvoice(
        string? customerId,
        DateOnly date,
        IEnumerable<DocumentLineInput> lines,
        decimal vatRatePercent,
        string? reference = null)
    {
        var customer = GetCustomer(customerId);
        var (docLines, net, vat) = PrepareDocument(lines, vatRatePercent, a => a.Type == AccountType.Revenue,
            "SALES INVOICE LINES MUST USE A REVENUE (4XXX) ACCOUNT");
        var number = $"SI{NextSequence("SI"):00000}";

        var journalLines = new List<JournalLineInput>
        {
            new(SystemAccounts.Receivables, net + vat, 0, $"{customer.Id} {number}", customer.Id),
        };
        journalLines.AddRange(docLines.Select(l => new JournalLineInput(l.AccountCode, 0, l.Net, l.Description)));
        if (vat != 0)
        {
            journalLines.Add(new JournalLineInput(SystemAccounts.VatOutput, 0, vat, $"VAT {vatRatePercent:0.##}% ON {number}"));
        }

        var journal = Post(JournalSource.SalesLedger, date, $"SALES INV {number} {customer.Name}", number, journalLines, allowControlAccounts: true);
        var invoice = new SalesInvoice();
        FillInvoice(invoice, number, customer.Id, date, customer.TermsDays, reference, docLines, vatRatePercent, net, vat, journal);
        SalesInvoices.Add(invoice);
        return invoice;
    }

    /// <summary>Records money received from a customer: DR bank, CR receivables control. Allocates to open invoices.</summary>
    public CustomerReceipt PostCustomerReceipt(
        string? customerId,
        DateOnly date,
        decimal amount,
        string? bankAccount = SystemAccounts.Bank,
        string? invoiceNumber = null,
        string? reference = null)
    {
        var customer = GetCustomer(customerId);
        var bank = GetBankAccount(bankAccount);
        var open = SalesInvoices.Where(i => i.PartyId == customer.Id).Cast<Invoice>();
        var allocations = Allocate(open, amount, invoiceNumber, "CUSTOMER");
        var number = $"SR{NextSequence("SR"):00000}";

        var journal = Post(JournalSource.SalesLedger, date, $"RECEIPT {number} {customer.Name}", number,
        [
            new(bank.Code, amount, 0, $"{customer.Id} {number}"),
            new(SystemAccounts.Receivables, 0, amount, $"{customer.Id} {number}", customer.Id),
        ], allowControlAccounts: true);

        var receipt = new CustomerReceipt();
        FillSettlement(receipt, number, customer.Id, date, amount, bank.Code, reference, allocations, journal);
        CustomerReceipts.Add(receipt);
        ApplyAllocations(SalesInvoices, allocations);
        return receipt;
    }

    /// <summary>Customer balance according to the sales ledger (invoices less receipts).</summary>
    public decimal CustomerBalance(string customerId) =>
        SalesInvoices.Where(i => i.PartyId == customerId).Sum(i => i.Gross)
        - CustomerReceipts.Where(r => r.PartyId == customerId).Sum(r => r.Amount);

    // ---------------------------------------------------------------- Suppliers / Accounts Payable (purchase ledger)

    public Supplier? FindSupplier(string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : Suppliers.FirstOrDefault(s => string.Equals(s.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));

    public Supplier GetSupplier(string? id) =>
        FindSupplier(id) ?? throw new BookkeepingException($"SUPPLIER '{id?.Trim()}' NOT FOUND");

    public Supplier AddSupplier(string? name, string? contact = null, int termsDays = 30)
    {
        var supplier = new Supplier
        {
            Name = Required(name, "SUPPLIER NAME"),
            Contact = string.IsNullOrWhiteSpace(contact) ? null : Clean(contact, MaxTextLength),
            TermsDays = CheckTerms(termsDays),
        };
        supplier.Id = $"S{NextSequence("SUPP"):0000}";
        Suppliers.Add(supplier);
        return supplier;
    }

    /// <summary>Records a supplier's invoice: DR expense/asset (net), DR VAT input (tax), CR payables control (gross).</summary>
    public PurchaseInvoice PostPurchaseInvoice(
        string? supplierId,
        DateOnly date,
        IEnumerable<DocumentLineInput> lines,
        decimal vatRatePercent,
        string? supplierReference = null)
    {
        var supplier = GetSupplier(supplierId);
        var (docLines, net, vat) = PrepareDocument(lines, vatRatePercent,
            a => a.Type is AccountType.Expense or AccountType.Asset && a.Control == ControlType.None && a.Code != SystemAccounts.VatInput,
            "PURCHASE LINES MUST USE AN EXPENSE OR (NON-CONTROL) ASSET ACCOUNT");
        var number = $"PI{NextSequence("PI"):00000}";

        var journalLines = docLines.Select(l => new JournalLineInput(l.AccountCode, l.Net, 0, l.Description)).ToList();
        if (vat != 0)
        {
            journalLines.Add(new JournalLineInput(SystemAccounts.VatInput, vat, 0, $"VAT {vatRatePercent:0.##}% ON {number}"));
        }

        journalLines.Add(new JournalLineInput(SystemAccounts.Payables, 0, net + vat, $"{supplier.Id} {number}", supplier.Id));

        var journal = Post(JournalSource.PurchaseLedger, date, $"PURCH INV {number} {supplier.Name}", number, journalLines, allowControlAccounts: true);
        var invoice = new PurchaseInvoice();
        FillInvoice(invoice, number, supplier.Id, date, supplier.TermsDays, supplierReference, docLines, vatRatePercent, net, vat, journal);
        PurchaseInvoices.Add(invoice);
        return invoice;
    }

    /// <summary>Pays a supplier: DR payables control, CR bank. Allocates to open purchase invoices.</summary>
    public SupplierPayment PostSupplierPayment(
        string? supplierId,
        DateOnly date,
        decimal amount,
        string? bankAccount = SystemAccounts.Bank,
        string? invoiceNumber = null,
        string? reference = null)
    {
        var supplier = GetSupplier(supplierId);
        var bank = GetBankAccount(bankAccount);
        var open = PurchaseInvoices.Where(i => i.PartyId == supplier.Id).Cast<Invoice>();
        var allocations = Allocate(open, amount, invoiceNumber, "SUPPLIER");
        var number = $"PP{NextSequence("PP"):00000}";

        var journal = Post(JournalSource.PurchaseLedger, date, $"PAYMENT {number} {supplier.Name}", number,
        [
            new(SystemAccounts.Payables, amount, 0, $"{supplier.Id} {number}", supplier.Id),
            new(bank.Code, 0, amount, $"{supplier.Id} {number}"),
        ], allowControlAccounts: true);

        var payment = new SupplierPayment();
        FillSettlement(payment, number, supplier.Id, date, amount, bank.Code, reference, allocations, journal);
        SupplierPayments.Add(payment);
        ApplyAllocations(PurchaseInvoices, allocations);
        return payment;
    }

    /// <summary>Supplier balance according to the purchase ledger (invoices less payments).</summary>
    public decimal SupplierBalance(string supplierId) =>
        PurchaseInvoices.Where(i => i.PartyId == supplierId).Sum(i => i.Gross)
        - SupplierPayments.Where(p => p.PartyId == supplierId).Sum(p => p.Amount);

    // ---------------------------------------------------------------- Shared sub-ledger helpers

    private (List<DocumentLine> Lines, decimal Net, decimal Vat) PrepareDocument(
        IEnumerable<DocumentLineInput> input,
        decimal vatRatePercent,
        Func<Account, bool> allowed,
        string notAllowedMessage)
    {
        if (vatRatePercent < 0 || vatRatePercent > 100 || decimal.Round(vatRatePercent, 2) != vatRatePercent)
        {
            throw new BookkeepingException("VAT RATE MUST BE A PERCENTAGE BETWEEN 0 AND 100");
        }

        var lines = new List<DocumentLine>();
        var number = 0;
        foreach (var line in input)
        {
            if (string.IsNullOrWhiteSpace(line.AccountCode) && string.IsNullOrWhiteSpace(line.Description) && line.Net == 0)
            {
                continue;
            }

            number++;
            var label = $"LINE {number}:";
            var account = FindAccount(line.AccountCode)
                ?? throw new BookkeepingException($"{label} ACCOUNT '{line.AccountCode?.Trim()}' NOT FOUND IN CHART OF ACCOUNTS");
            if (!allowed(account))
            {
                throw new BookkeepingException($"{label} {notAllowedMessage}");
            }

            if (line.Net <= 0)
            {
                throw new BookkeepingException($"{label} NET AMOUNT MUST BE GREATER THAN ZERO");
            }

            CheckAmount(line.Net, label);
            lines.Add(new DocumentLine
            {
                AccountCode = account.Code,
                Description = string.IsNullOrWhiteSpace(line.Description) ? account.Name : Clean(line.Description, MaxTextLength),
                Net = line.Net,
            });
        }

        if (lines.Count == 0)
        {
            throw new BookkeepingException("ENTER AT LEAST ONE INVOICE LINE");
        }

        var net = lines.Sum(l => l.Net);
        var vat = decimal.Round(net * vatRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        CheckAmount(net + vat, "INVOICE TOTAL:");
        return (lines, net, vat);
    }

    private static void FillInvoice(
        Invoice invoice,
        string number,
        string partyId,
        DateOnly date,
        int termsDays,
        string? reference,
        List<DocumentLine> lines,
        decimal vatRate,
        decimal net,
        decimal vat,
        JournalEntry journal)
    {
        invoice.Number = number;
        invoice.PartyId = partyId;
        invoice.Date = date;
        invoice.DueDate = date.AddDays(termsDays);
        invoice.Reference = string.IsNullOrWhiteSpace(reference) ? null : Clean(reference, 20);
        invoice.Lines = lines;
        invoice.VatRate = vatRate;
        invoice.Net = net;
        invoice.Vat = vat;
        invoice.Gross = net + vat;
        invoice.JournalNumber = journal.Number;
    }

    private static void FillSettlement(
        Settlement settlement,
        string number,
        string partyId,
        DateOnly date,
        decimal amount,
        string bank,
        string? reference,
        List<Allocation> allocations,
        JournalEntry journal)
    {
        settlement.Number = number;
        settlement.PartyId = partyId;
        settlement.Date = date;
        settlement.Amount = amount;
        settlement.BankAccount = bank;
        settlement.Reference = string.IsNullOrWhiteSpace(reference) ? null : Clean(reference, 20);
        settlement.Allocations = allocations;
        settlement.JournalNumber = journal.Number;
    }

    private Account GetBankAccount(string? code)
    {
        var bank = GetAccount(string.IsNullOrWhiteSpace(code) ? SystemAccounts.Bank : code);
        if (bank.Type != AccountType.Asset || bank.Control != ControlType.None || bank.Code == SystemAccounts.VatInput)
        {
            throw new BookkeepingException($"{bank.Code} IS NOT A BANK OR CASH ACCOUNT - USE AN ASSET SUCH AS 1000 OR 1050");
        }

        return bank;
    }

    /// <summary>Works out which open invoices a payment settles: a named invoice, or oldest first.</summary>
    private static List<Allocation> Allocate(IEnumerable<Invoice> invoices, decimal amount, string? invoiceNumber, string party)
    {
        if (amount <= 0)
        {
            throw new BookkeepingException("AMOUNT MUST BE GREATER THAN ZERO");
        }

        CheckAmount(amount, "AMOUNT:");
        var open = invoices.Where(i => i.Outstanding > 0).OrderBy(i => i.Date).ThenBy(i => i.Number).ToList();

        if (!string.IsNullOrWhiteSpace(invoiceNumber))
        {
            var target = invoices.FirstOrDefault(i => string.Equals(i.Number, invoiceNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new BookkeepingException($"INVOICE '{invoiceNumber.Trim().ToUpperInvariant()}' NOT FOUND FOR THIS {party}");
            if (target.Outstanding <= 0)
            {
                throw new BookkeepingException($"INVOICE {target.Number} IS ALREADY FULLY PAID");
            }

            if (amount > target.Outstanding)
            {
                throw new BookkeepingException($"AMOUNT EXCEEDS {target.Number} OUTSTANDING BALANCE OF {target.Outstanding:N2}");
            }

            return [new Allocation { InvoiceNumber = target.Number, Amount = amount }];
        }

        var balance = open.Sum(i => i.Outstanding);
        if (amount > balance)
        {
            throw new BookkeepingException($"AMOUNT EXCEEDS {party} BALANCE OF {balance:N2}");
        }

        var allocations = new List<Allocation>();
        var remaining = amount;
        foreach (var invoice in open)
        {
            if (remaining == 0)
            {
                break;
            }

            var applied = Math.Min(remaining, invoice.Outstanding);
            allocations.Add(new Allocation { InvoiceNumber = invoice.Number, Amount = applied });
            remaining -= applied;
        }

        return allocations;
    }

    private static void ApplyAllocations<T>(IEnumerable<T> invoices, IEnumerable<Allocation> allocations)
        where T : Invoice
    {
        foreach (var allocation in allocations)
        {
            invoices.First(i => i.Number == allocation.InvoiceNumber).AmountPaid += allocation.Amount;
        }
    }

    private static int CheckTerms(int termsDays) =>
        termsDays is < 0 or > 365 ? throw new BookkeepingException("PAYMENT TERMS MUST BE 0-365 DAYS") : termsDays;
}
