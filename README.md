# 👚 Saleling: Sales & Inventory Management System

**Saleling** is a comprehensive **Point-of-Sale (POS) and Inventory Management System** specifically designed for a **clothing apparel business**. Developed as a **Windows Forms application**, it utilizes an **event-driven** approach and strictly adheres to the **UI-Model-Controller-Repository (UMCR)** architectural pattern to ensure scalability, maintainability, and a clear separation of concerns.

---

## ✨ Core Features

This system provides all the essential functionalities needed for retail operations, with robust technical and data-integrity features.

### Business Functionalities

| Feature Area | Description | Requirement |
| :--- | :--- | :--- |
| **User & Access** | Secure Login with role-based access | Login, User Role Management |
| **Dashboards** | Dedicated **Admin Dashboard** for high-level oversight and a streamlined **Cashier Dashboard** for sales operations. | Admin Dashboard, Cashier Dashboard |
| **Product Management** | Complete **Product Maintenance (CRUD)** functionality, initialized with **50 product records**. Products can be searched efficiently by **name or category**. | Product Maintenance, Product Search |
| **Inventory Control** | Real-time tracking of the **Number of Inventory Items**. Automated **Stock Level Alerts** notify staff when apparel stock is low. | Inventory Items, Stock Level Alerts |
| **Sales & Transactions** | Core **Cashiering/Sales** module. Every transaction is saved to the **Transaction History** for auditing. | Cashiering/Sales, Transaction History |
| **Reporting** | Generation of detailed **Inventory and Sales Reports** based on collected sales and stock data. | Inventory & Sales Report |

---

## 🛠️ Technical Implementation

### Architecture: UI-Model-Controller-Repository (UMCR)

The project separates concerns across four distinct layers:

* **UI (User Interface):** Built with **Windows Forms** to display data and handle all **event-driven programming** interactions.
* **Model:** Simple C\# objects that define the data structure (e.g., `Apparel`, `User`, `Transaction`).
* **Controller:** Contains the application's **business logic**. It receives input from the UI and coordinates data flow with the Repository.
* **Repository:** Manages all **data access** operations (CRUD) against the database, abstracting the data source from the Controller.

### Code & Security Principles

* **Secure Password Hashing:** User passwords are secured using the **BCrypt algorithm**, providing strong, slow hashing to protect against brute-force attacks.
* **Error Management:** Extensive use of **`try-catch` / `throw exception`** throughout the application to handle and log errors gracefully, especially in data access and critical business logic.
* **Data Structures:** **C\# Collections** (e.g., `List<T>`) are used to efficiently store and manage groups of related objects, like the product catalog or items in a shopping cart.

### Data Integrity & Database Features

* **Transaction Management:** Implementation of **Database Transact / Rollback** logic ensures data integrity during multi-step operations (e.g., if inventory update fails, the sale record rolls back).
* **Performance:** Utilizes **Stored Procedure** for complex or frequently executed queries.
* **Views:** Employs **Database View** to simplify complex reporting queries and enhance data security.

### Configuration & Logging

* **Configuration File:** An external configuration file holds the **database connection string** and other application settings, allowing for easy updates without code changes.
* **Log File:** All application events, errors, and critical transaction details are saved to the file named **`Saleling.logs`**. This file can be found in the application's runtime directory: `\bin\Debug\net8.0-windows`.

---

## 👥 Team / Collaborators

This project was developed by the following students:

| Role | Github | Email |
| :--- | :--- | :--- |
| **Leader** | [mowtiie](https://github.com/mowtiie) | contact@mowtiie.cc |
| **Member** | [Tutsy](https://github.com/Tutsy) | markkerwinballelos@gmail.com |
| **Member** | [villalobosjames30](https://github.com/villalobosjames30) | villalobosjamescarl30@gmail.com |
| **Member** | [Adrienne20894](https://github.com/Adrienne20894) | jaloconadrienne@gmail.com |

---

## 🎓 Instructor / Professor

| Role | Github | Email |
| :--- | :--- | :--- |
| **Professor** | [ErvinAbiadSTI](https://github.com/ErvinAbiadSTI) | ervin.abiad@gmail.com |

---

## 🛠️ Tech Stack

* **Programming Language:** C# (.NET / WinForms or ASP.NET)
* **Database:** Microsoft SQL Server
* **Version Control:** Git & Github
* **IDE:** Microsoft Visual Studio & SQL Server Management Studio

---

## 📦 Required NuGet Packages

The following packages must be installed in your project to handle configuration, database access, and password hashing:

* **`System.Configuration.ConfigurationManager`:** Used to read the database connection string and application settings from the configuration file.
* **`System.Data.SqlClient (4.8.6)`:** The primary provider for connecting to and interacting with the Microsoft SQL Server database.
* **`BCrypt.Net-Next`:** Provides the implementation of the **BCrypt hashing algorithm** for securing user passwords.

---

## 🚀 Setup & Execution

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/mowtiie/Saleling
    ```
2.  **Database Configuration:**
    * Publish your local SQL Server database schema.
    * The application is configured to use the following **connection string** for local development:
      ```
      Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Saleling.DB;Integrated Security=True
      ```
    * Ensure this connection string is correctly configured in your application's configuration file (e.g., `app.config`).
3.  **Run the Project:**
    * Open the solution file (`.sln`) in Visual Studio.
    * Build the solution and run the application (F5).

### Default Login Credentials

*Note: Passwords are BCrypt-hashed in the database. These are the plaintext values for login.*

| Role | Username | Password |
| :--- | :--- | :--- |
| **Admin** | `sysadmin` | `footpath1dangeravenge2` |
| **Cashier** | `johndoe` | `buggysnow1fieldapproach2` |
| **Cashier** | `janedoe` | `brown1nosemule2` |
