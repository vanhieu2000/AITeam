# F1-02 · Tiến độ yêu cầu: đếm task đúng, đóng / hủy phiếu

- **Agent:** C
- **Nhánh:** `agent-c/F1-02-tien-do-yeu-cau`
- **Ưu tiên:** cao
- **Ngân sách:** ~260 dòng
- **Yêu cầu:** R-015
- **Thời gian tối đa:** 30 phút
- **Độ khó:** M
- **Review AI:** không (Claude review)

## Tóm tắt
- Chuẩn bị chức năng mới: trang Yêu cầu / Văn phòng hiện đúng tiến độ từng yêu cầu (x/y task xong) và có nút Đóng / Hủy phiếu (giao diện Claude làm sau).
- Hiện mọi phiếu hiện "0/0" vì Hub chỉ đếm bảng "Chia việc" trong phiếu (hầu như không điền); 7 phiếu cũ treo "Đang làm" vì không có cách đóng.
- Task này làm phần lõi: gom task theo dòng `- **Yêu cầu:** R-xxx` trong file task + trạng thái trên bảng việc; ghi trạng thái phiếu an toàn.

## Đọc (CHỈ những file này)
- `hub.tests/AcceptanceF102RequestProgressTests.cs` (test nghiệm thu, đọc TRƯỚC)
- `hub/Services/RequestProgressService.cs` (khung do Claude viết)
- `docs/design/R-015-chuc-nang.md` mục F1-02
- `hub/Services/BoardTable.cs` (bộ đọc bảng việc F1-06 — dùng lại, không sửa)
- `hub/Services/RequestService.cs` (chỉ đọc), `hub/Services/WorkOverviewService.cs` (hàm `ParseStatus`, chỉ đọc)
- `hub/Services/HubV3Registration.cs`

## Mục tiêu
1. Điền thân `RequestProgressService` (GetAll, Get, SetState) đúng thiết kế mục F1-02.
2. Đăng ký DI `IRequestProgressService` → `RequestProgressService` (singleton, `TryAdd`), `TimeProvider.System` nếu chưa đăng ký.

## Phạm vi (được sửa những gì)
- `hub/Services/RequestProgressService.cs`
- `hub/Services/HubV3Registration.cs`
- `hub.tests/F102RequestProgressExtraTests.cs` (tạo mới nếu cần)
- `tasks/F1-02-tien-do-yeu-cau.md`

## Không làm
- KHÔNG sửa `hub.tests/AcceptanceF102RequestProgressTests.cs`, `hub/Services/BoardTable.cs`, `RequestService.cs`, `WorkOverviewService.cs`.
- Không sửa `hub/Pages/**`, `hub/wwwroot/**`, `team.ps1`, các phiếu thật trong `requests/`.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- `id` từ form web là KHÔNG tin cậy: chỉ nhận `^R-\d{3}$`; file phải nằm trong `<AITeamRoot>\requests`; chặn reparse point ở file và thư mục.
- Ghi phiếu: chỉ đổi dòng Trạng thái + thêm 1 dòng nhật ký; mọi byte khác giữ nguyên (BOM, CRLF / LF). Ghi nguyên tử (file tạm cùng thư mục rồi thay thế).
- `note` không tin cậy: lọc ký tự điều khiển, gộp khoảng trắng, ≤ 200 ký tự; trong bảng nhật ký thay `|` bằng `/`.
- Đọc nhiều dự án: dự án không có trên máy (`ExistsLocally` false) bỏ qua; lỗi đọc 1 file task không làm hỏng cả danh sách.
- [x] Khung có trong commit đầu "Test nghiệm thu F1-02 + khung (Claude)". KHÔNG đổi tên / kiểu.
- `.cs` giữ UTF-8 có BOM.

## Dữ liệu mẫu thật
- Hằng `RequestText` (phiếu R-014 thật rút gọn, BOM + CRLF) và `Board` (bảng việc thật có dòng trống, dòng thiếu `|`) trong test nghiệm thu.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceF102RequestProgressTests.cs`

## Tiêu chí hoàn thành
- [ ] Build `AITeam.sln` với `-warnaserror` 0 warning
- [ ] `dotnet test AITeam.sln` xanh hết, gồm toàn bộ test nghiệm thu F1-02 (không sửa)
- [ ] `.cs` giữ BOM UTF-8
- [ ] Commit: `F1-02: tiến độ yêu cầu theo file task + đóng / hủy phiếu`

## Báo cáo của agent

## Review (Claude)

## Kết quả
