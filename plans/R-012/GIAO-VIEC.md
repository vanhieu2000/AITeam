# Giao việc R-012 / R-013 — 02/11/2026

**Không dùng Antigravity (`agy`) cho bất kỳ việc nào dưới đây.** Luồng chủ dự án chốt 02/11/2026:

| Trường hợp | Viết code | QC | Nghiệm thu + merge |
|---|---|---|---|
| Ưu tiên | **OpenCode** (chỉ DeepSeek) | **Codex** (`team.ps1 qc`, chỉ đọc) | Claude |
| OpenCode hết hạn mức / lỗi lặp / làm hỏng 2 lần | **Codex** (`assign -Dev codex`) | Claude | Claude |

Cho tới khi O-02 xong (`assign -Dev opencode`), Claude gọi `opencode run` trực tiếp trong worktree của task.
Trả lời chủ dự án bằng tiếng Việt, ngắn, gạch đầu dòng.

## 0. Trước khi làm
1. Repo: `vanhieu2000/AITeam`. `git pull` trên `main`.
   - Máy công ty (`D:\HIEUDV\AITeam`) đã đồng bộ 02/11 (thay đổi `projects.json` cũ chỉ là đường dẫn máy, đã có trong `team.local.json`).
2. Đọc: `QUY-TRINH.md`, `QUAN-LY-PHU.md`, `AGENTS.md` (mục "Lỗi hay gặp"), `BAN-GIAO.md`, `tasks/README.md`.
3. Luật giữ nguyên:
   - Mỗi task một nhánh `agent-c/<MÃ>-<tên>` tách từ `origin/main`; KHÔNG push `main`, không force-push, không `team.ps1 merge`.
   - Xong task: `.\team.ps1 verify -Project AITeam -Task <MÃ>` phải ĐẠT (build 0 warning, toàn bộ test pass, test nghiệm thu còn nguyên) ⇒ ghi "## Kết quả" trong file task trên nhánh ⇒ push nhánh ⇒ thêm 1 dòng vào mục "Chờ Claude merge" của `QUAN-LY-PHU.md`. Claude chỉ merge.
   - Không sửa test nghiệm thu `Acceptance*Tests.cs` của Claude, TRỪ khi task dưới đây cho phép rõ.
   - Giữ BOM UTF-8 cho `.ps1` / `.cs`. Sửa `team.ps1` ⇒ chạy `powershell -File tools\test-verify.ps1` trước commit.
   - Kho AITeam hiện đang **PUBLIC** trên GitHub: tuyệt đối không đưa bí mật, mật khẩu, khóa API, đường dẫn nội bộ công ty vào file / log / commit.
   - Mỗi sự kiện ghi 1 dòng có giờ vào Nhật ký của phiếu `requests/R-xxx`.
   - Cùng một lỗi còn sau 2 lần sửa ⇒ dừng, ghi lý do, báo chủ dự án. Không vá vòng tròn.
4. Mở 2 phiếu: `.\team.ps1 new-request -Title "Gỡ task tắc + sửa Hub (giao Codex)"` (R-012) và `.\team.ps1 new-request -Title "Tích hợp OpenCode làm developer dự phòng"` (R-013). Chép nguyên văn yêu cầu của chủ dự án vào phiếu.

## 1. R-012 · Gỡ 5 task đang tắc (làm theo thứ tự)
Các nhánh đã có trên GitHub; làm TIẾP trên nhánh cũ (không tạo nhánh mới), đọc `logs/<MÃ>-verify-*.md` mới nhất trước.

| Thứ tự | Task | Vướng | Cần làm |
|---|---|---|---|
| 1 | H7-04 Thẻ tác nhân + chat giao việc | Hub 748 + Core 690 pass; KHÔNG ĐẠT chỉ vì 2 warning xUnit2031 ở `AcceptanceH704OfficeTests.cs:242,247` | ĐƯỢC PHÉP sửa đúng 2 dòng này (đổi assert theo gợi ý xUnit2031, giữ nguyên ý kiểm tra). Verify lại ⇒ ĐẠT |
| 2 | H7-06 Menu, danh sách dự án, bảng duyệt | Hub 24 test lỗi; `NullReferenceException` ở `Projects.cshtml:4` | Tìm gốc null (model trang Dự án chưa khởi tạo khi không có dữ liệu / chế độ giả), sửa trong code trang, không nới test |
| 3 | H7-02 Điều phối plan/prep/QC theo Map | Core 2 test S1-04 lỗi trên `acbece9`; worktree từng xung đột `team.ps1` | Rebase / giải xung đột `team.ps1` với `main`, xác định 2 test lỗi do task này hay do harness; sửa đúng chỗ |
| 4 | W1-03 Sinh mockup, khóa prep trước duyệt | Core 6 test lỗi trên `ebef924`; HEAD hiện `84a1513` chưa có cổng KT | Verify trên HEAD hiện tại trước, rồi sửa phần còn đỏ. Xong ⇒ làm tiếp W1-04 (đang chờ W1-03) |
| 5 | V6-02 Chặn giao lại, xin quyết định | Còn 2 ca `Fresh_ai_log` đỏ | Sửa logic nhận diện log AI mới; không đổi test |

Sau khi H7-04 vào hàng chờ merge: làm tiếp H7-05 (đang chờ H7-04).

## 2. R-012 · Sửa giao diện Hub (task nhỏ, mỗi dòng 1 task, ≤ ~150 dòng)
Hub: `hub/` (ASP.NET Core Razor Pages + HTMX). Chạy giả lập để xem: `$env:HUB_FAKE=1; $env:HUB_PORT=5058; dotnet run --project hub`. Mỗi task giao diện: kiểm ở 1000px không tràn ngang, chụp ảnh vào `docs/screenshots/<MÃ>/`.

| Mã gợi ý | Trang | Sửa |
|---|---|---|
| H8-01 | Phòng nghiên cứu | Sau "Hỏi" dùng Post-Redirect-Get để F5 không gửi lại; nhà cung cấp đang nghỉ (hết hạn mức) thì khóa nút Hỏi và hiện lý do ngay |
| H8-02 | Tài khoản AI | Antigravity: tách "Đăng nhập" và "Đang chạy việc" thành 2 trạng thái riêng; chế độ `HUB_FAKE` bấm Lưu KHÔNG được ghi `accounts.json` thật |
| H8-03 | Task | Các vòng review không hiện trùng (danh sách + Markdown gốc); review cũ không có `###` vẫn tách đúng vòng; bỏ số trang ghi cứng 8 trong `PaginationSummary` |
| H8-04 | Tổng quan | Cột phải không cắt chữ khi cửa sổ hẹp (xuống dòng / co cột) |
| H8-05 | Việc chạy nền | Đọc output PowerShell 5.1 đúng code page để tiếng Việt không lỗi phông; giới hạn danh sách job trong bộ nhớ (giữ 200 gần nhất) |

## 3. R-012 · Chức năng / dây chuyền
| Mã gợi ý | Sửa |
|---|---|
| V9-01 | Xóa cache `.state/agy-models.json` khi agy báo model không hợp lệ và sau `switch-account` |
| V9-02 | Test V8-02 / V8-03 chập chờn khi máy bận: bỏ phụ thuộc thời gian thật (đồng hồ giả / chờ có điều kiện) |
| V9-03 | Cảnh báo khi một nhà cung cấp AI lỗi ≥ 3 lần liên tiếp (ghi `.state/providers.json`, hiện trên Hub trang Nhà cung cấp) |
| V9-04 | Nhận diện tiêu đề `##<tab>` trong parser review (V6-01 còn mở) |
| V9-05 | `W1-02`: dispose singleton tạo bằng factory khi `Build` ném lỗi (helper test) |

## 4. R-013 · Tích hợp OpenCode (Claude đã cài sẵn trên máy công ty)
Thông tin đã kiểm chứng ngày 02/11/2026 trên máy công ty:
- Cài: `npm install -g opencode-ai@1.19.5` ⇒ lệnh `opencode` (`%APPDATA%\npm\opencode.ps1` / `.cmd`). Máy khác cài cùng lệnh.
- Chạy không giao diện: `opencode run --dir <thư mục> -m <provider/model> --format json "<lời nhắn>"`. Cờ khác: `--agent`, `-f/--file`, `-c/--continue`, `-s/--session`, `--variant`, `--attach http://localhost:4096`, `--auto` (tự duyệt quyền — NGUY HIỂM, chỉ dùng trong worktree riêng).
- PHẢI đóng stdin (chuyển hướng từ file rỗng / NUL), như với agy. `--help` in ra stderr.
- Output JSON: mỗi dòng 1 sự kiện `{"type": "step_start" | "text" | "step_finish" | …, "sessionID": …, "part": {…}}`; `step_finish.part` có `reason` và `tokens` (total/input/output).
- Chưa đăng nhập nhà cung cấp nào (`opencode auth list` = 0).
- **CHỈ DÙNG DEEPSEEK (chủ dự án quyết 02/11/2026).** Cấu hình người dùng `%USERPROFILE%\.config\opencode\opencode.jsonc` đã khóa: `model` = `opencode/deepseek-v4.1-flash-free`, `enabled_providers` = `opencode`, `deepseek`, `provider.opencode.whitelist` = `deepseek-v4.1-flash-free`. `opencode models` chỉ còn 1 model; gọi `-m opencode/big-pickle` bị lỗi. KHÔNG sửa file này, KHÔNG tạo `opencode.json` trong repo / worktree để mở model khác, KHÔNG truyền `-m` model ngoài DeepSeek. `team.ps1` phải từ chối giá trị `opencode.model` trong `team.json` nếu không chứa `deepseek`.
- Model free (kể cả DeepSeek free) KHÔNG cam kết riêng tư ⇒ chỉ cho chạy trên repo AITeam. HDVTool / ChinaAndroidToolbox (private) chỉ được dùng OpenCode khi có DeepSeek trả phí (khóa API DeepSeek chính chủ hoặc OpenCode Go) — chủ dự án quyết sau khi xem kết quả O-06.
- Thông tin đăng nhập nằm ở `~\.local\share\opencode\auth.json` — không đọc, không in, không commit.

| Mã | Việc | Tiêu chí xong |
|---|---|---|
| O-02 | `team.ps1`: thêm developer `opencode` cho `assign -Dev opencode` (prompt ghi ra file, `-f` đính kèm; stdin rỗng; `--format json`; `--dir` = worktree; model lấy từ `team.json` → `opencode.model`, mặc định `opencode/deepseek-v4.1-flash-free`, chỉ chấp nhận model DeepSeek). `doctor` kiểm tra có lệnh `opencode`. Ghi log `logs/<MÃ>-<n>.json`, đọc `tokens` cho `metrics`. Nhận diện hết hạn mức / lỗi 429 | Test Pester / `tools\test-verify.ps1` bằng `opencode` giả (script trả JSON mẫu), không gọi mạng trong test |
| O-03 | Định tuyến nhà cung cấp: thứ tự agy → codex → opencode khi hết hạn mức; tôn trọng `.state/providers.json` (đang nghỉ thì bỏ qua) | Test: agy nghỉ + codex nghỉ ⇒ chọn opencode; cả 3 nghỉ ⇒ dừng, báo rõ |
| O-04 | Agent review chỉ đọc: tạo `.opencode/agent/reviewer.md` (hoặc cấu hình tương đương của bản 1.19.5) cấm ghi file + cấm bash; `aiteam review --provider opencode` dùng agent này | Test: review không làm thay đổi `git status` |
| O-05 | Hub trang Tài khoản AI / Nhà cung cấp: thêm OpenCode (đã cài? phiên bản, model đang dùng, trạng thái nghỉ) | Ảnh 1000px; chế độ giả không gọi `opencode` thật |
| O-06 | Chạy thật: giao 1 task nhỏ của AITeam (ví dụ V9-04) bằng `assign -Dev opencode`; ghi phút / số lượt / cổng KT / token vào phiếu R-013, so với Codex | Bảng so sánh trong phiếu + đề xuất: có nên mua OpenCode Go 10 đô/tháng không |

## 5. Báo cáo cho chủ dự án (cuối mỗi đợt)
- Task nào đã vào "Chờ Claude merge", task nào dừng và vì sao.
- Lỗi phát hiện thêm ⇒ đã ghi vào `AGENTS.md` mục "Lỗi hay gặp".
- Việc cần chủ dự án: đăng nhập / mua gói / quyết định để kho AITeam public hay private.
