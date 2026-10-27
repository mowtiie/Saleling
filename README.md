# Saleling: Sales & Inventory Management System

**Saleling** is a point-of-sale and inventory system for a clothing store. It is a Windows Forms desktop app built with C# and .NET 8, and it keeps its data in Microsoft SQL Server.

---

## Features

| Area | What it does |
| :--- | :--- |
| **Login and roles** | Users log in as either an Admin or a Cashier, and each role only sees what it needs. |
| **Dashboards** | Admins get an overview of the store. Cashiers get a simple screen for making sales. |
| **Products** | Add, edit and remove products. Search by name, category, SKU or supplier. Comes with 100 sample products. |
| **Inventory** | Track how many items are in stock. Low-stock alerts warn staff when apparel is running out. |
| **Sales** | Cashiers ring up sales, and every sale is saved to the transaction history. |
| **Reports** | Inventory and sales reports built from the saved data. |

---

## Screenshots

### Login

![Login](docs/screenshots/login.png)

### Admin

| Dashboard | Product Maintenance |
| :---: | :---: |
| ![Admin Dashboard](docs/screenshots/admin-dashboard.png) | ![Product Maintenance](docs/screenshots/product-maintenance.png) |

| Inventory Management | Reports |
| :---: | :---: |
| ![Inventory Management](docs/screenshots/inventory-management.png) | ![Reports](docs/screenshots/reports.png) |

### Cashier

| Dashboard | Point of Sale |
| :---: | :---: |
| ![Cashier Dashboard](docs/screenshots/cashier-dashboard.png) | ![Point of Sale](docs/screenshots/point-of-sale.png) |

---

## How It Is Built

The code is split into four layers, so each part has one job:

* **UI:** The Windows Forms screens the user sees.
* **Model:** Simple classes that describe the data, such as products, users and sales.
* **Controller:** The business rules. It takes requests from the UI and asks the Repository for data.
* **Repository:** Reads from and writes to the database.

A few things worth knowing:

* **Passwords are hashed** with BCrypt, so they are never stored as plain text.
* **Roles are enforced in the code, not just the screens.** Only Admins can manage products, adjust inventory and see store-wide sales. Only Cashiers can make sales, and they can only see their own. Stock can never go below zero.
* **Sales are all-or-nothing.** If updating the stock fails, the sale is cancelled too.
* **Reports come from stored procedures** in the database.
* **Settings live in a config file.** The database connection string is in `Saleling.UI/App.config`.
* **Errors are logged** to `Saleling-Debug.logs` in the app's folder (`bin\Debug\net8.0-windows`).

---

## Team

| Role | GitHub | Email |
| :--- | :--- | :--- |
| **Leader** | [mowtiie](https://github.com/mowtiie) | contact@mowtiie.cc |
| **Member** | [Tutsy](https://github.com/Tutsy) | markkerwinballelos@gmail.com |
| **Member** | [villalobosjames30](https://github.com/villalobosjames30) | villalobosjamescarl30@gmail.com |
| **Member** | [Adrienne20894](https://github.com/Adrienne20894) | jaloconadrienne@gmail.com |

### Instructor

| Role | GitHub | Email |
| :--- | :--- | :--- |
| **Professor** | [ErvinAbiadSTI](https://github.com/ErvinAbiadSTI) | ervin.abiad@gmail.com |

---

## Tech Stack

* C# (.NET 8, Windows Forms)
* Microsoft SQL Server
* Git and GitHub
* Visual Studio and SQL Server Management Studio (SSMS)

NuGet packages (installed automatically):

* `Microsoft.Data.SqlClient`: connects the app to SQL Server.
* `System.Configuration.ConfigurationManager`: reads the connection string from the config file.
* `BCrypt.Net-Next`: hashes passwords.

---

## Getting Started

### What you need

* **Visual Studio 2022** with the **.NET desktop development** and **Data storage and processing** workloads. The `Saleling.DB` project needs the second one, and `dotnet build` cannot build it.
* **.NET 8 SDK**
* **A SQL Server to hold the database.** Pick one:
  * **LocalDB** is the easiest. It comes with the Data storage and processing workload.
  * **A full SQL Server instance** (Express or Developer edition) also works. See [Using SSMS or a full SQL Server](#using-ssms-or-a-full-sql-server).

### Steps

1.  **Download the project:**
    ```bash
    git clone https://github.com/mowtiie/Saleling
    ```
2.  **Open the solution:** Open `Saleling.sln` in Visual Studio.
3.  **Create the database:**
    * Right-click the `Saleling.DB` project and choose **Publish**.
    * Set the target server to `(localdb)\MSSQLLocalDB` (or your own SQL Server) and the database name to **`Saleling.DB`**. The app expects this exact name.
    * Click **Publish**. This creates the tables and adds the starting data: users, categories, suppliers, a walk-in customer and 100 sample products.
    * Publishing again is safe. Existing data is not duplicated.
4.  **Check the connection string** in `Saleling.UI/App.config`. By default it is:
    ```
    Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Saleling.DB;Integrated Security=True
    ```
    If you use a different server, change it here. You can also set the `SALELING_CONNECTION_STRING` environment variable instead of editing the file. It takes priority over `App.config`.
5.  **Run the app:** Set `Saleling.UI` as the startup project and press F5.

### Using SSMS or a full SQL Server

SSMS is just a tool for looking at a database. It does not replace LocalDB, but it can connect to it.

**Option A: Keep LocalDB and open it in SSMS**

1. Open SSMS.
2. For **Server name**, enter `(localdb)\MSSQLLocalDB`.
3. For **Authentication**, choose Windows Authentication and click **Connect**.
4. Expand **Databases** to find `Saleling.DB`. It is the same database the app uses.

**Option B: Use a full SQL Server instead of LocalDB**

1. Install SQL Server (Express or Developer) and SSMS. Note the instance name, for example `localhost\SQLEXPRESS`.
2. Connect to that instance in SSMS to make sure it works.
3. In Visual Studio, publish `Saleling.DB` to that instance (step 3 above). Keep the database name `Saleling.DB`.
4. Tell the app where the database is, either by editing `Saleling.UI/App.config` or by setting the environment variable (then restart Visual Studio):
   ```powershell
   setx SALELING_CONNECTION_STRING "Data Source=localhost\SQLEXPRESS;Initial Catalog=Saleling.DB;Integrated Security=True;TrustServerCertificate=True"
   ```
5. Run the app. Open the same instance in SSMS to see the data change as you use it.

> **Tip:** Keep `TrustServerCertificate=True` in the connection string for a local SQL Server. Without it, the app may fail with a certificate error. If you log in with a SQL username instead of Windows, replace `Integrated Security=True` with `User ID=yourname;Password=yourpassword`.

### Running the tests

```bash
dotnet test Saleling.Controller.Tests
```

Most tests do not need a database. The database tests build a temporary copy of the database on LocalDB (`(localdb)\MSSQLLocalDB`) and delete it afterwards, so your real `Saleling.DB` data is never touched. If LocalDB is not installed, those tests are skipped instead of failing. They always use LocalDB, even if `SALELING_CONNECTION_STRING` is set.

The same tests run automatically on GitHub for every push and pull request (see `.github/workflows/ci.yml`).

---

## Default Logins (Demo Only)

> **Warning:** These accounts are created for demonstration and grading only. Their passwords are public in this README. Never use them in a real deployment. Delete them or change their passwords first.

| Role | Username | Password |
| :--- | :--- | :--- |
| **Admin** | `sysadmin` | `footpath1dangeravenge2` |
| **Cashier** | `johndoe` | `buggysnow1fieldapproach2` |
| **Cashier** | `janedoe` | `brown1nosemule2` |
