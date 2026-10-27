/* ============================================================
   jsERP - SYS_ROLEGROUPS trigger + SYS_PROGRAM_D data        2026-10-25 17:00 added
   Safe to run many times.

   Rule:
     INSERT a role group  -> add one SYS_PROGRAM_D row for every SYS_PROGRAM.FORM_ID
                             (IS_VIEW / IS_ADD / IS_EDIT / IS_DELETE / IS_PRINT = 1)
     DELETE a role group  -> delete its SYS_PROGRAM_D rows

   Note: all columns use Chinese_Taiwan_Stroke_CI_AS now (05_fix_collation.sql),
         so COLLATE is not needed.   2026-10-25 19:00 modified
   ============================================================ */
USE JSERP;
GO

/* ---------- Step 1: trigger on SYS_ROLEGROUPS ---------- */
-- 2026-10-26 11:00 modified: no CROSS JOIN, use #TB1 / #TB2 + WHILE loop (same style as zzz_JWERP.SP_MN2, SP_MN4)
CREATE OR ALTER TRIGGER dbo.TR_SYS_ROLEGROUPS
   ON dbo.SYS_ROLEGROUPS
   AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @cmd varchar(10), @n int, @cnt int, @n2 int, @cnt2 int,
            @rolegroupid varchar(12), @formid varchar(30)

    IF (EXISTS(SELECT * FROM inserted) AND NOT EXISTS(SELECT * FROM deleted)) SET @cmd = 'INSERT'
    IF (NOT EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted)) SET @cmd = 'DELETE'

    /* New role group: one row per program, all rights = 1 */
    IF @cmd = 'INSERT'
    BEGIN

        /* #TB1: new role groups (usually only 1 row) */
        IF OBJECT_ID('tempdb..#TB1') IS NOT NULL DROP TABLE #TB1
        CREATE TABLE #TB1
        (
        N_SEQ INT IDENTITY(1,1),
        ROLEGROUP_ID VARCHAR(12) NULL
        )

        INSERT INTO #TB1
        SELECT ROLEGROUP_ID FROM inserted

        /* #TB2: all programs */
        IF OBJECT_ID('tempdb..#TB2') IS NOT NULL DROP TABLE #TB2
        CREATE TABLE #TB2
        (
        N_SEQ INT IDENTITY(1,1),
        FORM_ID VARCHAR(30) NULL
        )

        INSERT INTO #TB2
        SELECT FORM_ID FROM dbo.SYS_PROGRAM ORDER BY FORM_ID

        SET @n=1
        SELECT @cnt=COUNT(*) FROM #TB1

        WHILE(@n<=@cnt)
        BEGIN

            SELECT @rolegroupid = ROLEGROUP_ID FROM #TB1 WHERE N_SEQ = @n

            SET @n2=1
            SELECT @cnt2=COUNT(*) FROM #TB2

            WHILE(@n2<=@cnt2)
            BEGIN

                SELECT @formid = FORM_ID FROM #TB2 WHERE N_SEQ = @n2

                IF NOT EXISTS(SELECT * FROM dbo.SYS_PROGRAM_D WHERE ROLEGROUP_ID=@rolegroupid AND FORM_ID=@formid)
                BEGIN
                    INSERT INTO dbo.SYS_PROGRAM_D
                    (ROLEGROUP_ID, FORM_ID, IS_VIEW, IS_ADD, IS_EDIT, IS_DELETE, IS_PRINT)
                    VALUES
                    (@rolegroupid, @formid, 1, 1, 1, 1, 1)
                END

                SET @n2+=1
            END

            SET @n+=1
        END

    END

    /* Removed role group: remove its rows */
    IF @cmd = 'DELETE'
    BEGIN
        DELETE FROM dbo.SYS_PROGRAM_D
        WHERE ROLEGROUP_ID IN (SELECT ROLEGROUP_ID FROM deleted)
    END
END
GO

/* ---------- Step 2: fill SYS_PROGRAM_D for the role groups that already exist ---------- */
-- Role groups x programs (6 x 24 = 144 rows). Rows that already exist are skipped.
-- 2026-10-26 11:00 modified: no CROSS JOIN, use #TB1 / #TB2 + WHILE loop
DECLARE @n int, @cnt int, @n2 int, @cnt2 int,
        @rolegroupid varchar(12), @formid varchar(30)

/* #TB1: all role groups */
IF OBJECT_ID('tempdb..#TB1') IS NOT NULL DROP TABLE #TB1
CREATE TABLE #TB1
(
N_SEQ INT IDENTITY(1,1),
ROLEGROUP_ID VARCHAR(12) NULL
)

INSERT INTO #TB1
SELECT ROLEGROUP_ID FROM dbo.SYS_ROLEGROUPS ORDER BY ROLEGROUP_ID

/* #TB2: all programs */
IF OBJECT_ID('tempdb..#TB2') IS NOT NULL DROP TABLE #TB2
CREATE TABLE #TB2
(
N_SEQ INT IDENTITY(1,1),
FORM_ID VARCHAR(30) NULL
)

INSERT INTO #TB2
SELECT FORM_ID FROM dbo.SYS_PROGRAM ORDER BY FORM_ID

SET @n=1
SELECT @cnt=COUNT(*) FROM #TB1

WHILE(@n<=@cnt)
BEGIN

    SELECT @rolegroupid = ROLEGROUP_ID FROM #TB1 WHERE N_SEQ = @n

    SET @n2=1
    SELECT @cnt2=COUNT(*) FROM #TB2

    WHILE(@n2<=@cnt2)
    BEGIN

        SELECT @formid = FORM_ID FROM #TB2 WHERE N_SEQ = @n2

        IF NOT EXISTS(SELECT * FROM dbo.SYS_PROGRAM_D WHERE ROLEGROUP_ID=@rolegroupid AND FORM_ID=@formid)
        BEGIN
            INSERT INTO dbo.SYS_PROGRAM_D
            (ROLEGROUP_ID, FORM_ID, IS_VIEW, IS_ADD, IS_EDIT, IS_DELETE, IS_PRINT)
            VALUES
            (@rolegroupid, @formid, 1, 1, 1, 1, 1)
        END

        SET @n2+=1
    END

    SET @n+=1
END

DROP TABLE #TB1
DROP TABLE #TB2
GO

/* ---------- Check ---------- */
SELECT ROLEGROUP_ID, COUNT(*) AS PROGRAM_COUNT
FROM dbo.SYS_PROGRAM_D
GROUP BY ROLEGROUP_ID
ORDER BY ROLEGROUP_ID;
GO

/* ---------- Test (run by hand) ----------
INSERT INTO dbo.SYS_ROLEGROUPS (ROLEGROUP_ID, ROLEGROUP_DESC) VALUES ('QC', 'QC');
SELECT COUNT(*) FROM dbo.SYS_PROGRAM_D WHERE ROLEGROUP_ID = 'QC';   -- 24
DELETE FROM dbo.SYS_ROLEGROUPS WHERE ROLEGROUP_ID = 'QC';
SELECT COUNT(*) FROM dbo.SYS_PROGRAM_D WHERE ROLEGROUP_ID = 'QC';   -- 0
*/
