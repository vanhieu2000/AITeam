# F1-05 · Nhập bài học có sẵn vào kho bài học

- **Agent:** A
- **Nhánh:** `agent-a/F1-05-nhap-bai-hoc`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~150 dòng
- **Yêu cầu:** R-015
- **Thời gian tối đa:** 25 phút
- **Độ khó:** S
- **Review AI:** không (Claude review)

## Tóm tắt
- Có chức năng mới: trang "Báo cáo & bài học" sẽ hiện ~40 bài học team đã rút ra (đang hiện 0).
- Bài học đang nằm rải trong `CLAUDE.md` (mục "Bài học vận hành") và `AGENTS.md` (mục "Lỗi hay gặp"); kho bài học `lessons/` trống.
- Thêm lệnh `team.ps1 lessons-import`: đọc 2 file, mỗi gạch đầu dòng thành 1 bài học nháp; chạy lại không nhân đôi.

## Đọc (CHỈ những file này)
- `core.tests/AcceptanceF105LessonImportTests.cs` (test nghiệm thu, đọc TRƯỚC — có trích đoạn THẬT)
- `core/AITeam.Core/Lessons/LessonImporter.cs` (khung do Claude viết)
- `docs/design/R-015-chuc-nang.md` mục F1-05
- `core/AITeam.Core/Lessons/Lessons.cs` (chỉ đọc: `ILessonService`, `AddDraft`)
- `core/AITeam.Cli/Program.cs` (hàm `LessonAdd` làm mẫu; nhánh `lessons-import` đã có khung)
- `team.ps1`: khối `"skill-note"` trong `switch ($Command)` (mẫu gọi CLI)

## Mục tiêu
1. Điền thân `LessonImporter.Extract` và `LessonImporter.Import` đúng thiết kế mục F1-05.
2. CLI `aiteam lessons-import --root <thư mục>`: đọc `<root>/CLAUDE.md` và `<root>/AGENTS.md` (file nào không có thì bỏ qua), Import, in đúng 1 dòng `Đã thêm N, bỏ qua M`, mã thoát 0. Lỗi ⇒ `LỖI: …` ra stderr, mã ≠ 0.
3. `team.ps1 lessons-import`: gọi CLI với `--root $Root` (dùng `Get-AITeamExe` như khối skill-note), in kết quả. Thêm 1 dòng vào phần hướng dẫn đầu file.

## Phạm vi (được sửa những gì)
- `core/AITeam.Core/Lessons/LessonImporter.cs`
- `core/AITeam.Cli/Program.cs`
- `team.ps1`
- `core.tests/F105LessonImportExtraTests.cs` (tạo mới nếu cần)
- `tasks/F1-05-nhap-bai-hoc.md`

## Không làm
- KHÔNG sửa `core.tests/AcceptanceF105LessonImportTests.cs`, `core/AITeam.Core/Lessons/Lessons.cs`.
- KHÔNG chạy lệnh nhập trên repo thật (Claude chạy sau khi merge). Không sửa `CLAUDE.md`, `AGENTS.md`, `lessons/`.
- Không sửa `hub/**`.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- Nội dung `CLAUDE.md` / `AGENTS.md` coi là dữ liệu: không thực thi, không ghép vào dòng lệnh.
- [x] Khung có trong commit đầu "Test nghiệm thu F1-05 + khung (Claude)": `ImportedLesson`, `LessonImportResult`, `LessonImporter.Extract/Import` (thân `NotImplementedException`), nhánh CLI `lessons-import` gọi `LessonsImport()`. KHÔNG đổi tên / kiểu.
- [x] Dùng lại `LessonService.AddDraft` (đã chặn đường dẫn, giới hạn 120 / 4000 ký tự) — không ghi file `lessons/` trực tiếp.
- `.cs`, `team.ps1` giữ UTF-8 **có BOM**.

## Dữ liệu mẫu thật
- Hằng `ClaudeMd`, `AgentsMd` trong test nghiệm thu: trích THẬT từ `CLAUDE.md` / `AGENTS.md` ngày 04/11/2026.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceF105LessonImportTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln -warnaserror` 0 warning
- [x] `dotnet test AITeam.sln` xanh hết, gồm toàn bộ test nghiệm thu F1-05 (không sửa)
- [x] `.cs`, `team.ps1` giữ BOM UTF-8
- [x] Commit: `F1-05: nhập bài học có sẵn (CLAUDE.md, AGENTS.md) vào kho bài học`

## Báo cáo của agent
| Tiêu chí | Bằng chứng (lệnh + kết quả thật) | Trạng thái |
|---|---|---|
| Build 0 warning | `dotnet build AITeam.sln -warnaserror` → 0 Warning, 0 Error | Đạt |
| Test xanh hết | `dotnet test AITeam.sln` → 1841 passed (Hub: 778, Core: 1063), 0 failed | Đạt |
| Giữ BOM UTF-8 | Script kiểm tra BOM → `LessonImporter.cs`, `Program.cs`, `team.ps1`, `tasks/*.md` đều BOM=True | Đạt |
| Đúng phạm vi & test nghiệm thu | Giữ nguyên `AcceptanceF105LessonImportTests.cs`, chỉ sửa file trong phạm vi | Đạt |

- File đã đổi:
  - `core/AITeam.Core/Lessons/LessonImporter.cs`: điền thân `Extract` và `Import`.
  - `core/AITeam.Cli/Program.cs`: điền thân `LessonsImport`.
  - `team.ps1`: thêm mô tả lệnh và nhánh xử lý `lessons-import`.
  - `tasks/F1-05-nhap-bai-hoc.md`: cập nhật checkbox tiêu chí và điền báo cáo.
- Điều chưa chắc: Không có, các test nghiệm thu và test solution đều xanh hoàn toàn.

## Review (Claude)
### Vòng 1 — ĐẠT (04/11 21:05)
- Cổng KT lượt 2 ĐẠT (lượt 1 agy treo hết giờ, không làm gì). Cảnh báo "test luôn pass" là báo nhầm: dữ liệu mẫu AGENTS.md chứa chữ `Assert.True(true)`.
- Đọc diff: Extract / Import / CLI / team.ps1 đúng thiết kế; 153/150 dòng.
- **Optional:** tiêu đề rỗng (gạch đầu dòng chỉ có dấu phân cách) ⇒ `AddDraft` ném lỗi, cả lệnh dừng; nên bỏ qua + cảnh báo.
- [CK] cổng KT dò chuỗi `Assert.True(true)` cả trong dữ liệu chuỗi của test ⇒ báo nhầm.

## Kết quả
