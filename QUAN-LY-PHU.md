# Sổ tay QUẢN LÝ PHỤ (Codex) — bàn giao từ Claude 25/10/2026 ~12:45

Bạn (Codex) thay Claude điều phối team khi Claude hết hạn mức. Chủ dự án: HieuDV. Trả lời ngắn, tiếng Việt, gạch đầu dòng.
Đọc thêm: `QUY-TRINH.md` (mục "Quy trình v4"), `AGENTS.md` (Lỗi hay gặp), `BAN-GIAO.md`. Chạy trong PowerShell với quyền đầy đủ (cần git push + Antigravity).

## Vai trò
| Ai | Làm |
|---|---|
| Quản lý phụ (bạn) | Viết brief → `plan` → duyệt plan.md → `prep` / `assign` → theo dõi → chuẩn bị ĐỀ XUẤT nghiệm thu |
| Claude (quản lý chính) | **Chốt cuối: nghiệm thu ("### Vòng N — ĐẠT") + `merge` (merge, clone sạch, push main)** — quản lý phụ KHÔNG làm bước này |
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
# merge KHÔNG phải việc của quản lý phụ — Claude chốt + merge + push main
```
Duyệt plan = thêm mục `## Quyết định của quản lý (…)` có chữ **ĐÃ DUYỆT** cuối plan.md, commit + push.
Đề xuất nghiệm thu (việc của bạn): cổng KT ĐẠT + QC PASS ⇒ ghi vào file task TRÊN NHÁNH (worktree `D:\AITeam-work\AITeam\<a|b>-<mã>`) mục "### Đề xuất nghiệm thu (Codex quản lý phụ)": đã kiểm gì, phát hiện còn mở, đề xuất ĐẠT / CẦN SỬA; + "## Kết quả" nháp; commit + push NHÁNH; thêm 1 dòng vào mục "Chờ Claude chốt" cuối file này. Claude đọc rồi tự ghi "### Vòng N — ĐẠT" và merge. Task có giao diện: chạy Hub chế độ giả (`$env:HUB_FAKE=1; $env:HUB_PORT=5058; dotnet run --project hub`), xem 1000px không tràn ngang, ghi vào đề xuất.

## Việc đang dở (ưu tiên theo chủ dự án: SKILL + HỆ THỐNG trước, sửa giao diện sau)
1. **R-010 bộ skill:** S1-01/02/03/05/08 đã merge theo bảng việc. S1-04 đã prep, nhánh `agent-a/S1-04-noi-skill-vao-plan-prep-va-giao-code`, worktree `D:\AITeam-work\AITeam\a-s1-04`; lượt 1 hết 25 phút lúc 16:46, helper còn thay đổi chưa commit, chưa có cổng KT/QC cho lượt này. Không coi status SUCCESS của agy là hoàn thành.
   - Chưa giao lại: `assign` gọi `Save-RunMetrics`, tự commit `metrics/runs/**` rồi push main (team.ps1:1699–1701), ngoài danh sách chủ dự án cho phép trong vòng điều phối. `prep` cũng tự commit file task lên main (team.ps1:1060–1062). Cần luồng chạy không push các file này trước khi tiếp tục; không sửa team.ps1 trong vòng này.
   - S1-06 chờ S1-04 được Claude chốt/merge; S1-07 chờ S1-04 + S1-06. Không giao song song task sửa team.ps1.
2. **R-008 hệ thống tự vận hành:** V8-01 đã merge vòng 1. Tiếp theo V8-02 → V8-03 theo plan đã duyệt; cùng vướng giới hạn commit tự động của prep/assign, ưu tiên sau bộ skill.
3. **R-006:** V6-02 giao sau R-008.
4. **R-009 Hub v6.1:** H7-01 đã merge; H7-07 cổng KT ĐẠT (`H7-07-verify-6.md`: build 0 warning, 527 Hub + 425 Core pass) và QC PASS (`H7-07.review-4.json`: 0 phát hiện). Trong vòng này phiên Claude khác đã nghiệm thu rồi merge (`26dc880`, bảng `f954fbf`); không sửa chồng hay đưa task đã merge vào chờ chốt. Trình duyệt của phiên quản lý phụ không khả dụng (inventory rỗng), chưa tự xác minh UI; báo cáo Claude trên nhánh ghi đã xem 1000×800. H7-03 chưa giao; giữ ưu tiên skill + hệ thống.
5. **R-007:** phần còn lại bỏ, thay bằng R-009.
## Luật bắt buộc
- Commit do chính `team.ps1` tự làm khi chạy plan / prep / assign / verify / qc (bảng việc, `metrics/runs/**`, nhật ký phiếu, file task mới) là ĐƯỢC PHÉP. Chỉ cấm: `team.ps1 merge`, tự đẩy code lên main, force-push, xóa nhánh / dữ liệu.
- Không force-push main; repo private; không đưa bí mật vào file / log / prompt.
- Giữ BOM UTF-8 cho `.ps1` / `.cs`; sửa `team.ps1` ⇒ chạy `powershell -File tools\test-verify.ps1` trước khi commit.
- Task cùng sửa `team.ps1` chạy tuần tự. Tối đa 2 lượt Antigravity + 1 Codex cùng lúc (hạn mức Codex nhỏ).
- Mỗi sự kiện ghi 1 dòng có giờ vào mục Nhật ký của `requests/R-xxx-*.md`. Lỗi mới khi nghiệm thu ⇒ thêm vào `AGENTS.md` (Lỗi hay gặp) và skill tương ứng khi đã có `skills/`.
- Quyết định sản phẩm / tải phần mềm / việc không hoàn tác ⇒ hỏi chủ dự án.

## Chờ Claude chốt (quản lý phụ ghi, Claude xóa dòng khi đã merge)
- (trống)

## Nhật ký quản lý phụ
- 16:52 25/10/2026: Một vòng điều phối: đối chiếu bảng việc/log/worktree; H7-07 được phiên Claude khác chốt và merge trong lúc kiểm tra; S1-04 hết thời gian, chưa commit helper. Không chạy lại prep/assign vì tự commit/push ngoài danh sách cho phép; không có đề xuất mới chờ Claude. Không chạy build/test mới; dùng kết quả cổng KT và QC có sẵn.
