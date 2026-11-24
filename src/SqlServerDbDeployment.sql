DROP TABLE IF EXISTS dbo.[Task];

CREATE TABLE dbo.[Task] (
    Id       INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Text]   NVARCHAR(255) NOT NULL,
    [Day]    NVARCHAR(32)  NOT NULL,
    Reminder BIT           NOT NULL
);

-- Preserve the original (gapped) Id values for the seed data against the identity column.
SET IDENTITY_INSERT dbo.[Task] ON;

INSERT INTO dbo.[Task] (Id, [Text], [Day], Reminder)
VALUES
    (1, N'Doctor''s Appointment', N'2017-06-01T08:30', 0),
    (2, N'Food Shopping',         N'2017-06-02T12:00', 1),
    (3, N'Homework',              N'2017-06-03T09:30', 0),
    (4, N'Review Forms',          N'2022-06-30T15:30', 1);

SET IDENTITY_INSERT dbo.[Task] OFF;
