/* ============================================================
   jsERP - Login + Menu setup                    2026-10-24 17:22 added
   Rule: only ADD tables / columns / rows. Never DROP / ALTER existing ones.
   Safe to run many times (every step checks first).

   Menu permission chain:
     USERS.USERID
       -> SYS_ROLES.USER_ID          (ROLE_ID)
       -> SYS_ROLEGROUPS_D.ROLE_ID   (ROLEGROUP_ID)
       -> SYS_SECURITY.ROLEGROUP_ID  (FORM_NAME, ACTIVE = 'Y')
       -> SYS_PROGRAM.FORM_ID        (menu text, parent, controller, action)   2026-10-25 16:30 modified
   ============================================================ */
USE JSERP;
GO

/* ---------- Step 1: USERS.PWD2 (plain text password, no encryption yet) ---------- */
-- CS = case sensitive, so "Abc" and "abc" are different passwords
IF COL_LENGTH('dbo.USERS', 'PWD2') IS NULL
BEGIN
    ALTER TABLE dbo.USERS ADD PWD2 varchar(50) COLLATE Chinese_Taiwan_Stroke_CS_AS NULL;
END
GO

-- Default password for testing: PWD2 = USERID (only for empty rows)
UPDATE dbo.USERS SET PWD2 = USERID WHERE PWD2 IS NULL;
GO

/* ---------- Step 2: SYS_PROGRAM (menu definition, copied from mvcstore.z_sys_program) ---------- */
-- All columns use the DB default collation Chinese_Taiwan_Stroke_CI_AS (see 05_fix_collation.sql).   2026-10-25 19:00 modified
IF OBJECT_ID('dbo.SYS_PROGRAM', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SYS_PROGRAM (
        ROW_ID          int IDENTITY(1,1) NOT NULL PRIMARY KEY,                   -- 2026-10-25 16:00 renamed from PROGRAM_ID
        FORM_ID         varchar(30)   NOT NULL CONSTRAINT UQ_SYS_PROGRAM_FORM_ID UNIQUE,  -- z_sys_program.mno   (= SYS_SECURITY.FORM_NAME)   2026-10-25 19:00 modified: remove COLLATE
        FORM_CAPTION    nvarchar(100) NULL,             -- z_sys_program.mname (menu text)   2026-10-25 19:00 modified: remove COLLATE
        PARENT_NAME     varchar(30)   NULL,             -- z_sys_program.parentno (empty = top menu)   2026-10-25 19:00 modified: remove COLLATE
        SORT_NO         varchar(10)   NULL,             -- z_sys_program.no_sort   2026-10-25 19:00 modified: remove COLLATE
        CONTROLLER_NAME varchar(100)  NULL,             -- z_sys_program.controllername   2026-10-25 19:00 modified: remove COLLATE
        ACTION_NAME     varchar(100)  NULL,             -- z_sys_program.actionname   2026-10-25 19:00 modified: remove COLLATE
        ROUTE_NAME      varchar(120)  NULL,             -- z_sys_program.routename (query string)   2026-10-25 19:00 modified: remove COLLATE
        CODE_NO         varchar(10)   NULL,             -- z_sys_program.code_no (M = admin, U = user)   2026-10-25 19:00 modified: remove COLLATE
        IS_ENABLED      int            NOT NULL DEFAULT 1,
        REMARK          nvarchar(500)  NULL
    );
END
GO

-- Rename PROGRAM_ID -> ROW_ID for databases created before this change   2026-10-25 16:00 added
IF COL_LENGTH('dbo.SYS_PROGRAM', 'PROGRAM_ID') IS NOT NULL
    EXEC sp_rename 'dbo.SYS_PROGRAM.PROGRAM_ID', 'ROW_ID', 'COLUMN';
GO

-- Rename FORM_NAME -> FORM_ID for databases created before this change   2026-10-25 16:30 added
IF COL_LENGTH('dbo.SYS_PROGRAM', 'FORM_NAME') IS NOT NULL
    EXEC sp_rename 'dbo.SYS_PROGRAM.FORM_NAME', 'FORM_ID', 'COLUMN';
GO

-- Copy programs that are not in SYS_PROGRAM yet
INSERT INTO dbo.SYS_PROGRAM
    (FORM_ID, FORM_CAPTION, PARENT_NAME, SORT_NO, CONTROLLER_NAME, ACTION_NAME, ROUTE_NAME, CODE_NO, IS_ENABLED, REMARK)
SELECT
    p.mno, p.mname, ISNULL(p.parentno, ''), p.no_sort, p.controllername, p.actionname, p.routename,
    UPPER(p.code_no), ISNULL(p.is_enabled, 0), p.remark
FROM mvcstore.dbo.z_sys_program p
WHERE p.mno IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.SYS_PROGRAM s WHERE s.FORM_ID = p.mno);  -- 2026-10-25 19:00 modified: remove COLLATE
GO

/* ---------- Step 3: SYS_SECURITY (which role group can use which program) ---------- */
-- MIS group: all programs (M + U)
-- ACC / MAK / PUR / SAL / STK groups: user programs (U)
INSERT INTO dbo.SYS_SECURITY (ROLEGROUP_ID, FORM_NAME, FORM_CAPTION, ACTIVE)
SELECT g.ROLEGROUP_ID, p.FORM_ID, p.FORM_CAPTION, 'Y'   -- 2026-10-25 16:30 modified
FROM dbo.SYS_ROLEGROUPS g
JOIN dbo.SYS_PROGRAM p
  ON g.ROLEGROUP_ID = 'MIS'
  OR (g.ROLEGROUP_ID IN ('ACC', 'MAK', 'PUR', 'SAL', 'STK') AND p.CODE_NO = 'U')
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.SYS_SECURITY s
    WHERE s.ROLEGROUP_ID = g.ROLEGROUP_ID AND s.FORM_NAME = p.FORM_ID   -- 2026-10-25 16:30 modified
);
GO

/* ---------- Check: menu of one user ---------- */
SELECT DISTINCT p.FORM_ID, p.FORM_CAPTION, p.PARENT_NAME, p.SORT_NO   -- 2026-10-25 16:30 modified
FROM dbo.USERS u
JOIN dbo.SYS_ROLES r         ON r.USER_ID = u.USERID
JOIN dbo.SYS_ROLEGROUPS_D gd ON gd.ROLE_ID = r.ROLE_ID
JOIN dbo.SYS_SECURITY s      ON s.ROLEGROUP_ID = gd.ROLEGROUP_ID AND s.ACTIVE = 'Y'
JOIN dbo.SYS_PROGRAM p       ON p.FORM_ID = s.FORM_NAME   -- 2026-10-25 16:30 modified
WHERE u.USERID = 'JOSH' AND p.IS_ENABLED = 1
ORDER BY p.SORT_NO, p.FORM_ID;
GO
