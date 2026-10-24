# R-002 · Nang cap AITeam thanh he thong multi-AI

- **Nhận lúc:** 23:16 23/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** 🟡 Đang làm
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
| P0-8 Hub hiện kết luận review | AITeam | (sau) | | | |

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
Dán kết quả `.\team.ps1 request-report -Request R-002`, rồi điền:

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian (nhận → báo xong) | |
| Thời gian Claude (phân tích + thiết kế + review + tự sửa) | |
| Thời gian agent (cộng mọi lượt) | |
| Thời gian chờ không ai làm (vd. chờ hạn mức, chờ chủ dự án) | |
| Số lượt agent / số vòng review | |
| Dòng code / ngân sách | |
| Claude tự sửa: số lần / tổng dòng | |
| Lỗi theo nguyên nhân: TK / AG / CK | |

**Đánh giá nhân viên** (chi tiết ghi thêm vào `staff/<mã>-<tên>.md`):

| Nhân viên | Task | Lượt · phút | Vòng | Dòng / ngân sách | Làm tốt | Lỗi AG | Hướng cải thiện |
|---|---|---|---|---|---|---|---|

**Team đang bất ổn ở đâu** (mỗi mục ghi bằng chứng từ nhật ký):
- Kỹ thuật (công cụ, build, cổng kiểm tra, môi trường, model):
- Quy trình (thiết kế, chia task, giao việc, review, phối hợp 2 máy):
- Quản lý (Claude: lỗi TK, quyết định sai, chậm review):
- **Tốn thời gian nhất ở đâu:**
- **Cái gì làm tốt (giữ lại):**
- **Cải tiến quy trình áp dụng cho yêu cầu sau** (tối đa 2, cụ thể, sửa file nào):
  1.
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có / chưa
