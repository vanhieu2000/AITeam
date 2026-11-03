# V10-02 · Chạy AI theo danh bạ: 3 adapter chung + factory đọc registry

- **Agent:** B
- **Model:** `gemini-3.8-flash-high`
- **Nhánh:** `agent-o/V10-02-adapter-ai-tu-danh-ba`
- **Yêu cầu:** R-014
- **Loại việc:** Tính năng lõi / CLI
- **Ngân sách:** ~550 dòng
- **Thời gian tối đa:** 40 phút
- **Độ khó:** L

## Tóm tắt
- Không có chức năng mới trên màn hình (màn "Tài khoản AI" do Claude làm sau, UI-08).
- Thêm AI mới (vd. một CLI khác hoặc một API kiểu OpenAI) chỉ bằng cách thêm dòng trong danh bạ `office.db`, không phải sửa code C#.
- Codex / OpenCode / Antigravity vẫn chạy bằng lớp riêng như cũ (an toàn), nhưng được chọn qua danh bạ.

## Đọc (CHỈ những file này)
- `docs/design/R-014-van-phong-ai.md` mục 4.6, 5 ("Bỏ viết cứng danh sách AI"), 8.
- `core/AITeam.Core/AiServices/AiServiceModels.cs` (hợp đồng V10-01, KHÔNG đổi).
- `core/AITeam.Core/Providers/ProviderModels.cs`, `ProviderConfig.cs`, `OpenCodeProvider.cs` (cách dựng ProcessSpec, phân loại lỗi hạn mức).
- `core/AITeam.Core/Process/` (IProcessRunner, ProcessSpec).
- `core/AITeam.Core/Research/Research.cs` chỉ lớp `SecretRedactor`.

## Mục tiêu
**Khung code đã có trên nhánh** (commit "Test nghiệm thu V10-02 (Claude)"): `core/AITeam.Core/AiServices/Adapters/AiAdapters.cs` (chữ ký chốt, thân `NotImplementedException`) + `ProviderKind.Generic`. Agent điền thân, KHÔNG đổi chữ ký. Hợp đồng:
```csharp
namespace AITeam.Core.AiServices.Adapters;

/// Tra giá trị khóa trong két theo TÊN. Core không biết két nằm đâu; Hub cắm vào sau.
public interface ISecretLookup { string? Get(string name); }

public interface IAiProviderFactory
{
    /// Dịch vụ có Id "codex" / "opencode" / "agy" hoặc "antigravity" (mã trong danh bạ) ⇒ lớp riêng có sẵn (CodexProvider / OpenCodeProvider / AgyProvider),
    /// dựng ProviderConfig từ AiService (Command → Exe, Models[0] → Model, MaxJobs → MaxConcurrent, Paid).
    /// Còn lại theo Adapter: CliJson ⇒ CliJsonProvider, CliText ⇒ CliTextProvider, OpenAiCompatibleHttp ⇒ OpenAiHttpProvider.
    /// Dịch vụ tắt (Enabled=false) ⇒ InvalidOperationException("Dịch vụ AI <id> đang tắt.").
    IAgentProvider Create(AiService service);

    /// List(includeDisabled:false) của registry, lọc theo vai trò, giữ thứ tự registry.
    IReadOnlyList<AiService> ForRole(AiRole role);
}

public sealed class AiProviderFactory(IAiServiceRegistry registry, IProcessRunner runner, ISecretLookup secrets, HttpMessageHandler? http = null) : IAiProviderFactory { … }
```
- `Create` KHÔNG kiểm lệnh / URL (chỉ kiểm Enabled); lỗi cấu hình trả ra lúc `RunAsync` với `Status=ConfigError`, không gọi tiến trình / mạng.
- `ProviderKind` thêm giá trị **Generic** (cuối enum, không đổi số của giá trị cũ). Ba provider chung có `Kind = Generic`, `Id = service.Id`.
- **`CliTextProvider`**: chạy `service.Command` với `service.Args` (mảng). Mỗi phần tử được thay chỗ giữ `{prompt_file}`, `{workdir}`, `{model}` bằng giá trị thật — thay TRONG phần tử, không ghép thành chuỗi lệnh, không qua shell. Prompt đưa qua file (StdinFile = prompt file nếu không có `{prompt_file}` trong Args). Kết quả = stdout. `HasReadOnlySandbox = false`.
- **`CliJsonProvider`**: như CliText, nhưng stdout là JSON lines; Text = nối các trường `text` của dòng có `"type":"text"` (hoặc `part.text`), token lấy từ `tokens.input/output` nếu có; dòng không phải JSON bỏ qua.
- **`OpenAiHttpProvider`**: POST `{BaseUrl}/chat/completions`, body `{model, messages:[{role:"user",content:<nội dung prompt file>}]}`; header `Authorization: Bearer <secrets.Get(SecretKeyName)>` khi có tên khóa. Text = `choices[0].message.content`, token từ `usage.prompt_tokens / completion_tokens`. Chỉ hỗ trợ `RunMode.ReadOnly`; `Develop` ⇒ `Status=ConfigError`, Detail "Adapter HTTP không viết code được.". `HasReadOnlySandbox = true` (không chạm file).
- **Phân loại lỗi** (cả 3): mã thoát / HTTP 429, hoặc chữ `rate limit`, `quota`, `usage limit`, `insufficient balance`, `Free usage limit` ⇒ `QuotaExhausted`; HTTP 401/403 ⇒ `NotLoggedIn`; không tìm thấy exe ⇒ `Unavailable`; quá `request.Timeout` ⇒ `TimedOut` (dừng tiến trình / hủy request).
- `CheckAsync`: CLI ⇒ chạy `Command --version` (timeout 15 s); HTTP ⇒ `GET {BaseUrl}/models` (timeout 15 s). Không in giá trị khóa.
- DI trong `hub/Services/HubV3Registration.cs`: `ISecretLookup` = lớp nhỏ mới `hub/Services/VaultSecretLookup.cs`. `IVaultService` cố ý KHÔNG có hàm trả giá trị (giao diện không được đọc khóa) ⇒ thêm vào lớp `VaultService` hàm `internal string? ReadValue(string name)` (dùng `ReadVault()` có sẵn, tên sai mẫu ⇒ null) — KHÔNG thêm vào `IVaultService`. `VaultSecretLookup(VaultService vault)` gọi hàm này. `IAiProviderFactory` singleton.

## Phạm vi (được sửa những gì)
- `core/AITeam.Core/AiServices/Adapters/AiAdapters.cs` (khung có sẵn — điền thân; được tách thêm file trong thư mục này)
- `core/AITeam.Core/AiServices/Adapters/` (thư mục)
- `core/AITeam.Core/Providers/ProviderModels.cs` (đã thêm Generic — không sửa thêm)
- `hub/Services/VaultSecretLookup.cs` (tạo mới)
- `hub/Services/VaultService.cs` (chỉ thêm hàm ReadValue)
- `hub/Services/HubV3Registration.cs`
- `tasks/V10-02-adapter-ai-tu-danh-ba.md`

## Không làm
- **KHÔNG sửa giao diện**: `hub/Pages/**`, `hub/wwwroot/**`.
- Không xóa / đổi `ProviderSettings.Create`, `AiCatalog`, `team.ps1` (đợt sau mới chuyển hết).
- Không gọi mạng thật trong test (dùng `HttpMessageHandler` giả và `IProcessRunner` giả).

## Ràng buộc (bảo mật)
- Giá trị khóa KHÔNG được xuất hiện trong: `AgentRunResult.Text`, `Detail`, log, ngoại lệ, file tạm. Mọi chuỗi trả ra đi qua `SecretRedactor.Redact` và thay thêm chính giá trị khóa bằng `***`.
- `BaseUrl` chỉ nhận `https://…` hoặc `http://127.0.0.1…` / `http://localhost…`; khác ⇒ `ConfigError`. Không tự theo chuyển hướng (`AllowAutoRedirect=false`).
- `Command` phải là tên / đường dẫn file không chứa `& | ; < > ^ " \n`; sai ⇒ `ConfigError`.
- Phản hồi HTTP đọc tối đa 2 MB; stdout CLI giữ tối đa 2 MB (cắt phần thừa, ghi Detail "đã cắt").

## Test nghiệm thu
`core.tests/V1002AiAdapterTests.cs` — Claude đã viết sẵn, đang đỏ 12/12 (runner giả, handler HTTP giả, registry SQLite thư mục tạm). Gồm:
- Factory: id `codex` ⇒ `CodexProvider`; `opencode` ⇒ `OpenCodeProvider`; dịch vụ `mycli` adapter cli-text ⇒ `CliTextProvider` với `Id="mycli"`, `Kind=Generic`; dịch vụ tắt ⇒ InvalidOperationException; `ForRole(Chat)` chỉ trả dịch vụ bật có vai trò Chat.
- CliText: Args `["run","{prompt_file}","--m","{model}"]` ⇒ ProcessSpec nhận đúng mảng đã thay, không có shell; stdout ⇒ Text.
- CliJson: 3 dòng JSON (1 text, 1 rác, 1 tokens) ⇒ Text đúng, Tokens đúng.
- HTTP: 200 ⇒ Text + Tokens; header Bearer có giá trị khóa giả nhưng Text / Detail khi server trả lại chính khóa ⇒ đã thành `***`; 429 ⇒ QuotaExhausted; 401 ⇒ NotLoggedIn; 302 sang host khác ⇒ không theo; BaseUrl `http://evil.com` ⇒ ConfigError; Mode Develop ⇒ ConfigError.
- Chuỗi `Free usage limit reached` trong stderr CLI ⇒ QuotaExhausted.
- Command `codex & del x` ⇒ ConfigError.

## Tiêu chí hoàn thành
- [x] 12 test trong file nghiệm thu xanh; không sửa file nghiệm thu.
- [x] Build 0 warning; toàn bộ test xanh; giữ BOM (cả file mới).
- [x] Không chạm `hub/Pages/**`, `hub/wwwroot/**`; không gọi mạng / AI thật trong test.
- [x] Commit: "V10-02: chạy AI theo danh bạ — 3 adapter chung + factory (Antigravity)".

## Báo cáo của agent
| Tiêu chí | Lệnh đã chạy | Kết quả thật |
|---|---|---|
| 12 test nghiệm thu xanh | `dotnet test core.tests --filter "FullyQualifiedName~V1002AiAdapterTests"` | Passed: 12, Failed: 0, Skipped: 0 |
| Build 0 warning | `dotnet build AITeam.sln -warnaserror` | Build succeeded. 0 Warning(s), 0 Error(s) |
| Toàn bộ test xanh | `dotnet test AITeam.sln --no-build` | Hub: 858 passed; Core: 996 passed (1854 passed, 0 failed) |
| Giữ BOM UTF-8 | python script kiểm tra byte đầu | 4/4 file .cs đúng BOM/chuẩn UTF-8 |
| Phạm vi & bảo mật | `git status` | Không chạm Pages/wwwroot, che bí mật qua SecretRedactor |

- File đã đổi: `core/AITeam.Core/AiServices/Adapters/AiAdapters.cs`, `hub/Services/VaultService.cs`, `hub/Services/VaultSecretLookup.cs` (mới), `hub/Services/HubV3Registration.cs`, `tasks/V10-02-adapter-ai-tu-danh-ba.md`.
- Điều chưa chắc: Không có (toàn bộ 1854 test hệ thống đều pass).

## Review (Claude)
### Vòng 1 — ĐẠT
- Antigravity Gemini 3.8 (Agent B), 25′, ~85k token đầu ra. 12/12 test nghiệm thu (Claude) xanh, file nghiệm thu không bị sửa.
- Đã kiểm: build 0 warning (không tăng dần); Hub 858/858; Core 996/996; không chạm `hub/Pages/**`, `hub/wwwroot/**`; BOM giữ (VaultService.cs gốc không có BOM).
- Bảo mật: khóa thay `***` + SecretRedactor trước khi trả Text / Detail / lỗi; HTTP không theo chuyển hướng (SocketsHttpHandler AllowAutoRedirect=false); BaseUrl chỉ https hoặc http 127.0.0.1 / localhost / [::1]; lệnh chặn ký tự shell, tham số dạng mảng; stdout / phản hồi tối đa 2 MB; `ReadValue` chỉ `internal`, không vào `IVaultService`.
- Optional: `VaultSecretLookup` ở chế độ HUB_FAKE vẫn tạo `VaultService` thật (đọc két thật nếu dùng adapter HTTP) — nên dùng két giả khi FAKE; mỗi lần gọi tạo `HttpClient` mới.
- Lượt kết thúc với lỗi mạng "no such host" sau khi đã push; `team.ps1` phân loại nhầm thành HẾT HẠN MỨC — [CK] cần sửa nhận diện hạn mức.

## Kết quả
- Thêm AI mới chỉ bằng thêm dòng trong danh bạ office.db (adapter cli-text / cli-json / openai-compatible-http); Codex / OpenCode / Antigravity vẫn dùng lớp riêng nhưng chọn qua danh bạ. Chưa có màn hình (UI-08).