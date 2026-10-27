/* ============================================================
   jsERP - change column collation to Chinese_Taiwan_Stroke_CI_AS    2026-10-25 19:00 added
   Safe to run many times.

   Why: JSERP / tempdb default = Chinese_Taiwan_Stroke_CI_AS,
        but old tables (USERS, SYS_ROLES ...) used Chinese_PRC_CI_AS.
        Two different collations cannot be compared (error 468),
        so the SQL had to add COLLATE everywhere.
        After this script all columns use the DB default and COLLATE is not needed.

   Steps (use #TABLE, same idea as zzz_JWERP.SP_ACYCLE1):
     1. Drop the UNIQUE constraint on SYS_PROGRAM.FORM_ID (SQL Server cannot change
        the collation of a column that is in an index / constraint)
     2. #TB1 : every column that is still Chinese_PRC_xxx
     3. Loop #TB1 : ALTER TABLE ... ALTER COLUMN ... COLLATE Chinese_Taiwan_Stroke_CI_AS
     4. Add the UNIQUE constraint back (with a fixed name)
   ============================================================ */
USE JSERP;
GO

/* ---------- Step 1: drop UNIQUE on SYS_PROGRAM.FORM_ID ---------- */
DECLARE @uq sysname, @sql1 nvarchar(500);

SELECT @uq = k.name
FROM sys.key_constraints k
WHERE k.parent_object_id = OBJECT_ID('dbo.SYS_PROGRAM')
  AND k.type = 'UQ';

IF @uq IS NOT NULL
BEGIN
    SET @sql1 = 'ALTER TABLE dbo.SYS_PROGRAM DROP CONSTRAINT [' + @uq + ']';
    EXEC (@sql1);
END
GO

/* ---------- Step 2 + 3: change every Chinese_PRC column ---------- */
DECLARE @n int, @cnt int, @sql nvarchar(1000);

CREATE TABLE #TB1
(
    N_SEQ     INT IDENTITY(1,1),
    TBL       sysname NULL,
    COL       sysname NULL,
    TYPE_NAME sysname NULL,
    LEN_TEXT  VARCHAR(10) NULL,
    NULL_TEXT VARCHAR(10) NULL
)

/*
#TB1: one row per column to change
  nvarchar / nchar: max_length is bytes, so divide by 2
  max_length = -1 means MAX
*/
INSERT INTO #TB1 (TBL, COL, TYPE_NAME, LEN_TEXT, NULL_TEXT)
SELECT t.name, c.name, ty.name,
       CASE WHEN c.max_length = -1 THEN 'MAX'
            WHEN ty.name IN ('nvarchar', 'nchar') THEN CAST(c.max_length / 2 AS varchar(10))
            ELSE CAST(c.max_length AS varchar(10)) END,
       CASE WHEN c.is_nullable = 1 THEN 'NULL' ELSE 'NOT NULL' END
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE c.collation_name LIKE 'Chinese_PRC%'
  AND ty.name IN ('varchar', 'nvarchar', 'char', 'nchar')
ORDER BY t.name, c.column_id

SET @n = 1
SELECT @cnt = COUNT(*) FROM #TB1

WHILE (@n <= @cnt)
BEGIN
    SELECT @sql = 'ALTER TABLE dbo.[' + TBL + '] ALTER COLUMN [' + COL + '] '
                + TYPE_NAME + '(' + LEN_TEXT + ') COLLATE Chinese_Taiwan_Stroke_CI_AS ' + NULL_TEXT
    FROM #TB1 WHERE N_SEQ = @n

    PRINT @sql
    EXEC (@sql)

    SET @n += 1
END

DROP TABLE #TB1
GO

/* ---------- Step 4: add UNIQUE back ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_SYS_PROGRAM_FORM_ID')
    ALTER TABLE dbo.SYS_PROGRAM ADD CONSTRAINT UQ_SYS_PROGRAM_FORM_ID UNIQUE (FORM_ID);
GO

/* ---------- Check: should be only Chinese_Taiwan_Stroke_CI_AS (+ USERS.PWD2 = CS_AS) ---------- */
SELECT COLLATION_NAME, COUNT(*) AS COLUMN_COUNT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLLATION_NAME IS NOT NULL
GROUP BY COLLATION_NAME;
GO
