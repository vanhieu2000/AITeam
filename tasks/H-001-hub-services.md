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
- [x] `dotnet build AITeam.sln` 0 warning, `dotnet test AITeam.sln` pass hết.
- [x] Mỗi dịch vụ có test với thư mục tạm + fixtures. Tối thiểu:
  - config: có / không có `team.local.json`; ghi giữ nguyên khóa lạ + có `.bak`;
  - board: đủ 4 trạng thái, link, kế hoạch, tiêu chí `[x]`/`[ ]`, file task thiếu;
  - runs: SUCCESS, ERROR, file rỗng, JSON hỏng, có BOM, bỏ qua `.prompt.md`;
  - vault: Set → ListNames → giải mã bằng chính công thức của team.ps1 (`ProtectedData.Unprotect` với entropy) → Remove; tên sai bị từ chối; ExportEnv từ chối khi không bị ignore (dùng `git init` trong thư mục tạm);
  - inbox: thêm, đọc, dòng `[x]`, nội dung nhiều dòng.
- [x] Không test nào đụng file thật của AITeam, `%USERPROFILE%`, hay tiến trình thật (trừ `GetRunning` chỉ đọc).

## Báo cáo của agent
### 1. Đã làm gì
- Hiện thực đầy đủ 6 dịch vụ dữ liệu trong `hub/Services/`:
  - `TeamConfigService.cs`: đọc / ghi `team.json` và `team.local.json`, phân giải đường dẫn dự án theo chuẩn `Resolve-Project` trong `team.ps1`, ghi đè đường dẫn máy vào `team.local.json`, bảo toàn các khóa lạ (như `_ghichu`), định dạng JSON thụt lề 2 không escape tiếng Việt và tạo file sao lưu `.bak`.
  - `AgentRunService.cs`: quét và phân tích log agent từ thư mục `<WorkDir>\logs`, phân biệt trạng thái Running (file rỗng), Success (`status == "SUCCESS"`), Error (kể cả khi JSON hỏng); hỗ trợ UTF-8 có hoặc không có BOM; lọc bỏ các file `.prompt.md`, `.stderr.txt`, `ask-*`; truy vấn tiến trình ngầm `agy.exe` bằng WMI và dừng tiến trình an toàn đúng tên `agy.exe`.
  - `TaskBoardService.cs`: phân tích bảng markdown trong `tasks/README.md`, nhận diện 4 trạng thái task qua emoji (🟡, 🔵, 🔴, ✅), trích xuất link task, tách nội dung `PlanMarkdown`, trích xuất các mục `##` và checklist tiêu chí trong file task, ghép lịch sử lần chạy agent; xử lý an toàn khi task bị thiếu file markdown.
  - `HealthService.cs`: kiểm tra tình trạng hệ thống theo chuẩn `team.ps1 doctor` (git, gh auth status, agy exe & version, Claude integration skill + CLAUDE.md, pre-commit hook chặn bí mật, sự tồn tại dự án trên máy); hỗ trợ kiểm tra đăng nhập agy với stdin đóng tránh treo pipe; cài đặt tích hợp Claude và mở cửa sổ đổi tài khoản PowerShell.
  - `VaultService.cs`: quản lý bí mật cục bộ với DPAPI Windows (`CurrentUser`) và entropy UTF-8 `AITeam.vault.v1`, định dạng JSON `{ "TEN": "<base64>" }` tương thích 2 chiều 100% với `team.ps1`; kiểm tra regex tên bí mật; `ExportEnv` kiểm tra trạng thái ignore của `.env.local` qua git trước khi xuất file CRLF.
  - `InboxService.cs`: đọc và ghi yêu cầu vào `inbox.md`, tự sinh tiêu đề `# Hộp thư yêu cầu cho team` nếu chưa có file, gộp nội dung nhiều dòng thành 1 dòng theo đúng quy chuẩn.
- Đăng ký 6 dịch vụ singleton trong `hub/Program.cs` khi `HUB_FAKE != "1"`.
- Tạo thư mục fixtures và xây dựng bộ unit test toàn diện cho cả 6 dịch vụ trên thư mục tạm cô lập (`TestEnvironment`), hoàn toàn không can thiệp hay sửa đổi file thật và registry của hệ thống.

### 2. File nào đã đổi
- Tạo mới các dịch vụ:
  - `hub/Services/TeamConfigService.cs`
  - `hub/Services/AgentRunService.cs`
  - `hub/Services/TaskBoardService.cs`
  - `hub/Services/HealthService.cs`
  - `hub/Services/VaultService.cs`
  - `hub/Services/InboxService.cs`
- Cập nhật DI:
  - `hub/Program.cs`
- Thêm unit test & fixtures:
  - `hub.tests/TestEnvironment.cs`
  - `hub.tests/TeamConfigServiceTests.cs`
  - `hub.tests/AgentRunServiceTests.cs`
  - `hub.tests/TaskBoardServiceTests.cs`
  - `hub.tests/HealthServiceTests.cs`
  - `hub.tests/VaultServiceTests.cs`
  - `hub.tests/InboxServiceTests.cs`
  - `hub.tests/Fixtures/team.json`
  - `hub.tests/Fixtures/team.local.json`
  - `hub.tests/Fixtures/projects.json`
  - `hub.tests/Fixtures/tasks/README.md`
  - `hub.tests/Fixtures/tasks/T-001-sample.md`
  - `hub.tests/Fixtures/logs/` (T-001-1.json, T-001-2.json, T-001-3.json, T-001-4.json, T-001-5.json, T-001-1.prompt.md, T-001-1.stderr.txt, ask-20261020-120000.json)
- Cập nhật task report:
  - `tasks/H-001-hub-services.md`

### 3. Đã kiểm tra thế nào
- Build: `dotnet build AITeam.sln`
```
  Determining projects to restore...
  All projects are up-to-date for restore.
  AITeam.Hub -> D:\AITeam-work\AITeam\a-h-001\hub\bin\Debug\net10.0-windows\AITeam.Hub.dll
  AITeam.Hub.Tests -> D:\AITeam-work\AITeam\a-h-001\hub.tests\bin\Debug\net10.0-windows\AITeam.Hub.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```
- Test: `dotnet test AITeam.sln`
```
Test run for D:\AITeam-work\AITeam\a-h-001\hub.tests\bin\Debug\net10.0-windows\AITeam.Hub.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    21, Skipped:     0, Total:    21, Duration: 1 s - AITeam.Hub.Tests.dll (net10.0)
```

### 4. Còn gì chưa chắc
- Không có vấn đề tồn đọng. Toàn bộ 6 dịch vụ đã thỏa mãn đúng hợp đồng interface, tài liệu `docs/HUB-DESIGN.md` và vượt qua 21 test case.

## Review (Claude)
<!-- Claude điền -->
