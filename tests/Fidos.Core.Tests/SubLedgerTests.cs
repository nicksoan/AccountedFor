using Fidos.Core;

namespace Fidos.Core.Tests;

public class SubLedgerTests
{
    private static readonly DateOnly Day = new(2026, 3, 1);

    [Fact]
    public void SalesInvoice_PostsReceivableRevenueAndVat()
    {
        var company = Company.CreateNew();
        var customer = company.AddCustomer("Acme Ltd", termsDays: 14);

        var invoice = company.PostSalesInvoice(customer.Id, Day, [new("4000", "Widgets", 100), new("4100", "Install", 50)], 20);

        Assert.Equal("C0001", customer.Id);
        Assert.Equal("SI00001", invoice.Number);
        Assert.Equal(150, invoice.Net);
        Assert.Equal(30, invoice.Vat);
        Assert.Equal(180, invoice.Gross);
        Assert.Equal(Day.AddDays(14), invoice.DueDate);
        Assert.Equal(180, company.Balance(SystemAccounts.Receivables));
        Assert.Equal(-100, company.Balance("4000"));
        Assert.Equal(-30, company.Balance(SystemAccounts.VatOutput));
        Assert.Equal(180, company.CustomerBalance(customer.Id));
        var journal = company.GetJournal(invoice.JournalNumber);
        Assert.Equal(JournalSource.SalesLedger, journal.Source);
        Assert.Equal(customer.Id, journal.Lines.Single(l => l.AccountCode == SystemAccounts.Receivables).PartyId);
    }

    [Fact]
    public void SalesInvoice_RequiresRevenueAccounts()
    {
        var company = Company.CreateNew();
        var customer = company.AddCustomer("Acme");

        Assert.Throws<BookkeepingException>(() => company.PostSalesInvoice(customer.Id, Day, [new("6000", "x", 10)], 0));
        Assert.Throws<BookkeepingException>(() => company.PostSalesInvoice(customer.Id, Day, [], 0));
        Assert.Throws<BookkeepingException>(() => company.PostSalesInvoice("C9999", Day, [new("4000", "x", 10)], 0));
        Assert.Throws<BookkeepingException>(() => company.PostSalesInvoice(customer.Id, Day, [new("4000", "x", 10)], 101));
    }

    [Fact]
    public void CustomerReceipt_AllocatesOldestFirst()
    {
        var company = Company.CreateNew();
        var customer = company.AddCustomer("Acme");
        var first = company.PostSalesInvoice(customer.Id, Day, [new("4000", "a", 100)], 0);
        var second = company.PostSalesInvoice(customer.Id, Day.AddDays(1), [new("4000", "b", 100)], 0);

        var receipt = company.PostCustomerReceipt(customer.Id, Day.AddDays(5), 150);

        Assert.True(first.IsPaid);
        Assert.Equal(50, second.Outstanding);
        Assert.Equal(2, receipt.Allocations.Count);
        Assert.Equal(50, company.CustomerBalance(customer.Id));
        Assert.Equal(50, company.Balance(SystemAccounts.Receivables));
        Assert.Equal(150, company.Balance(SystemAccounts.Bank));
    }

    [Fact]
    public void CustomerReceipt_CannotExceedBalanceOrInvoice()
    {
        var company = Company.CreateNew();
        var customer = company.AddCustomer("Acme");
        var invoice = company.PostSalesInvoice(customer.Id, Day, [new("4000", "a", 100)], 0);

        Assert.Throws<BookkeepingException>(() => company.PostCustomerReceipt(customer.Id, Day, 101));
        Assert.Throws<BookkeepingException>(() => company.PostCustomerReceipt(customer.Id, Day, 101, invoiceNumber: invoice.Number));
        Assert.Throws<BookkeepingException>(() => company.PostCustomerReceipt(customer.Id, Day, 10, bankAccount: "4000"));
        Assert.Throws<BookkeepingException>(() => company.PostCustomerReceipt(customer.Id, Day, 0));
        Assert.Equal(100, invoice.Outstanding);
        Assert.Single(company.Journals);
    }

    [Fact]
    public void PurchaseInvoiceAndPayment_FlowThroughPayables()
    {
        var company = Company.CreateNew();
        var supplier = company.AddSupplier("Paper Co");

        var invoice = company.PostPurchaseInvoice(supplier.Id, Day, [new("6300", "Paper", 200)], 20, "INV-77");
        var payment = company.PostSupplierPayment(supplier.Id, Day.AddDays(10), 240, invoiceNumber: invoice.Number);

        Assert.Equal("S0001", supplier.Id);
        Assert.Equal("INV-77", invoice.Reference);
        Assert.True(invoice.IsPaid);
        Assert.Equal("PP00001", payment.Number);
        Assert.Equal(200, company.Balance("6300"));
        Assert.Equal(40, company.Balance(SystemAccounts.VatInput));
        Assert.Equal(0, company.Balance(SystemAccounts.Payables));
        Assert.Equal(-240, company.Balance(SystemAccounts.Bank));
        Assert.Equal(0, company.SupplierBalance(supplier.Id));
    }

    [Fact]
    public void SubLedgerJournals_CannotBeReversedInGl()
    {
        var company = Company.CreateNew();
        var customer = company.AddCustomer("Acme");
        var invoice = company.PostSalesInvoice(customer.Id, Day, [new("4000", "a", 100)], 0);

        Assert.Throws<BookkeepingException>(() => company.ReverseJournal(invoice.JournalNumber, Day));
    }

    [Fact]
    public void Reconciliation_ControlAccountsAgreeWithSubLedgers()
    {
        var company = Company.CreateNew();
        var customer = company.AddCustomer("Acme");
        var supplier = company.AddSupplier("Paper Co");
        company.PostSalesInvoice(customer.Id, Day, [new("4000", "a", 100)], 20);
        company.PostPurchaseInvoice(supplier.Id, Day, [new("1200", "stock", 60)], 20);
        company.PostCustomerReceipt(customer.Id, Day, 50);

        var rows = company.Reconcile();

        Assert.All(rows, r => Assert.True(r.Agrees));
        Assert.Equal(70, rows[0].ControlBalance);
        Assert.Equal(72, rows[1].ControlBalance);
    }

    [Fact]
    public void AgedReceivables_BucketsByDaysOverdue()
    {
        var company = Company.CreateNew();
        var customer = company.AddCustomer("Acme", termsDays: 30);
        company.PostSalesInvoice(customer.Id, new DateOnly(2026, 1, 1), [new("4000", "old", 100)], 0);
        company.PostSalesInvoice(customer.Id, new DateOnly(2026, 3, 1), [new("4000", "new", 40)], 0);

        var row = Assert.Single(company.AgedReceivables(new DateOnly(2026, 3, 15)));

        Assert.Equal(40, row.Current);
        Assert.Equal(100, row.Days31To60);
        Assert.Equal(140, row.Total);
    }

    [Fact]
    public void CustomerStatement_RunsBalance()
    {
        var company = Company.CreateNew();
        var customer = company.AddCustomer("Acme");
        company.PostSalesInvoice(customer.Id, Day, [new("4000", "a", 100)], 0);
        company.PostCustomerReceipt(customer.Id, Day.AddDays(1), 30);

        var rows = company.CustomerStatement(customer.Id);

        Assert.Equal([100m, 70m], rows.Select(r => r.Balance));
        Assert.Equal(30, rows[1].Credit);
    }
}
