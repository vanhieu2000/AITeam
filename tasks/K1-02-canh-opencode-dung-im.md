# K1-02 · Tự dừng OpenCode khi đứng im quá lâu (hết lượt free không báo lỗi)

- **Agent:** C
- **Nhánh:** `agent-c/K1-02-canh-opencode-dung-im`
- **Ưu tiên:** cao
- **Ngân sách:** ~90 dòng
- **Yêu cầu:** R-014 (lỗi [CK] số 4)
- **Thời gian tối đa:** 25 phút
- **Độ khó:** S
- **Review AI:** không (Claude review)

## Tóm tắt
- Không có chức năng mới cho người dùng.
- Khi OpenCode bản miễn phí hết lượt, nó đứng im (không in gì, không thoát) tới hết giờ tối đa 90–150 phút. Đêm 03/11 V10-03, V10-04 treo hơn 1 giờ.
- Sau sửa: `aiteam run --provider opencode` (và `team.ps1 assign -Dev opencode`) tự dừng khi OpenCode không in dòng nào trong 20 phút. Kết quả là "Quá thời gian" kèm lý do "đứng im", để quản lý biết mà chuyển AI khác.

## Đọc (CHỈ những file này)
- `core.tests/AcceptanceK102IdleWatchdogTests.cs` (test nghiệm thu, đọc TRƯỚC)
- `core/AITeam.Core/Process/ProcessRunner.cs`
- `core/AITeam.Core/Providers/OpenCodeProvider.cs`
- `core.tests/O02OpenCodeTests.cs` (mẫu test OpenCode, không sửa)
- Tham khảo (KHÔNG sửa, không nằm trong repo): bộ canh tạm `oc-run.ps1` Claude dùng đêm 03/11 — dừng khi log đứng 15 phút.

## Bối cảnh
- `ProcessRunner.RunAsync` chỉ có `Timeout` cứng. OpenCode chạy `--format json`, in mỗi sự kiện một dòng (step_start, tool_use, step_finish, text…). Đang làm việc thì cứ vài giây tới vài phút lại có dòng mới; lệnh dài nhất (build + toàn bộ test) mất khoảng 5–8 phút.
- `OpenCodeProvider.Parse` đã đổi `TimedOut` thành `RunStatus.TimedOut`.

## Mục tiêu
1. `ProcessSpec`: thêm tham số tùy chọn cuối `TimeSpan? IdleTimeout = null`. Thêm vào cuối danh sách tham số để mọi chỗ gọi cũ vẫn biên dịch.
2. `ProcessResult`: thêm tham số tùy chọn cuối `bool Stalled = false`.
3. `ProcessRunner.RunAsync`, khi `IdleTimeout` có giá trị:
   - Ghi mốc thời gian mỗi khi nhận một dòng stdout HOẶC stderr. Dùng `Interlocked` / `Volatile` trên ticks, vì sự kiện chạy ở luồng khác.
   - Song song với chờ thoát, kiểm định kỳ mỗi `min(IdleTimeout/4, 5 giây)`, tối thiểu 100 ms. Quá `IdleTimeout` không có dòng mới ⇒ `Kill(entireProcessTree: true)` và trả `Stalled = true`, `TimedOut = false`.
   - Ưu tiên khi trùng: hủy từ người gọi (`ct`) vẫn ném `OperationCanceledException` như cũ; quá `Timeout` cứng ⇒ `TimedOut = true`, `Stalled = false`.
   - Không có `IdleTimeout` ⇒ hành vi giữ y nguyên.
4. `OpenCodeProvider`:
   - Thêm `public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(20);` và truyền vào `ProcessSpec` khi chạy.
   - `Parse`: `result.Stalled` ⇒ trả `RunStatus.TimedOut` với Detail đúng câu: `"OpenCode đứng im {N} phút không in gì (treo hoặc hết lượt miễn phí) — đã dừng."`, trong đó N = `(int)IdleTimeout.TotalMinutes`. Giữ `session` nếu đọc được, để có thể chạy tiếp.

## Phạm vi (được sửa những gì)
- `core/AITeam.Core/Process/ProcessRunner.cs`
- `core/AITeam.Core/Providers/OpenCodeProvider.cs`
- `core.tests/K102IdleWatchdogExtraTests.cs` (tạo mới, nếu agent cần thêm test)
- `tasks/K1-02-canh-opencode-dung-im.md`

## Không làm
- KHÔNG sửa `core.tests/AcceptanceK102IdleWatchdogTests.cs`.
- Không bật `IdleTimeout` cho agy / codex / provider khác (agy in im lặng tới khi xong — bật là giết nhầm).
- Không tự chạy lại / resume phiên OpenCode; chỉ dừng và báo.
- Không sửa `team.ps1`, `hub/**`.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- Thread-safe: callback stdout/stderr chạy trên luồng thread pool. Không dùng `DateTime.Now` cho mốc thời gian: dùng `Stopwatch` / `Environment.TickCount64`.
- Dọn dẹp: vòng kiểm phải dừng (hủy token riêng) khi tiến trình thoát. Không để task nền chạy tiếp, không rò `CancellationTokenSource` (dùng `using`).
- Giết cả cây tiến trình (OpenCode sinh node/dotnet con).
- [x] Gọi tiến trình ngoài: có giới hạn thời gian cứng + giới hạn đứng im; đọc output không chặn (đã bất đồng bộ).
- [x] Giữ tương thích: ~39 chỗ gọi `new ProcessSpec(...)` / `new ProcessResult(...)` không phải sửa.
- [x] Khung đã có trong commit đầu "Test nghiệm thu K1-02 + khung (Claude)": tham số `IdleTimeout` của `ProcessSpec`, `Stalled` của `ProcessResult`, hằng `OpenCodeProvider.IdleTimeout` (cả 3 trong Phạm vi). Agent chỉ viết logic, KHÔNG đổi tên / kiểu / giá trị mặc định.

## Dữ liệu mẫu thật
- Hiện tượng thật 03/11 (nhật ký R-014): V10-03, V10-04 chạy OpenCode xong test rồi đứng im hơn 1 giờ, không lỗi, không tiến trình con. `oc-run.ps1` bắt được bằng luật "log đứng 15 phút".
- Test dùng `node -e` giả lập (in 1 dòng rồi im), không gọi OpenCode thật.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceK102IdleWatchdogTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln -warnaserror -nodeReuse:false` 0 warning
- [ ] `dotnet test AITeam.sln --no-build` xanh hết, gồm 7/7 test K102 (không sửa) và test cũ `O02OpenCodeTests`, `AcceptanceV409DevRunnerTests`
- [ ] File `.cs` giữ BOM UTF-8
- [ ] Commit: `K1-02: OpenCode đứng im 20 phút thì tự dừng, báo rõ lý do`

## Báo cáo của agent

| Tiêu chí | Bằng chứng thật |
|---|---|
| Build 0 warning | Bản cuối: dotnet build AITeam.sln -warnaserror -nodeReuse:false --no-restore -m:1 → thành công, 0 warning, 0 error. |
| K102 và hồi quy | Bản cuối: dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceK102IdleWatchdogTests|FullyQualifiedName~K102IdleWatchdogExtraTests|FullyQualifiedName~O02OpenCodeTests|FullyQualifiedName~AcceptanceV409DevRunnerTests" --blame-hang-timeout 2m -m:1 -nodeReuse:false → 28 pass, 0 fail, 0 skip; gồm 7/7 nghiệm thu K102 nguyên vẹn. |
| Toàn solution | dotnet test AITeam.sln --no-build --blame-hang-timeout 2m -m:1 -nodeReuse:false → Hub 914 pass/10 fail; Core có lỗi File.Replace bị từ chối quyền, Git signal pipe và PowerShell timeout; dừng khi vượt 3 phút. Lượt này trước chỉnh mốc khởi động, chưa xác minh toàn bộ bản cuối. |
| BOM UTF-8 | Đọc byte đầu bằng PowerShell: cả 3 file C# đã sửa/tạo có EF BB BF; mọi lần ghi tiếp đều dùng UTF8Encoding(true). |
| Commit/push | Không chạy lệnh git theo chỉ dẫn Codex; hệ thống tự commit/push, chưa có commit được kiểm. Thông điệp đề nghị: K1-02: OpenCode đứng im 20 phút thì tự dừng, báo rõ lý do. |
- Đổi ProcessRunner.cs: canh stdout/stderr bằng TickCount64 + Interlocked, hủy/dọn bộ canh, giết cây tiến trình, ưu tiên hủy rồi timeout cứng.
- Đổi OpenCodeProvider.cs: bật 20 phút khi run, báo đúng câu đứng im và giữ session; không bật cho provider khác.
- Thêm core.tests/K102IdleWatchdogExtraTests.cs: 2 ca timeout trùng mốc và giữ session/lý do chính xác; cập nhật báo cáo trong task này.
- Tự quyết: tính mốc đứng im sau Start thành công để không tính thời gian khởi động; dùng một worker vì build mặc định lỗi không có chẩn đoán.
- Chưa xác minh: chạy OpenCode thật 20 phút; toàn bộ solution xanh trong môi trường không bị chặn quyền. Không sửa lỗi ngoài phạm vi.

## Review (Claude)

## Kết quả
