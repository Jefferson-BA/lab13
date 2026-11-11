USE master;
GO
IF DB_ID(N'VeterinariaDB') IS NULL
    CREATE DATABASE VeterinariaDB;
GO
USE VeterinariaDB;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
        Nombre NVARCHAR(80) NOT NULL,
        Apellido NVARCHAR(80) NOT NULL,
        Dni NVARCHAR(8) NOT NULL,
        Telefono NVARCHAR(20) NOT NULL,
        Email NVARCHAR(120) NOT NULL
    );
END;
IF OBJECT_ID(N'dbo.Mascotas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Mascotas (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Mascotas PRIMARY KEY,
        Nombre NVARCHAR(80) NOT NULL,
        Especie NVARCHAR(50) NOT NULL,
        Raza NVARCHAR(80) NOT NULL,
        Edad INT NOT NULL CONSTRAINT CK_Mascotas_Edad CHECK (Edad >= 0 AND Edad <= 30),
        ClienteId INT NOT NULL,
        CONSTRAINT FK_Mascotas_Clientes FOREIGN KEY (ClienteId)
            REFERENCES dbo.Clientes(Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_Mascotas_ClienteId ON dbo.Mascotas(ClienteId);
END;
-- Repetir el script no borra ni duplica los registros existentes.
IF NOT EXISTS (SELECT 1 FROM dbo.Clientes)
BEGIN
    INSERT INTO dbo.Clientes (Nombre, Apellido, Dni, Telefono, Email) VALUES
        (N'Ana', N'Torres', N'12345678', N'987654321', N'ana@example.com'),
        (N'Luis', N'Ramos', N'23456789', N'976543210', N'luis@example.com'),
        (N'María', N'Flores', N'34567890', N'965432109', N'maria@example.com');
    DECLARE @Ana INT = (SELECT Id FROM dbo.Clientes WHERE Dni = N'12345678');
    DECLARE @Luis INT = (SELECT Id FROM dbo.Clientes WHERE Dni = N'23456789');
    DECLARE @Maria INT = (SELECT Id FROM dbo.Clientes WHERE Dni = N'34567890');
    INSERT INTO dbo.Mascotas (Nombre, Especie, Raza, Edad, ClienteId) VALUES
        (N'Luna', N'Perro', N'Labrador', 4, @Ana),
        (N'Milo', N'Gato', N'Mestizo', 2, @Ana),
        (N'Rocky', N'Perro', N'Beagle', 6, @Luis),
        (N'Nina', N'Gato', N'Siamés', 3, @Luis),
        (N'Coco', N'Ave', N'Canario', 1, @Maria);
END;
COMMIT;
GO
