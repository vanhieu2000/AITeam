# Quy trình v5.1 — ĐANG ÁP DỤNG (chủ dự án chốt 02/11/2026, R-012; v5.1 03/11 sau đánh giá R-012)

Mục v5 này THẮNG mọi mục cũ bên dưới (v3 / v4 chỉ còn để tra cứu).
Hệ thống là gì, mục đích, vai trò, trạng thái task, dữ liệu, chức năng F1–F14, phân loại nguyên nhân lỗi: **`docs/MO-TA-HE-THONG.md`** (đọc trước khi thiết kế / viết task).

## Vai trò
| Ai | Làm | KHÔNG làm |
|---|---|---|
| Chủ dự án | Đưa yêu cầu; có thể giao thẳng cho Codex bằng `plans/R-xxx/YEU-CAU-CODEX.md` | — |
| **Codex** (ChatGPT, CLI) | Viết code, việc khó / sửa `team.ps1` / nhiều file | **Không QC nữa** |
| **OpenCode** (CHỈ DeepSeek) | Viết code, việc vừa / nhỏ, chạy song song với Codex | Không dùng model khác |
| Antigravity | **Chỉ giao khi tài khoản có model Argon 4** (`team.ps1 models`). 02/11: chưa có ⇒ KHÔNG giao | — |
| Claude | Chẩn đoán khi AI tắc, review, sửa nhỏ ≤ 50 dòng, **chốt + merge**, ghi phiếu | Không giao lại cùng một lỗi lần 3 mà chưa tự tìm gốc |

- Chia task cho nhiều người cùng lúc, **khác file**; 2 task cùng sửa `team.ps1` hoặc cùng file test ⇒ tuần tự.
- Cùng một lỗi đỏ sau 2 lượt AI ⇒ Claude tự đọc stack trace tìm gốc trước (02/11: H7-06 đỏ 10 lượt chỉ vì bộ test dựng trang sai, tìm ra trong 5 phút).

## Chạy OpenCode (đã kiểm chứng 02/11, bản 1.19.5)
- Cài: `npm install -g opencode-ai@1.19.5`. Khóa DeepSeek ở `%USERPROFILE%\.config\opencode\opencode.jsonc` (mẫu: `templates/opencode-config.jsonc`). Không tạo `opencode.json` trong repo.
- Quyền hẹp cho mỗi lần chạy: `$env:OPENCODE_CONFIG = <bản sao templates/opencode-quyen-han.json>` (chỉ sửa file + `git`/`dotnet`; cấm webfetch, websearch, đọc ngoài thư mục). **KHÔNG dùng `--auto`** (bị chặn, và nguy hiểm).
- Lệnh: `opencode run "Lam dung theo file dinh kem." -f <prompt.md> --dir <worktree> --format json --title <Mã> < NUL`. Lời nhắn PHẢI đứng trước `-f` (`-f` là mảng, nuốt mọi thứ phía sau).
- Kết quả: dòng `type=text` cuối trong JSON. Bị từ chối quyền ⇒ OpenCode dừng luôn (không báo lỗi) ⇒ đọc sự kiện `tool_use` có `status=error`.

## Chạy Codex CLI (đã kiểm chứng 02/11, bản 0.166.0)
- Cài: `npm install -g @openai/codex@0.166.0`; dùng đăng nhập ChatGPT sẵn có (`codex login status`).
- **Luôn ghi đè theo lần gọi, KHÔNG sửa `~/.codex/config.toml`:** `-c openai_base_url="https://chatgpt.com/backend-api/codex"` (config máy công ty trỏ cầu nối 127.0.0.1:17841; cầu nối tắt ⇒ Codex treo vô hạn) và `-c windows.sandbox="unelevated"` (sandbox mặc định cần UAC ⇒ lỗi 1223, không chạy được lệnh nào).
- Lệnh: `codex exec --cd <worktree> --skip-git-repo-check --sandbox workspace-write -c windows.sandbox="unelevated" -c notify=[] -c openai_base_url="…" - < prompt.md`.
- Sandbox unelevated **không ghi được metadata git** của worktree ⇒ người điều phối tự `git fetch` + `git merge origin/main` TRƯỚC khi giao, và tự commit + push SAU khi Codex xong. Test có fixture git / ghi file có thể đỏ giả trong sandbox ⇒ chạy lại test ngoài sandbox trước khi kết luận.
- Sau mỗi lần Codex build: `dotnet build-server shutdown`. MSBuild giữ tiến trình build ngầm mang quyền hạn chế của sandbox ⇒ lần build sau báo "Access to the path … is denied". Build tay dùng `-nodeReuse:false`.

## Worktree, cổng kiểm tra, merge
- Thư mục: `<workDir>\<a|b>-<mã viết thường>` (vd. `a-h7-06`), nếu không `team.ps1 verify/qc` báo "Chưa có thư mục làm việc". Tạo: `git worktree add <thư mục> -B <nhánh> origin/<nhánh>`; tên git riêng bằng `git config --worktree user.name`.
- Không chạy nhiều `dotnet build` / `verify` song song trên máy (khóa file `obj`).
- Cổng KT báo "sửa test nghiệm thu" khi quản lý đã CHO PHÉP sửa phần dựng môi trường ⇒ ghi rõ trong "### Vòng N — ĐẠT" (file, dòng, vì sao) rồi `team.ps1 merge`.
- `merge` xung đột (thường `hub.css` khi 2 task cùng thêm style cuối file) ⇒ giữ cả hai khối, build + toàn bộ test + clone sạch, rồi mới push.

## Đổi hợp đồng và viết task (v5.1, sau R-012)
- **Đổi hợp đồng** (route, status code, mã lỗi, giới hạn, định dạng file trạng thái) ⇒ trong CÙNG commit: `git grep -n "<chuỗi / giá trị cũ>" -- "*Tests.cs"` và sửa mọi test còn đòi hợp đồng cũ. R-012: bỏ bước này ⇒ 2 đợt Codex dừng hàng loạt (8 nhánh).
- Task có trang / lệnh mới thay chỗ cái cũ ⇒ ghi rõ "giữ route / tab / lệnh cũ nào" (R-012 H8-12 mất tab Nhân viên).
- Logic đã có trong C# (CLI `AITeam.exe`) ⇒ task ghi "`team.ps1` chỉ gọi CLI, không viết lại bằng PowerShell" (R-012 V9-15 viết hai lần).

---

# Quy trình v3 — chốt từ đầu, agent chỉ code, Claude review (cũ, tra cứu)

Áp dụng từ 21/10/2026 (sau đánh giá R-001). `CLAUDE.md` chỉ để tra cứu chi tiết.

## Vai trò (cố định, không đổi giữa chừng)
| Ai | Làm | KHÔNG làm |
|---|---|---|
| **Claude** | Phân tích → thiết kế → **chốt task đầy đủ một lần** (khung code, test nghiệm thu, bố cục, file cần đọc, ngân sách, thời gian) → giao → review → tự sửa lỗi nhỏ → merge → chụp ảnh / chạy app → báo cáo | Không giao task mơ hồ để agent tự nghĩ |
| **Agent (Minh A, Lan B)** | **Chỉ viết code** trong file được giao cho test nghiệm thu pass; commit sớm; báo cáo ≤ 15 dòng | Không tự thiết kế, không lục tài liệu, không chụp ảnh, không chạy app, không làm thêm ngoài task |

## Các bước (mỗi yêu cầu của chủ dự án)
1. `team.ps1 new-request -Title "…"` → phiếu `requests/R-xxx`, ghi giờ nhận.
2. **Chốt task một lần** (`templates/task.md`), đủ các mục:
   - **Tóm tắt** cho chủ dự án;
   - **Đọc** (chỉ những file này);
   - **Ngân sách** dòng code; **Thời gian tối đa** (mặc định 25–30 phút, quá thì agy tự dừng);
   - khung code + **test nghiệm thu** commit trước lên nhánh — **thứ tự: file task lên `main` trước, rồi mới tạo nhánh từ `origin/main`** (ngược lại sẽ xung đột file task khi merge; `merge` tự xử lý nhưng tránh được thì hơn);
   - **Bố cục giao diện** cụ thể nếu có UI (chạy `tools/UiSnapshot` trước, liệt kê lỗi hiện có);
   - việc đơn giản ghi `Model: gemini-3.8-flash-medium`.
   - Việc ≤ ~100 dòng: giao thẳng Codex (`assign -Dev codex`), Claude chỉ theo dõi + nghiệm thu.
3. `team.ps1 assign -Project X -Task T-xxx` (nền). Cổng kiểm tra tự chạy: commit, BOM, build 0 warning, test (chặn treo 3 phút), phạm vi; vượt ngân sách chỉ cảnh báo.
4. `team.ps1 review -Project X -Task T-xxx` → đọc gói review. **Claude sửa code (dù 1 dòng) ⇒ chạy build + TOÀN BỘ test trước khi commit; test và commit là 2 lệnh riêng — đọc kết quả test rồi mới commit** (IMP-001, IMP-005; Hub đang chạy khóa `hub\bin` ⇒ tạm dừng Hub, test, bật lại). Chụp ảnh bằng `tools/UiSnapshot` nếu có UI.
   - Lỗi nhỏ: **giao Codex sửa** (`-Dev codex -Resume -Feedback`), Claude không tự sửa.
   - Còn sót nhưng phần chính đạt: merge, tách **task nhỏ mới**.
   - Chỉ trả agent khi phải làm lại nhiều (`assign -Resume -Feedback`).
   - Lỗi ghi `- [AG|TK|CK] (loại) …`.
5. Ghi "### Vòng N — ĐẠT" + "## Kết quả" (cho chủ dự án) → (việc cần chủ dự án quyết: `team.ps1 approval-request -Kind merge|decision|improvement …` ⇒ chủ dự án bấm trên Hub trang Cần duyệt; `merge` tự chặn nếu chờ / từ chối / nhánh đổi sau khi duyệt) → `team.ps1 merge -Project X -Task T-xxx`: merge, bảng ✅, clone sạch, dọn worktree, làm mới `ban-chay-thu\`.
6. Dừng task: `team.ps1 stop -Task T-xxx` (dừng cả lệnh assign, không bị tự giao lại).
6b. **Sau MỖI task (ngay khi merge):** điền mục "## Đánh giá quy trình" trong file task — 7 giai đoạn (tiếp nhận & quản lý · ý tưởng · plan · giao việc · code · review/QC · đánh giá & bàn giao), điểm 1–5, thời gian, vấn đề, nguyên nhân (TK/AG/CK/NV/HT), đề xuất.
6c. **Cứ 3 task merge:** tạo `improvements/IMP-xxx-*.md` (mẫu `templates/improvement.md`): điểm TB từng giai đoạn so chu kỳ trước, vấn đề lặp lại, tối đa 3 giải pháp kèm chỉ số đo. Nhỏ ⇒ áp dụng ngay; lớn (quy trình / kiến trúc) ⇒ chờ chủ dự án duyệt. Chu kỳ sau điền mục "Kết quả" (có tác dụng không ⇒ giữ / bỏ).
7. Xong yêu cầu: `team.ps1 request-report -Request R-xxx` + `staff-report` → đánh giá trong phiếu → 1 dòng `docs/HIEU-SUAT-LOG.md` → tối đa 2 cải tiến, sửa ngay vào công cụ / mẫu → tăng phiên bản quy trình.
8. Báo chủ dự án **ngắn, gạch đầu dòng**: sửa gì · làm được gì · kiểm chứng · cần cải tiến · cách test (`CHAY-THU.cmd`). Cuối phiên cập nhật `BAN-GIAO.md`.

## Quy trình v4 — ĐANG THỬ (R-006, chủ dự án chọn 25/10): mỗi AI 1 việc nhỏ, tiết kiệm token Claude
| # | Bước | Ai | Lệnh / đầu ra |
|---|---|---|---|
| 1 | Nhận yêu cầu + ý tưởng | Claude | `plans/R-xxx/brief.md` ≤ 20 dòng: mục tiêu, phạm vi, ràng buộc, tiêu chí xong, tiền tố mã task |
| 2 | Research + kế hoạch | Codex (ChatGPT), CHỈ ĐỌC | `.\team.ps1 plan -Request R-xxx -Name V6` ⇒ `plans/R-xxx/plan.md` |
| 3 | Duyệt kế hoạch | Claude | đọc 1 trang, sửa trực tiếp plan.md (không viết lại) |
| 4 | Chia việc | Codex | `.\team.ps1 prep -Request R-xxx -Task V6-01` ⇒ file task + khung + test nghiệm thu, build 0 lỗi, tự giao |
| 5 | Code | Antigravity (Minh / Lan) | như cũ; cổng KT tự động (0 token AI) |
| 6 | QC | Codex | `Review AI: có` trong mọi task do `prep` tạo ⇒ CẦN SỬA tự giao lại |
| 7 | Kiểm cuối | Claude | `.\team.ps1 review …` (gói gọn) + kiểm test nghiệm thu có thật sự kiểm hành vi + bấm thử nếu có UI ⇒ `merge` |
- **Chia đều Minh / Lan** (chủ dự án 25/10): cùng 1 tài khoản Antigravity, đều code chính, không chia vùng. `prep` / `assign` tự giao người đang ít task 🟡 hơn; Agent trong kế hoạch chỉ là gợi ý.
- Codex nghỉ / lỗi ở bước 2 hoặc 4 ⇒ bước đó Claude làm (v3). Bước 6 Codex nghỉ ⇒ BLOCKED ⇒ Claude review (Antigravity không tự duyệt code của mình).
- Thử trên 1 yêu cầu nhỏ, đo: phút + token Claude / Codex / Antigravity, vòng review, lỗi lọt tới bước 7 — rồi mới áp dụng cho mọi việc.

## Tiết kiệm token / thời gian — đã cài vào công cụ
- Agent: giới hạn thời gian theo task; chỉ đọc file liệt kê; không ảnh / không chạy app; báo cáo ≤ 15 dòng; commit sớm; test có `--blame-hang-timeout`.
- Claude sửa `team.ps1` ⇒ chạy `tools\test-verify.ps1` (test hồi quy cổng kiểm tra, IMP-001) trước khi commit. **Không để `team.ps1` sửa dở khi giao việc** — `assign` chạy đúng file trên đĩa (R-002: bảng việc ghi nhầm "AI review PASS").
- **Codex viết code** (R-004): `assign -Dev codex` (việc NHỎ — hạn mức Codex nhỏ); agy hết hạn mức ⇒ tự chuyển Codex (`-Dev agy` để tắt). Codex không ghi được .git ⇒ team.ps1 commit + push thay; task do Codex code ⇒ reviewer AI không có ⇒ Claude review. Mỗi lệnh assign tối đa 2 lượt agent.
- **Thêm tham số / nhánh mới cho `assign`** ⇒ chạy thử cả lệnh với 1 task nhỏ trước khi dùng thật; `tools\test-verify.ps1` có kiểm tra tĩnh biến cục bộ đè tham số (R-004: `$dev` đè `-Dev`).
- **Review AI tự động** (R-002): sau cổng KT, `assign` gọi `aiteam review --auto` cho task `Độ khó: L` hoặc có `- **Review AI:** có`; reviewer luôn khác agy; CẦN SỬA ⇒ tự giao lại 1 lần; BLOCKED ⇒ Claude review. Kết quả: `<workDir>\logs\<Task>.review-n.json`, Hub tab Báo cáo & review. Tắt: `-NoReview`.
- Claude: đọc trang này thay vì sổ tay 167 dòng; `review` / `merge` thay chuỗi lệnh tay; tự sửa nhỏ thay vòng agent 20–30 phút; số đo tự ghi (`metrics/runs`).

## Mục tiêu đo ở R-002 (so với R-001)
| Chỉ số | R-001 | Mục tiêu R-002 |
|---|---|---|
| Phút agent / task | 124 (gồm treo) | ≤ 30 |
| Token / task | ~2,4 triệu | ≤ 800 nghìn |
| Đạt vòng 1 | 1/3 | ≥ 2/3 |
| Thời gian treo | ~120 phút | 0 |
