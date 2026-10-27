/* ============================================================
   jsERP - SP_MENU : menu of the login user                  2026-10-25 18:00 added
   Safe to run many times.

   Steps (same idea as zzz_JWERP.SP_ACYCLE1, use #TABLE step by step):
     1. @USER_ID must exist in USERS
     2. #TB1 : ROLE_ID of the user        (SYS_ROLES)
     3. #TB2 : ROLEGROUP_ID of the roles  (SYS_ROLEGROUPS_D)   e.g. JOSH -> MIS, STK
     4. #TB3 : FORM_ID the groups can see (SYS_PROGRAM_D, IS_VIEW = 1)
     5. Add the parent menu of each FORM_ID, so a child never loses its parent
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
        SELECT ROW_ID, FORM_ID, FORM_CAPTION, PARENT_NAME, SORT_NO,
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

    /* Step 4: #TB3 <- SYS_PROGRAM_D  (programs of MIS, STK) */
    -- 2026-10-25 19:00 modified: remove COLLATE
    INSERT INTO #TB3 (FORM_ID)
    SELECT DISTINCT P.FORM_ID
    FROM SYS_PROGRAM_D P
    JOIN #TB2 B ON B.ROLEGROUP_ID = P.ROLEGROUP_ID
    WHERE P.IS_VIEW = 1

    /* Step 5: add the parent menu (PARENT_NAME) if it is not in #TB3 yet */
    INSERT INTO #TB3 (FORM_ID)
    SELECT DISTINCT S.PARENT_NAME
    FROM SYS_PROGRAM S
    JOIN #TB3 C ON C.FORM_ID = S.FORM_ID
    WHERE ISNULL(S.PARENT_NAME, '') <> ''
      AND S.PARENT_NAME NOT IN (SELECT FORM_ID FROM #TB3)

    /* Step 6: return the menu */
    SELECT S.ROW_ID, S.FORM_ID, S.FORM_CAPTION, S.PARENT_NAME, S.SORT_NO,
           S.CONTROLLER_NAME, S.ACTION_NAME, S.ROUTE_NAME
    FROM SYS_PROGRAM S
    JOIN #TB3 C ON C.FORM_ID = S.FORM_ID
    WHERE S.IS_ENABLED = 1
    ORDER BY S.SORT_NO, S.FORM_ID
END
GO

/*

EXEC SP_MENU 'JOSH'

*/
