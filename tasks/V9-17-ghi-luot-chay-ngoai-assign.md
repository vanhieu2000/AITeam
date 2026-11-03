# V9-17 · Ghi số đo cho lượt chạy ngoài `assign` (OpenCode / Codex chạy trực tiếp)

- **Agent:** O
- **Nhánh:** `agent-o/V9-17-ghi-luot-chay-ngoai-assign`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~250 dòng
- **Yêu cầu:** R-012 (cải tiến 2 sau đánh giá)
- **Thời gian tối đa:** 30 phút
- **Độ khó:** M

## Tóm tắt
- Không có chức năng mới cho người dùng Hub. Đây là công cụ đo cho quản lý.
- Thêm lệnh `team.ps1 log-run`: đọc log của một lượt OpenCode (`opencode run --format json`) hoặc Codex (`codex exec`) chạy trực tiếp, rồi ghi một file số đo vào `metrics/runs/` giống lượt chạy qua `assign`.
- Vì sao: R-012 có 30 task nhưng `request-report` chỉ thấy 10 task / 14 lượt, vì Codex và OpenCode được chạy trực tiếp, không qua `assign`, nên không có số đo. Đánh giá phải tính tay.

## Đọc (CHỈ những file này)
- `team.ps1`: khối `param(...)` (dòng ~41), `Get-RunRecord`, `Save-RunMetrics`, `Invoke-RequestReport`, `Get-RunDeltas`, bảng lệnh `switch ($Command)` cuối file, phần `help` đầu file.
- `core/AITeam.Cli/Program.cs`: bảng lệnh và một lệnh mẫu đọc file trả JSON (`ReviewFindings` / `DiagnoseRun`).
- `core/AITeam.Core/Metrics/FindingParser.cs` (mẫu lớp tĩnh thuần, dễ test).
- `core.tests/V910TokenMetricsTests.cs` (mẫu test số đo + harness PowerShell).

## Bối cảnh
- `request-report` và `staff-report` chỉ đọc `metrics/runs/*.json`. Hiện chỉ `assign` gọi `Get-RunRecord` + `Save-RunMetrics`.
- Theo QUY-TRINH v5.1: logic đã nằm trong C# thì `team.ps1` chỉ gọi CLI. Phần ĐỌC log phải viết bằng C# (có test); `team.ps1` chỉ gọi CLI rồi ghép bản ghi và gọi `Save-RunMetrics` có sẵn.

## Mục tiêu
1. **C#** `core/AITeam.Core/Metrics/ExternalRunLog.cs`: lớp tĩnh `ExternalRunLog.Parse(string dev, string logPath)` trả bản ghi:
   `durationSec` (int), `tokensKnown` (bool), `inputTokens`, `outputTokens`, `totalTokens` (long?, null khi không biết), `status` ("SUCCESS" | "KHÔNG CÓ KẾT QUẢ"), `at` (giờ kết thúc, ISO "s"), `hasOutput` (bool).
   - **opencode** (JSON Lines, mỗi dòng 1 sự kiện):
     - `durationSec` = (timestamp lớn nhất − nhỏ nhất) / 1000, làm tròn xuống; `timestamp` là mili-giây Unix ở cấp trên cùng của mỗi dòng.
     - Token: cộng mọi sự kiện `"type":"step_finish"`: `part.tokens.input` → input, `part.tokens.output + part.tokens.reasoning` → output; `totalTokens` = input + output (KHÔNG cộng `cache.read`). Không có `step_finish` nào ⇒ `tokensKnown=false`, các trường token null.
     - `hasOutput` / `status=SUCCESS` khi có ít nhất một sự kiện `"type":"text"` có `part.text` không rỗng.
     - Dòng hỏng / không phải JSON ⇒ bỏ qua dòng đó, không ném lỗi.
   - **codex** (văn bản `codex exec`):
     - Token: dòng `tokens used` rồi dòng kế tiếp là số có dấu phẩy ngăn nghìn (`37,701`) ⇒ `totalTokens`=37701, input/output null, `tokensKnown=true`. Không thấy ⇒ `tokensKnown=false`.
     - `durationSec` = LastWriteTime − CreationTime của file (log không có giờ bắt đầu). Ghi chú điều này trong comment.
     - `hasOutput` = có dòng `codex` (nguyên dòng) mà sau nó còn chữ.
   - `dev` khác hai giá trị trên ⇒ `ArgumentException`. File không có ⇒ `FileNotFoundException`.
2. **CLI** `parse-run-log --dev <opencode|codex> --log <đường dẫn>` in JSON bản ghi trên (camelCase), mã thoát 0; lỗi tham số ⇒ mã 2 qua `catch` có sẵn.
3. **team.ps1** lệnh `log-run`:
   ```
   .\team.ps1 log-run -Project AITeam -Task H8-12 -Dev opencode -Log <file.jsonl> [-Path <worktree>]
   ```
   - Thêm tham số `[string] $Log = ""` vào `param`. Thiếu `-Project`, `-Task`, `-Dev` (chỉ `opencode` | `codex`) hoặc `-Log` không tồn tại ⇒ `throw` thông báo tiếng Việt.
   - Gọi `& (Get-AITeamExe) parse-run-log --dev $Dev --log $Log`, mã thoát ≠ 0 ⇒ `throw`.
   - Ghép bản ghi cùng khóa với `Get-RunRecord` (xem file mẫu `metrics/runs/*.json`): `task` = mã ngắn (`H8-12`, cắt theo `^[A-Z]\d+-\d+`), `agent` = `O` (opencode) / `C` (codex), `model` = `opencode-deepseek` / `codex`, `machine`, `project`, `request` (đọc từ file task như `Get-RunRecord`), `linesAdded/linesRemoved/filesChanged` đếm như `Get-RunRecord` khi có `-Path` (không có ⇒ 0), `verifyOk=$null`, `autoRetry=$false`, `conversationId=""` (BẮT BUỘC rỗng: `Get-RunDeltas` trừ dồn theo conversationId, lượt OpenCode không cộng dồn).
   - `run` = số lượt lớn nhất đã có trong `metrics/runs` cho cùng `project`+`task` cộng 1. `log` = `"<task>-<run>.json"` (để tên file số đo không trùng).
   - Chống ghi trùng: đã có bản ghi cùng `project`, `task`, `machine` và cùng `sourceLog` (thêm khóa `sourceLog` = tên file log gốc + kích thước byte) ⇒ báo "Đã ghi lượt này" màu vàng, không ghi.
   - Gọi `Save-RunMetrics` có sẵn (tự commit + push số đo). Cuối cùng in một dòng tóm tắt: task, phút, token.
   - Thêm dòng vào phần help đầu file.

## Phạm vi (được sửa những gì)
- `core/AITeam.Core/Metrics/ExternalRunLog.cs` (mới)
- `core/AITeam.Cli/Program.cs`
- `team.ps1`
- `core.tests/V917ExternalRunLogTests.cs` (mới)
- `core.tests/fixtures/run-logs/` (mới)
- `tasks/V9-17-ghi-luot-chay-ngoai-assign.md`

## Không làm
- Không sửa `Invoke-RequestReport`, `Invoke-StaffReport`, `Get-RunDeltas`, `Save-RunMetrics`, `Get-RunRecord`.
- Không đọc thư mục log tự động / quét hàng loạt (lệnh nhận đúng một file).
- Không viết lại bộ đọc log bằng PowerShell.
- Không sửa assert của test cũ.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- Log là dữ liệu không tin cậy, có thể chứa prompt và nội dung file: KHÔNG chép `part.text`, `state`, nội dung lệnh vào bản ghi số đo — chỉ số và trạng thái.
- File log lớn (log thật tới vài MB): đọc theo dòng (`File.ReadLines`), không nạp cả file vào một chuỗi; dòng > 1 MB bỏ qua.
- `-Log` là đường dẫn do người gõ: chặn reparse point / symlink như `Add-RunFlagsJournal` đang làm.
- Bản ghi phải giữ đủ khóa cũ để `request-report` và Hub (`PerformanceService`) đọc được; thêm khóa mới (`sourceLog`) là được.
- [x] File cho CLI: đường dẫn qua tham số, không qua stdin.
- [x] Hàm dùng (`Save-RunMetrics`, `Get-AITeamExe`, `Find-TaskFile`) đã có trên main.

## Dữ liệu mẫu thật
- OpenCode: dạng sự kiện lấy từ log thật `oc-H8-15.jsonl` (03/11, opencode 1.19.5):
  ```
  {"type":"step_start","timestamp":1793702722212,"sessionID":"ses_…","part":{"type":"step-start", …}}
  {"type":"tool_use","timestamp":1793702724818,"sessionID":"ses_…","part":{"type":"tool","tool":"bash","state":{…}}}
  {"type":"step_finish","timestamp":1793702730221,"sessionID":"ses_…","part":{"type":"step-finish","reason":"tool-calls","tokens":{"total":23280,"input":21312,"output":164,"reasoning":12,"cache":{"write":0,"read":1792}},"cost":0}}
  {"type":"text","timestamp":1793703397865,"sessionID":"ses_…","part":{"type":"text","text":"…","time":{"start":1793703395225,"end":1793703397863}}}
  ```
- Codex: cuối log `codex exec` 0.166.0 thật (`b-h7-02.codex.log`):
  ```
  codex
  - Chưa thể thực hiện: …
  tokens used
  37,701
  ```
- Agent tự dựng fixture NHỎ (≤ 30 dòng) trong `core.tests/fixtures/run-logs/` theo đúng dạng trên, nội dung giả — KHÔNG chép log thật (có prompt).

## Test nghiệm thu
`core.tests/V917ExternalRunLogTests.cs` (agent viết, Claude review kỹ — không được có nhánh `return` sớm):
- OpenCode 3 `step_finish` ⇒ input/output/total đúng (output gồm reasoning, không gồm cache).
- OpenCode không có `step_finish` ⇒ `tokensKnown=false`, token null.
- OpenCode có dòng hỏng giữa file ⇒ vẫn đọc các dòng còn lại.
- OpenCode không có `text` ⇒ `status="KHÔNG CÓ KẾT QUẢ"`.
- `durationSec` = (max − min timestamp) / 1000.
- Codex `tokens used` / `37,701` ⇒ 37701; không có dòng đó ⇒ `tokensKnown=false`.
- `dev="agy"` ⇒ ArgumentException; file không có ⇒ FileNotFoundException.
- Bản ghi JSON không chứa chuỗi trong `part.text` của fixture (đặt một chuỗi đánh dấu, assert không xuất hiện).
- Harness PowerShell (theo mẫu V910): `log-run` hai lần cùng log ⇒ chỉ 1 file trong `metrics/runs` tạm; `run` tăng khi đã có lượt cũ của cùng task; `conversationId` rỗng; `agent`=`O`. Dùng thư mục tạm, KHÔNG ghi vào `metrics/runs` thật, giả `Save-RunMetrics` để không commit/push.

## Tiêu chí hoàn thành
- `dotnet build AITeam.sln -warnaserror -nodeReuse:false` 0 warning; toàn bộ test xanh.
- Chạy thật trên máy: `log-run` với một log OpenCode của R-012 trong thư mục tạm cho ra bản ghi có phút/token khớp số Claude đo tay (H8-15: ~11 phút; Claude đo input+output ≈ 78k, thêm reasoning thì cao hơn chút).
- Giữ BOM UTF-8 cho `team.ps1` và file `.cs`.
- Báo cáo agent ≤ 15 dòng.
- Commit: `V9-17: log-run ghi số đo lượt chạy ngoài assign (OpenCode)`.

## Báo cáo của agent

## Báo cáo của agent — OpenCode
- Đã làm: thêm `ExternalRunLog.Parse` (opencode JSONL + codex text), lệnh CLI `parse-run-log`, lệnh `team.ps1 log-run` ghép bản ghi, chống ghi trùng theo `sourceLog`, run tăng dần, `conversationId` rỗng, chỉ gọi CLI để đọc log.
- File: `core/AITeam.Core/Metrics/ExternalRunLog.cs` (mới), `core/AITeam.Cli/Program.cs`, `team.ps1`, `core.tests/V917ExternalRunLogTests.cs` (mới), `core.tests/Fixtures/run-logs/*` (mới), file task này.
- Build: `dotnet build AITeam.sln -warnaserror -nodeReuse:false` → 0 warning, 0 error.
- Test: `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m` → 837/837 (hub) + 949/949 (core), 0 đỏ.
- Test mới: 12/12 xanh, gồm harness PowerShell (gọi `Invoke-LogRun` 2 lần cùng log ⇒ 1 file, run=4, agent=O, conversationId rỗng) trên thư mục tạm với `Save-RunMetrics` giả.
- Chưa xác minh: chưa chạy `team.ps1 log-run` với log OpenCode thật R-012 trên máy (không có file log trong repo; công cụ bị giới hạn lệnh), phần đọc đã test bằng fixture đúng dạng.
- Quyết định tự chịu: `at` của opencode lấy mốc timestamp lớn nhất (giờ địa phương); codex lấy LastWriteTime; token reasoning cộng vào output, cache.read không cộng.

### Vòng 2 — OpenCode
- Sửa `Invoke-LogRun`: `inputTokens`/`outputTokens`/`totalTokens` giữ `$null` khi CLI trả null (Codex chỉ biết tổng), chỉ ép `[long]` khi có giá trị.
- Thêm test harness `PowerShell_log_run_keeps_null_tokens_for_codex`: `-Dev codex` + `codex-tokens.log` ⇒ `inputTokens`/`outputTokens` là JSON null, `totalTokens`=37701, `agent`=`C`.
- Build 0 warning; test 837/837 (hub) + 950/950 (core), 0 đỏ.

## Review (Claude)
