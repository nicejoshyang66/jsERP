/* ============================================================
   jsERP - 角色群組設定 (ZSYSRoleGroup)                        2026-10-30 19:32 added
   Safe to run many times.

   Run 03_program_trigger.sql first (TR_SYS_PROGRAM adds the SYS_PROGRAM_D rows,
   so every role group can see the new menu: IS_VIEW = 1).

   1. Menu MISP003 under 系統管理 (MIS), between MISP002 (02) and MISP008 (08)
   ============================================================ */
USE JSERP;
GO

/* ---------- Step 1: add the menu ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.SYS_PROGRAM WHERE FORM_ID = 'MISP003')
BEGIN
    INSERT INTO dbo.SYS_PROGRAM
        (FORM_ID, FORM_CAPTION, PARENT_ID, SORT_NO, CONTROLLER_NAME, ACTION_NAME, ROUTE_NAME, IS_ENABLED, REMARK)
    VALUES
        ('MISP003', N'角色群組設定', 'MIS', '03', 'ZSYSRoleGroup', 'List', NULL, 1, NULL);
END
GO

/* ---------- Check ---------- */
SELECT FORM_ID, FORM_CAPTION, PARENT_ID, SORT_NO, CONTROLLER_NAME, ACTION_NAME
FROM dbo.SYS_PROGRAM
WHERE PARENT_ID = 'MIS'
ORDER BY SORT_NO;

SELECT ROLEGROUP_ID, IS_VIEW
FROM dbo.SYS_PROGRAM_D
WHERE FORM_ID = 'MISP003'
ORDER BY ROLEGROUP_ID;
GO
