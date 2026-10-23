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

## Kết quả

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | Minh (A) | | | | | |
| 6. Review / QC | Cổng KT + Claude | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
