# Q6-01 · Cong cu quy trinh v6 phan vai va test vung

- **Loại:** KT
- **Chức năng:** CN-01, CN-02, CN-03, CN-04, CN-05
- **Agent:** C
- **Nhánh:** `agent-c/Q6-01-cong-cu-quy-trinh-v6`
- **Ưu tiên:** cao
- **Ngân sách:** ~450 dòng
- **Yêu cầu:** R-018
- **Thời gian tối đa:** 60 phút
- **Độ khó:** M
- **Test nghiệm thu:** Không — Codex viết kịch bản trong tools/test-verify.ps1, Claude nghiệm thu bằng các tình huống cố tình làm sai

## Tóm tắt
- Chức năng mới cho người dùng: Không. Đây là luật tự động của công cụ giao việc.
- Giao đúng người theo loại việc: Codex làm giao diện và test, Antigravity làm backend.
- Antigravity sửa file giao diện thì cổng kiểm tra không cho qua.
- Cổng kiểm tra chỉ chạy test của vùng task, toàn bộ test chạy một lần khi merge.

## Đọc (CHỈ những file này)
- `QUY-TRINH.md` (mục Quy trình v6 ở đầu file)
- `docs/KE-HOACH-DU-AN.md`
- `docs/TEST-CHUAN.md` (mục Chuẩn test v6)
- `team.ps1`
- `tools/test-verify.ps1`
- `projects.json`
- `team.json`
- `config/workflow.json`
- `templates/task.md`

## Bối cảnh
Thiết kế đã chốt ở `QUY-TRINH.md` mục Quy trình v6. File task mới có 3 dòng khai báo: Loại, Chức năng, Test vùng. `team.ps1` chưa đọc các dòng này.

## Mục tiêu
**CN-01 · Phân vai khi giao việc (`Invoke-Assign`)**
- Đọc `Loại` (UI, TEST, BE, KT), `Chức năng` (các mã `CN-xx`), `Test vùng` từ file task.
- Giao mới (không có `-Resume`): thiếu hoặc sai `Loại`, thiếu `Chức năng`, hoặc mã `CN-xx` không có trong `docs/KE-HOACH-DU-AN.md` của dự án thì báo lỗi tiếng Việt và không giao. Thiếu file kế hoạch cũng báo lỗi.
- `-Resume` với task cũ chưa có các dòng này vẫn chạy như trước để không kẹt việc đang dở.
- UI, TEST: mặc định `codex`; truyền `-Dev` khác thì báo lỗi. Codex hết hạn mức thì đặt bảng việc `⏸ Chờ Claude làm`, không chuyển sang AI khác.
- BE: mặc định `agy`; truyền `-Dev` khác thì báo lỗi. Antigravity hết hạn mức thì đặt bảng việc `⏸ Chờ Antigravity`, KHÔNG tự chuyển sang Codex.
- KT: giữ định tuyến hiện tại.

**CN-02 · Chặn sửa sai vùng (`verify`)**
- Task BE có file đổi khớp danh sách khóa giao diện thì KHÔNG ĐẠT, nêu rõ từng file.
- Danh sách mặc định: `**/*.Designer.cs`, `**/*.resx`, `**/*.cshtml`, `**/*.razor`, `**/wwwroot/**`, `docs/design/**`. Mỗi dự án thêm được bằng khóa `uiLocked` trong `projects.json`.
- Task UI, TEST, BE sửa `docs/KE-HOACH-DU-AN.md` thì KHÔNG ĐẠT.
- Thêm `uiLocked` cho MyControls: `MyControls/Inputs/**`, `MyControls/Containers/**`, `MyControls/Navigation/**`, `MyControls/DataViews/**`, `MyControls/Feedback/**`, `MyControls/Industrial/**`, `MyControls/Scada/**`, `MyControls/Reports/**`, `MyControls/Core/**`.

**CN-03 · Test vùng (`verify`)**
- Task có `Test vùng`: cổng kiểm tra chạy lệnh test của dự án kèm bộ lọc đó thay vì toàn bộ test. Bộ lọc không khớp test nào thì KHÔNG ĐẠT.
- Task UI, TEST, BE thiếu `Test vùng` thì KHÔNG ĐẠT. Task KT thiếu thì chạy toàn bộ như cũ.
- `merge` giữ nguyên: chạy toàn bộ test một lần trên clone sạch.

**CN-04 · Lời giao việc theo vai**
- Lời giao việc thêm vai trò đúng theo `Loại`, nội dung lấy từ mục phân vai của `QUY-TRINH.md`, và yêu cầu đọc `docs/KE-HOACH-DU-AN.md`.
- Thay câu chạy đủ Build + Test của dự án bằng: chỉ chạy `Test vùng`, không chạy toàn bộ test.
- Đặt `review.maxRounds` trong `team.json` thành 2.

**CN-05 · Dự án mới và luồng làm việc**
- `add-project` tạo `docs/KE-HOACH-DU-AN.md` từ `templates/KE-HOACH-DU-AN.md`, không ghi đè file đã có.
- `config/workflow.json`: bước giao diện do `codex`, dự phòng `claude`; bước code backend do `minh`, không dự phòng. Giữ đúng định dạng file hiện tại.

## Phạm vi (được sửa những gì)
- `team.ps1`
- `tools/test-verify.ps1`
- `projects.json`
- `team.json`
- `config/workflow.json`
- `core.tests/`
- `tasks/Q6-01-cong-cu-quy-trinh-v6.md`

## Không làm
- Không sửa `QUY-TRINH.md`, `CLAUDE.md`, `AGENTS.md`, `templates/`, `docs/`.
- Không đổi lệnh `merge`, trừ phần cần để giữ hành vi hiện tại.
- Không sửa `hub/`, `core/`.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- `team.ps1` giữ UTF-8 có BOM, CRLF, tương thích PowerShell 5.1.
- Bộ lọc `Test vùng` là dữ liệu trong file task: chỉ nhận ký tự an toàn (chữ, số, `~`, `=`, `.`, `_`, `|`, `&`, `!`, `(`, `)`, khoảng trắng), sai thì KHÔNG ĐẠT, không ghép thẳng vào dòng lệnh.
- So khớp đường dẫn dùng dấu `/`, không phân biệt hoa thường.
- Các tình huống mới phải có kịch bản trong `tools/test-verify.ps1`, bỏ phần code mới thì kịch bản phải đỏ. Không gọi AI thật, không dùng mạng.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
Không có file riêng. Codex viết kịch bản trong công cụ test, Claude nghiệm thu bằng các tình huống cố tình làm sai.

## Tiêu chí hoàn thành
- [ ] Build `dotnet build AITeam.sln` 0 warning
- [ ] `powershell -File tools\test-verify.ps1` đạt các kịch bản mới
- [x] Giao task UI cho `agy` bị từ chối, giao task BE cho `codex` bị từ chối
- [x] Task BE sửa `.Designer.cs` bị cổng kiểm tra từ chối
- [x] Task thiếu mã chức năng trong kế hoạch không giao được

## Báo cáo của agent

| Tiêu chí | Bằng chứng thực tế vòng 1 |
|---|---|
| Build 0 warning | Chưa đạt: `dotnet build AITeam.sln --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false`: 0 lỗi, 5 warning NU1900 do không truy cập NuGet. |
| Kịch bản v6; UI/BE sai người; BE sửa Designer; CN thiếu | `powershell -NoProfile -File tools/test-verify.ps1 -WorkflowOnly`: 33/33 đạt, gồm đủ các trường hợp từ chối; chưa chạy toàn script vì có git. |
| Review 1: mã hóa | `projects.json`, `team.json` đã bỏ BOM; kiểm byte bằng `[IO.File]::ReadAllBytes`: hai JSON không BOM, hai script có BOM và CRLF, không LF lẻ. |
| Review 2: harness | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceS104SkillPromptsTests\|FullyQualifiedName~V915QueueTests\|FullyQualifiedName~AcceptanceV804FlakyRetryTests" --blame-hang-timeout 2m`: 55 đạt/0 lỗi/0 bỏ qua (18 giây); dấu \ trước dấu phân cách chỉ để hiển thị bảng. |
| Review 3–4: KT và STACK | WorkflowOnly có 3 kiểm tra mới đạt: KT thiếu bộ lọc dùng lệnh test dự án; có bộ lọc dùng Test vùng; add-project truyền STACK cho mẫu. |
| Test solution | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m`: Hub 848 đạt/8 lỗi; Core 1078 đạt/13 lỗi (2 phút 38 giây); tổng 1926 đạt/21 lỗi/0 bỏ qua; lỗi quyền File.Replace và Git signal pipe trong sandbox. |
| File đổi/lý do | `team.ps1`: lời giao KT, STACK; hai JSON: bỏ BOM; ba lớp test nêu trên: chỉ cập nhật môi trường, giữ assert; `tools/test-verify.ps1`: thêm 3 kiểm tra; file task: báo cáo. |
| Kiểm tra bổ sung | Build Core khi test giữ DLL bị MSB3027/MSB3021; sau test chạy lại `dotnet build core.tests/AITeam.Core.Tests.csproj --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false`: 0 lỗi/3 NU1900; filter 55/55 chạy lại đạt. |
| Quyết định/chưa xác minh | Không sửa ngoài phạm vi review, không nới assert; không chạy app/chụp ảnh. Không gọi git trực tiếp hoặc merge main; hệ thống tự commit/push, chưa có commit được kiểm. |
## Review (Claude)
### Vòng 1 — CẦN SỬA
Claude đã sửa phần tài liệu trên main (S107, W104), Codex merge main rồi sửa đủ 4 điểm:
1. Xóa BOM vừa thêm vào `projects.json` và `team.json`, giữ nguyên mã hóa cũ.
2. Được phép cập nhật phần dựng môi trường của `core.tests/AcceptanceS104SkillPromptsTests.cs`, `core.tests/V915QueueTests.cs`, `core.tests/AcceptanceV804FlakyRetryTests.cs`: nạp thêm các hàm v6, dữ liệu task mẫu có Loại, Chức năng và file kế hoạch. Không nới lỏng assert.
3. Lời giao việc của task KT không có Test vùng: giữ câu chạy lệnh test của dự án, không in Test vùng rỗng.
4. `add-project`: truyền thêm `STACK` khi tạo `docs/KE-HOACH-DU-AN.md`.
## Kết quả
