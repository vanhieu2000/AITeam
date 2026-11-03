# Yêu cầu cho Codex — hoàn thiện AITeam (bản đầy đủ, 03/11/2026)

Chủ dự án HieuDV giao TRỰC TIẾP cho Codex. Trả lời bằng tiếng Việt, ngắn, gạch đầu dòng.

**ĐỌC TRƯỚC (bắt buộc, theo thứ tự):**
1. `docs/MO-TA-HE-THONG.md` — hệ thống là gì, mục đích, vai trò, trạng thái task, dữ liệu, chức năng F1–F14. Mỗi task dưới đây trỏ về mục trong file này; làm đúng mô tả đó.
2. `QUY-TRINH.md` mục v5 — cách chạy Codex / OpenCode, worktree, cổng kiểm tra.
3. `AGENTS.md` — "Lỗi hay gặp", nhất là các dòng cuối file.

## 0. Luật làm việc
- Repo `vanhieu2000/AITeam`. Mỗi task một nhánh `agent-c/<MÃ>-<tên-ngắn>` từ `origin/main` (nhánh đã có thì làm tiếp, `git merge --no-edit origin/main` trước). Tạo file `tasks/<MÃ>-<tên>.md` từ `templates/task.md`: Tóm tắt (cho chủ dự án, lời thường), Đọc, Phạm vi (đường dẫn thật), Không làm, Tiêu chí (kiểm được), Ngân sách dòng, `- **Yêu cầu:** R-012`. Thêm dòng vào `tasks/README.md`.
- Test trước khi code khi có logic (`hub.tests/` hoặc `core.tests/`, dùng helper `core.tests/Support` / `hub.tests/Support`). Không cảnh báo trong test (`Assert.Single(x, f => …)`). Test trang Razor: theo `AGENTS.md` (PageContext).
- Xong mỗi task: `dotnet build AITeam.sln -warnaserror -nodeReuse:false` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết; sửa `team.ps1` ⇒ thêm `powershell -File tools\test-verify.ps1`. Điền "Báo cáo của agent" (≤ 15 dòng). Commit, **push nhánh**. KHÔNG merge / push `main` — Claude review + chốt.
- Không sửa / xóa / Skip test có sẵn. Test nghiệm thu chỉ được sửa phần dựng môi trường khi chứng minh sai (không sửa assert), ghi trong báo cáo.
- BOM UTF-8 cho `.ps1` / `.cs` / `.cshtml`. Không bí mật trong file / commit (kho đang public). Không dùng Antigravity.
- Giao diện: tiếng Việt, 1000 px không tràn ngang, có trạng thái rỗng / lỗi; xem bằng `$env:HUB_FAKE=1; $env:HUB_PORT=5058; dotnet run --project hub`; dữ liệu giả thêm vào `hub/DevData` nếu cần để nhìn thấy.
- Cùng một test đỏ sau 2 lần sửa ⇒ dừng task đó, ghi stack trace vào báo cáo, sang task khác.
- Thứ tự làm: Nhóm A → B → C → D. Trong nhóm, task không cùng file có thể làm song song.

---

## Nhóm A — Việc dở (gỡ trước)

### A1 · H7-02 Điều phối plan / prep / QC theo Map (F2, F3)
- **Mục đích:** bước lập kế hoạch và chuẩn bị task đọc bảng phân vai (Map) để biết ai làm bước nào, thay vì ghi cứng.
- **Hiện trạng:** nhánh `agent-b/H7-02-dieu-phoi-plan-prep-qc-theo-map` (7f21688). Codex 03/11 đã sửa harness S1-04 (91/91 test S1-04 + H7-02 xanh) nhưng CHƯA merge main; trong sandbox còn 9 Hub + 5 Core đỏ do bị chặn quyền ghi.
- **Làm:** merge `origin/main` (xung đột dễ có ở `team.ps1` và `core.tests/AcceptanceS104SkillPromptsTests.cs` — W1-03 cũng sửa file test này, giữ cả hai phần); chạy test NGOÀI sandbox; sửa phần đỏ thật trong Phạm vi của file task.
- **Xong khi:** toàn bộ test xanh trên nhánh đã gộp main.

### A2 · V6-02 Chặn giao lại vô hạn và xin quyết định (F3, mục 5.2 "Tạm dừng")
- **Mục đích:** khi một task đã quá số vòng hoặc lặp cùng lỗi, hệ thống không tự giao lại nữa mà chuyển "Tạm dừng" và tạo đơn xin quyết định.
- **Hiện trạng:** nhánh `agent-a/V6-02-chan-giao-lai-va-xin-quyet-dinh` chậm main 182 commit; còn 2 ca `Fresh_ai_log` đỏ (nhận diện log AI "mới" sai).
- **Làm:** merge main; sửa logic nhận diện log AI mới theo tiêu chí trong file task; không đổi test.
- **Xong khi:** test xanh; giao lại lần 4 bị chặn kèm đơn trong `approvals/`.

### A3 · H7-05 và W1-04 (theo plan R-009 / R-011)
- H7-05 phụ thuộc H7-04 (đã merge 03/11). W1-04 phụ thuộc W1-03 (nhánh xanh, chờ Claude review — chỉ bắt đầu khi W1-03 vào main).
- Làm theo `plans/R-009/plan.md`, `plans/R-011/plan.md`; tạo file task nếu chưa có.

---

## Nhóm B — Đo đạc nền tảng (không có số đo thì không đánh giá được)

### B1 · V9-10 Ghi token đúng cho mọi lượt (F9 "Đã dùng")
- **Mục đích:** mỗi lượt chạy phải biết tốn bao nhiêu token để đánh giá chi phí và AI.
- **Hiện trạng:** `metrics/runs/*.json` có `outputTokens/thinkingTokens/totalTokens` nhưng lượt Codex luôn = 0; OpenCode chưa có.
- **Làm:** `core/AITeam.Core/Providers/CodexProvider.cs`: đọc sự kiện token trong output `codex exec --json` (kiểm bằng output THẬT — lưu 1 mẫu ẩn danh vào `core.tests/fixtures/codex-exec-sample.jsonl` trước khi viết parser); OpenCode: cộng `part.tokens` của mọi sự kiện `step_finish`. Ghi vào bản ghi lượt; không đo được ⇒ ghi `"tokensKnown": false`, không ghi 0.
- **Xong khi:** test parser với mẫu thật; lượt mới của Codex / OpenCode có token > 0.

### B2 · V9-11 Hạn mức còn lại và giờ hồi (F9 "Còn lại", "Dự báo")
- **Mục đích:** không bị dừng giữa chừng vì hết hạn mức; biết trước để chuyển AI.
- **Làm:** mở rộng `.state/providers.json` mỗi provider: `usedPercent`, `windowMinutes`, `resetsAt`, `source` (`provider` = nhà cung cấp trả về | `estimate` = ước tính từ cấu hình | `unknown`). Codex: lấy từ sự kiện rate limit trong `exec --json` NẾU có (kiểm chứng bằng mẫu thật; không có thì `unknown`). Cấu hình hạn mức khai báo trong `team.json` → `quotas` (ví dụ OpenCode Go: 12 / 30 / 60 USD theo 5 giờ / tuần / tháng) để ước tính. Dự báo giờ hết = tốc độ 24 giờ qua.
- **Xong khi:** test cho 3 nguồn; hết hạn mức (429 / quota) ⇒ provider "nghỉ đến `resetsAt`".

### B3 · V9-12 Bản ghi lỗi có cấu trúc (F7, mục 7.2)
- **Mục đích:** đếm được lỗi theo nguyên nhân gốc để cải tiến đúng chỗ.
- **Làm:** parser đọc các dòng `- [MÃ] (nhóm) mô tả` trong mục Review / Đánh giá của file task; MÃ ∈ TK, TN, AG, CK, HT, HM, CH, TO, DP (mục 7.2) — chấp nhận dữ liệu cũ chỉ có AG / TK / CK. Trả danh sách Finding (task, vòng, mã, nhóm, mô tả). Dùng chung cho `staff-report`, `metrics` và Hub.
- **Xong khi:** test với 5 file task thật trong `tasks/` (có review cũ); mã lạ ⇒ "khác", không lỗi.

### B4 · V9-13 Tự gắn cờ sau mỗi lượt (F7 "Dấu hiệu tự phát hiện")
- **Mục đích:** máy tự chỉ ra lượt có vấn đề (chậm, tốn, kẹt, hạ tầng) mà không tốn token AI.
- **Làm:** sau mỗi lượt, so bản ghi lượt với ngưỡng: phút > 2 × thời gian tối đa ⇒ CH; token > 2 × TB cùng độ khó ⇒ TO; lượt kết thúc không token + không output ⇒ HT; log có 429 / quota ⇒ HM; cùng test đỏ với cùng stack trace ở 2 lượt liền ⇒ gợi ý TN ("có thể test nghiệm thu sai") và chuyển task "Tạm dừng". Ghi cờ vào bản ghi lượt + 1 dòng nhật ký phiếu R.
- **Xong khi:** test cho từng luật; chạy lại trên dữ liệu 26–28/10 phải gắn cờ H7-06 là TN.

---

## Nhóm C — Điều phối tự động (thay chủ dự án)

### C1 · O-02 OpenCode là người làm thứ hai (F3, mục 4.1)
- **Làm:** `team.ps1 assign -Dev opencode` + provider C# tương ứng, đúng cách chạy trong `QUY-TRINH.md` v5: prompt ra file, `opencode run "<lời nhắn>" -f <prompt> --dir <worktree> --format json --title <Mã>`, stdin rỗng, `OPENCODE_CONFIG` = bản sao `templates/opencode-quyen-han.json`, KHÔNG `--auto`; báo cáo = dòng `type=text` cuối; dừng do bị từ chối quyền ⇒ ghi rõ lệnh bị từ chối. Chỉ chấp nhận model chứa `deepseek` (`team.json` → `opencode.model`, mặc định `opencode/deepseek-v4.1-flash-free`). Repo private + `opencode.paid != true` ⇒ từ chối. `doctor` kiểm có lệnh `opencode` và model khóa đúng.
- **Xong khi:** test bằng `opencode` giả (script in JSON mẫu), không gọi mạng; chạy thật 1 task nhỏ ghi được lượt có token.

### C2 · V9-06 Codex chạy ổn định trên mọi máy (F3, HT)
- **Làm:** `CodexProvider`: thêm `-c windows.sandbox="unelevated"`; `team.json` → `codex.baseUrl` (nếu có ⇒ `-c openai_base_url=…`); sau mỗi lượt gọi `dotnet build-server shutdown`; trước khi giao, bộ điều phối tự `git fetch` + `git merge origin/main` trong worktree; sau khi Codex xong, bộ điều phối tự commit + push (sandbox không ghi được metadata git).
- **Xong khi:** test dựng tham số; chạy thật 1 task trên máy công ty không treo, không "Access denied" ở lần build sau.

### C3 · V9-14 Định tuyến tự động theo luật (mục 4.2)
- **Làm:** hàm chọn người làm theo đúng 6 luật mục 4.2 (Dev chỉ định, riêng tư, nghỉ, độ khó, cân tải, không ai ⇒ "Chờ người làm"); bỏ Antigravity khi `team.ps1 models` không có model chứa `argon-4` (hoặc tên khai báo trong `team.json` → `agy.requiredModel`). Kết quả + lý do chọn ghi vào nhật ký phiếu.
- **Xong khi:** test bảng quyết định cho mọi luật.

### C4 · V9-15 Khóa file và hàng đợi tự chạy (F3)
- **Làm:** trước khi giao, khóa các đường dẫn trong Phạm vi task (`.state/locks.json`: task, người, giờ); task có Phạm vi giao nhau với task đang 🟡 ⇒ chờ. Lệnh `team.ps1 auto -Project X [-Max 2]`: lặp — lấy task "Chờ làm" đủ phụ thuộc ⇒ định tuyến (C3) ⇒ giao song song tối đa N ⇒ khi xong ⇒ cổng KT ⇒ "Chờ review" ⇒ lấy task tiếp; tôn trọng "Tạm dừng" (B4) và hạn mức (B2). Dừng sạch bằng `team.ps1 stop -All`.
- **Xong khi:** test: 2 task cùng file không chạy cùng lúc; task B chờ A merge; hết hạn mức chuyển người.

### C5 · V9-16 Tự kiểm chất lượng task trước khi giao (F2)
- **Mục đích:** chặn lỗi TK / TN từ gốc (H7-04, H7-06 kẹt 10 lượt vì test nghiệm thu sai).
- **Làm:** lệnh `team.ps1 lint-task -Task X`, tự chạy trước `assign`: có đủ mục bắt buộc; mọi đường dẫn Phạm vi tồn tại (hoặc ghi "tạo mới"); Tiêu chí có checkbox; test nghiệm thu build được với `-warnaserror` trên nhánh task; test nghiệm thu chạy ĐỎ (chưa có code) nhưng KHÔNG đỏ vì `NullReferenceException` trong harness / lỗi dựng trang (dấu hiệu TN). Không đạt ⇒ không giao, ghi lý do.
- **Xong khi:** test với 3 file task mẫu (đủ / thiếu mục / test có cảnh báo).

---

## Nhóm D — Hub: xem tổng quan → chi tiết, đánh giá, tài khoản

### D1 · H8-10 Trang chủ toàn hệ thống + Kanban dự án (F5 cấp 1–2)
- **Mục đích:** một màn hình biết mọi dự án đang chờ / đang / đã làm gì và việc nào cần chủ dự án.
- **Làm:** khối đầu trang Văn phòng hoặc trang mới `/Work`: số task theo 10 trạng thái mục 5.2 cho mọi dự án (bấm vào lọc); danh sách "Cần anh" (Bị chặn, Chờ duyệt, Tạm dừng > 24 giờ) lên đầu. Trang dự án: Kanban 5 cột (Chờ làm · Đang làm · Chờ review · Cần sửa/Tạm dừng · Đã merge 7 ngày), thẻ task có mã, tiêu đề, người làm, phút, lượt, cờ (B4).
- **Xong khi:** số liệu khớp `tasks/README.md` + `metrics/runs`; 1000 px không tràn.

### D2 · H8-11 Chi tiết yêu cầu, task, lượt (F5 cấp 3–5)
- **Làm:** Yêu cầu: lời nguyên văn, dây chuyền 10 bước (mục 5.3) có bước hiện tại, task + trạng thái, tổng phút / token, nhật ký. Task: dòng thời gian các lượt (đã có Gantt — dùng lại), vòng review, lỗi theo mã (B3), Kết quả, Đánh giá. Trang Lượt mới `/Run?task=X&n=N`: người làm, model, phút, token, dòng +/−, cờ, kết quả cổng KT, 50 dòng cuối log (mã hóa HTML), báo cáo AI.
- **Xong khi:** từ trang chủ tới log một lượt ≤ 3 cú bấm.

### D3 · H8-12 Hiệu suất + bảng điểm AI (F7, F8)
- **Làm:** trang `/Performance`: 7 chỉ số mục 2 (tuần này / tuần trước); biểu đồ cột chồng lỗi theo mã nguyên nhân theo tuần (B3); bảng điểm mỗi provider + model: task, % đạt vòng 1, vòng TB, phút TB theo độ khó, token TB, lỗi AG theo nhóm, số lần hết hạn mức; gợi ý "loại việc nên giao" (giao diện / PowerShell / C# lõi / test — suy từ Phạm vi task).
- **Xong khi:** số khớp dữ liệu; có trạng thái "chưa đủ dữ liệu" khi < 3 task.

### D4 · H8-13 Tài khoản AI: token, hạn mức, link (F9, F10)
- **Làm:** mỗi tài khoản một thẻ: CLI đã cài + phiên bản, đăng nhập (tách khỏi "đang chạy việc"), model được phép (OpenCode: chỉ DeepSeek), gói, đã dùng hôm nay / tuần, **còn lại + giờ hồi** (B2, ghi rõ nguồn provider / ước tính / không rõ), cảnh báo < 20 %, **link** trang quản lý hạn mức / thanh toán / tài liệu (lấy từ `team.json` → `accounts[].links`), nút "Kiểm tra lại". Thêm OpenCode. Chế độ `HUB_FAKE` không ghi file thật.
- **Xong khi:** đủ thẻ Codex, OpenCode, Antigravity, Claude; không hiện bất kỳ bí mật nào.

### D5 · H8-01 … H8-06 Sửa nhỏ giao diện
| Mã | Trang | Vấn đề hiện tại → Cần làm |
|---|---|---|
| H8-01 | Phòng nghiên cứu | F5 gửi lại câu hỏi ⇒ Post-Redirect-Get. Nhà cung cấp đang nghỉ vẫn cho hỏi rồi mới lỗi ⇒ khóa nút, hiện lý do + giờ hồi |
| H8-03 | Task | Vòng review hiện 2 lần (danh sách + Markdown gốc) ⇒ hiện 1 lần; review cũ không có `###` ⇒ vẫn tách đúng vòng; `PaginationSummary` ghi cứng 8 ⇒ tính thật |
| H8-04 | Tổng quan / Hiệu suất | Cột phải cắt chữ khi cửa sổ hẹp ⇒ xuống dòng / co cột |
| H8-05 | Việc chạy nền | Log PowerShell 5.1 lỗi phông tiếng Việt ⇒ đọc đúng code page; danh sách job không giới hạn ⇒ giữ 200 gần nhất |
| H8-06 | Đơn duyệt | Dữ liệu giả thêm 2 đơn "Chờ duyệt" để thấy nút Duyệt / Từ chối trong dòng |
(H8-02 đã gộp vào D4.)

### D6 · V9-01 … V9-05, V9-07 Sửa nhỏ chức năng
| Mã | Vấn đề → Cần làm |
|---|---|
| V9-01 | Đổi tài khoản / model không hợp lệ vẫn dùng danh sách model cũ ⇒ xóa `.state/agy-models.json` khi agy báo model không hợp lệ và sau `switch-account` |
| V9-02 | Test V8-02 / V8-03 đỏ ngẫu nhiên khi máy bận ⇒ dùng đồng hồ giả / chờ có điều kiện, không `Sleep` cố định |
| V9-03 | Gộp vào B4 + B2 (provider lỗi ≥ 3 lần liền ⇒ nghỉ + cảnh báo) |
| V9-04 | Parser review bỏ sót tiêu đề `##<tab>` ⇒ nhận diện |
| V9-05 | Helper test W1-02 không dispose singleton khi `Build` lỗi ⇒ dispose |
| V9-07 | Cổng KT: build `-nodeReuse:false`, không 2 build song song; file task có `- **Cho phép sửa harness:** <file>` ⇒ không chặn sửa phần dựng môi trường của file đó, vẫn chặn mọi dòng có `Assert.` |

---

## Báo cáo cho chủ dự án (cuối mỗi đợt)
- Bảng: Mã · nhánh · trạng thái (đã push chờ Claude review / dừng + lý do) · phút · token.
- Lỗi mới gặp ⇒ 1 dòng cuối `AGENTS.md` theo mã nguyên nhân mục 7.2.
- Việc cần chủ dự án: đăng nhập, mua gói, quyết kho AITeam public / private, duyệt mockup (nếu D1–D4 đổi bố cục lớn ⇒ làm mockup HTML tĩnh trong `docs/design/` và DỪNG chờ duyệt trước khi code).
