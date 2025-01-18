# OOTD-API-ASP.NET-CORE

## 部署方式
手動部署:
> [!NOTE]
> 請先確保安裝.NET 8.0 SDK環境
```sh
git clone https://github.com/HeavenManySugar/OOTD-API-CORE.git
cd OOTD-API-CORE/OOTD-API-ASP.NET-CORE
dotnet restore
dotnet run 
```
對於Windows用戶也可直接使用Visual Studio開啟OOTD-API-ASP.NET-CORE.sln

快速體驗 (Docker Compose):

[OOTD-FullStack](https://github.com/HeavenManySugar/OOTD-FullStack)

現有的Dockerfile為過時的部署方式，不保證能正確運行



## Online DEMO

[Swagger 頁面](https://ootd-api-core.ruien.me/swagger)

[ReDoc 頁面](https://ootd-api-core.ruien.me/redoc/index.html?url=/swagger/v1/swagger.json)

## 目前實現版本 OOTD-API Commit 5f485fe

由於.NET Framework 和 .NET Core 某些程式碼的實現不同，可能存在許多未知的漏洞
