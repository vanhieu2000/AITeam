# R-002 · Nang cap AITeam thanh he thong multi-AI

- **Nhận lúc:** 23:16 23/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** ✅ Xong P0 08:58 24/10/2026 (còn: user Windows thứ 2 — chờ chủ dự án)
- **Lời chủ dự án:**
  > NHIỆM VỤ: NÂNG CẤP AITEAM THÀNH HỆ THỐNG MULTI-AI TỐI ƯU CHO CÁ NHÂN (bản đầy đủ 17 mục do chủ dự án dán vào chat 23/10: audit, kiến trúc Role ↔ Provider, Codex review độc lập, cross-provider review, adapter, job state, Hub, hiệu suất, memory, bảo mật, nghiên cứu Agent Office, roadmap P0/P1/P2, triển khai P0)

## 1. Phân tích (Claude)
- Mục tiêu hiểu được:
- Dự án / phần bị ảnh hưởng:
- Quyết định đã tự chọn (không hỏi):
- Điều cần chủ dự án quyết (nếu có):
- Thiết kế: `docs/design/V4-MULTI-AI.md` (audit + kiến trúc + lộ trình)

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| P0-1 khung AITeam.Core + aiteam.exe | AITeam | Claude | ~300 | — | Chốt kiến trúc |
| P0-2 Provider adapter (agy, Codex) | AITeam | Claude | ~350 | — | Chốt hợp đồng provider |
| V4-03 Codex review chỉ đọc | AITeam | Minh (A) | ~250 | mặc định | Khung + test nghiệm thu có sẵn |
| V4-06 Trạng thái job + phục hồi | AITeam | Lan (B) | ~250 | mặc định | Khung + test nghiệm thu có sẵn |
| V4-07 định tuyến (P0-4/5) | AITeam | Minh (A) | ~230 | mặc định | lớp thuần, 39 ca nghiệm thu |
| V4-08 lệnh aiteam review (P0-3/7) | AITeam | Lan (B) | ~250 | mặc định | sau V4-07 |
| P0-7 gắn review vào team.ps1 assign | AITeam | Claude | ~60 | | glue PowerShell |
| P0-8 Hub hiện kết luận review | AITeam | Claude | ~120 | | nhỏ, Claude tự làm |

Mỗi file task phải có dòng `- **Yêu cầu:** R-002` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 09:00 | Nhận yêu cầu | |
| 09:20 | Audit xong: đọc mã AITeam + mã Agent Office (clone nông, chỉ đọc), kiểm tra CLI trên máy | Máy chưa có Codex CLI, chưa có Claude CLI; agy lưu đăng nhập ở Credential Manager; GitHub không bảo vệ nhánh được (gói miễn phí) |
| 09:35 | Viết `docs/design/V4-MULTI-AI.md` | Chờ chủ dự án chốt 3 quyết định kiến trúc |
| 09:50 | Chủ dự án chốt: C# AITeam.Core + aiteam.exe · cài Codex ngay · nghiên cứu user Windows thứ 2 ngay | |
| 09:55 | Cài Codex CLI 0.164.0 (npm `@openai/codex`). Máy đã đăng nhập ChatGPT sẵn (app Codex desktop) | `~/.codex/config.toml` là của app desktop ⇒ AITeam KHÔNG sửa, chỉ ghi đè theo lần gọi (`-m`, `-c mcp_servers={}`) |
| 10:05 | Thử review chỉ đọc: lỗi model `gpt-5.3-codex` không hỗ trợ tài khoản ChatGPT; lời nhắn phải qua stdin (PowerShell 5.1) ⇒ chạy được với `-m gpt-6.1-sol`: 39 giây, ~80k token vào (58k cache), 934 ra, JSON đúng lược đồ, repo không đổi | Đủ điều kiện làm P0-3 |
| 10:10 | Codex tìm 2 lỗ hổng thật trong `tools/secret-scan.ps1` (miễn `templates/` cả file `.pem`; bỏ qua cả dòng có chữ "example"). Claude kiểm chứng đúng, tự sửa (~6 dòng), thử trên repo tạm + quét lại 60 commit × 3 repo không chặn nhầm | Bằng chứng giá trị của reviewer thứ hai |
| 10:20 | Ghi nghiên cứu user Windows thứ 2 (B3b): khả thi bằng cách chính thống (Codex desktop cũng tạo user sandbox riêng); cần chủ dự án tạo user (UAC) + thử nghiệm dừng tiến trình khác user | Chờ chủ dự án |
| 10:50 | Claude làm P0-1 + P0-2: `core/AITeam.Core` (ProcessRunner không qua shell + giết cả cây khi quá giờ; IAgentProvider; AgyProvider; CodexProvider chỉ đọc bằng sandbox CLI; ErrorClassifier; ProviderSettings đọc team.json / team.local.json, cấu hình hỏng thì báo lỗi), `aiteam.exe providers / task-info`, 19 test với mẫu output thật | Build 0 warning; 19 + 159 test; chạy thật: agy OK, codex OK |
| 11:10 | Claude viết khung + 37 ca test nghiệm thu (đỏ đúng mong đợi) cho V4-03 Codex review và V4-06 trạng thái job, đặt trên nhánh task (không lên main) | Sửa 1 dòng cẩu thả của chính Claude trong test (`|| true`) |
| 11:15 | Giao V4-03 → Minh (A), V4-06 → Lan (B), song song, mỗi task ~250 dòng / 25 phút | Quy trình v3 lần đầu áp dụng đủ: khung code + Đọc + ngân sách + thời gian tối đa |
| 23:42 | V4-03 (Minh) xong **9 phút**, cổng kiểm tra ĐẠT lượt đầu (40 + 159 test), 321/250 dòng | So T-007: 73 phút ⇒ quy trình v3 (khung + test nghiệm thu + Đọc + thời gian tối đa) hiệu quả |
| 23:50 | Dogfood: dùng ReviewService của Minh + Codex thật review chính V4-03 ⇒ [TK] (test) lộ lỗi file lược đồ có BOM bị Codex từ chối; fail-safe đúng (BLOCKED). Claude sửa 2 dòng + test | Test giả không bắt được ⇒ cần 1 test tích hợp chạy thật (tùy chọn) |
| 23:55 | Codex hết hạn mức ('usage limit… try again Oct 24 4:19 AM') ⇒ phân loại QuotaExhausted ⇒ BLOCKED | Codex hạn mức nhỏ (dùng chung app desktop) ⇒ P0-4/5 phải chọn lọc task cần Codex review + reviewer dự phòng |
| 00:05 | Lần đầu chạy `team.ps1 merge` thật: [CK] (công cụ) xung đột add/add file task, bảng việc không khớp dòng link, kết quả clone chỉ in 1 dòng ⇒ Claude sửa cả 3; merge V4-03 xong, clone sạch 41 + 159 | |
| 23:56 | V4-06 (Lan) xong ~13 phút, 3 lượt: [TK] (phạm vi) Claude quên `JobModels.cs` trong Phạm vi · [CK] (phạm vi) cổng kiểm tra coi file khung của Claude là agent sửa ngoài phạm vi ⇒ tự giao lại 2 lần, Lan gộp file để qua (giữ nguyên 100% bảng trạng thái) | |
| 00:15 | Claude tách lại `JobModels.cs`, sửa cổng kiểm tra (file khung không bị đổi thì không tính), `verify` lại ĐẠT, `merge` V4-06: clone sạch 59 + 159 | `merge` lần 2 chạy trơn tru |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

## 4. Đánh giá sau khi xong (Claude)
`.\team.ps1 request-report -Request R-002`: 4 task giao agent (V4-03, V4-06, V4-07, V4-08) · 6 lượt · 46 phút agent · 499k token · vòng TB 1,00 · đạt vòng 1: 4/4 · cổng KT đạt lần đầu 3/4. Claude tự làm P0-1, P0-2, P0-7, P0-8.

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian (nhận → báo xong) | 23/10 tối → 24/10 ~09:30 (xen giữa: nghỉ đêm, R-003 Hub v3) |
| Thời gian Claude | ~3 giờ (audit + thiết kế V4 + P0-1/2 ~1,5 giờ; khung + nghiệm thu 4 task ~1 giờ; P0-7/8 + review + chạy thật ~40′) |
| Thời gian agent (cộng mọi lượt) | 46 phút (9 · 23 · 7 · 7) |
| Thời gian chờ không ai làm | Codex hết hạn mức 23/10 23:50 → 24/10 04:19 (không chặn việc: đổi sang Claude review) |
| Số lượt agent / số vòng review | 6 / 4 (V4-06: 2 lượt thừa do cổng KT báo nhầm) |
| Dòng code / ngân sách (agent viết) | ~1.100 / 950 (V4-03 321/250 · V4-06 242/250 · V4-07 176/230 · V4-08 ~330/220) |
| Claude tự sửa: số lần / tổng dòng | 5 / ~60 dòng (NoBom lược đồ, tách JobModels, Arg, RunAsync fail-safe, test CLI) |
| Lỗi theo nguyên nhân: TK / AG / CK / HT | 6 / 2 / 2 / 1 |

**Đánh giá nhân viên**:

| Nhân viên | Task | Lượt · phút | Vòng | Dòng / ngân sách | Làm tốt | Lỗi AG | Hướng cải thiện |
|---|---|---|---|---|---|---|---|
| Minh (A) | V4-03, V4-07 | 2 · 16′ | 1 / 1 | 497 / 480 | Regex chống treo, không gọi DateTime.Now, ghi file an toàn; giữ BOM sau khi có Lưu ý riêng | — | — |
| Lan (B) | V4-06, V4-08 | 4 · 30′ | 1 / 1 | ~570 / 470 | Xử lý đúng khi cổng KT báo nhầm (ghi lý do); CLI đủ mã thoát | BOM (lần 1) · test CLI phụ thuộc thứ tự build | Test chạy tiến trình con phải dùng file đi kèm bản build (ProjectReference), không trỏ bin\Debug của project khác |

**Team đang bất ổn ở đâu:**
- Kỹ thuật: cổng KT với luồng "khung trên nhánh" (V4-03, V4-06) — đã có `tools/test-verify.ps1`; clean-clone là chốt cuối bắt được test phụ thuộc thứ tự build (V4-08).
- Quy trình: Claude sửa `team.ps1` trên main khi agent đang chạy ⇒ wrapper chạy bản dở (V4-08).
- Quản lý (Claude): 6 lỗi TK — 2 lỗi bảo mật / độ bền chỉ Codex bắt được (Arg, lỗi bất ngờ).
- **Tốn thời gian nhất ở đâu:** thiết kế + chạy thử CLI thật (Codex, agy) ở P0-1/2/3 — đổi lại agent làm 7–9′ / task.
- **Cái gì làm tốt (giữ lại):** chạy thật với Codex trước khi merge (bắt 2 lỗi thật); định tuyến đọc giờ reset từ câu lỗi thật; 4/4 đạt vòng 1.
- **Cải tiến quy trình áp dụng cho yêu cầu sau:**
  1. Không sửa `team.ps1` trực tiếp trên main khi có agent đang chạy (assign đọc file trên đĩa): commit + `tools/test-verify.ps1` trước khi giao việc kế tiếp — ghi vào `QUY-TRINH.md`.
  2. Task có ràng buộc bảo mật / tham số dòng lệnh / dữ liệu ngoài ⇒ ghi `- **Review AI:** có` (Codex chạy thật đã bắt 4 lỗi thật: 2 ở secret-scan, 2 ở V4-08) — thêm vào mục kiểm tra `templates/task.md`.
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có