USE RaceDayDb;
GO

IF COL_LENGTH('dbo.Events', 'BannerUrl') IS NULL
BEGIN
    ALTER TABLE dbo.Events
    ADD BannerUrl NVARCHAR(2048) NULL;
END;
GO

SELECT COL_LENGTH('dbo.Events', 'BannerUrl') AS ColumnSize;