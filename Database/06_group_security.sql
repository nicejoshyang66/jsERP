/* ============================================================
   jsERP - 群組權限資料維護 (ZSYSGroupSecurity)                2026-10-30 11:13 added
   Safe to run many times.

   Run 02_rolegroup_trigger.sql first (it adds SYS_PROGRAM_D.IS_SIGN).

   1. Menu MISP008 -> controller ZSYSGroupSecurity (was GroupSecurity)
   ============================================================ */
USE JSERP;
GO

/* ---------- Step 1: menu points to the new controller ---------- */
UPDATE dbo.SYS_PROGRAM
SET CONTROLLER_NAME = 'ZSYSGroupSecurity', ACTION_NAME = 'List'
WHERE CONTROLLER_NAME = 'GroupSecurity';
GO

/* ---------- Check ---------- */
SELECT FORM_ID, FORM_CAPTION, CONTROLLER_NAME, ACTION_NAME
FROM dbo.SYS_PROGRAM
WHERE CONTROLLER_NAME = 'ZSYSGroupSecurity';

SELECT ROLEGROUP_ID, COUNT(*) AS PROGRAM_COUNT, SUM(ISNULL(IS_SIGN, 0)) AS SIGN_COUNT
FROM dbo.SYS_PROGRAM_D
GROUP BY ROLEGROUP_ID
ORDER BY ROLEGROUP_ID;
GO
