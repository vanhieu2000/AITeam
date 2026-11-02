# Yêu cầu sửa giao diện và chức năng AITeam — giao thẳng cho Codex

Chủ dự án HieuDV giao TRỰC TIẾP cho Codex (03/11/2026). Trả lời bằng tiếng Việt, ngắn, gạch đầu dòng.

## 0. Luật làm việc (quy trình v5 — `QUY-TRINH.md` mục đầu)
- Repo `vanhieu2000/AITeam`. Đầu mỗi việc: `git pull` trên `main`, đọc `QUY-TRINH.md` (mục v5), `AGENTS.md` (Lỗi hay gặp, nhất là các dòng cuối file).
- Mỗi task một nhánh. Nhánh đã có thì làm tiếp trên nhánh đó (`git fetch` + `git merge --no-edit origin/main` trước). Task mới: nhánh `agent-c/<MÃ>-<tên>` từ `origin/main`, tạo file task từ `templates/task.md` (có Tóm tắt, Phạm vi, Không làm, Tiêu chí, Ngân sách, `- **Yêu cầu:** R-012`).
- Xong mỗi task: `dotnet build AITeam.sln -warnaserror -nodeReuse:false` (0 warning) + `dotnet test AITeam.sln --blame-hang-timeout 3m` (pass hết). Sửa `team.ps1` ⇒ thêm `powershell -File tools\test-verify.ps1`. Điền "Báo cáo của agent" trong file task, commit, **push nhánh**. KHÔNG merge / push `main` — Claude review, chốt, merge.
- KHÔNG sửa / xóa / Skip test. Test nghiệm thu `Acceptance*Tests.cs`: chỉ được sửa phần dựng môi trường (fixture / harness) khi chứng minh được nó sai, KHÔNG sửa assert, ghi rõ trong báo cáo.
- Giữ BOM UTF-8 cho `.ps1` / `.cs` / `.cshtml`. Kho đang PUBLIC: không đưa bí mật, khóa, mật khẩu, đường dẫn nội bộ công ty vào file / commit.
- Cùng một test đỏ sau 2 lần sửa ⇒ dừng task đó, ghi lỗi + stack trace vào báo cáo, chuyển task khác.
- Không dùng Antigravity. Hub chạy giả lập để xem giao diện: `$env:HUB_FAKE=1; $env:HUB_PORT=5058; dotnet run --project hub`; task giao diện phải không tràn ngang ở 1000px.

## 1. Việc dở — làm trước (theo thứ tự)
| # | Task | Nhánh | Tình trạng 03/11 | Cần làm |
|---|---|---|---|---|
| 1 | H7-02 Điều phối plan/prep/QC theo Map | `agent-b/H7-02-dieu-phoi-plan-prep-qc-theo-map` (7f21688) | Đã sửa harness S1-04 (91/91 test S1-04 + H7-02 xanh) nhưng CHƯA merge main; lần chạy trong sandbox có 9 Hub + 5 Core đỏ do bị chặn quyền ghi | Merge `origin/main` (chú ý `team.ps1` và `core.tests/AcceptanceS104SkillPromptsTests.cs` — W1-03 cũng sửa file test này), chạy test NGOÀI sandbox, sửa phần đỏ thật |
| 2 | V6-02 Chặn giao lại, xin quyết định | `agent-a/V6-02-chan-giao-lai-va-xin-quyet-dinh` | Chậm main 182 commit; còn 2 ca `Fresh_ai_log` đỏ | Merge main, sửa logic nhận diện log AI mới; không đổi test |
| 3 | H7-05 | (theo `tasks/H7-05-*` nếu có, hoặc plan R-009) | Chờ H7-04 — H7-04 ĐÃ merge | Làm theo kế hoạch R-009 |
| 4 | W1-04 | (theo plan R-011) | Chờ W1-03 — W1-03 xanh, đang chờ Claude review | Bắt đầu sau khi W1-03 vào main |

## 2. Giao diện Hub (mỗi dòng 1 task nhỏ ≤ ~150 dòng, nhánh `agent-c/H8-0x-…`)
| Mã | Trang | Yêu cầu |
|---|---|---|
| H8-01 | Phòng nghiên cứu | Sau "Hỏi" dùng Post-Redirect-Get để F5 không gửi lại câu hỏi. Nhà cung cấp đang nghỉ (hết hạn mức) ⇒ khóa nút Hỏi và hiện lý do ngay, không gửi rồi mới báo lỗi |
| H8-02 | Tài khoản AI | Antigravity: tách 2 trạng thái "Đăng nhập" và "Đang chạy việc" (hiện tại "Đang chạy việc" che mất trạng thái đăng nhập). Chế độ `HUB_FAKE` bấm Lưu KHÔNG được ghi `accounts.json` thật |
| H8-03 | Task (chi tiết) | Các vòng review không hiện trùng (danh sách + Markdown gốc); review cũ không có `###` vẫn tách đúng vòng; bỏ số trang ghi cứng 8 trong `PaginationSummary` |
| H8-04 | Tổng quan / Hiệu suất | Cột phải không cắt chữ khi cửa sổ hẹp (cho xuống dòng / co cột) |
| H8-05 | Việc chạy nền (Văn phòng → điều khiển) | Đọc output PowerShell 5.1 đúng code page để tiếng Việt không lỗi phông; giới hạn danh sách job trong bộ nhớ (giữ 200 gần nhất) |
| H8-06 | Đơn duyệt | Dữ liệu giả (`hub/DevData`) thêm 2 đơn "Chờ duyệt" để nhìn thấy nút Duyệt / Từ chối ngay trong dòng bảng |

## 3. Chức năng / dây chuyền (nhánh `agent-c/V9-0x-…`)
| Mã | Yêu cầu |
|---|---|
| V9-01 | Xóa cache `.state/agy-models.json` khi agy báo model không hợp lệ và sau `switch-account` |
| V9-02 | Test V8-02 / V8-03 chập chờn khi máy bận: bỏ phụ thuộc thời gian thật (đồng hồ giả / chờ có điều kiện) |
| V9-03 | Cảnh báo khi một nhà cung cấp AI lỗi ≥ 3 lần liên tiếp (ghi `.state/providers.json`, hiện trên Hub trang Tài khoản / Nhà cung cấp) |
| V9-04 | Parser review nhận diện tiêu đề `##<tab>` (V6-01 còn mở) |
| V9-05 | Helper test W1-02: dispose singleton tạo bằng factory khi `Build` ném lỗi |
| V9-06 | `CodexProvider` (core/AITeam.Core/Providers/CodexProvider.cs): thêm `-c windows.sandbox="unelevated"` và cho phép ghi đè `openai_base_url` qua `team.json` → `codex.baseUrl`; sau mỗi lần chạy gọi `dotnet build-server shutdown` (xem QUY-TRINH v5 mục "Chạy Codex CLI") |
| V9-07 | Cổng KT (`team.ps1 verify`): build với `-nodeReuse:false` và không chạy song song 2 build; khi file task có dòng `- **Cho phép sửa harness:** <file>` thì không chặn sửa phần dựng môi trường của file đó (vẫn chặn sửa assert — so khớp dòng `Assert.`) |

## 4. R-013 · OpenCode (CHỈ DeepSeek) làm developer thứ hai
Đã cài máy công ty: OpenCode 1.19.5, cấu hình khóa DeepSeek (`templates/opencode-config.jsonc`), quyền hẹp (`templates/opencode-quyen-han.json`). Cách chạy đúng: `QUY-TRINH.md` mục v5 "Chạy OpenCode".

| Mã | Yêu cầu | Xong khi |
|---|---|---|
| O-02 | `team.ps1 assign -Dev opencode`: ghi prompt ra file; gọi `opencode run "<lời nhắn>" -f <prompt> --dir <worktree> --format json --title <Mã>` với stdin rỗng và `OPENCODE_CONFIG` = bản sao `templates/opencode-quyen-han.json`; KHÔNG `--auto`; đọc dòng `type=text` cuối làm báo cáo, `step_finish.part.tokens` cho `metrics`; nhận diện 429 / hết hạn mức. `doctor` kiểm có lệnh `opencode`. Chỉ chấp nhận model chứa `deepseek` (`team.json` → `opencode.model`, mặc định `opencode/deepseek-v4.1-flash-free`) | Test bằng `opencode` giả (script in JSON mẫu), không gọi mạng trong test |
| O-03 | Định tuyến khi hết hạn mức: codex → opencode (bỏ agy khi không có model Argon 4); tôn trọng `.state/providers.json` | Test: codex nghỉ ⇒ chọn opencode; cả hai nghỉ ⇒ dừng, báo rõ |
| O-04 | Hub trang Tài khoản AI: thêm OpenCode (đã cài?, phiên bản, model đang khóa, trạng thái nghỉ) | Ảnh 1000px; chế độ giả không gọi `opencode` thật |
| O-05 | Model free không cam kết riêng tư ⇒ `assign -Dev opencode` từ chối dự án private (HDVTool, ChinaAndroidToolbox) trừ khi `team.json` có `opencode.paid = true` | Test 2 ca |

## 5. Báo cáo cho chủ dự án (cuối đợt)
- Task nào đã push nhánh, sẵn sàng cho Claude review; task nào dừng và vì sao.
- Lỗi mới gặp ⇒ thêm 1 dòng vào CUỐI `AGENTS.md`.
- Việc cần chủ dự án: đăng nhập / mua gói DeepSeek trả phí / quyết để kho AITeam public hay private.
