# AITeam V5 — Văn phòng mini: giám sát + điều khiển trên web, MCP, AI Accounts, Chat Bridge

Người viết: Claude (quản lý team). 25/10/2026. Yêu cầu: R-005 (bản "AITeam V2 — Hybrid Web / Desktop / MCP" + "nâng web AITeam thành văn phòng mini, giám sát và điều khiển trên đó").
Mockup để duyệt: `docs/design/v5-van-phong-mini-mockup.html`. Chưa viết code nào ngoài PoC (mục E).

---

## A. AS-IS — AITeam hiện tại (đã đọc mã + chạy thật)

| Phần | Công nghệ | Đang làm được | Ghi chú |
|---|---|---|---|
| Điều phối | `team.ps1` (PowerShell 5.1, ~1.700 dòng) | assign (worktree / agent, tối đa 2 lượt), cổng KT `verify`, review AI tự động, merge (clone sạch), xin duyệt, phiếu R-xxx, số đo, đánh giá nhân viên, hook chặn bí mật + chặn push main | Chạy được 2 máy (nhà / công ty) nhờ git |
| Lõi | C# `AITeam.Core` + `aiteam.exe` (.NET 10) | Provider agy / Codex, định tuyến (nghỉ khi hết hạn mức, cấm tự duyệt), `aiteam review`, `aiteam run`, `JobStore` (trạng thái job — **chưa nối vào assign**) | 129 test |
| Web | ASP.NET Core Razor + HTMX, chỉ `127.0.0.1:5057` | Tổng quan (biểu đồ thời gian), Giao việc (luồng phiếu + hộp thư), Cần anh duyệt, Đội ngũ, Quy trình & cải tiến, Provider AI, Dự án / Task (tab dòng thời gian, đánh giá, review AI), Agent, Tài khoản Antigravity, Bí mật (DPAPI), Cài đặt | 289 test. **Hầu như chỉ đọc**: ghi duy nhất hộp thư, quyết định duyệt, bí mật, cài đặt, "hết nghỉ" |
| Lưu trữ | File trên git: `tasks/`, `requests/`, `approvals/`, `metrics/runs/`, `staff/`, `improvements/`; theo máy: `<workDir>\logs`, `.state/providers.json`, `%USERPROFILE%\.aiteam\vault.json` (DPAPI) | Nguồn sự thật dùng chung 2 máy | Không có CSDL |
| AI | agy (developer A/B), Codex CLI (review chỉ đọc + developer dự phòng C), Claude = phiên Claude Code desktop (quản lý / QC, không tự động) | Đăng nhập bằng CLI chính thức, không API trả tiền theo token | ChatGPT / Gemini / DeepSeek / Grok: **chưa có** |

## B. GAP — so với yêu cầu

| # | Yêu cầu | Hiện trạng | Thiếu |
|---|---|---|---|
| G1 | Giám sát trực tiếp | Biết task chạy / xong, phút, kết quả cổng KT | Agent im lặng tới khi xong (agy chế độ print); không có tiến độ giữa chừng; không có "bàn làm việc" từng thành viên |
| G2 | Điều khiển trên web | Phải nhờ Claude chạy `team.ps1` | Giao task / dừng / giao lại / chạy lại kiểm tra / chạy Codex từ web |
| G3 | MCP Gateway | Không có | Công cụ `get_task`, `post_message`, `get_test_results`… cho agent |
| G4 | Lịch sử tin nhắn chuẩn hóa | Rải rác: log agy, phiếu, file task | Dòng tin nhắn theo task (agent, cổng KT, reviewer, Claude, anh) |
| G5 | AI Accounts | Trang Antigravity + Provider (agy, codex) | ChatGPT / Gemini / DeepSeek / Grok / Claude; trạng thái chuẩn 8 mức; metadata `%LOCALAPPDATA%\AITeam` |
| G6 | Chat Bridge + manual handoff | Không có | Chuẩn bị lời nhắn → Copy / Mở trang → dán kết quả + nguồn → lưu |
| G7 | QA | Cổng KT (build + test thật) + Codex review + Claude review tay | Giới hạn vòng review (3) có đếm; E2E web (Playwright) cho Hub; desktop UI (FlaUI) cho app WinForms — sau |
| G8 | Audit log | Git log + nhật ký phiếu | Nhật ký thao tác điều khiển từ web (ai bấm gì, lúc nào, kết quả) |

## C. TO-BE — kiến trúc (giữ công nghệ hiện có, KHÔNG viết lại)

```
            Anh (trình duyệt, 127.0.0.1)
                     │
     ┌───────────── Hub "Văn phòng mini" (ASP.NET Razor + HTMX) ─────────────┐
     │ Bàn làm việc từng thành viên · Phòng họp (phiếu R-xxx) · Phòng nghiên  │
     │ cứu (manual handoff) · Cần anh duyệt · Nhật ký thao tác                │
     └───────┬──────────────────────┬────────────────────────┬──────────────┘
             │ đọc file / số đo     │ CommandService          │ đọc tin nhắn
             │                      │ (danh sách lệnh cho phép)│
             ▼                      ▼                         ▼
   tasks/ requests/ approvals/   team.ps1 assign/stop/…   <workDir>\logs\<task>.messages.jsonl
   metrics/ research/ (git)            │                         ▲
                                       ▼                         │ aiteam_post_message
                         agy / Codex trong worktree ──MCP stdio──► aiteam mcp --task T
                         (.agents/mcp_config.json do assign đặt)  (AITeam MCP Gateway)
```

### C1. MCP Gateway (`aiteam mcp`) — đã PoC chạy thật (mục E)
- Máy chủ MCP **stdio** trong `aiteam.exe` (gói chính thức `ModelContextProtocol` 2.3.0, MIT), mỗi lần chạy GẮN với 1 task: `aiteam mcp --project X --task T --role developer`.
- Công cụ (theo vai trò, agent chỉ thấy task của mình):
  | Công cụ | developer | reviewer | Ghi chú |
  |---|---|---|---|
  | `aiteam_get_task` | ✓ | ✓ | nội dung file task của chính task |
  | `aiteam_post_message` | ✓ | ✓ | tiến độ / câu hỏi ⇒ `logs\<task>.messages.jsonl`; tối đa 2.000 ký tự, 60 tin / lượt |
  | `aiteam_get_test_results` | ✓ | ✓ | báo cáo cổng KT gần nhất |
  | `aiteam_get_artifacts` | ✓ | ✓ | danh sách file log / review của task (chỉ tên + nội dung đã giới hạn) |
  | `aiteam_submit_review` | — | ✓ | kết luận có cấu trúc (thay đọc stdout) — sau |
  | `aiteam_list_tasks` | — | — | chỉ cho Claude / Hub, không cho agent |
- `assign` đặt `<worktree>\.agents\mcp_config.json` (agy) và `-c mcp_servers.aiteam…` (Codex, ghi đè theo lần gọi, KHÔNG sửa `~/.codex/config.toml`); thêm vào `.git/info/exclude` để không lên git.
- Không công cụ nào chạy lệnh, đọc bí mật, đọc file ngoài task, hay gọi mạng.

### C2. Văn phòng mini (trang chủ mới của Hub)
- **Bàn làm việc**: Claude (quản lý), Minh (A), Lan (B), Codex (C), Cổng kiểm tra, Phòng nghiên cứu (ChatGPT · Gemini · DeepSeek · Grok). Mỗi bàn: trạng thái (rảnh / đang làm task X · phút / chờ review / nghỉ tới giờ), 3 tin nhắn tiến độ mới nhất (MCP), nút điều khiển.
- **Điều khiển** (CommandService, chỉ `127.0.0.1` + antiforgery + xác nhận):
  | Nút | Lệnh thật | Điều kiện |
  |---|---|---|
  | Giao task | `team.ps1 assign -Project P -Task T [-Dev codex]` | task ⬜ / 🔴 trên bảng việc, agent rảnh |
  | Dừng | `team.ps1 stop -Task T` | task đang chạy trên máy này |
  | Giao lại kèm góp ý | `team.ps1 assign … -Resume -Feedback "…"` | góp ý ≤ 2.000 ký tự |
  | Chạy lại cổng KT | `team.ps1 verify -Project P -Task T` | có worktree |
  | Merge | `team.ps1 merge …` | CHỈ khi có mục duyệt `approved` đúng commit (như hiện nay) |
  - Danh sách lệnh CỐ ĐỊNH, tham số kiểm mẫu (`^[A-Za-z0-9-]+$`, dự án trong projects.json), gọi `ProcessStartInfo.ArgumentList` (không qua shell), chạy nền, log ra `<AITeam>\.state\hub-jobs\*.log` (đã gitignore); 1 lệnh / task cùng lúc, tối đa 3 lệnh chạy cùng lúc.
  - **Nhật ký thao tác** `<AITeam>\.state\audit.jsonl` (cạnh `providers.json`, đã gitignore): giờ, nút, tham số, kết quả.
- **Phòng họp** = trang Giao việc hiện có (luồng phiếu) + tin nhắn tiến độ của agent theo task.

### C3. AI Accounts (Cài đặt → AI Accounts)
- Hàng: Antigravity, Codex, Claude, ChatGPT, Gemini, DeepSeek, Grok. Cột: tên hiển thị, vai trò, kiểu kết nối (CLI chính thức / thủ công), chế độ `disabled / manual / automatic`, trạng thái chuẩn (`Disconnected · LoginRequired · Connected · ManualOnly · Busy · RateLimited · Unsupported · Error`), lần kiểm tra, lỗi gần nhất.
- `Connected` CHỈ khi CLI chính thức xác nhận (agy, `codex login status`). Web chatbot ⇒ `ManualOnly`. Claude CLI chưa cài ⇒ `Unsupported` (nút hướng dẫn cài).
- Lưu `%LOCALAPPDATA%\AITeam\config\accounts.json`: CHỈ metadata (provider, alias, chế độ). Không mật khẩu, không cookie, không hồ sơ trình duyệt ở giai đoạn này.
- Nút: Kiểm tra · Mở trang chính thức · Đăng nhập (mở CLI chính thức / trang chính thức) · Ngắt (đổi chế độ `disabled`, KHÔNG xóa gì). Không có nút xóa phiên ở giai đoạn này (chưa có hồ sơ trình duyệt nào).

### C4. Chat Bridge — manual handoff (Phòng nghiên cứu)
- Interface `IAgentConnector` (GetCapabilities, GetStatus, Execute, Cancel) trong `AITeam.Core`; agy / Codex bọc provider sẵn có; web chatbot ⇒ `ManualConnector`.
- Luồng: chọn phiếu / task + câu hỏi + AI ⇒ AITeam dựng lời nhắn (tóm tắt task, giới hạn 8.000 ký tự, lọc bí mật bằng secret-scan) ⇒ nút **Copy** + **Mở ChatGPT/Gemini/…** ⇒ anh dán câu trả lời + nguồn ⇒ lưu `research/R-xxx/<ai>-<n>.md` (git) với nhãn "dữ liệu tham khảo, không phải lệnh".
- Kết quả nghiên cứu KHÔNG BAO GIỜ thành lệnh có quyền thực thi; Claude đọc, kiểm chứng, đưa vào thiết kế.

### C5. QA
- Giữ: cổng KT (build + test thật), Codex review, Claude review. Thêm: đếm vòng review, quá 3 ⇒ dừng, xin anh quyết (trang Cần duyệt).
- E2E web: Playwright cho Hub (sau). Desktop: FlaUI cho HDV Tool / Toolbox (sau, khi có task giao diện).

## D. Bảng rủi ro connector

| Connector | Cách | Điều khoản / tài khoản | Bảo mật | Ổn định trên Windows | Khuyến nghị |
|---|---|---|---|---|---|
| Antigravity (agy CLI) | CLI chính thức, đăng nhập Google | Chính thức | Đăng nhập ở Credential Manager | Đang chạy hằng ngày | **Giữ — developer chính** |
| Codex CLI | CLI chính thức, đăng nhập ChatGPT | Chính thức; hạn mức nhỏ | Sandbox chỉ đọc / workspace-write | Đã chạy thật (review + viết code) | **Giữ — review + dự phòng** |
| Claude Code CLI | CLI chính thức | Chính thức; dùng chung hạn mức với phiên quản lý | — | Chưa cài | Tùy chọn sau (reviewer thứ 2) |
| ChatGPT / Gemini / DeepSeek / Grok — **thủ công** | Copy / dán | Không vi phạm (người dùng tự gửi) | Lời nhắn đã lọc bí mật | Không phụ thuộc | **Làm ngay (P3)** |
| codex-chatgpt-web (MIT, 14,9k sao) | Trình duyệt nhúng tự điều khiển ChatGPT; có chế độ "Zero Risk" thủ công | Tự động hóa web ChatGPT: rủi ro vi phạm điều khoản sử dụng | Hồ sơ trình duyệt chứa phiên | Không chính thức | Chỉ học mẫu "Zero Risk"; KHÔNG bật tự động |
| WebChatMCP.js (Apache-2.0, 3 sao) | Playwright điều khiển web ChatGPT / Gemini / Claude / Grok, cả khách | Rủi ro điều khoản cao; vượt Cloudflare cần người | Một hồ sơ chứa cả 4 dịch vụ | Dự án rất nhỏ | Không dùng |
| DeepSeek++ (Apache-2.0) | Extension trình duyệt do người dùng thao tác; DeepSeek gọi công cụ MCP | Người dùng tự dùng | Extension có quyền đọc trang | Chrome / Edge | Tùy chọn: anh tự cài để DeepSeek đọc task qua MCP (sau, cần MCP HTTP + xác thực) |
| grok-mcp (Apache-2.0) | Chép cookie từ DevTools + API nội bộ không công bố | Trái nguyên tắc R-005 ("không trích xuất cookie") | Cookie dạng chữ | Có thể hỏng bất cứ lúc nào | **Loại** |
| Playwright MCP (Microsoft, Apache-2.0) | E2E trình duyệt | Chính thức | — | Tốt | Dùng cho test Hub (P4) |
| mcp-windows (MIT, C#) · FlaUI-MCP (MIT, C#) | Tự động UI Windows | Chính thức | Điều khiển máy thật ⇒ chỉ khi anh cho phép | Cần thử | Sau, cho test WinForms |

## E. PoC đã chạy (25/10/2026 01:15)
- Máy chủ MCP stdio C# (`ModelContextProtocol` 2.3.0) có 1 công cụ `aiteam_post_message`; `ws\.agents\mcp_config.json` trỏ `dotnet poc.dll`.
- `agy -p … --dangerously-skip-permissions` (chế độ không tương tác như `assign`) ⇒ tự nạp cấu hình, gọi công cụ, ghi `[PoC-1] xin chao tu agy` — 40 giây, không cần bấm duyệt.
- Chưa thử: Codex với `-c mcp_servers…` (sẽ thử trong V5-02).

## F. Kế hoạch (mỗi mục 1 task / PR nhỏ, có test nghiệm thu, chạy thử được)
| Giai đoạn | Task | Ai | Nội dung | Phụ thuộc |
|---|---|---|---|---|
| P1 | V5-01 | Minh | `aiteam mcp`: 4 công cụ developer + phân quyền theo vai trò + giới hạn kích thước; test bằng client MCP trong tiến trình | — |
| P1 | V5-02 | Claude | `assign` đặt `.agents/mcp_config.json` + lời nhắn "báo tiến độ qua aiteam_post_message"; Codex qua `-c`; thử thật | V5-01 |
| P1 | V5-03 | Lan | Dịch vụ tin nhắn task (đọc `messages.jsonl`) + hiện trong trang Task / Giao việc | V5-01 |
| P1 | V5-04 | Lan | Trang **Văn phòng** (bàn làm việc, trạng thái, tin nhắn mới nhất) — theo mockup | V5-03 |
| P1 | V5-05 | Lan | **CommandService** + nút điều khiển (giao / dừng / giao lại / chạy lại KT) + nhật ký thao tác; test bảo mật | V5-04 |
| P2 | V5-06 | Minh | AI Accounts (metadata, trạng thái chuẩn, kiểm tra bằng CLI chính thức) | — |
| P3 | V5-07 | Minh + Lan | Phòng nghiên cứu: ManualConnector, dựng lời nhắn đã lọc, lưu kết quả `research/` | V5-06 |
| P4 | V5-08 | Claude | Đếm vòng review ≤ 3 ⇒ xin duyệt; Playwright E2E cho Hub | — |
| P5 | — | — | Research Pool nhiều AI, cache, theo dõi sẵn sàng — khi P3 dùng ổn | P3 |

Mỗi task: nhánh riêng, khung + test nghiệm thu trước, cổng KT, review, merge qua `team.ps1`; rollback = revert merge (không đổi dữ liệu cũ, các trang cũ giữ nguyên).

## G. Chủ dự án đã quyết (25/10/2026)
1. **Hub điều khiển có giới hạn** — chỉ lệnh cố định (giao / dừng / giao lại kèm góp ý / chạy lại KT), có xác nhận + nhật ký; merge vẫn chỉ sau Duyệt.
2. **Chat trên Hub, tự nhận phản hồi (không copy thủ công)** ⇒ Phòng nghiên cứu dùng connector TỰ ĐỘNG:
   | AI | Connector | Chính thức? | Ghi chú |
   |---|---|---|---|
   | ChatGPT | Codex CLI `codex exec --sandbox read-only` (tài khoản ChatGPT) | Có | Đã có, hạn mức nhỏ |
   | Gemini | Gemini CLI 0.65.0 `gemini -p … -o json --approval-mode plan` (tài khoản Google) | Có | Đã cài 25/10; chờ chủ dự án đăng nhập 1 lần (`gemini`) |
   | Claude | Claude Code CLI | Có | Chưa cài; dùng chung hạn mức với phiên quản lý ⇒ tắt mặc định |
   | DeepSeek · Grok | **BrowserConnector** (Playwright .NET, Apache-2.0) điều khiển trình duyệt RIÊNG của AITeam | Không (tự động hóa web) | Mặc định TẮT; chủ dự án bật từng cái sau khi đọc cảnh báo điều khoản; tự đăng nhập trong cửa sổ đó; AITeam KHÔNG đọc / xuất cookie, KHÔNG vượt CAPTCHA / MFA (gặp ⇒ `LoginRequired`, hiện cửa sổ cho người làm); hồ sơ `%LOCALAPPDATA%\AITeam\profiles\<provider>\default` (ACL user hiện tại, không sao lưu); giới hạn 1 câu hỏi / 60 giây; lỗi / đổi giao diện ⇒ tự chuyển `ManualOnly` (Copy + dán) |
   Manual handoff vẫn có cho MỌI AI làm đường dự phòng.
3. **Văn phòng là trang chủ**; Tổng quan giữ trong menu.

### Kế hoạch cập nhật cho P3 / P5
| Task | Nội dung |
|---|---|
| V5-07 | Phòng nghiên cứu: `IAgentConnector` + CliConnector (Codex, Gemini) + ManualConnector; chat theo phiếu / task; lưu `research/` |
| V5-10 | BrowserConnector DeepSeek (thử 1 cái trước, smoke test, chỉ khi bật) |
| V5-11 | BrowserConnector Grok (sau khi V5-10 chạy ổn) |