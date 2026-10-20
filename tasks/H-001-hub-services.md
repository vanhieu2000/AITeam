# H-001 · AITeam Hub: các dịch vụ đọc / ghi dữ liệu

- **Agent:** A
- **Nhánh:** `agent-a/H-001-hub-services`
- **Ưu tiên:** cao

## Bối cảnh
AITeam Hub là bảng điều khiển web cục bộ của team AI. Thiết kế đầy đủ: `docs/HUB-DESIGN.md`.
Claude đã dựng khung:
- `hub/AITeam.Hub.csproj`;
- `hub/Program.cs`;
- `hub/Models/HubModels.cs` (record);
- `hub/Services/Contracts.cs` (interface);
- `hub/Services/HubPaths.cs`;
- `hub.tests/`.

Agent B đang làm giao diện **song song** dựa trên chính các interface này.

## Mục tiêu
Hiện thực đầy đủ 6 dịch vụ trong `Contracts.cs`, đúng mô tả ở `docs/HUB-DESIGN.md` mục 3, 5, 6, có test đầy đủ.

## Phạm vi (được sửa)
- `hub/Services/`: file mới `TeamConfigService.cs`, `TaskBoardService.cs`, `AgentRunService.cs`, `HealthService.cs`, `VaultService.cs`, `InboxService.cs`.
  - KHÔNG sửa `Contracts.cs`, `HubPaths.cs`, `hub/Models/`.
- `hub/Program.cs`: đăng ký dịch vụ thật (singleton) khi biến môi trường `HUB_FAKE` khác `1`.
- `hub.tests/`: test và `Fixtures/`.

## Yêu cầu chi tiết
- **TeamConfigService**
  - Đọc `team.json`. Ghi đè bằng `team.local.json` cho `projectsRoot`, `workRoot`, `agyExe` (→ `agy.exe`), và `paths.<tên dự án>`.
  - Đường dẫn dự án theo đúng quy tắc của `Resolve-Project` trong `team.ps1`: ưu tiên `paths.<tên>`; nếu không tồn tại thì dùng `path` trong `projects.json`; vẫn không tồn tại thì dùng `projectsRoot\<tên>`.
  - `workDir` mặc định là `workRoot\<tên>`.
  - Khi ghi:
    - giữ nguyên các khóa khác (kể cả `_ghichu`);
    - JSON thụt lề 2, UTF-8 không BOM, tiếng Việt không bị escape (`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`);
    - tạo `.bak` trước khi ghi;
    - đường dẫn riêng của máy (`ProjectsRoot`, `WorkRoot`) ghi vào `team.local.json`, KHÔNG ghi vào `team.json`.
- **TaskBoardService**
  - Đọc bảng markdown đầu tiên trong `tasks/README.md` có cột "Task". Lấy mã + tiêu đề từ link `[T-001 Tiêu đề](file.md)`. Trạng thái theo emoji: 🟡 Đã giao, 🔵 Chờ review, 🔴 Cần sửa, ✅ Đã merge (ví dụ "✅ Đã merge (vòng 1)").
  - `PlanMarkdown` = nội dung từ heading `## Kế hoạch` hoặc `## Hàng chờ` (tìm theo tiền tố) tới hết.
  - Chi tiết task: tách các mục `##`. `Criteria` lấy các dòng checkbox trong mục bắt đầu bằng "Tiêu chí".
  - Ghép thông tin lần chạy từ `IAgentRunService`.
- **AgentRunService**
  - Log nằm ở `<WorkDir>\logs\<Task>-<n>.json`; tên task có thể là `T-001`, `H-002`… Bỏ qua `*.prompt.md`, `*.stderr.txt`, `ask-*`.
  - File rỗng = Running. JSON `status` "SUCCESS" = Success, còn lại = Error.
  - JSON có thể có BOM hoặc chữ tiếng Việt UTF-8: đọc đúng. Log hỏng thì `State = Error`, không ném lỗi.
  - `GetRunning`: WMI `Win32_Process` với `Name='agy.exe'`; lấy Task / Project từ đường dẫn `...\<workDir>\logs\<Task>-<n>.prompt.md` trong `CommandLine`.
  - `Stop(pid)`: chỉ dừng nếu tiến trình đó tên `agy.exe`.
- **HealthService**: các mục như `team.ps1 doctor` (xem hàm `Invoke-Doctor`):
  - git và GitHub CLI (`gh auth status`; gh có thể nằm ở `C:\Program Files\GitHub CLI\gh.exe`);
  - agy: có file + `--version`;
  - skill Claude, khối AITeam trong `~\.claude\CLAUDE.md`;
  - pre-commit hook của từng dự án (`git rev-parse --git-common-dir` + `hooks\pre-commit` chứa chữ "AITeam");
  - dự án có trên máy chưa.
  - `CheckAgyLoginAsync`: chạy agy như `team.ps1`. stdin là file rỗng, KHÔNG để pipe mở (agy sẽ treo). Timeout 2 phút.
  - `InstallClaudeIntegration`: giống `Install-Skill` + `Invoke-Setup` (phần CLAUDE.md, KHÔNG thêm trùng khối).
  - `OpenSwitchAccountWindow`: giống `Invoke-SwitchAccount`.
- **VaultService**
  - Định dạng PHẢI khớp `team.ps1`: `{ "TEN": "<base64 của ProtectedData.Protect(UTF8(value), UTF8("AITeam.vault.v1"), CurrentUser)>" }`.
  - Tên hợp lệ: `^[A-Za-z_][A-Za-z0-9_]*$`.
  - `ExportEnv`: chạy `git -C <dự án> check-ignore -q .env.local`. Nếu chưa bị ignore thì ném lỗi tiếng Việt rõ ràng. Ghi `TEN=giá trị` mỗi dòng, CRLF.
- **InboxService**
  - Dòng `- [ ] yyyy-MM-dd HH:mm · <dự án>: <nội dung>` hoặc `- [x] …`.
  - `Add` nối thêm dòng mới; tạo file kèm tiêu đề `# Hộp thư yêu cầu cho team` nếu chưa có.
  - Nội dung nhiều dòng thì gộp thành 1 dòng.

## Không làm
- Không làm giao diện (`hub/Pages`, `wwwroot`): việc của Agent B.
- Không sửa `team.ps1` và các file cấu hình thật.

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln` 0 warning, `dotnet test AITeam.sln` pass hết.
- [ ] Mỗi dịch vụ có test với thư mục tạm + fixtures. Tối thiểu:
  - config: có / không có `team.local.json`; ghi giữ nguyên khóa lạ + có `.bak`;
  - board: đủ 4 trạng thái, link, kế hoạch, tiêu chí `[x]`/`[ ]`, file task thiếu;
  - runs: SUCCESS, ERROR, file rỗng, JSON hỏng, có BOM, bỏ qua `.prompt.md`;
  - vault: Set → ListNames → giải mã bằng chính công thức của team.ps1 (`ProtectedData.Unprotect` với entropy) → Remove; tên sai bị từ chối; ExportEnv từ chối khi không bị ignore (dùng `git init` trong thư mục tạm);
  - inbox: thêm, đọc, dòng `[x]`, nội dung nhiều dòng.
- [ ] Không test nào đụng file thật của AITeam, `%USERPROFILE%`, hay tiến trình thật (trừ `GetRunning` chỉ đọc).

## Báo cáo của agent
<!-- Agent điền -->

## Review (Claude)
<!-- Claude điền -->
