/* ============================================================
   jsERP - 客戶資料 (_T_COMPAY) / 客戶分類 (_T_CPYCLS)          2026-11-09 11:25 added
   Safe to run many times.

   1. Create _T_COMPAY  (structure copied from zzz_JWERP, only the needed columns)
   2. Create _T_CPYCLS  (DEBIT_ID, CLASS_ID)
   3. Insert 100 fake companies (DEBIT_ID = 8-digit string)
   4. Random 80 companies -> CLASS_ID 'A', the other 20 -> CLASS_ID 'B'
   ============================================================ */
USE JSERP;
GO

/* ---------- Step 1: create _T_COMPAY ---------- */
IF OBJECT_ID('dbo._T_COMPAY', 'U') IS NULL
BEGIN
    CREATE TABLE dbo._T_COMPAY (
        DEBIT_ID        NVARCHAR(25)  NOT NULL,              -- 客戶編號 (8 digits)
        DEBIT_NAME      NVARCHAR(60)  NULL,                  -- 客戶名稱
        DEBIT_NICK      NVARCHAR(20)  NOT NULL,              -- 客戶簡稱
        TAX_ID          NVARCHAR(10)  NULL,                  -- 統一編號
        CAREER_PIC      NVARCHAR(20)  NULL,                  -- 負責人
        CONTACT         NVARCHAR(20)  NULL,                  -- 聯絡人
        ADDRESS         NVARCHAR(120) NULL,                  -- 公司地址
        MAILING_ADDRESS NVARCHAR(120) NULL,                  -- 郵寄地址
        BILLING_ADDRESS NVARCHAR(120) NULL,                  -- 帳單地址
        TEL             NVARCHAR(50)  NULL,                  -- 電話
        FAX             NVARCHAR(50)  NULL,                  -- 傳真
        EMAIL           NVARCHAR(50)  NULL,                  -- Email
        MCLOSING_DAY    NVARCHAR(4)   NULL DEFAULT (N''),    -- 月結日
        CREDIT_DAYS     FLOAT         NULL DEFAULT (0),      -- 信用天數
        BANK_ID         NVARCHAR(3)   NULL,                  -- 銀行代碼
        BRANCH_CODE     NVARCHAR(50)  NULL,                  -- 分行代碼
        ACCOUNT_NAME    NVARCHAR(50)  NULL,                  -- 帳戶名稱
        ACCOUNT_NUMB    NVARCHAR(50)  NULL,                  -- 帳號
        RMT_FEE         FLOAT         NULL,                  -- 匯費
        IS_STOP         BIT           NULL,                  -- 停用
        REMARK          NTEXT         NULL,                  -- 備註
        CONSTRAINT PK__T_COMPAY PRIMARY KEY (DEBIT_ID)
    );
END
GO

/* ---------- Step 2: create _T_CPYCLS ---------- */
IF OBJECT_ID('dbo._T_CPYCLS', 'U') IS NULL
BEGIN
    CREATE TABLE dbo._T_CPYCLS (
        DEBIT_ID  NVARCHAR(25) NOT NULL,   -- 客戶編號
        CLASS_ID  NVARCHAR(6)  NOT NULL,   -- 分類代號 (A / B)
        CONSTRAINT PK__T_CPYCLS PRIMARY KEY (DEBIT_ID, CLASS_ID)
    );
END
GO

/* ---------- Step 3: 100 fake companies (only when table is empty) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo._T_COMPAY)
BEGIN
    DECLARE @i        INT = 1;
    DECLARE @id       NVARCHAR(25);
    DECLARE @nick     NVARCHAR(20);
    DECLARE @name     NVARCHAR(60);
    DECLARE @city     INT;
    DECLARE @addr     NVARCHAR(120);
    DECLARE @areaCode NVARCHAR(3);

    -- Random numbers must be saved in variables first.              2026-11-09 11:32 fixed
    -- (CHOOSE + NEWID() inside it may return NULL, because NEWID() runs again for each choice)
    DECLARE @rBrand INT, @rType INT, @rRoad INT;
    DECLARE @rName1 INT, @rName2 INT, @rName3 INT, @rName4 INT;
    DECLARE @rClose INT, @rCredit INT, @rBank INT, @rFee INT;

    WHILE @i <= 100
    BEGIN
        -- Random 8-digit id, try again if it already exists
        SET @id = RIGHT('00000000' + CAST(ABS(CHECKSUM(NEWID())) % 100000000 AS VARCHAR(8)), 8);

        IF NOT EXISTS (SELECT 1 FROM dbo._T_COMPAY WHERE DEBIT_ID = @id)
        BEGIN
            -- Get all random numbers for this row                    2026-11-09 11:32 fixed
            SET @rBrand  = ABS(CHECKSUM(NEWID())) % 20 + 1;
            SET @rType   = ABS(CHECKSUM(NEWID())) % 10 + 1;
            SET @city    = ABS(CHECKSUM(NEWID())) % 5 + 1;
            SET @rRoad   = ABS(CHECKSUM(NEWID())) % 6 + 1;
            SET @rName1  = ABS(CHECKSUM(NEWID())) % 8 + 1;
            SET @rName2  = ABS(CHECKSUM(NEWID())) % 8 + 1;
            SET @rName3  = ABS(CHECKSUM(NEWID())) % 8 + 1;
            SET @rName4  = ABS(CHECKSUM(NEWID())) % 8 + 1;
            SET @rClose  = ABS(CHECKSUM(NEWID())) % 3 + 1;
            SET @rCredit = ABS(CHECKSUM(NEWID())) % 4 + 1;
            SET @rBank   = ABS(CHECKSUM(NEWID())) % 5 + 1;
            SET @rFee    = ABS(CHECKSUM(NEWID())) % 3 + 1;

            -- Nick = brand + industry, e.g. 宏達電子
            SET @nick = CHOOSE(@rBrand,
                            N'宏達', N'台積', N'聯發', N'鴻海', N'華碩', N'廣達', N'仁寶', N'光寶', N'台達', N'研華',
                            N'大同', N'東元', N'統一', N'永豐', N'長榮', N'正新', N'巨大', N'美利', N'新光', N'中興')
                      + CHOOSE(@rType,
                            N'電子', N'科技', N'實業', N'貿易', N'機械', N'食品', N'精密', N'企業', N'光電', N'資訊');
            SET @name = @nick + N'股份有限公司';

            -- City decides address and phone area code
            SET @areaCode = CHOOSE(@city, N'02', N'03', N'04', N'06', N'07');
            SET @addr = CHOOSE(@city, N'台北市信義區', N'桃園市中壢區', N'台中市西屯區', N'台南市東區', N'高雄市前鎮區')
                      + CHOOSE(@rRoad, N'中山路', N'中正路', N'民生路', N'復興路', N'建國路', N'和平路')
                      + CAST(ABS(CHECKSUM(NEWID())) % 300 + 1 AS NVARCHAR(5)) + N'號';

            INSERT INTO dbo._T_COMPAY
                (DEBIT_ID, DEBIT_NAME, DEBIT_NICK, TAX_ID, CAREER_PIC, CONTACT,
                 ADDRESS, MAILING_ADDRESS, BILLING_ADDRESS, TEL, FAX, EMAIL,
                 MCLOSING_DAY, CREDIT_DAYS, BANK_ID, BRANCH_CODE, ACCOUNT_NAME, ACCOUNT_NUMB,
                 RMT_FEE, IS_STOP, REMARK)
            VALUES
                (@id,
                 @name,
                 @nick,
                 RIGHT('00000000' + CAST(ABS(CHECKSUM(NEWID())) % 100000000 AS VARCHAR(8)), 8),          -- TAX_ID
                 CHOOSE(@rName1, N'陳', N'林', N'黃', N'張', N'李', N'王', N'吳', N'劉')
                   + CHOOSE(@rName2, N'志明', N'淑芬', N'建宏', N'美玲', N'俊傑', N'雅婷', N'家豪', N'怡君'),  -- CAREER_PIC  2026-11-09 11:32 fixed
                 CHOOSE(@rName3, N'蔡', N'楊', N'許', N'鄭', N'謝', N'郭', N'洪', N'曾')
                   + CHOOSE(@rName4, N'小姐', N'先生', N'經理', N'副理', N'專員', N'主任', N'課長', N'協理'),  -- CONTACT     2026-11-09 11:32 fixed
                 @addr,                                                                                    -- ADDRESS
                 @addr,                                                                                    -- MAILING_ADDRESS
                 @addr,                                                                                    -- BILLING_ADDRESS
                 @areaCode + N'-' + CAST(ABS(CHECKSUM(NEWID())) % 9000000 + 1000000 AS NVARCHAR(10)),      -- TEL
                 @areaCode + N'-' + CAST(ABS(CHECKSUM(NEWID())) % 9000000 + 1000000 AS NVARCHAR(10)),      -- FAX
                 N'service' + @id + N'@example.com',                                                       -- EMAIL
                 CHOOSE(@rClose, N'25', N'30', N'31'),                                                     -- MCLOSING_DAY  2026-11-09 11:32 fixed
                 CHOOSE(@rCredit, 0, 30, 60, 90),                                                          -- CREDIT_DAYS   2026-11-09 11:32 fixed
                 CHOOSE(@rBank, N'004', N'012', N'013', N'808', N'822'),                                   -- BANK_ID       2026-11-09 11:32 fixed
                 RIGHT('0000' + CAST(ABS(CHECKSUM(NEWID())) % 10000 AS VARCHAR(4)), 4),                    -- BRANCH_CODE
                 @name,                                                                                    -- ACCOUNT_NAME
                 RIGHT('000000' + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS VARCHAR(6)), 6)
                   + RIGHT('000000' + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS VARCHAR(6)), 6),            -- ACCOUNT_NUMB (12 digits)
                 CHOOSE(@rFee, 0, 15, 30),                                                                 -- RMT_FEE       2026-11-09 11:32 fixed
                 CASE WHEN ABS(CHECKSUM(NEWID())) % 10 = 0 THEN 1 ELSE 0 END,                              -- IS_STOP (about 10%)
                 NULL);                                                                                    -- REMARK

            SET @i = @i + 1;
        END
    END
END
GO

/* ---------- Step 4: random 80 -> 'A', the other 20 -> 'B' ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo._T_CPYCLS)
BEGIN
    -- Random 80 companies get class A
    INSERT INTO dbo._T_CPYCLS (DEBIT_ID, CLASS_ID)
    SELECT TOP 80 DEBIT_ID, 'A'
    FROM dbo._T_COMPAY
    ORDER BY NEWID();

    -- The rest (20) get class B
    INSERT INTO dbo._T_CPYCLS (DEBIT_ID, CLASS_ID)
    SELECT c.DEBIT_ID, 'B'
    FROM dbo._T_COMPAY c
    WHERE NOT EXISTS (SELECT 1 FROM dbo._T_CPYCLS x WHERE x.DEBIT_ID = c.DEBIT_ID);
END
GO

/* ---------- Check ---------- */
SELECT COUNT(*) AS COMPANY_COUNT FROM dbo._T_COMPAY;

SELECT CLASS_ID, COUNT(*) AS CNT
FROM dbo._T_CPYCLS
GROUP BY CLASS_ID
ORDER BY CLASS_ID;

SELECT TOP 5 c.DEBIT_ID, c.DEBIT_NAME, c.DEBIT_NICK, c.TEL, x.CLASS_ID
FROM dbo._T_COMPAY c
JOIN dbo._T_CPYCLS x ON x.DEBIT_ID = c.DEBIT_ID
ORDER BY c.DEBIT_ID;
GO
