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
| Tiêu chí | Bằng chứng thực tế |
|---|---|
| Build 0 warning | `dotnet build AITeam.sln --no-restore -warnaserror -m:1`: đạt, 0 warning, 0 lỗi (37,16 giây); restore mặc định thất bại không hiện chẩn đoán. |
| Test nghiệm thu nguyên vẹn | `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~F102RequestProgress" --blame-hang-timeout 2m`: 16 pass, 0 fail, 0 skip (15 nghiệm thu + 1 bổ sung). |
| Test toàn solution | `dotnet test AITeam.sln --no-build --no-restore --blame-hang-timeout 2m -m:1`: Hub 802 pass/8 fail/0 skip; Core chưa hoàn tất, dừng ở giới hạn ~3 phút. Lỗi ngoài phạm vi: File.Replace bị từ chối quyền, Git con lỗi signal pipe Win32 5; không báo toàn suite đạt. |
| BOM UTF-8 | Kiểm 3 byte đầu bằng PowerShell: cả 3 file `.cs` sửa/tạo đều EF BB BF. |
| Commit/push | Không chạy git theo chỉ thị Codex; hệ thống tự commit/push, chưa xác minh commit. |
- File đổi: `RequestProgressService.cs` (gom/đếm task, ghi trạng thái an toàn); `HubV3Registration.cs` (DI singleton TryAdd); `F102RequestProgressExtraTests.cs` (LF không BOM, note điều khiển, trạng thái/id sai); file task (báo cáo).
- Quyết định: dùng `File.Move(..., true)` nguyên tử cùng thư mục theo thiết kế vì sandbox chặn `File.Replace`; không sửa test nghiệm thu hay code ngoài phạm vi.
- Chưa xác minh: toàn suite xanh ngoài sandbox; không chạy app/chụp ảnh; lượt test DLL cũ ban đầu không dùng làm bằng chứng cho bản mới.
## Review (Claude)

## Kết quả
