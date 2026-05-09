USE Northwind;
GO

IF COL_LENGTH('dbo.Orders', 'Latitude') IS NULL
    ALTER TABLE dbo.Orders ADD Latitude DECIMAL(18,15) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'Longitude') IS NULL
    ALTER TABLE dbo.Orders ADD Longitude DECIMAL(18,15) NULL;
GO
