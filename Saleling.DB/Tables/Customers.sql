CREATE TABLE Customers (
    CustomerID INT PRIMARY KEY IDENTITY(1,1),
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Phone VARCHAR(15),
    Email VARCHAR(100),
    LoyaltyPoints INT DEFAULT 0
);

-- A plain UNIQUE constraint allows only one NULL, so only one customer could ever lack a phone
-- or email. These indexes enforce uniqueness only for customers who actually have a value.
CREATE UNIQUE NONCLUSTERED INDEX UX_Customers_Phone ON Customers (Phone) WHERE Phone IS NOT NULL;

CREATE UNIQUE NONCLUSTERED INDEX UX_Customers_Email ON Customers (Email) WHERE Email IS NOT NULL;