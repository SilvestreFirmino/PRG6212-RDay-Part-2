-- RaceDay database script
-- Run this file in SQL Server Management Studio on a clean SQL Server instance.
-- The database structure matches the RaceDay ERD and the Part 2 API models.

CREATE DATABASE RaceDayDb;
GO

USE RaceDayDb;
GO

-- Stores both Organiser and Participant accounts.
CREATE TABLE Users
(
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    Email        NVARCHAR(254) NOT NULL,
    PasswordHash NVARCHAR(512) NOT NULL,
    FirstName    NVARCHAR(80) NOT NULL,
    LastName     NVARCHAR(80) NOT NULL,
    PhoneNumber  NVARCHAR(20) NULL,
    Role         NVARCHAR(20) NOT NULL,
    IsActive     BIT NOT NULL DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2 NULL,

    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Organiser', N'Participant'))
);
GO

-- Stores extra information for a Participant only.
CREATE TABLE ParticipantProfiles
(
    ParticipantProfileId INT IDENTITY(1,1) PRIMARY KEY,
    UserId                INT NOT NULL,
    DateOfBirth           DATE NOT NULL,
    Gender                NVARCHAR(20) NOT NULL,
    EmergencyContactName  NVARCHAR(160) NOT NULL,
    EmergencyContactPhone NVARCHAR(20) NOT NULL,
    MedicalNotes          NVARCHAR(500) NULL,
    ClubName              NVARCHAR(120) NULL,

    CONSTRAINT UQ_ParticipantProfiles_UserId UNIQUE (UserId),
    CONSTRAINT FK_ParticipantProfiles_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- Stores events created by Organisers.
CREATE TABLE Events
(
    EventId                INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId            INT NOT NULL,
    Name                   NVARCHAR(160) NOT NULL,
    Description            NVARCHAR(2000) NOT NULL,
    EventType              NVARCHAR(20) NOT NULL,
    StartDateTime          DATETIME2 NOT NULL,
    EndDateTime            DATETIME2 NOT NULL,
    TimeZoneId             NVARCHAR(64) NOT NULL,
    VenueName              NVARCHAR(160) NOT NULL,
    AddressLine1           NVARCHAR(160) NOT NULL,
    City                   NVARCHAR(100) NOT NULL,
    Province               NVARCHAR(100) NOT NULL,
    PostalCode             NVARCHAR(10) NULL,
    RegistrationOpenUtc    DATETIME2 NOT NULL,
    RegistrationCloseUtc   DATETIME2 NOT NULL,
    Status                 NVARCHAR(20) NOT NULL DEFAULT N'Draft',
    CreatedAtUtc           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc           DATETIME2 NULL,

    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId)
        REFERENCES Users(UserId)
);
GO

-- Stores distance or age categories for each event.
CREATE TABLE Categories
(
    CategoryId        INT IDENTITY(1,1) PRIMARY KEY,
    EventId           INT NOT NULL,
    Name              NVARCHAR(100) NOT NULL,
    Description       NVARCHAR(500) NULL,
    DistanceKm        DECIMAL(7,2) NOT NULL,
    EntryFee          DECIMAL(10,2) NOT NULL,
    Capacity          INT NOT NULL,
    MinimumAge        INT NULL,
    MaximumAge        INT NULL,
    CategoryStartTime TIME NULL,
    IsActive          BIT NOT NULL DEFAULT 1,

    CONSTRAINT UQ_Categories_Event_Name UNIQUE (EventId, Name),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES Events(EventId) ON DELETE CASCADE
);
GO

-- Stores a Participant's selected event and category.
CREATE TABLE EventEnrollments
(
    EnrollmentId       INT IDENTITY(1,1) PRIMARY KEY,
    EventId            INT NOT NULL,
    CategoryId         INT NOT NULL,
    ParticipantId      INT NOT NULL,
    BibNumber          NVARCHAR(20) NULL,
    Status             NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    PaymentStatus      NVARCHAR(20) NOT NULL DEFAULT N'Unpaid',
    FeePaid            DECIMAL(10,2) NOT NULL,
    EmergencyConsent   BIT NOT NULL,
    EnrolledAtUtc      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc       DATETIME2 NULL,

    CONSTRAINT UQ_EventEnrollments_Event_Participant UNIQUE (EventId, ParticipantId),
    CONSTRAINT FK_EventEnrollments_Events FOREIGN KEY (EventId)
        REFERENCES Events(EventId),
    CONSTRAINT FK_EventEnrollments_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),
    CONSTRAINT FK_EventEnrollments_Users FOREIGN KEY (ParticipantId)
        REFERENCES Users(UserId)
);
GO

-- A bib number can be used only once within an event.
CREATE UNIQUE INDEX UX_EventEnrollments_Event_BibNumber
ON EventEnrollments(EventId, BibNumber)
WHERE BibNumber IS NOT NULL;
GO

-- Stores the final result for an event enrolment.
CREATE TABLE Results
(
    ResultId              INT IDENTITY(1,1) PRIMARY KEY,
    EnrollmentId          INT NOT NULL,
    RecordedByOrganiserId INT NOT NULL,
    ResultStatus          NVARCHAR(10) NOT NULL,
    DurationMilliseconds  BIGINT NULL,
    OverallPosition       INT NULL,
    CategoryPosition      INT NULL,
    Notes                 NVARCHAR(500) NULL,
    RecordedAtUtc         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc          DATETIME2 NULL,

    CONSTRAINT UQ_Results_Enrollment UNIQUE (EnrollmentId),
    CONSTRAINT FK_Results_EventEnrollments FOREIGN KEY (EnrollmentId)
        REFERENCES EventEnrollments(EnrollmentId) ON DELETE CASCADE,
    CONSTRAINT FK_Results_Users FOREIGN KEY (RecordedByOrganiserId)
        REFERENCES Users(UserId)
);
GO

-- Sample users. The password for each sample account is Password123!.
-- These hashes are for local demonstration data only.
INSERT INTO Users (Email, PasswordHash, FirstName, LastName, PhoneNumber, Role)
VALUES
    (N'lerato@jozievents.co.za', N'AQIDBAUGBwgJCgsMDQ4PEA==.asTsfMfYM0AG5BVkQcjt6dH2Gw8L84DpMiBV83B0m3Q=', N'Lerato', N'Mokoena', N'0712345678', N'Organiser'),
    (N'andile@capeteamevents.co.za', N'ERITFBUWFxgZGhscHR4fIA==.zzk/ucN3vUrf+eCdVck16kZnDU//Ery+15C5H3qfUdM=', N'Andile', N'Jacobs', N'0723456789', N'Organiser'),
    (N'nomsa@example.co.za', N'ISIjJCUmJygpKissLS4vMA==.ClivtdQyxxEJfD5MjP6Ky15Qlcw+xUpYCnOH86HXVFw=', N'Nomsa', N'Mthembu', N'0734567890', N'Participant'),
    (N'thabo@example.co.za', N'MTIzNDU2Nzg5Ojs8PT4/QA==.kz2+KjQWZC1AMSGHXaIK7bZms+k7QEPNilh6eNAziLo=', N'Thabo', N'Nkosi', N'0745678901', N'Participant');
GO

-- Participant profiles for the two Participants.
INSERT INTO ParticipantProfiles
    (UserId, DateOfBirth, Gender, EmergencyContactName, EmergencyContactPhone, MedicalNotes, ClubName)
SELECT UserId, '1992-04-18', N'Female', N'Sipho Mthembu', N'+27 73 555 1313', NULL, N'Jozi Road Runners'
FROM Users WHERE Email = N'nomsa@example.co.za';

INSERT INTO ParticipantProfiles
    (UserId, DateOfBirth, Gender, EmergencyContactName, EmergencyContactPhone, MedicalNotes, ClubName)
SELECT UserId, '1988-10-05', N'Male', N'Tumi Nkosi', N'+27 82 444 2299', N'Carries an inhaler.', N'Durban Striders'
FROM Users WHERE Email = N'thabo@example.co.za';
GO

-- Three events: one running, one cycling, and one walking event.
INSERT INTO Events
    (OrganiserId, Name, Description, EventType, StartDateTime, EndDateTime, TimeZoneId, VenueName, AddressLine1, City, Province, PostalCode, RegistrationOpenUtc, RegistrationCloseUtc, Status)
SELECT UserId, N'Soweto Heritage Run 2026', N'A completed community road race through Soweto.', N'Running',
       '2026-09-24T06:00:00', '2026-09-24T12:00:00', N'South Africa Standard Time', N'Orlando Stadium', N'1 Mooki Street', N'Soweto', N'Gauteng', N'1804',
       '2026-07-01T06:00:00', '2026-09-20T23:59:00', N'Completed'
FROM Users WHERE Email = N'lerato@jozievents.co.za';

INSERT INTO Events
    (OrganiserId, Name, Description, EventType, StartDateTime, EndDateTime, TimeZoneId, VenueName, AddressLine1, City, Province, PostalCode, RegistrationOpenUtc, RegistrationCloseUtc, Status)
SELECT UserId, N'Cape Town Cycle Challenge 2026', N'A scenic cycling event along the Cape Peninsula.', N'Cycling',
       '2026-11-15T06:00:00', '2026-11-15T15:00:00', N'South Africa Standard Time', N'Green Point Precinct', N'Fritz Sonnenberg Road', N'Cape Town', N'Western Cape', N'8001',
       '2026-08-01T06:00:00', '2026-11-08T23:59:00', N'Published'
FROM Users WHERE Email = N'andile@capeteamevents.co.za';

INSERT INTO Events
    (OrganiserId, Name, Description, EventType, StartDateTime, EndDateTime, TimeZoneId, VenueName, AddressLine1, City, Province, PostalCode, RegistrationOpenUtc, RegistrationCloseUtc, Status)
SELECT UserId, N'Durban Beach Walk 2026', N'A relaxed charity walk along Durban beachfront.', N'Walking',
       '2026-12-06T07:00:00', '2026-12-06T11:00:00', N'South Africa Standard Time', N'North Beach', N'Marine Parade', N'Durban', N'KwaZulu-Natal', N'4001',
       '2026-09-01T06:00:00', '2026-11-29T23:59:00', N'Published'
FROM Users WHERE Email = N'lerato@jozievents.co.za';
GO

-- Three categories for each event.
INSERT INTO Categories (EventId, Name, Description, DistanceKm, EntryFee, Capacity, MinimumAge, MaximumAge, CategoryStartTime)
SELECT EventId, N'5 km Fun Run', N'Family-friendly 5 km route.', 5.00, 80.00, 500, 10, NULL, '06:30'
FROM Events WHERE Name = N'Soweto Heritage Run 2026'
UNION ALL
SELECT EventId, N'10 km Road Race', N'Competitive 10 km road race.', 10.00, 150.00, 800, 14, NULL, '06:15'
FROM Events WHERE Name = N'Soweto Heritage Run 2026'
UNION ALL
SELECT EventId, N'21 km Half Marathon', N'Half marathon for experienced runners.', 21.10, 250.00, 600, 16, NULL, '06:00'
FROM Events WHERE Name = N'Soweto Heritage Run 2026';

INSERT INTO Categories (EventId, Name, Description, DistanceKm, EntryFee, Capacity, MinimumAge, MaximumAge, CategoryStartTime)
SELECT EventId, N'20 km Social Ride', N'Entry-level social cycling route.', 20.00, 120.00, 600, 12, NULL, '08:00'
FROM Events WHERE Name = N'Cape Town Cycle Challenge 2026'
UNION ALL
SELECT EventId, N'50 km Challenge', N'Mid-distance cycling route.', 50.00, 220.00, 900, 14, NULL, '07:00'
FROM Events WHERE Name = N'Cape Town Cycle Challenge 2026'
UNION ALL
SELECT EventId, N'100 km Endurance Ride', N'Long-distance cycling route.', 100.00, 350.00, 500, 16, NULL, '06:00'
FROM Events WHERE Name = N'Cape Town Cycle Challenge 2026';

INSERT INTO Categories (EventId, Name, Description, DistanceKm, EntryFee, Capacity, MinimumAge, MaximumAge, CategoryStartTime)
SELECT EventId, N'3 km Family Walk', N'Easy family walking route.', 3.00, 40.00, 600, 5, NULL, '08:30'
FROM Events WHERE Name = N'Durban Beach Walk 2026'
UNION ALL
SELECT EventId, N'5 km Charity Walk', N'Five kilometre beachfront walk.', 5.00, 60.00, 800, 8, NULL, '08:15'
FROM Events WHERE Name = N'Durban Beach Walk 2026'
UNION ALL
SELECT EventId, N'10 km Power Walk', N'Longer route for regular walkers.', 10.00, 100.00, 400, 12, NULL, '08:00'
FROM Events WHERE Name = N'Durban Beach Walk 2026';
GO

-- Sample enrolments for the completed and upcoming events.
INSERT INTO EventEnrollments
    (EventId, CategoryId, ParticipantId, BibNumber, Status, PaymentStatus, FeePaid, EmergencyConsent)
SELECT e.EventId, c.CategoryId, u.UserId, N'R102', N'Confirmed', N'Paid', c.EntryFee, 1
FROM Events e
JOIN Categories c ON c.EventId = e.EventId AND c.Name = N'10 km Road Race'
JOIN Users u ON u.Email = N'nomsa@example.co.za'
WHERE e.Name = N'Soweto Heritage Run 2026';

INSERT INTO EventEnrollments
    (EventId, CategoryId, ParticipantId, BibNumber, Status, PaymentStatus, FeePaid, EmergencyConsent)
SELECT e.EventId, c.CategoryId, u.UserId, N'R215', N'Confirmed', N'Paid', c.EntryFee, 1
FROM Events e
JOIN Categories c ON c.EventId = e.EventId AND c.Name = N'21 km Half Marathon'
JOIN Users u ON u.Email = N'thabo@example.co.za'
WHERE e.Name = N'Soweto Heritage Run 2026';

INSERT INTO EventEnrollments
    (EventId, CategoryId, ParticipantId, BibNumber, Status, PaymentStatus, FeePaid, EmergencyConsent)
SELECT e.EventId, c.CategoryId, u.UserId, NULL, N'Pending', N'Unpaid', c.EntryFee, 1
FROM Events e
JOIN Categories c ON c.EventId = e.EventId AND c.Name = N'50 km Challenge'
JOIN Users u ON u.Email = N'nomsa@example.co.za'
WHERE e.Name = N'Cape Town Cycle Challenge 2026';
GO

-- Results for the completed Soweto event.
INSERT INTO Results
    (EnrollmentId, RecordedByOrganiserId, ResultStatus, DurationMilliseconds, OverallPosition, CategoryPosition, Notes)
SELECT en.EnrollmentId, org.UserId, N'Finished', 2785000, 18, 4, N'Completed successfully.'
FROM EventEnrollments en
JOIN Events e ON e.EventId = en.EventId
JOIN Users org ON org.Email = N'lerato@jozievents.co.za'
WHERE en.BibNumber = N'R102' AND e.Name = N'Soweto Heritage Run 2026';

INSERT INTO Results
    (EnrollmentId, RecordedByOrganiserId, ResultStatus, DurationMilliseconds, OverallPosition, CategoryPosition, Notes)
SELECT en.EnrollmentId, org.UserId, N'Finished', 6112000, 42, 9, N'Completed successfully.'
FROM EventEnrollments en
JOIN Events e ON e.EventId = en.EventId
JOIN Users org ON org.Email = N'lerato@jozievents.co.za'
WHERE en.BibNumber = N'R215' AND e.Name = N'Soweto Heritage Run 2026';
GO

-- Verification queries: run these at the end to show the seeded data.
SELECT * FROM Users;
SELECT * FROM ParticipantProfiles;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM EventEnrollments;
SELECT * FROM Results;
GO
