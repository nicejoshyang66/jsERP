/* ============================================================
   jsERP - SYS_PROGRAM trigger                                 2026-10-25 17:30 added
   Safe to run many times.

   Rule:
     INSERT a program  -> add one SYS_PROGRAM_D row for every SYS_ROLEGROUPS.ROLEGROUP_ID
                          (IS_VIEW / IS_ADD / IS_EDIT / IS_DELETE / IS_PRINT = 1)
                          e.g. 7 role groups -> 7 new rows
     DELETE a program  -> delete its SYS_PROGRAM_D rows (7 role groups -> 7 rows)

   Note: all columns use Chinese_Taiwan_Stroke_CI_AS now (05_fix_collation.sql),
         so COLLATE is not needed.   2026-10-25 19:00 modified
   ============================================================ */
USE JSERP;
GO

/* ---------- Trigger on SYS_PROGRAM ---------- */
-- 2026-10-26 11:00 modified: no CROSS JOIN, use #TB1 / #TB2 + WHILE loop (same style as zzz_JWERP.SP_MN2, SP_MN4)
CREATE OR ALTER TRIGGER dbo.TR_SYS_PROGRAM
   ON dbo.SYS_PROGRAM
   AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @cmd varchar(10), @n int, @cnt int, @n2 int, @cnt2 int,
            @formid varchar(30), @rolegroupid varchar(12)

    IF (EXISTS(SELECT * FROM inserted) AND NOT EXISTS(SELECT * FROM deleted)) SET @cmd = 'INSERT'
    IF (NOT EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted)) SET @cmd = 'DELETE'

    /* New program: one row per role group, all rights = 1 */
    IF @cmd = 'INSERT'
    BEGIN

        /* #TB1: new programs (usually only 1 row) */
        IF OBJECT_ID('tempdb..#TB1') IS NOT NULL DROP TABLE #TB1
        CREATE TABLE #TB1
        (
        N_SEQ INT IDENTITY(1,1),
        FORM_ID VARCHAR(30) NULL
        )

        INSERT INTO #TB1
        SELECT FORM_ID FROM inserted

        /* #TB2: all role groups */
        IF OBJECT_ID('tempdb..#TB2') IS NOT NULL DROP TABLE #TB2
        CREATE TABLE #TB2
        (
        N_SEQ INT IDENTITY(1,1),
        ROLEGROUP_ID VARCHAR(12) NULL
        )

        INSERT INTO #TB2
        SELECT ROLEGROUP_ID FROM dbo.SYS_ROLEGROUPS ORDER BY ROLEGROUP_ID

        SET @n=1
        SELECT @cnt=COUNT(*) FROM #TB1

        WHILE(@n<=@cnt)
        BEGIN

            SELECT @formid = FORM_ID FROM #TB1 WHERE N_SEQ = @n

            SET @n2=1
            SELECT @cnt2=COUNT(*) FROM #TB2

            WHILE(@n2<=@cnt2)
            BEGIN

                SELECT @rolegroupid = ROLEGROUP_ID FROM #TB2 WHERE N_SEQ = @n2

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

    /* Removed program: remove its rows */
    IF @cmd = 'DELETE'
    BEGIN
        DELETE FROM dbo.SYS_PROGRAM_D
        WHERE FORM_ID IN (SELECT FORM_ID FROM deleted)
    END
END
GO

/* ---------- Check ---------- */
SELECT FORM_ID, COUNT(*) AS ROLEGROUP_COUNT
FROM dbo.SYS_PROGRAM_D
GROUP BY FORM_ID
ORDER BY FORM_ID;
GO

/* ---------- Test (run by hand) ----------
SELECT COUNT(*) FROM dbo.SYS_ROLEGROUPS;                              -- e.g. 7
INSERT INTO dbo.SYS_PROGRAM (FORM_ID, FORM_CAPTION) VALUES ('TEST01', N'Test');
SELECT COUNT(*) FROM dbo.SYS_PROGRAM_D WHERE FORM_ID = 'TEST01';      -- 7
DELETE FROM dbo.SYS_PROGRAM WHERE FORM_ID = 'TEST01';
SELECT COUNT(*) FROM dbo.SYS_PROGRAM_D WHERE FORM_ID = 'TEST01';      -- 0
*/
