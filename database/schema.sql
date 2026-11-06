-- AgriStream user management and device registry.
-- Matches the Entity Framework model used by AgriStream.Web.

CREATE TABLE Users (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL,
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Farmer', 'Agronomist', 'Admin'))
);

CREATE TABLE Devices (
    DeviceId    NVARCHAR(50)  NOT NULL PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    FieldName   NVARCHAR(50)  NOT NULL,
    Status      NVARCHAR(60)  NOT NULL DEFAULT 'Idle',
    LastCommand NVARCHAR(120) NULL,
    UpdatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

INSERT INTO Devices (DeviceId, Name, FieldName) VALUES
    ('VALVE-A1', 'Valve A1', 'Field-A'),
    ('VALVE-B1', 'Valve B1', 'Field-B'),
    ('PUMP-C1',  'Pump C1',  'Field-C');

SELECT * FROM Users;
SELECT * FROM Devices;
