-- Ejecutar después de VeterinariaDB.sql. No elimina registros existentes.
USE VeterinariaDB;
GO
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
        Correo NVARCHAR(120) NOT NULL,
        ClaveHash NVARCHAR(100) NOT NULL,
        Rol NVARCHAR(30) NOT NULL,
        CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN (N'Recepcionista', N'Veterinario'))
    );
    CREATE UNIQUE INDEX UX_Usuarios_Correo ON dbo.Usuarios(Correo);
END;
GO

-- Cuentas exclusivas del laboratorio. Clave de prueba: Veterinaria2026!
-- Hashes obtenidos mediante POST /api/auth/registro; el rol Veterinario se asignó con UPDATE.
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Correo=N'recepcion@veterinaria.test')
    INSERT INTO dbo.Usuarios (Correo, ClaveHash, Rol) VALUES
        (N'recepcion@veterinaria.test', N'$2a$12$qI/Lfsp8iZRY9o.Tfj0gFeSAYwpM0MPZG7eRf6eQGBbwPxsQVG0H2', N'Recepcionista');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Correo=N'veterinario@veterinaria.test')
    INSERT INTO dbo.Usuarios (Correo, ClaveHash, Rol) VALUES
        (N'veterinario@veterinaria.test', N'$2a$12$sFfHSjNq1JsyAa1DvFzjfOPdloq3WMm9xqfnKkTACLYVOGWYDi8wq', N'Veterinario');
GO
