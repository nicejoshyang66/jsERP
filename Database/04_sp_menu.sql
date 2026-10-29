/* ============================================================
   jsERP - SP_MENU : menu of the login user                  2026-10-25 18:00 added
   Safe to run many times.

   Steps (same idea as zzz_JWERP.SP_ACYCLE1, use #TABLE step by step):
     1. @USER_ID must exist in USERS
     2. #TB1 : ROLE_ID of the user        (SYS_ROLES)
     3. #TB2 : ROLEGROUP_ID of the roles  (SYS_ROLEGROUPS_D)   e.g. JOSH -> MIS, STK
     4. #TB3 : FORM_ID with IS_ENABLED = 1 and IS_VIEW = 1        2026-10-28 12:10 modified
     5. Remove children whose parent menu is hidden              2026-10-28 12:10 modified
     6. Return SYS_PROGRAM rows of #TB3 -> MENU

   Note: all columns use Chinese_Taiwan_Stroke_CI_AS now (= tempdb default),
         so #TABLE and JOIN do not need COLLATE.   2026-10-25 19:00 modified
   ============================================================ */
USE JSERP;
GO

CREATE OR ALTER PROCEDURE dbo.SP_MENU
(
    @USER_ID varchar(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    /* #TB1: roles of the user */
    CREATE TABLE #TB1
    (
        N_SEQ   INT IDENTITY(1,1),
        ROLE_ID VARCHAR(12) NULL   -- 2026-10-25 19:00 modified: remove COLLATE
    )

    /* #TB2: role groups of the roles (one row per group) */
    CREATE TABLE #TB2
    (
        N_SEQ        INT IDENTITY(1,1),
        ROLEGROUP_ID VARCHAR(12) NULL   -- 2026-10-25 19:00 modified: remove COLLATE
    )

    /* #TB3: programs the role groups can see (one row per program) */
    CREATE TABLE #TB3
    (
        N_SEQ   INT IDENTITY(1,1),
        FORM_ID VARCHAR(30) NULL   -- 2026-10-25 19:00 modified: remove COLLATE
    )

    /* Step 1: user not found -> empty menu */
    IF NOT EXISTS (SELECT 1 FROM USERS WHERE USERID = @USER_ID)
    BEGIN
        SELECT ROW_ID, FORM_ID, FORM_CAPTION, PARENT_ID, SORT_NO,   -- 2026-10-28 09:24 modified
               CONTROLLER_NAME, ACTION_NAME, ROUTE_NAME
        FROM SYS_PROGRAM
        WHERE 1 = 0
        RETURN
    END

    /* Step 2: #TB1 <- SYS_ROLES  (JOSH -> MIS00, STK02 ...) */
    INSERT INTO #TB1 (ROLE_ID)
    SELECT DISTINCT ROLE_ID
    FROM SYS_ROLES
    WHERE USER_ID = @USER_ID

    /* Step 3: #TB2 <- SYS_ROLEGROUPS_D  (MIS00 -> MIS, STK02 -> STK) */
    INSERT INTO #TB2 (ROLEGROUP_ID)
    SELECT DISTINCT D.ROLEGROUP_ID
    FROM SYS_ROLEGROUPS_D D
    JOIN #TB1 A ON A.ROLE_ID = D.ROLE_ID

    /* Step 4: #TB3 <- programs that can be shown                2026-10-28 12:10 modified
         A) SYS_PROGRAM.IS_ENABLED = 0 -> never show (do not check IS_VIEW)
         B) SYS_PROGRAM.IS_ENABLED = 1 -> check SYS_PROGRAM_D (ROLEGROUP_ID + IS_VIEW)
            IS_VIEW = 1 show, IS_VIEW = 0 not show */
    INSERT INTO #TB3 (FORM_ID)
    SELECT DISTINCT S.FORM_ID
    FROM SYS_PROGRAM S
    JOIN SYS_PROGRAM_D P ON P.FORM_ID = S.FORM_ID
    JOIN #TB2 B ON B.ROLEGROUP_ID = P.ROLEGROUP_ID
    WHERE S.IS_ENABLED = 1
      AND P.IS_VIEW = 1

    /* Step 5: parent menu is hidden -> its children are hidden too   2026-10-28 12:10 modified
       (old way added the parent back; now the parent must pass Step 4 itself) */
    DELETE C
    FROM #TB3 C
    JOIN SYS_PROGRAM S ON S.FORM_ID = C.FORM_ID
    WHERE ISNULL(S.PARENT_ID, '') <> ''
      AND S.PARENT_ID NOT IN (SELECT FORM_ID FROM #TB3)

    /* Step 6: return the menu */
    SELECT S.ROW_ID, S.FORM_ID, S.FORM_CAPTION, S.PARENT_ID, S.SORT_NO,   -- 2026-10-28 09:24 modified
           S.CONTROLLER_NAME, S.ACTION_NAME, S.ROUTE_NAME
    FROM SYS_PROGRAM S
    JOIN #TB3 C ON C.FORM_ID = S.FORM_ID   -- 2026-10-28 12:10 modified: IS_ENABLED checked in Step 4
    ORDER BY S.SORT_NO, S.FORM_ID
END
GO

/*

EXEC SP_MENU 'JOSH'

*/
