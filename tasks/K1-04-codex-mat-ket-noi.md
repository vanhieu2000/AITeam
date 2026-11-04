# K1-04 · Tự dừng Codex khi mất kết nối (chỉ in "Reconnecting…") hoặc đứng im

- **Agent:** C
- **Nhánh:** `agent-c/K1-04-codex-mat-ket-noi`
- **Ưu tiên:** cao
- **Ngân sách:** ~70 dòng
- **Yêu cầu:** R-014
- **Nguồn:** hàng chờ sau review K1-02
- **Thời gian tối đa:** 25 phút
- **Độ khó:** S
- **Review AI:** không (Claude review)

## Tóm tắt
- Không có chức năng mới cho người dùng.
- 04/11 12:13: proxy Codex không chạy ⇒ Codex in "Reconnecting… waiting for network" đều đặn suốt 25 phút rồi mới hết giờ; không sửa được file nào. Bộ canh đứng im (K1-02) không bắt được vì Codex vẫn in dòng.
- Sau sửa: dòng mất kết nối / thử lại KHÔNG tính là hoạt động; Codex không có hoạt động thật 15 phút ⇒ dừng, báo "Codex mất kết nối tới máy chủ AI … kiểm tra mạng hoặc openai_base_url"; `team.ps1 assign` báo LỖI MẠNG (không phải hết hạn mức).
- Kèm: nới khoảng đứng im trong 2 test K102 (1,5 → 5 giây) để hết chập chờn khi máy bận (Claude đã làm trong commit đầu).

## Đọc (CHỈ những file này)
- `core.tests/AcceptanceK104CodexNetworkStallTests.cs` (test nghiệm thu, đọc TRƯỚC — có mẫu stdout/stderr THẬT)
- `core/AITeam.Core/Process/ProcessRunner.cs` (bộ canh đứng im K1-02)
- `core/AITeam.Core/Providers/CodexProvider.cs`
- `team.ps1`: hàm `Test-NetworkError` (~dòng 1231)

## Mục tiêu
1. `ProcessRunner.RunAsync`: khi `spec.IsActivity` khác null, chỉ dòng (stdout hoặc stderr) mà `IsActivity(line)` trả true mới làm mới mốc hoạt động. Dòng vẫn được ghi vào StdOut / StdErr như cũ. `IsActivity` ném ngoại lệ ⇒ coi như true (không làm sập bộ đọc). Null ⇒ hành vi K1-02 y nguyên.
2. `CodexProvider.IsActivityLine(line)` trả **false** khi:
   - dòng JSON có `"type":"error"` (sự kiện thử lại, vd. "Reconnecting… 2/5", "waiting for network");
   - dòng JSON `item.completed` / `item.started` có `item.type == "error"` (vd. "Falling back from WebSockets…");
   - dòng log thường (không phải JSON) chứa ` ERROR codex_` (vd. "failed to refresh available models", "failed to connect to Responses WebSocket").
   Còn lại true — kể cả `agent_message` nhắc chữ "Reconnecting" (đọc JSON thật bằng `JsonDocument`, KHÔNG dò chuỗi trên cả dòng). JSON hỏng ⇒ true.
3. `CodexProvider.RunAsync` truyền `IdleTimeout = CodexProvider.IdleTimeout` (khung: 15 phút) và `IsActivity = IsActivityLine` vào `ProcessSpec`. `CheckAsync` (login status) KHÔNG bật bộ canh.
4. `CodexProvider.Parse`: `res.Stalled` ⇒ `RunStatus.TimedOut`, giữ `thread_id` (đọc như luồng thường), Detail:
   - có dòng thử lại / mất kết nối (theo mục 2) trong StdOut hoặc StdErr ⇒ `"Codex mất kết nối tới máy chủ AI suốt {N} phút (Reconnecting… / waiting for network) — đã dừng. Kiểm tra mạng hoặc openai_base_url trong ~/.codex/config.toml."`
   - không có ⇒ `"Codex đứng im {N} phút không có hoạt động — đã dừng."`
   - N = `(int)IdleTimeout.TotalMinutes`. `res.TimedOut` (giờ cứng) giữ nguyên "Quá thời gian".
5. `team.ps1` `Test-NetworkError`: thêm `mất kết nối tới máy chủ AI|Reconnecting|os error 10061|actively refused` vào regex (vẫn chỉ dò trên `Get-AgentErrorText`).

## Phạm vi (được sửa những gì)
- `core/AITeam.Core/Process/ProcessRunner.cs`
- `core/AITeam.Core/Providers/CodexProvider.cs`
- `team.ps1`
- `core.tests/K104CodexExtraTests.cs` (tạo mới, nếu cần)
- `tasks/K1-04-codex-mat-ket-noi.md`

## Không làm
- KHÔNG sửa `core.tests/AcceptanceK104CodexNetworkStallTests.cs`, `core.tests/AcceptanceK102IdleWatchdogTests.cs`.
- Không bật bộ canh cho agy (in im lặng tới khi xong). Không đổi `OpenCodeProvider`.
- Không sửa `~/.codex/config.toml`; không tự chạy lại Codex.
- Không sửa `hub/**`.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- Thread-safe như K1-02 (`Interlocked`, `TickCount64`); `IsActivity` gọi trên luồng sự kiện.
- [x] Giữ tương thích: tham số mới ở CUỐI `ProcessSpec`, mặc định null ⇒ ~40 chỗ gọi cũ không phải sửa.
- [x] Khung đã có trong commit đầu "Test nghiệm thu K1-04 + khung (Claude)": `ProcessSpec.IsActivity`, `CodexProvider.IdleTimeout`, `CodexProvider.IsActivityLine` (thân `NotImplementedException`). KHÔNG đổi tên / kiểu / giá trị.
- `team.ps1` giữ UTF-8 **có BOM**; `.cs` giữ BOM.

## Dữ liệu mẫu thật
- stdout / stderr thật của `codex exec --json` 04/11 07:11Z khi proxy 127.0.0.1:17841 không chạy: hằng `RealStdOut`, `RealStdErr` trong test nghiệm thu.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceK104CodexNetworkStallTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln -warnaserror -nodeReuse:false` 0 warning
- [ ] `dotnet test AITeam.sln --no-build` xanh hết, gồm toàn bộ K104 + K102 + O02 + test Codex cũ (không sửa)
- [ ] File `.cs`, `team.ps1` giữ BOM UTF-8
- [ ] Commit: `K1-04: Codex mất kết nối / đứng im 15 phút thì tự dừng, báo LỖI MẠNG`

## Báo cáo của agent

| Tiêu chí | Bằng chứng thực tế |
|---|---|
| Build 0 warning | `dotnet build AITeam.sln -warnaserror -nodeReuse:false --no-restore -m:1`: thành công, 0 warning, 0 error, 42,47 giây. |
| Build chuẩn | `dotnet build AITeam.sln -warnaserror -nodeReuse:false`: thất bại ở restore, mã 1, 0 warning/0 error; lần chẩn đoán cũng thất bại, chưa rõ nguyên nhân. |
| Test toàn solution | `dotnet test AITeam.sln --no-build --blame-hang-timeout 2m`: chưa có kết quả sau khi bắt đầu Hub.Tests; đã gửi Ctrl+C, lệnh dừng tiến trình bị sandbox chặn. Chưa xác minh toàn bộ xanh. |
| K104/K102/O02/Codex | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --filter "FullyQualifiedName~K104\|FullyQualifiedName~K102\|FullyQualifiedName~O02\|FullyQualifiedName~Codex" --blame-hang-timeout 2m`: 89 pass, 0 fail, 0 skip, 13 giây (dấu phân cách thực tế là ký tự pipe). |
| BOM UTF-8 | Kiểm byte EF BB BF bằng PowerShell: ProcessRunner.cs, CodexProvider.cs, K104CodexExtraTests.cs, hai file nghiệm thu K104/K102 và team.ps1 đều có BOM. |
| Commit/push | Không chạy git theo chỉ đạo; hệ thống tự commit/push, chưa có commit được kiểm. |
- Giữ nguyên toàn bộ code và test; chỉ điền báo cáo trong file task, giữ BOM. Review (Claude) đang trống.
- Quyết định: build tuần tự không restore để kiểm chứng khi restore song song thất bại; không chạy app/chụp ảnh.
- Chưa chắc: toàn bộ test solution và nguyên nhân restore thất bại; không khẳng định đã hoàn thành mọi tiêu chí.

## Review (Claude)

## Kết quả
