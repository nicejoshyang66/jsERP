# Bootstrap 5 IntelliSense 設定

本專案需要在 VS Code 中有 Bootstrap 5 的 class 自動完成。
請依照以下步驟檢查並設定，已存在的檔案請合併內容，不要覆蓋。

## 1. 確認專案內有 Bootstrap CSS 檔

- 擴充套件只會掃描「專案內」的 .css 檔，CDN 連結掃不到。
- 先搜尋專案內是否已有 `bootstrap.css` 或 `bootstrap.min.css`（排除 bin、obj、node_modules）。
- ASP.NET Core MVC 專案通常已有 `wwwroot/lib/bootstrap/dist/css/bootstrap.css`，有就跳過此步。
- 若沒有：下載 Bootstrap 5.3.3 的 CSS 放到下列位置
  - ASP.NET Core 專案：`wwwroot/lib/bootstrap/dist/css/bootstrap.min.css`
  - 一般 HTML / Vue 目錄：`lib/bootstrap/css/bootstrap.min.css`
  - 下載網址：https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css

## 2. 建立 `.vscode/settings.json`

```json
{
  "html-css-class-completion.HTMLLanguages": [
    "html",
    "vue",
    "razor",
    "aspnetcorerazor",
    "blade",
    "handlebars",
    "twig",
    "django-html",
    "php",
    "markdown",
    "erb",
    "ejs",
    "svelte"
  ],
  "html-css-class-completion.excludeGlobPattern": "{**/bin/**,**/obj/**,**/node_modules/**}"
}
```

## 3. 建立 `.vscode/extensions.json`

```json
{
  "recommendations": [
    "zignd.html-css-class-completion"
  ]
}
```

- ASP.NET Core 專案另外加入 `"ms-dotnettools.csdevkit"`。

## 4. 確認 `.gitignore`

- 若 `.gitignore` 有排除 `.vscode/`，請加入例外，讓設定能跟著專案走：

```
!.vscode/settings.json
!.vscode/extensions.json
```

## 5. 完成後

- 提醒我執行 VS Code 指令「Developer: Reload Window」。
- 測試方式：在 .html 或 .cshtml 的 `class=""` 中輸入 `btn-`，應出現 Bootstrap class 提示。
