# Accounted For

**Accounted For** is a sandbox for learning double-entry bookkeeping. You learn by using
**FIDOS**, a pretend mainframe ERP system for the fictitious company **NorthSea**. It runs in
the browser and looks like a classic IBM 3270 green-screen terminal. You post real
double-entry transactions, watch them flow through the ledgers, and can work through
optional training levels.

It is a teaching tool, not a production accounting system. Do not enter real or personal
financial data.

## Features

| Area | What you can do |
| --- | --- |
| **General Ledger** | Chart of accounts, creating new ledger accounts, manual journals (debits must equal credits), journal list/day book, reversing journals, T-account inquiry, year-end close to retained earnings |
| **Accounts Receivable** (sales ledger) | Customers, sales invoices (with VAT), customer receipts, customer statements, invoice list, aged debtors |
| **Accounts Payable** (purchase ledger) | Suppliers, purchase invoices (with VAT), supplier payments, statements, aged creditors |
| **Reports** | Trial balance, profit & loss, balance sheet, sub-ledger to control account reconciliation |
| **Accounted For training** | 13 optional levels with briefings, objectives and hints, plus a score and rank, and a quick-fire "Debit or Credit?" drill |
| **Help** | Built-in tutorial on double entry, every screen, and how sub-ledgers feed the GL |

Each posting shows how a business event becomes a journal. For example, a sales invoice
posts `DR 1100 Trade Receivables / CR 4000 Sales / CR 2200 VAT Output`. The invoice is also
recorded against the customer in the sales ledger. The control accounts (1100, 2000) can
only be posted through AR/AP, so the sub-ledgers always reconcile to the general ledger.

### Training levels

1. First Logon: the chart of accounts
2. DEAD CLIC: debits and credits
3. Capital Injection: your first journal
4. A Ledger of Your Own: creating ledger accounts
5. Making a Sale: the sales ledger (accounts receivable)
6. Getting Paid: customer receipts
7. Buying on Credit: the purchase ledger (accounts payable)
8. Settling Up: supplier payments
9. Prove It: trial balance and control accounts
10. Oops!: correcting errors
11. Adjustments: accruals and depreciation
12. The Story in Numbers: financial statements
13. Year End: closing the books

Levels are optional. Every screen can be used freely at any time.

### Learning workspace

Pixel, the terminal mascot, shows a next step on each screen, from signing on to
opening your first lesson by choosing **5 Tutorial**, then **1 Training Levels**. Open **Lesson progress and
navigation** whenever you want navigation help or the full lesson checklist.
Choose an unlocked lesson to see its objectives and hints as you work;
the guide links to relevant screens and back to the briefing. It retains the selected
lesson when it is completed, with a link to the next current lesson. Use the main menu
or training links in the guide, or F12 in the terminal, whenever you need to get back.
The selected lesson is remembered in this browser for the current sandbox.

## Using the terminal

- **Command line** (`COMMAND ===>`): type a menu option number, a transaction code, or a
  path such as `=1.3`.
- **PF keys**: F1 help, F3 exit, F12 main menu, and other keys shown on each screen. You
  can press the real function key or click the label.
- **Enter** processes the screen.

| Code | Screen | Code | Screen |
| --- | --- | --- | --- |
| `COA` | Chart of accounts | `SINV` | Sales invoice |
| `NEWACCT` | Create ledger account | `RCPT` | Customer receipt |
| `JE` | Journal entry | `PINV` | Purchase invoice |
| `JNLS` | Journal list / day book | `PAY` | Supplier payment |
| `TACCT` | T-account inquiry | `CUST` / `SUPP` | Customer / supplier list |
| `YEC` | Year-end close | `AGEDAR` / `AGEDAP` | Aged debtors / creditors |
| `TB` | Trial balance | `LEVELS` | Training levels |
| `PL` / `BS` | Profit & loss / balance sheet | `DRILL` | Debit or credit drill |
| `RECON` | Control account reconciliation | `HELP` | Accounting fundamentals |

## Running it

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/AccountedFor.Web
```

Then open the URL printed in the console, for example http://localhost:5242.

### Docker

```bash
docker build -t accountedfor .
docker run -p 8080:8080 -v fidos-data:/app/App_Data accountedfor
```

Then open http://localhost:8080.

### Tests

```bash
dotnet test
```

## Sandboxes and sharing

Many people can use one running instance. Each browser gets its own private set of books
(a *sandbox*), saved as a JSON file under `App_Data/sandboxes`.

- The sandbox is linked to your browser by a cookie. Sign on again in the same browser
  to reopen your books. To continue on another device, copy the sandbox ID from
  **SANDBOX UTILITIES** (option 9), sign on there, then open the saved ID in **SANDBOX UTILITIES**.
- Use **START NEW SANDBOX** in **SANDBOX UTILITIES** for fresh books without erasing the
  old ones. Keep the old ID if you want to switch back later.
- Type `RESET` on the sandbox utilities screen to start again with fresh books.
- The only protection is the unguessable sandbox ID; there are no user accounts or
  passwords. That is fine for a training sandbox, but do not use it for anything real.

## Project layout

```
src/Fidos.Core          Domain: accounts, journals, AR/AP sub-ledgers, reports, year end, training levels
src/AccountedFor.Web    ASP.NET Core Razor Pages app: the 3270-style terminal UI
tests/Fidos.Core.Tests  xUnit tests for the bookkeeping rules and levels
```
