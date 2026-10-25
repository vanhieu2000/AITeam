# S1-06 · Lệnh ghi bài học có commit

- **Agent:** B
- **Nhánh:** `agent-b/S1-06-lenh-ghi-bai-hoc-co-commit`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~300 dòng
- **Yêu cầu:** R-010
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
- **Model:** `claude-opus-4-6-thinking`

## Tóm tắt
- Có chức năng mới: người dùng ghi bài học bằng lệnh `skill-note`, không cần tự sửa skill và tạo commit.
- Bài học vào đúng mục “Lỗi hay gặp”, giữ tiếng Việt và định dạng file cũ.
- Nội dung trùng không ghi hay commit lại; chỉ ghi nguồn khi người dùng cung cấp.
- Kiểm dữ liệu và file đang sửa trước khi ghi; commit đúng file skill, không push.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-010/brief.md`
- `tasks/S1-06-lenh-ghi-bai-hoc-co-commit.md`
- `team.ps1` (param, dot-source, switch cuối file, catch cuối file)
- `scripts/SkillNotes.ps1`
- `scripts/SkillPrompts.ps1` (mẫu kiểm reparse point; phụ thuộc S1-04 đã có)
- `core.tests/AcceptanceS106SkillNotesTests.cs`
- `core.tests/Fixtures/S1-06/06-chuan-bi-task/SKILL.md`
- `skills/06-chuan-bi-task/SKILL.md` (chỉ đọc mẫu thật)

## Bối cảnh
Theo brief R-010, mục Tự cập nhật và kế hoạch S1-06 đã duyệt. Đây là công cụ PowerShell 5.1, không phải dịch vụ Hub; không thêm C# sản phẩm hay DI. Khung đã thêm tham số, dot-source, nhánh lệnh; catch hiện có xử lý NotImplementedException bằng stderr `LỖI: chưa làm`, mã 1.


### Bằng chứng chuẩn bị (Codex, trước khi giao)
- Đã tạo 51 ca nghiệm thu (7 Fact + 44 dòng dữ liệu Theory); chưa chạy xUnit vì phần lớn ca gọi Git trong repo tạm, còn phiên chuẩn bị này cấm chạy lệnh Git.
- Build lần 1: `dotnet build AITeam.sln` thất bại, 0 warning/0 error được in, không có chẩn đoán nguyên nhân.
- Build lần 2: `dotnet build AITeam.sln -m:1 --disable-build-servers -v:minimal` thất bại, 0 warning/1 error CS0234 tại lời gọi Process.Start trong test.
- Đã sửa lời gọi thành System.Diagnostics.Process.Start để tránh namespace AITeam.Core.Process. CHƯA build lại sau sửa vì đã hết giới hạn hai lần; điều kiện bàn giao build xanh CHƯA xác minh.
- Parser PowerShell kiểm team.ps1 không có lỗi cú pháp. Kiểm CLI khung bằng bản sao runtime trong thư mục tạm: stderr `LỖI: chưa làm`; không có Git được chạy. Đây là trạng thái khung, không phải bằng chứng tính năng đạt.
- Fixture đã kiểm: BOM EF-BB-BF, 3361 byte, 35 CRLF, 0 LF đơn. S1-04 có sẵn scripts/SkillPrompts.ps1; khung không phụ thuộc hàm chưa tồn tại.
- Quyết định: chuẩn hóa và thông báo được chốt trong chú thích ///; wiring test chỉ sao runtime hiện tại khi build, tránh fixture code sản phẩm bị cũ. Git lỗi không yêu cầu rollback phá dữ liệu; giữ bài học chưa commit và báo thất bại.

## Mục tiêu
Triển khai phần thân `Invoke-SkillNote -Root <repo> -Skill <id> -Text <text> [-Source <source>]`. CLI: `.\team.ps1 skill-note -Skill <id> -Text <text> [-Source <source>]`; Root luôn lấy từ PSScriptRoot, không từ cwd. Hợp đồng chi tiết nằm trong chú thích `///` của khung.

## Phạm vi (được sửa)
- `team.ps1`
- `scripts/SkillNotes.ps1`

## Không làm
- Không đổi test nghiệm thu, fixture, wiring `core.tests/AITeam.Core.Tests.csproj`; đây là tài sản chuẩn bị đã có, không phải phần coder được sửa.
- Không sửa skill thật, hợp đồng Hub, cấu hình dự án, mẫu prompt hoặc lệnh khác.
- Không chạy agy/codex thật, không push trong lệnh skill-note; không reset/clean/stash dữ liệu người dùng.
- Không thêm giao diện, tự bịa nguồn hay mở rộng thành công cụ sửa Markdown tổng quát.

## Ràng buộc
- Giữ PowerShell 5.1; mọi file ps1/cs được sửa lưu UTF-8 có BOM. File skill đầu vào giữ đúng BOM có/không, CRLF/LF và newline cuối file; không cắt ký tự đầu bằng StartsWith(char BOM).
- ID chỉ ASCII chữ/số/gạch ngang/gạch dưới; chặn rỗng, `..`, tuyệt đối, dấu phân cách, tên DOS cả khác hoa thường và phần mở rộng. Chuẩn hóa và kiểm reparse point cả cha/file trước khi đọc/ghi. Lỗi: `LỖI: Skill không hợp lệ.`
- Text một dòng, không trắng, tối đa 1000 ký tự thô; Source tùy chọn tối đa 100 ký tự thô. CR hoặc LF trong cả hai bị cấm. Lỗi: `LỖI: Nội dung ghi chú không hợp lệ.`. Source trắng coi là thiếu.
- Chuẩn hóa Text bằng trim/gộp whitespace; chống trùng ordinal, phân biệt hoa thường, chỉ trong mục đích; bỏ hậu tố nguồn ` (Nguồn: …)` khi so bullet cũ. Trùng khác Source cũng không ghi, không đổi mtime, không commit; trả `Bài học đã tồn tại.`.
- Mục đích là tiêu đề cấp 2 bắt đầu bằng “Lỗi hay gặp”, cho phép hậu tố như mẫu thật. Chèn cuối mục trước tiêu đề cấp 1/2 kế tiếp; mục cuối chèn cuối file. Bullet đúng `- Text` hoặc `- Text (Nguồn: Source)`; bảo toàn mọi byte cũ ngoài điểm chèn. Thiếu file/mục: `LỖI: Không tìm thấy skill hoặc mục Lỗi hay gặp.`.
- Kiểm cả worktree và index đúng file trước mọi ghi, kể cả khi bài học trùng: `LỖI: File skill đang có thay đổi chưa commit.`. Không từ chối chỉ vì file khác bẩn. Commit chỉ file skill, không gom file khác đã stage; giữ nguyên index/dữ liệu khác.
- Git nhận đối số riêng, không nội suy qua shell; pathspec có `--`, timeout hữu hạn và kiểm exit code mọi lệnh. Không đổi cấu hình global; không push. Git lỗi: `LỖI: Không thể ghi hoặc commit bài học.`; không thông báo thành công, không hoàn tác bằng lệnh phá dữ liệu. Lỗi commit có thể giữ bài học chưa commit để phục hồi, phải báo thất bại.
- Thành công trả `Đã ghi và commit bài học.`; lỗi ném exception với thông điệp không tiền tố để catch CLI in stderr một lần, exit 1. Không nhận diện test/harness trong code sản phẩm.
- Test dùng repo tạm riêng, Git local cấu hình cô lập, chép runtime từ output build; không đọc dữ liệu thật/profiles khi chạy. Stdout/stderr UTF-8 hai phía; timeout 30 giây, đọc đồng thời.

## Dữ liệu mẫu thật
- Fixture `core.tests/Fixtures/S1-06/06-chuan-bi-task/SKILL.md` sao nguyên byte từ `skills/06-chuan-bi-task/SKILL.md`: 3361 byte, UTF-8 BOM, CRLF, có mục Lỗi hay gặp ở giữa và mục Ví dụ kế tiếp. Các biến thể LF/không BOM/mục cuối được tạo từ bản sao này; không bịa cấu trúc skill.
- Không đọc hay thay đổi projects.json hoặc phiếu requests vì lệnh này không dùng chúng.

## Test nghiệm thu
`core.tests/AcceptanceS106SkillNotesTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln`: 0 lỗi, 0 warning mới sau khi triển khai.
- [ ] `dotnet test AITeam.sln`: pass; giữ nguyên test nghiệm thu, không Skip/return sớm.
- [ ] Tám biến thể BOM/newline/vị trí mục, Unicode và giới hạn độ dài giữ đúng byte; bullet cũ/trùng khác nguồn không commit lại.
- [ ] Các lỗi ID/Text/Source/thiếu dữ liệu/file bẩn/liên kết ra ngoài trả đúng stderr và mã 1, không sửa dữ liệu ngoài đích.
- [ ] Repo có file khác đã stage/chưa stage chỉ commit skill; Git lỗi không báo thành công; không push.
- [ ] Báo cáo bảng từng tiêu chí → lệnh/kết quả thật, file đã đổi/lý do, quyết định và điều chưa xác minh.

## Báo cáo của agent
<!-- Antigravity điền sau triển khai; không lấy kết quả build khung làm bằng chứng tính năng đã hoàn tất. -->

| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Build và test | Chưa triển khai |
| Định dạng, chống trùng | Chưa triển khai |
| Bảo mật, Git và lỗi | Chưa triển khai |

File đã đổi và lý do: …
Quyết định tự đưa ra: …
Điều chưa xác minh: …

## Review (Claude)
<!-- Claude điền sau khi coder hoàn thành. -->

## Kết quả
<!-- Claude điền khi review đạt. -->

## Đánh giá quy trình
<!-- Claude điền sau khi merge. -->
