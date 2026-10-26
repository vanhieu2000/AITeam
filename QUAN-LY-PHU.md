# Sổ tay QUẢN LÝ (Codex) — chủ dự án giao toàn quyền điều phối 26/10/2026 ~14:10

Chủ dự án 26/10: "để việc của bạn (Claude) qua cho Codex làm; Claude chỉ phụ trách đẩy cập nhật lên git và chạy lại Hub sau khi hoàn thành". Bạn (Codex) điều phối TOÀN BỘ. Chủ dự án: HieuDV. Trả lời ngắn, tiếng Việt, gạch đầu dòng.
Đọc thêm: `QUY-TRINH.md` (mục "Quy trình v4"), `AGENTS.md` (Lỗi hay gặp), `BAN-GIAO.md`. Chạy trong PowerShell với quyền đầy đủ (cần git push + Antigravity).

## Vai trò
| Ai | Làm |
|---|---|
| Quản lý (bạn, Codex) | Viết brief → `plan` → duyệt plan.md → `prep` / `assign` / `pipeline` → theo dõi (việc 🟡 > 60 phút / test treo ⇒ xử lý ngay) → đọc cổng KT + QC → tự sửa qua `assign -Dev codex -Resume` → **NGHIỆM THU** (ghi "### Vòng N — ĐẠT (Codex)" + "## Kết quả" trên nhánh) → thêm dòng "Chờ Claude merge" |
| Claude | CHỈ `team.ps1 merge` (clone sạch + push main) các task trong mục "Chờ Claude merge", rồi chạy lại Hub. Không review, không sửa code. |
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
# merge: Claude làm (đọc mục "Chờ Claude merge"). Bạn KHÔNG chạy team.ps1 merge.
```
Duyệt plan = thêm mục `## Quyết định của quản lý (…)` có chữ **ĐÃ DUYỆT** cuối plan.md, commit + push.
Đề xuất nghiệm thu (việc của bạn): cổng KT ĐẠT + QC PASS ⇒ ghi vào file task TRÊN NHÁNH (worktree `D:\AITeam-work\AITeam\<a|b>-<mã>`) mục "### Đề xuất nghiệm thu (Codex quản lý phụ)": đã kiểm gì, phát hiện còn mở, đề xuất ĐẠT / CẦN SỬA; + "## Kết quả" nháp; commit + push NHÁNH; thêm 1 dòng vào mục "Chờ Claude chốt" cuối file này. Claude đọc rồi tự ghi "### Vòng N — ĐẠT" và merge. Task có giao diện: chạy Hub chế độ giả (`$env:HUB_FAKE=1; $env:HUB_PORT=5058; dotnet run --project hub`), xem 1000px không tràn ngang, ghi vào đề xuất.

## Việc đang dở (cập nhật 16:46 26/10/2026)
1. **R-009 Hub v6.1:** H7-01/03/07 đã merge. H7-06 đã kết thúc lượt 6 lúc 16:42, cổng KT KHÔNG ĐẠT: build 0 warning, Core 690/690 pass, Hub 739 pass/24 fail; lỗi NullReferenceException tại Projects.cshtml:4 vẫn còn sau thử lại. QC chưa chạy. Tạm dừng vì quá 3 lượt; không giao lượt 7, không sửa test nghiệm thu. Log H7-06-verify-2.md; nhánh ee20e18 đã push, worktree sạch.
2. **H7-02 tạm dừng:** đã có lượt code 4, vượt giới hạn 3 vòng. `H7-02-verify-2.md` KHÔNG ĐẠT: build 0 warning, Hub 721 pass; Core còn 2 ca S1-04 thiếu `Resolve-WorkflowAssignment` sau thử lại. Chờ Claude xử lý bất nhất harness/phạm vi; không sửa test nghiệm thu, không tự giao lượt 5.
3. **H7-04 tạm dừng:** lượt code 3 chỉ cập nhật báo cáo; `H7-04-verify-2.md` KHÔNG ĐẠT: 2 warning xUnit2031 trong test nghiệm thu bất biến và 4 lỗi Hub còn sau thử lại (V5-05/W1-02). Chờ Claude chốt cách xử lý test/thiết kế; không sửa test nghiệm thu hoặc tự giao thêm. H7-05 chờ H7-04 được chốt/merge.
4. **R-011:** W1-01/02/05/06/07 đã merge. Đã chạy nền `team.ps1 prep -Request R-011 -Task W1-03` lúc 16:46 26/10/2026, PID 39704; log: `D:\AITeam-work\AITeam\logs\W1-03.deputy-20261026-164647.out.txt` và `D:\AITeam-work\AITeam\logs\W1-03.deputy-20261026-164647.err.txt`. Vòng sau kiểm tra tiến trình, prep/assign/cổng KT/QC; không giao trùng. W1-04 chờ W1-03.
5. **Hệ thống + skill:** S1-01…08, V8-01/02/03/09/10 đã merge theo bảng việc; bỏ các chỉ dẫn giao lại cũ. Không đưa việc đã merge vào chờ chốt.
6. **V6-02:** tiếp tục tạm dừng, quá 3 vòng; còn 2 ca Fresh_ai_log ở lượt Codex 5. Không giao lại trong vòng này.
7. **Hàng chờ nhỏ:** test V8-02/V8-03 chập chờn khi máy bận; cache `.state/agy-models.json` khi switch-account; W1-05 ca QC trước Verify; W1-02 dispose singleton factory. Chưa mở rộng phạm vi.

Luật: test dùng `core.tests/Support`; không xóa test lượt trước; quá 3 vòng thì tạm dừng và ghi lý do. Chỉ đề xuất nghiệm thu khi cổng KT ĐẠT + QC PASS trên bản sửa hiện tại; Claude chốt và merge.
## Luật bắt buộc
- Commit do chính `team.ps1` tự làm khi chạy plan / prep / assign / verify / qc (bảng việc, `metrics/runs/**`, nhật ký phiếu, file task mới) là ĐƯỢC PHÉP. Chỉ cấm: `team.ps1 merge`, tự đẩy code lên main, force-push, xóa nhánh / dữ liệu.
- Không force-push main; repo private; không đưa bí mật vào file / log / prompt.
- Giữ BOM UTF-8 cho `.ps1` / `.cs`; sửa `team.ps1` ⇒ chạy `powershell -File tools\test-verify.ps1` trước khi commit.
- Task cùng sửa `team.ps1` chạy tuần tự. Tối đa 2 lượt Antigravity + 1 Codex cùng lúc (hạn mức Codex nhỏ).
- Mỗi sự kiện ghi 1 dòng có giờ vào mục Nhật ký của `requests/R-xxx-*.md`. Lỗi mới khi nghiệm thu ⇒ thêm vào `AGENTS.md` (Lỗi hay gặp) và skill tương ứng khi đã có `skills/`.
- Quyết định sản phẩm / tải phần mềm / việc không hoàn tác ⇒ hỏi chủ dự án.

## Chờ Claude merge (Codex ghi: `- <MÃ> · nhánh · Vòng N ĐẠT · giờ`; Claude xóa dòng khi đã merge)
- (trống)

## Chờ Claude chốt
- (trống; chưa có task mới đủ cổng KT ĐẠT + QC PASS)

## Nhật ký quản lý phụ
- 16:52 25/10/2026: Một vòng điều phối: đối chiếu bảng việc/log/worktree; H7-07 được phiên Claude khác chốt và merge trong lúc kiểm tra; S1-04 hết thời gian, chưa commit helper. Không chạy lại prep/assign vì tự commit/push ngoài danh sách cho phép; không có đề xuất mới chờ Claude. Không chạy build/test mới; dùng kết quả cổng KT và QC có sẵn.

- 17:16 25/10/2026: Một vòng điều phối: đã đồng bộ main, đối chiếu bảng việc/log/tiến trình/worktree; S1-04 cổng KT lượt 2 ĐẠT nhưng QC CẦN SỬA, lượt code 3 đang chạy nên không gọi lệnh giao trùng. Chưa có đề xuất nghiệm thu mới; cập nhật bảng việc và nhật ký R-010; không chạy build/test mới, không merge.

- 18:16 25/10/2026: Một vòng điều phối: main khớp origin/main; đối chiếu bảng việc, log và tiến trình. S1-04 đã merge; S1-06 chuyển Codex lượt 2 sau khi Antigravity hết hạn mức, còn chạy nên không gọi plan/prep/assign/verify/qc trùng. Chưa có task mới đủ cổng KT ĐẠT + QC PASS để đề xuất nghiệm thu. Cập nhật việc đang dở và nhật ký R-010; không chạy build/test mới, không merge.

- 16:16 26/10/2026: Một vòng điều phối: fetch origin, main khớp origin/main; đối chiếu bảng việc, log và tiến trình. H7-02 tạm dừng sau lượt 4; H7-04 vướng test nghiệm thu bất biến, không giao thêm. Giao lại H7-06 chạy nền PID 32068 sau lỗi quá tải (log H7-06.deputy-20261026-161620.out.txt/.err.txt); vòng sau xem tiếp. Chưa có đề xuất nghiệm thu mới; không chạy build/test riêng, không merge.

- 16:46 26/10/2026: Một vòng điều phối: origin/main khớp main; H7-06 kết thúc lượt 6, cổng KT KHÔNG ĐẠT (build 0 warning; Core 690 pass; Hub 739 pass/24 fail), tạm dừng theo giới hạn 3 lượt. H7-02/H7-04/V6-02 giữ tạm dừng. Chạy nền prep W1-03, PID 39704, log W1-03.deputy-20261026-164647.out.txt/.err.txt; vòng sau xem tiếp. Chưa có task mới đủ ĐẠT + PASS để đề xuất nghiệm thu; không chạy build/test riêng, không merge.
