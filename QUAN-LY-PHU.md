# Sổ tay QUẢN LÝ PHỤ (Codex) — bàn giao từ Claude 25/10/2026 ~12:45

Bạn (Codex) thay Claude điều phối team khi Claude hết hạn mức. Chủ dự án: HieuDV. Trả lời ngắn, tiếng Việt, gạch đầu dòng.
Đọc thêm: `QUY-TRINH.md` (mục "Quy trình v4"), `AGENTS.md` (Lỗi hay gặp), `BAN-GIAO.md`. Chạy trong PowerShell với quyền đầy đủ (cần git push + Antigravity).

## Vai trò
| Ai | Làm |
|---|---|
| Quản lý phụ (bạn) | Viết brief → `plan` → duyệt plan.md → `prep` → theo dõi → nghiệm thu → `merge` |
| Codex (CLI, dây chuyền) | Kế hoạch, chuẩn bị task, QC, soạn skill (chạy qua team.ps1 — sandbox, không push) |
| Minh (A) · Lan (B) | Antigravity viết code — CHIA ĐỀU, tự chọn người rảnh |
| Cổng KT | `team.ps1 verify` tự chạy sau mỗi lượt code |

## Lệnh (tại D:\AITeam)
```
.\team.ps1 plan -Request R-xxx -Name <TIỀN TỐ>     # Codex lập plans/R-xxx/plan.md từ brief.md
.\team.ps1 prep -Request R-xxx -Task <MÃ> [-NoLaunch]  # Codex chuẩn bị task + tự giao Antigravity
.\team.ps1 assign -Project AITeam -Task <MÃ> [-Dev codex] [-Resume -Feedback "…"]
.\team.ps1 verify -Project AITeam -Task <MÃ>        # chạy lại cổng KT
.\team.ps1 qc -Project AITeam -Task <MÃ>            # chạy lại Codex QC
.\team.ps1 review -Project AITeam -Task <MÃ>        # gói xem nhanh
.\team.ps1 merge -Project AITeam -Task <MÃ>         # cần dòng "### Vòng N — ĐẠT" trong mục "## Review (Claude)" của file task trên nhánh
```
Duyệt plan = thêm mục `## Quyết định của quản lý (…)` có chữ **ĐÃ DUYỆT** cuối plan.md, commit + push.
Nghiệm thu: cổng KT ĐẠT + QC PASS (phát hiện nhỏ ⇒ ghi Optional) ⇒ ghi "### Vòng 1 — ĐẠT …" + "## Kết quả" vào file task TRÊN NHÁNH (worktree `D:\AITeam-work\AITeam\<a|b>-<mã>`), commit + push, rồi `merge`. Task có giao diện: chạy Hub chế độ giả (`$env:HUB_FAKE=1; $env:HUB_PORT=5058; dotnet run --project hub`) xem 1000px không tràn ngang.

## Việc đang dở (ưu tiên theo chủ dự án: SKILL + HỆ THỐNG trước, sửa giao diện sau)
1. **R-010 bộ skill** (plan đã duyệt `plans/R-010/plan.md`):
   - S1-01 / S1-02 / S1-03 (nội dung skill): đang chạy nền `prep -NoLaunch` rồi `assign -Dev codex` lần lượt. Xong ⇒ nghiệm thu (reviewer có thể BLOCKED vì Codex tự viết ⇒ quản lý phụ tự xem cấu trúc), merge.
   - S1-05 (core, review dùng skill): đang `prep` + giao Antigravity.
   - S1-04 → S1-06 (sửa team.ps1): giao SAU khi V8-01 merge, lần lượt. S1-07 cuối.
2. **R-008 hệ thống tự vận hành** (plan đã duyệt): V8-01 Minh đang code (thử lại lỗi máy + nhật ký phiếu) ⇒ nghiệm thu ⇒ V8-02 (`pipeline`) ⇒ V8-03 (`accept-pack` / `accept`).
3. **R-006**: V6-02 (chặn quá 3 vòng review, sửa team.ps1) — giao sau R-008.
4. **R-009 Hub v6.1** (plan đã duyệt) — TẠM DỪNG tới khi 1–2 xong. H7-01 đã chuẩn bị (chưa giao); H7-03 prep lỗi (mã 255) ⇒ xem `D:\AITeam-work\AITeam\prep-h7-03`, chạy lại `prep … -Task H7-03 -NoLaunch` (nếu thư mục còn thì tự đi tiếp từ build).
5. R-007: H6-01/07/08 đã merge; phần còn lại BỎ (thay bằng R-009).

## Luật bắt buộc
- Không force-push main; repo private; không đưa bí mật vào file / log / prompt.
- Giữ BOM UTF-8 cho `.ps1` / `.cs`; sửa `team.ps1` ⇒ chạy `powershell -File tools\test-verify.ps1` trước khi commit.
- Task cùng sửa `team.ps1` chạy tuần tự. Tối đa 2 lượt Antigravity + 1 Codex cùng lúc (hạn mức Codex nhỏ).
- Mỗi sự kiện ghi 1 dòng có giờ vào mục Nhật ký của `requests/R-xxx-*.md`. Lỗi mới khi nghiệm thu ⇒ thêm vào `AGENTS.md` (Lỗi hay gặp) và skill tương ứng khi đã có `skills/`.
- Quyết định sản phẩm / tải phần mềm / việc không hoàn tác ⇒ hỏi chủ dự án.
