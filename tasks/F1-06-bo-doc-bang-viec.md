# F1-06 · Bộ đọc bảng việc dùng chung

- **Agent:** C
- **Nhánh:** `agent-c/F1-06-bo-doc-bang-viec`
- **Ưu tiên:** cao
- **Ngân sách:** ~220 dòng
- **Yêu cầu:** R-015
- **Thời gian tối đa:** 25 phút
- **Độ khó:** M
- **Review AI:** không (Claude review)

## Tóm tắt
- Không có chức năng mới cho người dùng.
- 04/11: một dòng trống giữa bảng `tasks/README.md` làm Hub mất 38 task; dòng thiếu dấu `|` cuối bị đọc sai trạng thái.
- Sau sửa: Hub dùng 1 bộ đọc bảng việc chung, đọc qua dòng trống, chịu dòng thiếu `|`, tìm cột theo tên.

## Đọc (CHỈ những file này)
- `hub.tests/AcceptanceF106BoardTableTests.cs` (test nghiệm thu, đọc TRƯỚC)
- `hub/Services/BoardTable.cs` (khung do Claude viết)
- `docs/design/R-015-chuc-nang.md` mục F1-06
- `hub/Services/TaskBoardService.cs`, `hub/Services/WorkOverviewService.cs`, `hub/Services/PerformanceService.cs` (hàm `ReadStatuses`), `hub/Services/WorkProgressService.cs`

## Mục tiêu
1. Điền thân `BoardTable.Parse` đúng thiết kế mục F1-06 (không đổi chữ ký / tên record).
2. 4 dịch vụ trên đọc bảng bằng `BoardTable.Parse` thay cho vòng lặp riêng; bỏ mọi `cells[^2]`. Kết quả với bảng chuẩn giữ nguyên (thứ tự task, `TaskState`, `OfficeTaskStatus`, FileName, Agent, Branch).
3. Test cũ (`TaskBoardServiceTests`, `WorkOverview…`, `W105/W106…`, `H812…`) vẫn xanh, không sửa.

## Phạm vi (được sửa những gì)
- `hub/Services/BoardTable.cs`
- `hub/Services/TaskBoardService.cs`
- `hub/Services/WorkOverviewService.cs`
- `hub/Services/PerformanceService.cs`
- `hub/Services/WorkProgressService.cs`
- `hub.tests/F106BoardTableExtraTests.cs` (tạo mới nếu cần)
- `tasks/F1-06-bo-doc-bang-viec.md`

## Không làm
- KHÔNG sửa `hub.tests/AcceptanceF106BoardTableTests.cs` và các test đang có.
- Không sửa `hub/Pages/**`, `hub/wwwroot/**`, `team.ps1`, `tasks/README.md`.
- Không đổi `TaskCard`, `ProjectBoard`, `OfficeTaskStatus`, `WorkOverviewService.ParseStatus`.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- `Parse` không bao giờ ném lỗi với nội dung hỏng / null; chạy O(số dòng).
- [x] Khung có trong commit đầu "Test nghiệm thu F1-06 + khung (Claude)": `BoardRow`, `BoardTable.Parse` (thân `NotImplementedException`). KHÔNG đổi tên / kiểu.
- [x] Không gọi tiến trình ngoài. Không đọc thêm file nào ngoài các file đang đọc.
- File `.cs` giữ UTF-8 có BOM.

## Dữ liệu mẫu thật
- Hằng `RealBoard` trong test nghiệm thu: rút gọn từ `tasks/README.md` của AITeam ngày 04/11/2026 (dòng trống giữa bảng, dòng thiếu `|`, bảng thứ 2 ở mục "Kế hoạch").

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceF106BoardTableTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln -warnaserror` 0 warning
- [ ] `dotnet test AITeam.sln` xanh hết, gồm toàn bộ test nghiệm thu F1-06 (không sửa)
- [x] File `.cs` giữ BOM UTF-8
- [ ] Commit: `F1-06: bộ đọc bảng việc dùng chung (chịu dòng trống, thiếu |)`

## Báo cáo của agent

| Tiêu chí | Bằng chứng / kết quả thật |
|---|---|
| Build 0 warning | Đạt: dotnet build AITeam.sln -warnaserror --no-restore -m:1; 0 warning, 0 error. |
| Test toàn solution | Chưa đạt: dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m --logger "console;verbosity=quiet"; Hub 783 pass/11 fail, Core 1039 pass/13 fail, 0 skip. |
| Parser và tương thích | Đạt 129 pass/0 fail: dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore -m:1 --filter "FullyQualifiedName~F106|FullyQualifiedName~TaskBoardServiceTests|FullyQualifiedName~WorkOverview|FullyQualifiedName~W105|FullyQualifiedName~W106|FullyQualifiedName~H812" --blame-hang-timeout 2m. |
| BOM UTF-8 | Đạt: kiểm ReadAllBytes ba byte EF BB BF ở cả 6 file C# đã sửa/tạo; giữ CRLF. |
| Commit/push | Hệ thống thực hiện theo chỉ dẫn Codex; không chạy git, chưa có commit được kiểm. |
- File đổi: BoardTable.cs điền parser; TaskBoardService.cs, WorkOverviewService.cs, PerformanceService.cs, WorkProgressService.cs dùng parser chung; F106BoardTableExtraTests.cs thêm 4 ca; task này ghi báo cáo.
- Giữ nguyên chữ ký, ParseStatus, test nghiệm thu và test cũ; không sửa giao diện, không chạy app/chụp ảnh.
- Tự quyết: dùng MSBuild -m:1 vì chạy mặc định thoát mã 1 dù báo 0 error; không sửa lỗi ngoài phạm vi.
- Đã sửa lỗi mã hóa PowerShell khi truyền mã sang Python; đã kiểm lại chuỗi tiếng Việt/emoji và chạy lại build/test.
- Chưa xác minh nguyên nhân 3 lỗi R014OfficeTrackingTests.cs:66,83,107 (thiếu link/mã trong HTML); không được đọc thêm file ngoài danh sách task.
- Các lỗi đã thấy gồm File.Replace và chạy git tiến trình con bị chặn quyền trong sandbox; Core còn lỗi AcceptanceS106SkillNotesTests (expected 1, actual 0). Không nhận toàn solution xanh.

## Review (Claude)

## Kết quả
