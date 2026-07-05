/*
    Seed the Cities table with test data to exercise the View Cities
    pagination, search, and state filter. Safe to re-run: rows whose
    (Name, State) already exist are skipped.

    Cleanup:  DELETE FROM Cities WHERE Name LIKE 'Test City %';
*/

SET NOCOUNT ON;

DECLARE @Count int = 200;   -- number of test cities to insert

-- Attribute the rows to an existing registered user so "Created by"
-- resolves to an email; falls back to a literal if none exist.
DECLARE @CreatedBy nvarchar(20) =
    (SELECT TOP (1) RegisteredUserId FROM RegisteredUsers ORDER BY Id);
IF @CreatedBy IS NULL SET @CreatedBy = N'USR-SEED01';

;WITH Numbers AS
(
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1 FROM Numbers WHERE n < @Count
),
States AS
(
    SELECT Abbr, ROW_NUMBER() OVER (ORDER BY Abbr) AS rn
    FROM (VALUES
        ('CA'),('TX'),('NY'),('FL'),('IL'),
        ('PA'),('OH'),('GA'),('NC'),('MI'),
        ('WA'),('CO'),('AZ'),('MA'),('TN')
    ) v(Abbr)
),
StateCount AS (SELECT COUNT(*) AS c FROM States)
INSERT INTO Cities (CityId, Name, State, CreatedAt, CreatedByUserId)
SELECT
    CONCAT('CTY-', RIGHT('000000' + CAST(num.n AS varchar(6)), 6)),
    CONCAT('Test City ', num.n),
    s.Abbr,
    TODATETIMEOFFSET(DATEADD(MINUTE, -num.n, SYSUTCDATETIME()), 0),
    @CreatedBy
FROM Numbers num
CROSS JOIN StateCount sc
JOIN States s ON s.rn = ((num.n - 1) % sc.c) + 1
WHERE NOT EXISTS
(
    SELECT 1 FROM Cities c
    WHERE c.Name = CONCAT('Test City ', num.n) AND c.State = s.Abbr
)
OPTION (MAXRECURSION 0);

PRINT CONCAT(@@ROWCOUNT, ' test cities inserted.');
