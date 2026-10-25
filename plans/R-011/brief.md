# Brief R-011 — Bộ test/QC chuẩn · Kế hoạch + mockup trước khi làm · Trang "Việc" hiện bước đang chạy

Chủ dự án (25/10 21:15): "test qc cần có bộ chuyên chứ mỗi lần lại viết lại thì chả lâu … bảo nó trả lời lại kế hoạch hay nếu liên quan đến giao diện thì cần vẽ giao diện để duyệt trước rồi mới làm … liệt kê hết việc cần làm theo dự án rồi tôi chọn và cái nào thì hiện ra xem đang đến bước nào ví dụ như giao việc hay đến bước code hay test nghiệm thu".

## Mục tiêu
1. **Bộ test/QC chuẩn dùng lại** — task mới KHÔNG tự viết harness / fixture / kiểm tra chung.
   - Đã có: `core.tests/Support/TestShell.cs` (`TestShell.PowerShell`, `TestShell.Run`, `TempRepo`). Mở rộng thêm (cùng thư mục `core.tests/Support/`):
     - `RealFile.Copy(relativePathInRepo, targetDir)`: chép file THẬT trong repo (giữ nguyên byte: BOM, CRLF, tên) — dùng thay fixture tự chế.
     - `TeamScript`: nạp hàm từ `team.ps1` / `tools/*.ps1` / `scripts/*.ps1` vào sandbox tạm (chép kèm `tools/`, `scripts/`, `templates/`), giả lập `agy` / `codex` / `git push` bằng hàm giả — gom lại các harness đang lặp ở `AcceptanceS104…`, `AcceptanceV801…`, `AcceptanceV802…`.
     - Hub: `hub.tests` đã có cách dựng trang — ghi rõ lớp dùng chung hiện có trong kế hoạch (CHƯA XÁC MINH tên).
   - Một file hướng dẫn ngắn `docs/TEST-CHUAN.md` (≤ 60 dòng): ca bắt buộc theo loại task (lệnh team.ps1 / dịch vụ Hub / trang Hub / tài liệu-skill), dùng helper nào, mẫu 1 test.
   - Codex QC: checklist cố định trong `skills/09-qc/SKILL.md` + đầu ra JSON như hiện có; không viết lại mỗi lần.
2. **Kế hoạch / mockup trả về trước khi làm** (quy trình): `team.ps1 plan` đã trả `plans/R-xxx/plan.md`. Bổ sung:
   - Task có giao diện ⇒ kế hoạch có mục `## Mockup` chứa 1 khối ```html tĩnh (≤ 120 dòng, dùng CSS của Hub); `team.ps1 plan` tách khối này ra `docs/design/<R-xxx>-mockup.html` để chủ dự án mở xem.
   - Chủ dự án / Claude duyệt (mục "Quyết định của quản lý … ĐÃ DUYỆT") rồi mới `prep`. `prep` từ chối khi plan chưa duyệt hoặc task giao diện chưa có mockup đã duyệt.
3. **Trang "Việc" trên Hub** (thay vị trí "Đơn duyệt"/bảng việc hiện có — tận dụng, không làm trùng):
   - Chọn dự án (menu "Dự án" chung như R-009) ⇒ danh sách yêu cầu `requests/R-xxx-*.md` + task thuộc từng yêu cầu (dòng `- **Yêu cầu:** R-xxx` trong `tasks/*.md`).
   - Chọn 1 việc ⇒ thanh bước: Tiếp nhận → Kế hoạch → Duyệt kế hoạch → Chuẩn bị → Code → Cổng KT → QC → Nghiệm thu → Merge; bước hiện tại tô đậm, có giờ + người (Minh / Lan / Codex / Claude), lỗi gần nhất nếu có.
   - Nguồn dữ liệu CÓ SẴN, chỉ đọc: nhật ký phiếu (`- <giờ> | <task> | Prep|Assign|Verify|Qc|Merge | lượt N | kết quả <!-- workflow:… -->`), trạng thái bảng `tasks/README.md` (🟡/🔵/🔴/✅), `plans/R-xxx/plan.md` (có "ĐÃ DUYỆT" hay chưa), `logs/<task>-verify-N.md`, `logs/<task>.review-N.json`.
   - Chỉ đọc, không nút chạy lệnh trong task này (nút duyệt đã có ở "Đơn duyệt").
   - Màn hình tối thiểu 1000px không tràn ngang; chuỗi dài nhất: tiêu đề yêu cầu ~140 ký tự.

## Ràng buộc
- Dữ liệu THẬT: đọc file thật trong repo trước khi đặc tả (projects.json là MẢNG; phiếu `requests/R-xxx-<slug>.md`, tiêu đề `## 3. Nhật ký (…)`, CRLF).
- Không đổi định dạng nhật ký / bảng việc đang dùng (các lệnh khác đang ghi vào).
- Task sửa `team.ps1` chạy tuần tự; không đụng V8-02 đang làm (`tools/workflow-pipeline.ps1`).
- Ưu tiên ít task; task ≤ ~100 dòng ghi "Codex làm thẳng" (không qua prep).
- Model task khó: `gemini-4-argon-high` (Opus hết lượt).

## Tiêu chí nghiệm thu của quản lý
- Task mới sau R-011 viết test chỉ bằng helper chung (cổng KT cảnh báo nếu file test mới có `new ProcessStartInfo`).
- `plan` cho yêu cầu có giao diện sinh ra file mockup mở được; `prep` chặn khi chưa duyệt.
- Trang Việc hiện đúng bước cho R-010 (✅ Merge), V8-02 (đang Code/Cổng KT), R-009 (Kế hoạch đã duyệt, task chưa làm) — đối chiếu nhật ký thật.
