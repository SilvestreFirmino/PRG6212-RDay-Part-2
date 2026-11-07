-- Run once in SSMS against the existing Part 2 database. No records are deleted.
USE RaceDayDb;
GO
IF COL_LENGTH('dbo.Events', 'BannerUrl') IS NULL
    ALTER TABLE dbo.Events ADD BannerUrl NVARCHAR(2048) NULL;
GO
