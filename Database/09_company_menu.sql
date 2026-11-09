/* ============================================================
   jsERP - 客戶 / 供應商基本資料 menu (BASComPay)              2026-11-09 14:30 added
   Safe to run many times.

   Both menus use the same controller / action (BASComPay/List).
   ROUTE_NAME is the query string, so the page knows which class to show:
     客戶   -> /BASComPay/List?CLASSID=A
     供應商 -> /BASComPay/List?CLASSID=B
   ============================================================ */
USE JSERP;
GO

/* ---------- Step 1: 客戶 (CLASS_ID = 'A') ---------- */
UPDATE dbo.SYS_PROGRAM SET
    CONTROLLER_NAME = 'BASComPay',
    ACTION_NAME     = 'List',
    ROUTE_NAME      = 'CLASSID=A'
WHERE FORM_CAPTION LIKE N'%客戶%';
GO

/* ---------- Step 2: 供應商 (CLASS_ID = 'B') ---------- */
UPDATE dbo.SYS_PROGRAM SET
    CONTROLLER_NAME = 'BASComPay',
    ACTION_NAME     = 'List',
    ROUTE_NAME      = 'CLASSID=B'
WHERE FORM_CAPTION LIKE N'%供應商%';
GO

/* ---------- Check ---------- */
SELECT FORM_ID, FORM_CAPTION, CONTROLLER_NAME, ACTION_NAME, ROUTE_NAME
FROM dbo.SYS_PROGRAM
WHERE FORM_CAPTION LIKE N'%客戶%' OR FORM_CAPTION LIKE N'%供應商%';
GO
