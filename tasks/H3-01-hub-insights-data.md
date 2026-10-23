# H3-01 · Hub v3: dịch vụ dữ liệu đánh giá team (insights)

- **Agent:** A
- **Nhánh:** `agent-a/H3-01-hub-insights-data`
- **Ưu tiên:** cao
- **Ngân sách:** ~320 dòng
- **Yêu cầu:** R-003
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Không có chức năng mới nhìn thấy ngay** — đây là phần dữ liệu cho các trang Hub v3 (Tổng quan, Đội ngũ, Quy trình & cải tiến).
- Đọc từ file task: độ khó, số vòng review, lỗi theo nguyên nhân, bảng "Đánh giá quy trình" 7 giai đoạn.
- Đọc số đo từng lượt agent (phút, lượt) và giờ merge từ git ⇒ dựng biểu đồ thời gian.
- Tổng hợp: thẻ điểm từng nhân viên, điểm trung bình từng giai đoạn, danh sách vấn đề, chu kỳ cải tiến IMP, chỉ số Tổng quan.

## Đọc (CHỈ những file này)
- `hub/Models/InsightModels.cs` (model đã chốt — KHÔNG đổi)
- `hub/Services/InsightParsers.cs` (khung — điền các chỗ `TODO(agent)`)
- `hub/Services/TeamInsightsService.cs` (khung — điền các chỗ `TODO(agent)`)
- `hub.tests/AcceptanceH301Tests.cs` (test nghiệm thu — đọc kỹ, đây là đặc tả chính xác)
- `hub/Services/Contracts.cs`, `hub/Models/HubModels.cs` (chỉ để biết kiểu có sẵn)

## Bối cảnh
Thiết kế: `docs/design/HUB-V3.md` mục 2–3. Chú thích trong khung code là đặc tả từng hàm; khi chú thích và test khác nhau thì theo test.

## Mục tiêu
Điền thân hàm `InsightParsers` (hàm thuần, chuỗi vào ⇒ dữ liệu ra, không ném lỗi với dữ liệu hỏng), `GitHistory.MergedAt` (gọi git bằng `ProcessStartInfo.ArgumentList`, không qua shell, chặn id lạ) và `TeamInsightsService` (ghép dữ liệu theo 9 bước trong chú thích lớp). Dùng `System.Text.Json` và `Regex` có sẵn; không thêm thư viện.

## Phạm vi (được sửa)
- `hub/Services/InsightParsers.cs`
- `hub/Services/TeamInsightsService.cs`
- `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `hub/Models/InsightModels.cs`, `Program.cs` (Claude tự đăng ký DI khi merge), không làm trang / giao diện, không sửa `team.ps1`.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceH301Tests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 30 ca nghiệm thu H3-01).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 30 ca nghiệm thu H3-01). | `dotnet build AITeam.sln` → `Build succeeded. 0 Warning(s) 0 Error(s)`. `dotnet test AITeam.sln --blame-hang-timeout 3m` → `AITeam.Core.Tests: Passed 59/59`, `AITeam.Hub.Tests: Passed 189/189` (trong đó `AcceptanceH301Tests`: `Passed 30/30`). |

- **Đã làm:** Hiện thực đầy đủ các hàm phân tích chuỗi thuần trong `InsightParsers`, truy vấn giờ merge an toàn qua `ProcessStartInfo.ArgumentList` + cache 60s trong `GitHistory.MergedAt`, và dịch vụ tổng hợp 9 bước trong `TeamInsightsService`.
- **File đã đổi:** `hub/Services/InsightParsers.cs`, `hub/Services/TeamInsightsService.cs`, `tasks/H3-01-hub-insights-data.md`.
- **Điều chưa chắc:** Không có.

## Review (Claude)
### Vòng 1 — ĐẠT (24/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 30/30 ca nghiệm thu; 59 + 189 test; build 0 warning; ~8 phút, 1 lượt.
- Code: regex gọn, chịu được dữ liệu hỏng (JSON hỏng, ô thiếu, file không có), gọi git bằng ArgumentList + chặn id lạ, đúng 9 bước ghép dữ liệu.
- [AG] (BOM) mất BOM UTF-8 ở 2 file khung `InsightParsers.cs`, `TeamInsightsService.cs`
- [CK] (BOM) cổng kiểm tra chỉ so BOM với `main` ⇒ không bắt được file khung mới chỉ có trên nhánh
- [CK] (phạm vi) cổng kiểm tra tính cả khung của Claude vào ngân sách ⇒ báo vượt nhầm 508/320 (thật: 285/320)
- [TK] (hiệu năng) khung không nói rõ cache giờ merge ⇒ mỗi phút gọi git lại cho mọi task đã merge
- Claude sửa (~10 dòng, 5 phút): trả BOM; đọc output git bất đồng bộ để giới hạn 10 giây có tác dụng; giờ merge đã biết thì nhớ luôn.
- Claude sửa cổng kiểm tra: so BOM với commit khung; ngân sách tính từ commit khung (chạy lại verify: 285/320, ĐẠT).
- Cảnh báo "ngồi chờ lệnh nền bị treo" là báo nhầm (lượt chạy 8 phút).

Nhân viên: Minh (A) — 1 lượt, ~8 phút, 285/320 dòng.

## Kết quả
- **Không có chức năng mới nhìn thấy ngay** — phần dữ liệu cho Hub v3: độ khó, số vòng, điểm 7 giai đoạn, lỗi theo nguyên nhân, thẻ điểm nhân viên, biểu đồ thời gian, chu kỳ cải tiến, chỉ số tổng quan.
- **Kiểm chứng:** 30 ca nghiệm thu dùng dữ liệu thật V4-03 / V4-06 và git thật; 248 test pass.
- **Cần cải tiến:** chưa có trang hiển thị (H3-02, H3-04, H3-05).

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | ~5' | Mở phiếu R-003, chép nguyên văn yêu cầu | — | — |
| 2. Ý tưởng | Claude | 5 | ~15' | Mockup có dữ liệu thật, chủ dự án duyệt ngay lần đầu | — | Giữ cách làm mockup bằng dữ liệu thật |
| 3. Plan / thiết kế | Claude | 4 | ~20' | Model chốt sẵn, 9 bước ghép dữ liệu rõ; quên nói cache giờ merge | TK | Thiết kế ghi rõ chi phí gọi tiến trình ngoài / cache |
| 4. Giao việc | Claude | 5 | ~10' | Task file + khung + 30 ca nghiệm thu, giao 1 lần | — | — |
| 5. Thực hiện code | Minh (A) | 4 | ~8' (1 lượt) | Code tốt, đúng đặc tả; mất BOM 2 file | AG | Lưu ý riêng đã có trong AGENTS.md; theo dõi lần tới |
| 6. Review / QC | Cổng KT + Claude | 3 | ~15' | Cổng KT lọt BOM file khung và báo vượt ngân sách nhầm | CK | Đã sửa cổng KT (so với commit khung) |
| 7. Đánh giá & bàn giao | Claude | 5 | ~5' | — | — | — |
