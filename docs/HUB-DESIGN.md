# Thiết kế: AITeam Hub — bảng điều khiển team AI

> Người thiết kế: Claude (quản lý). Agent chỉ hiện thực hóa theo tài liệu này; muốn đổi gì thì ghi vào báo cáo, không tự đổi.

## 1. Mục tiêu
HieuDV mở một trang web trên máy (hoặc mở trong HDV Tool) để:
- theo dõi mọi dự án, task, agent đang chạy, lịch sử chạy;
- xem / sửa cấu hình team, kiểm tra tài khoản, đổi tài khoản Antigravity;
- quản lý bí mật cục bộ: chỉ thấy **tên**, không bao giờ thấy giá trị;
- gửi yêu cầu cho team vào **hộp thư** để Claude xử lý.

Không thay Claude: thiết kế, giao việc và review vẫn do Claude làm. Hub để **xem, cấu hình, nhắn việc**.

## 2. Hình thức và công nghệ (đã chốt)
| Mục | Chọn | Lý do |
|---|---|---|
| Dạng | Web app chạy trên máy, `http://127.0.0.1:5057`. **Chỉ nghe localhost** | Dùng được ở mọi máy, mở trong trình duyệt hoặc nhúng vào HDV Tool bằng WebView2 |
| Backend | ASP.NET Core (.NET 10), Minimal API + Razor Pages | Cùng hệ sinh thái với HDV Tool; .NET 10 SDK đã có |
| Giao diện | Razor + HTMX (tải sẵn trong `wwwroot/lib`, không dùng CDN) + CSS thuần, sáng / tối | Không cần build JS, dễ cho agent làm, nhẹ |
| Dữ liệu | Đọc **trực tiếp file** của AITeam và repo dự án. Không có CSDL riêng | Một nguồn sự thật duy nhất (git + file) |
| Thư mục | `D:\AITeam\hub\` (trong repo AITeam), chạy bằng `team.ps1 hub` | Máy nào clone AITeam là có Hub |

## 3. Nguồn dữ liệu (chỉ đọc, trừ khi ghi rõ)
| Dữ liệu | Lấy từ |
|---|---|
| Cấu hình team | `team.json` + ghi đè `team.local.json` (cùng quy tắc với `team.ps1` `Get-Config`) |
| Dự án | `projects.json`; đường dẫn thật theo `team.local.json.paths`, nếu không có thì `projectsRoot\<tên>` |
| Bảng việc | Bảng markdown trong `<dự án>\tasks\README.md`, cột: Task (link), Agent, Nhánh, Trạng thái. Trạng thái nhận theo emoji 🟡 🔵 🔴 ✅ |
| Chi tiết task | `<dự án>\tasks\T-xxx-*.md`: tách các mục `##` (Bối cảnh, Mục tiêu, Phạm vi, Tiêu chí, Báo cáo của agent, Review (Claude)); checkbox `- [ ]` / `- [x]` |
| Lần chạy agent | `<workDir>\logs\<Task>-<n>.json`: JSON một dòng gồm `conversation_id, status, response, error, duration_seconds, usage.total_tokens`. File rỗng = đang chạy. Bỏ qua `*.prompt.md`, `*.stderr.txt` |
| Agent đang chạy | Tiến trình `agy.exe` (WMI `Win32_Process`): PID, giờ bắt đầu, dòng lệnh. Task lấy từ đường dẫn `...\logs\T-xxx-n.prompt.md` trong dòng lệnh |
| Tình trạng máy | Giống `team.ps1 doctor`: git, gh (`gh auth status`), agy (file tồn tại + `--version`; nút "Kiểm tra đăng nhập" mới chạy ping thật), skill `~\.claude\skills\aiteam\SKILL.md`, khối AITeam trong `~\.claude\CLAUDE.md`, pre-commit hook của từng dự án |
| Bí mật | `%USERPROFILE%\.aiteam\vault.json`: `{ "TEN": "<base64 DPAPI CurrentUser, entropy UTF8 'AITeam.vault.v1'>" }`. **Phải tương thích 2 chiều với `team.ps1`** |
| Hộp thư | `D:\AITeam\inbox.md` (ghi) |

## 4. Màn hình
Bố cục chung: thanh bên trái (logo "AITeam", menu), nội dung bên phải, nút sáng / tối, tiếng Việt.
Màu: trung tính, nhấn xanh dương `#2563eb`. Trạng thái: 🟡 vàng `#d97706`, 🔵 xanh `#2563eb`, 🔴 đỏ `#dc2626`, ✅ xanh lá `#16a34a`.

1. **Tổng quan** `/`
   - Hàng thẻ số liệu: số dự án · task đang làm · chờ review · agent đang chạy.
   - Danh sách dự án (thẻ): tên, stack, đếm task theo trạng thái, hoạt động gần nhất, link GitHub.
   - Khối "Agent đang chạy": task, dự án, chạy được bao lâu. Tự làm mới mỗi 15 giây (HTMX `hx-trigger="every 15s"`).
   - Khối "Tình trạng máy": các dòng OK / thiếu, kèm câu hướng dẫn sửa giống `doctor`.
2. **Dự án** `/p/{tên}`: bảng kanban 4 cột theo trạng thái.
   - Mỗi thẻ: mã task, tiêu đề, agent, số lần chạy, lần chạy cuối (thời lượng, token, trạng thái).
   - Tab phụ: "Kế hoạch / hàng chờ" (hiển thị mục đó của `tasks/README.md`).
3. **Task** `/p/{tên}/t/{mã}`: hiển thị các mục của file task dạng markdown.
   - Tiêu chí có checkbox (chỉ xem).
   - Báo cáo của agent, Review (Claude).
   - Danh sách lần chạy: thời lượng, token, trạng thái, nút "Xem kết quả" (mở `response`).
   - Link nhánh trên GitHub.
4. **Agent** `/agents`: bảng tiến trình agy đang chạy.
   - Nút **Dừng**: hỏi xác nhận, rồi dừng đúng PID đó.
   - Lịch sử 20 lần chạy gần nhất của mọi dự án.
5. **Cài đặt** `/settings`: form sửa
   - `team.json`: `agy.model`, `review.maxRounds`, chuyên môn của từng developer;
   - `team.local.json`: `projectsRoot`, `workRoot`, đường dẫn riêng của từng dự án;
   - `projects.json`: `build`, `test`, `stack`, `areas` của từng dự án.
   - Ghi file JSON thụt lề 2, UTF-8 không BOM. Kiểm tra hợp lệ trước khi ghi; giữ bản sao `.bak`.
6. **Tài khoản** `/accounts`
   - **GitHub**: tài khoản đang đăng nhập (`gh auth status`), owner trong `team.json`.
   - **Antigravity**: nút "Kiểm tra đăng nhập" (ping ≤ 2 phút) và nút "Đổi tài khoản". Nút đổi mở cửa sổ PowerShell chạy agy, kèm hướng dẫn `/logout` → đăng nhập → `/quit`. Hiện danh sách tên tài khoản trong `team.json` (chỉ là nhãn).
   - **Claude**: skill đã cài chưa, khối CLAUDE.md có chưa; nút "Cài / sửa" (làm giống `team.ps1 setup`, phần skill + CLAUDE.md).
7. **Bí mật** `/secrets`
   - Danh sách TÊN.
   - Form thêm: tên + ô mật khẩu → mã hóa DPAPI, ghi vault.
   - Nút xóa có xác nhận.
   - Form "Xuất .env.local cho dự án": chọn dự án + các tên. Từ chối nếu `.env.local` chưa bị gitignore (`git check-ignore`).
   - **Không có API nào trả về giá trị bí mật.**
8. **Hộp thư** `/inbox`
   - Ô nhập yêu cầu cho team (chọn dự án hoặc "dự án mới"), nút Gửi.
   - Nội dung được thêm vào `inbox.md` dạng: `- [ ] {thời gian} · {dự án}: {nội dung}`.
   - Hiện danh sách yêu cầu: chưa xử lý / đã xử lý. Claude đánh dấu `[x]` khi đã nhận việc.

## 5. Bảo mật
- Chỉ nghe `127.0.0.1`. Không bật CORS.
- Mọi POST dùng antiforgery token.
- Bí mật: không ghi log, không trả về, không đưa vào HTML. Ô nhập dùng `type=password autocomplete=off`.
- Không chạy lệnh tùy ý; chỉ các hành động trong mục 4.
  - Dừng PID: chỉ cho dừng nếu tiến trình là `agy.exe`.
  - Ghi file: chỉ `team.json`, `team.local.json`, `projects.json`, `inbox.md`, vault, `.env.local`.

## 6. Cấu trúc code
```
hub/
  AITeam.Hub.csproj           (net10.0, Web SDK)
  Program.cs                  (đăng ký dịch vụ, Kestrel 127.0.0.1:5057, Razor Pages, endpoint HTMX)
  Services/
    HubPaths.cs               (gốc AITeam: thư mục cha của hub/, hoặc biến môi trường AITEAM_HOME)
    TeamConfigService.cs      (đọc / ghi team.json, team.local.json, projects.json, có merge)
    TaskBoardService.cs       (đọc bảng tasks/README.md + file task → TaskCard, TaskDetail)
    AgentRunService.cs        (đọc logs → AgentRun; liệt kê / dừng tiến trình agy)
    HealthService.cs          (kiểm tra máy giống doctor)
    VaultService.cs           (DPAPI, tương thích team.ps1)
    InboxService.cs
  Models/                     (record: ProjectInfo, TaskCard, TaskDetail, TaskSection, AgentRun, RunningAgent, HealthItem, InboxItem)
  Pages/                      (Index, Project, Task, Agents, Settings, Accounts, Secrets, Inbox, Shared/_Layout)
  wwwroot/                    (css/hub.css, lib/htmx.min.js)
hub.tests/
  AITeam.Hub.Tests.csproj     (xUnit)
  Fixtures/                   (tasks/README.md, task mẫu, log mẫu: thành công, lỗi, rỗng = đang chạy)
```
Dịch vụ không phụ thuộc giao diện; mọi dịch vụ đọc file đều nhận đường dẫn gốc qua constructor, để test được bằng thư mục tạm.

## 7. Kế hoạch giao việc
| Giai đoạn | Task | Agent | Nội dung |
|---|---|---|---|
| 1 | H-001 | A | Khung `hub/` + `hub.tests/`, toàn bộ `Services/` + `Models/` mục 6, test bằng fixtures (vault: test mã hóa → giải mã; định dạng khớp `team.ps1`) |
| 1 | H-002 | B | `_Layout`, CSS sáng / tối, trang Tổng quan / Dự án / Task với dữ liệu giả theo đúng record ở mục 6 (cùng chạy với H-001 vì chỉ chạm `Pages/`, `wwwroot/`) |
| 2 | H-003 | B | Nối các trang vào dịch vụ thật; trang Agent, Hộp thư; tự làm mới bằng HTMX |
| 2 | H-004 | A | Trang Cài đặt, Tài khoản, Bí mật + antiforgery; lệnh `team.ps1 hub` (build nếu cần, chạy, mở trình duyệt) |
| 3 | H-005 | B | Module "AITeam Hub" trong HDV Tool (WebView2 trỏ `http://127.0.0.1:5057`, nút mở Hub nếu chưa chạy) — dự án HDVTool |

Tiêu chí chung: build 0 warning, test pass. Claude review từng trang bằng ảnh chụp, đối chiếu với tài liệu này.
