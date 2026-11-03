# V10-01 · Danh bạ dịch vụ AI trong SQLite cục bộ (office.db) + xuất / nhập .ini

- **Agent:** C
- **Nhánh:** `agent-c/V10-01-danh-ba-ai-sqlite`
- **Yêu cầu:** R-014
- **Loại việc:** Tính năng lõi / CLI
- **Ngân sách:** ~450 dòng
- **Thời gian tối đa:** 40 phút
- **Độ khó:** L

## Tóm tắt
- Không có màn hình mới trong task này (giao diện do Claude làm sau, UI-08).
- Thêm kho cấu hình dịch vụ AI riêng từng máy: file SQLite `%USERPROFILE%\.aiteam\hub\office.db`. Thêm / bớt AI = thêm / tắt một dòng, không sửa code.
- Xuất / nhập `.ini` để sao lưu hoặc chép sang máy khác. Không bao giờ lưu giá trị bí mật — chỉ tên khóa trong két.
- Lần đầu chạy tự nhập dữ liệu cũ (team.json, team.local.json, accounts.json).

## Đọc (CHỈ những file này)
- `docs/design/R-014-van-phong-ai.md` mục 4.6, 5, 7, 8.
- `hub/Services/LocalChatStore.cs` (mẫu dùng SQLite + khóa file).
- `core/AITeam.Core/Accounts/AiAccounts.cs` (danh mục AI cũ `AiCatalog.All`, `accounts.json`).
- `core/AITeam.Core/Providers/ProviderConfig.cs` (đọc `team.json` `providers[]`, `team.local.json`).
- `hub/Services/HubV3Registration.cs` (đăng ký DI).

## Mục tiêu
Thư mục mới `core/AITeam.Core/AiServices/`. Thêm `Microsoft.Data.Sqlite` (cùng phiên bản Hub đang dùng: 10.0.14) vào `core/AITeam.Core/AITeam.Core.csproj`.

Hợp đồng (Claude chốt, giao diện sẽ gọi đúng các chữ ký này — KHÔNG đổi tên / kiểu):
```csharp
namespace AITeam.Core.AiServices;

public enum AiAdapter { CliJson, CliText, OpenAiCompatibleHttp }   // chuỗi lưu: cli-json | cli-text | openai-compatible-http
public enum AiRole { Code, Qc, Research, Chat }                      // chuỗi lưu: code | qc | research | chat

public sealed record AiService(
    string Id, string Name, AiAdapter Adapter,
    string? Command, IReadOnlyList<string> Args, string? BaseUrl,
    IReadOnlyList<string> Models, IReadOnlyList<AiRole> Roles,
    int MaxJobs, bool Paid, string? SecretKeyName, string? ManageUrl,
    bool Enabled, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record AiAccount(string Id, string ServiceId, string Alias, string Mode, string? LoginState, DateTimeOffset? CheckedAt);
public sealed record AiQuota(string ServiceId, string? AccountId, double? RemainingPct, DateTimeOffset? RestUntil, string Source, DateTimeOffset ObservedAt);
public sealed record AiServiceValidation(bool Ok, IReadOnlyList<string> Errors);     // lỗi tiếng Việt, dùng thẳng trên form
public sealed record IniImportResult(int Added, int Updated, IReadOnlyList<string> Skipped);

public interface IAiServiceRegistry
{
    string Location { get; }                                  // đường dẫn office.db (hiển thị)
    IReadOnlyList<AiService> List(bool includeDisabled = true);
    AiService? Get(string id);
    AiServiceValidation Validate(AiService service);
    AiService Upsert(AiService service);                      // Validate lỗi ⇒ ArgumentException(message gộp Errors)
    void SetEnabled(string id, bool enabled);
    IReadOnlyList<AiAccount> Accounts(string? serviceId = null);
    AiAccount UpsertAccount(AiAccount account);
    IReadOnlyList<AiQuota> LatestQuotas();                    // mỗi (service, account) bản mới nhất
    void RecordQuota(AiQuota quota);
    string? GetSetting(string key);                           // bảng settings(key,value) — dùng cho Trợ lý chat
    void SetSetting(string key, string? value);
    string ExportIni();
    IniImportResult ImportIni(string text);
}
```
- `SqliteAiServiceRegistry(string dbFile, string aiTeamRoot, string? legacyAccountsFile = null)`. `":memory:"` ⇒ dữ liệu chỉ trong bộ nhớ (cho HUB_FAKE / test).
- Bảng: `ai_service`, `ai_account`, `ai_quota`, `settings`, `schema_info(version)`. Mảng lưu JSON. Tạo bảng nếu chưa có; `schema_info.version = 1`.
- **Xóa = tắt** (`SetEnabled(false)`), không xóa dòng (giữ lịch sử số đo).
- **Nhập dữ liệu cũ một lần** khi DB mới tạo: Codex, OpenCode, Antigravity từ `team.json` / `team.local.json` (exe, model, roles, maxConcurrent), tài khoản từ `accounts.json` cũ nếu có, danh mục `AiCatalog.All` còn lại ⇒ `Enabled=false`. Ghi `settings['migrated_at']`; lần sau không nhập lại.
- **Kiểm hợp lệ** (`Validate`):
  - `Id` khớp `^[a-z0-9][a-z0-9-]{1,31}$`; `Name` 1–60 ký tự.
  - Adapter `cli-*`: `Command` bắt buộc, chỉ tên / đường dẫn kết thúc `.exe` `.cmd` `.bat` hoặc tên lệnh trơn (`codex`, `opencode`), KHÔNG chứa `& | ; < > ^ % " '` hay xuống dòng. `Args` mỗi phần tử ≤ 200 ký tự, không xuống dòng.
  - Adapter http: `BaseUrl` là `https://…` hoặc `http://127.0.0.1…` / `http://localhost…`; không có user:pass trong URL.
  - `ManageUrl` nếu có phải `https://`.
  - `SecretKeyName` khớp `^[A-Z][A-Z0-9_]{1,63}$` (chỉ TÊN khóa).
  - `MaxJobs` 1–8. `Roles` không rỗng.
- **.ini**: mỗi dịch vụ một mục `[service:<id>]`, khóa: `name, adapter, command, args (phân cách |), base_url, models (,), roles (,), max_jobs, paid, secret_key_name, manage_url, enabled`. `ImportIni`: giới hạn 256 KB (vượt ⇒ ArgumentException), bỏ khóa lạ, **bỏ và báo trong `Skipped`** mọi khóa tên chứa `key=`/`token`/`password`/`secret` mà KHÔNG phải `secret_key_name` (không bao giờ nhận giá trị bí mật), dịch vụ không hợp lệ ⇒ bỏ qua + lý do trong `Skipped`.
- Ghi đồng thời an toàn: khóa như `LocalChatStore` (một kết nối / thao tác, `BEGIN IMMEDIATE`), test 2 luồng ghi song song không mất dữ liệu.
- DI: trong `hub/Services/HubV3Registration.cs` đăng ký `IAiServiceRegistry` — thật: `%USERPROFILE%\.aiteam\hub\office.db` (dùng `HubPaths.UserProfile`); `HUB_FAKE=1`: `":memory:"` có 3 dịch vụ mẫu.

## Phạm vi (được sửa những gì)
- `core/AITeam.Core/AiServices/` (mới)
- `core/AITeam.Core/AITeam.Core.csproj`
- `hub/Services/HubV3Registration.cs`
- `core.tests/V1001AiServiceRegistryTests.cs` (mới)
- `tasks/V10-01-danh-ba-ai-sqlite.md`

## Không làm
- **KHÔNG sửa giao diện**: `hub/Pages/**`, `hub/wwwroot/**` (luật v5.2 — giao diện do Claude làm).
- Không đổi `ProviderKind`, factory, `AiCatalog` (để V10-02).
- Không đọc / ghi két bí mật, không lưu giá trị khóa.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- Dữ liệu không tin cậy: file `.ini` người dùng nhập, `accounts.json` cũ — kiểm như trên, không ném lỗi làm sập Hub với file cũ hỏng (bỏ qua + ghi lý do).
- SQL luôn tham số hóa, không ghép chuỗi.
- Đường dẫn DB: tạo thư mục nếu chưa có; chặn reparse point như `LocalChatStore`.
- Máy chưa có `team.local.json` / `accounts.json` ⇒ vẫn chạy.

## Test nghiệm thu
`core.tests/V1001AiServiceRegistryTests.cs` (agent viết, không có nhánh `return` sớm, dùng thư mục tạm):
- Tạo mới ⇒ có bảng + nhập dữ liệu cũ từ `team.json` mẫu (Codex, OpenCode bật; danh mục khác tắt); mở lần 2 không nhập trùng.
- Upsert / Get / List / SetEnabled(false) giữ dòng.
- Validate: id sai, command có `&`, base_url `http://evil.com`, URL có user:pass, secret_key_name chữ thường, MaxJobs 0 ⇒ đều lỗi có thông báo tiếng Việt.
- ExportIni ⇒ ImportIni trên DB rỗng ⇒ bằng nhau. Import có `api_key=sk-…` (ghép chuỗi lúc chạy để không vướng quét bí mật) ⇒ bị bỏ, có trong `Skipped`, không lưu ở đâu cả (đọc lại cả file DB không thấy chuỗi đó).
- Import > 256 KB ⇒ ArgumentException.
- 2 luồng Upsert song song 50 lần ⇒ đủ 100 dòng.
- Settings get / set / null.
- `":memory:"` không tạo file.

## Tiêu chí hoàn thành
- `dotnet build AITeam.sln -warnaserror -nodeReuse:false` 0 warning; toàn bộ test xanh.
- Giữ BOM UTF-8 cho `.cs`.
- Commit: `V10-01: danh bạ dịch vụ AI SQLite + xuất/nhập ini (Codex)`.

## Báo cáo của agent

## Báo cáo của agent — OpenCode
- Đã làm: thêm `AiServiceModels.cs` (enum/record/interface đúng hợp đồng) và `SqliteAiServiceRegistry.cs` (schema 5 bảng, nhập dữ liệu cũ một lần từ team.json/team.local.json/accounts.json, Upsert/Get/List/SetEnabled, Validate, tài khoản, hạn mức, settings, xuất/nhập .ini giới hạn 256 KB, chặn khóa bí mật, ghi BEGIN IMMEDIATE).
- File đã đổi: `core/AITeam.Core/AiServices/` (mới), `core/AITeam.Core/AITeam.Core.csproj` (Microsoft.Data.Sqlite 10.0.14), `hub/Services/HubV3Registration.cs` (DI: office.db thật, HUB_FAKE `:memory:` + 3 dịch vụ mẫu), `core.tests/V1001AiServiceRegistryTests.cs` (mới, 10 ca), file task này.
- Kiểm: `dotnet build AITeam.sln -warnaserror -nodeReuse:false` ⇒ 0 warning, 0 error; `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m` ⇒ 1797/1797 đạt (Hub 837, Core 960).
- Quyết định tự đưa ra: `legacyAccountsFile = null` nghĩa là bỏ qua accounts.json cũ (DI truyền đường dẫn thật) để test không đọc hồ sơ máy; dữ liệu cũ được ghi cả khi chưa đủ trường hợp lệ (hiện dạng tắt) để Hub không sập.
- Chưa xác minh: chưa chạy Hub thật / mở trang giao diện (ngoài phạm vi lõi); test hạn mức/đăng nhập thật của dịch vụ.

## Review (Claude)
