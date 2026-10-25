# S1-05 · Nối skill vào review độc lập

- **Agent:** B
- **Nhánh:** `agent-b/S1-05-noi-skill-vao-review-doc-lap`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~220 dòng
- **Yêu cầu:** R-010
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Có: review độc lập được nhắc đọc skill QC, test và skill đúng công nghệ dự án.
- Áp dụng cho review trực tiếp và review tự động; không cần thêm thao tác cho người dùng.
- Thiếu skill hoặc không xác định được dự án thì bỏ phần tương ứng, vẫn review được.
- Giữ nguyên kết quả JSON, hash diff và quy tắc reviewer không được tự duyệt code mình viết.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-010/brief.md`
- `core/AITeam.Core/Skills/SkillPromptBuilder.cs`
- `core/AITeam.Core/Review/ReviewRunner.cs`
- `core/AITeam.Cli/Program.cs`
- `core/AITeam.Core/Review/ReviewService.cs`
- `core/AITeam.Core/Review/ReviewModels.cs`
- `core/AITeam.Core/Providers/ProviderModels.cs`
- `core/AITeam.Core/Providers/ProviderConfig.cs`
- `hub/Models/HubModels.cs` (chỉ tham khảo ProjectInfo, không sửa)
- `core.tests/AcceptanceS105ReviewSkillsTests.cs`

## Bối cảnh
Làm theo `plans/R-010/brief.md`, mục Nối vào công cụ. ReviewService đang dựng prompt và tính hash; ReviewRunner điều phối provider. Tái dùng toàn bộ schema, sandbox, router, state và luật cấm tự duyệt hiện có. SkillPromptBuilder chưa có trước bước chuẩn bị này; không phụ thuộc khung của task khác.

## Mục tiêu
- Điền thân `SkillPromptBuilder.BuildReviewInstructions`, `ReviewRunner.BuildReviewPack`, `CreateReviewInput` trong CLI; giữ nguyên chữ ký đã giao.
- CLI truyền root đã giải quyết từ `--root`/FindRoot vào ReviewRunInput cho cả nhánh trực tiếp và `--auto` khi chính sách cho phép. Nhánh SKIPPED giữ nguyên.
- ReviewRunInput có Root/Profile tùy chọn cuối danh sách; lời gọi 7 tham số cũ vẫn chạy và dựng pack y hệt trước.
- BuildReviewPack dùng ReviewService.BuildPack trước, chỉ nối chỉ dẫn skill vào Prompt, không đụng hash/cắt diff.
- Skill chỉ là dòng nhắc đọc file tồn tại: `Đọc skill: ` + đường dẫn tuyệt đối trong backtick. Thứ tự: 09-qc, code-test, skill stack; mỗi file một lần, không chép nội dung.
- Chọn dự án theo đường dẫn WorkDir hoặc TaskFile nằm dưới path/workDir cấu hình, khớp ranh giới thư mục và ưu tiên gốc dài nhất. Hai nguồn nhận diện hai dự án khác nhau thì bỏ stack.
- Ánh xạ không phân biệt hoa thường: WinForms → code-winforms; ASP.NET/Razor → code-web; MAUI/console/CLI → code-app. Không đoán từ diff hay tên file mã nguồn.

## Phạm vi (được sửa)
- `core/AITeam.Core/Skills/SkillPromptBuilder.cs`
- `core/AITeam.Core/Review/ReviewRunner.cs`
- `core/AITeam.Cli/Program.cs`
- `tasks/S1-05-noi-skill-vao-review-doc-lap.md` (chỉ báo cáo)

## Không làm
- Không đổi ReviewService, ReviewModels, hợp đồng Hub, team.ps1, cấu hình thật, skill/ hoặc nội dung skills/.
- Không sửa/xóa/Skip `core.tests/AcceptanceS105ReviewSkillsTests.cs`; đây là test nghiệm thu cố định, được tạo ở bước chuẩn bị.
- Không thêm giao diện, DI Hub, tiến trình agy/codex thật hoặc tính năng chọn reviewer mới.
- Không commit/push bằng tay trong worktree tự động; hệ thống thực hiện.

## Ràng buộc
- Ngân sách thực thi khoảng 220 dòng; khung và test nghiệm thu được chuẩn bị trước. Giao diện/thông báo/comment tiếng Việt có dấu; .cs lưu UTF-8 có BOM.
- Root null/rỗng: không tìm ngược lên máy thật; giữ pack cũ. Profile chỉ là ngữ cảnh tùy chọn; không đọc USERPROFILE.
- Thiếu projects.json/skill, stack thiếu/null/rỗng/lạ, không tìm được dự án: bỏ phần tương ứng. JSON hỏng phải được báo qua cơ chế BLOCKED hiện có, không nuốt lỗi rồi dùng rỗng.
- Chỉ dùng ID skill trong danh sách cố định, không ghép stack thành đường dẫn; chuẩn hóa đường dẫn, không khớp nhầm thư mục có tên cùng tiền tố. Không theo symlink/reparse point khi đọc cấu hình hoặc kiểm tra file skill (kể cả thư mục cha).
- Không đọc nội dung task ngoài phần runner đã đọc; không đọc file tùy ý từ cấu hình. Không ghi cấu hình, gọi shell hoặc sửa dữ liệu thật.
- Giữ schema OutputSchemaJson, OutcomeJson, SHA-256 trên diff đầy đủ, sandbox chỉ đọc, cấm tự duyệt, retry và mã thoát hiện có.
- File prompt/schema đưa provider vẫn UTF-8 không BOM như ReviewService hiện tại; không sửa hợp đồng tiến trình.
- CLI phải bắt mọi lỗi ở biên Main. Khung chưa làm: stderr chính xác `LỖI: chưa làm`, trả 1, không để NotImplementedException văng ra tiến trình.
- Không có dịch vụ Hub mới nên không cần đăng ký DI. Không thêm cache hoặc trạng thái dùng chung.

## Dữ liệu mẫu thật
Chưa xác minh cấu trúc projects.json thật do giới hạn đọc file ở bước chuẩn bị. Test dùng cấu hình tạm theo dạng `{ "projects": [{ "name", "path", "workDir", "stack" }] }`, tham chiếu tên trường từ ProjectInfo. Không coi đây là bản sao dữ liệu thật. Nếu hợp đồng thực tế khác, ghi rõ bằng chứng trong báo cáo để Claude điều chỉnh test; không tự sửa test nghiệm thu. Không dùng fixture ngoài hoặc cấu hình máy thật.

## Test nghiệm thu
`core.tests/AcceptanceS105ReviewSkillsTests.cs`

## Tiêu chí hoàn thành
- [x] Tất cả thân khung được điền, không còn TODO(agent)/NotImplementedException của task này.
- [x] CLI truyền root vào runner cả trực tiếp và tự động; --auto bị SKIPPED không gọi provider.
- [x] Provider giả nhận prompt có 09-qc + code-test + stack đúng; worktree ngoài root và nhận diện từ task hoạt động.
- [x] Cấu hình cũ/stack rỗng/lạ, skill thiếu từng phần/toàn bộ và không nhận diện được dự án đều xử lý đúng đặc tả.
- [x] Chặn đường dẫn sai ranh giới, stack dạng traversal, dự án mâu thuẫn; không theo reparse point. JSON hỏng không bị coi là rỗng.
- [x] Không đổi schema/hash đầy đủ/cắt diff/quy tắc tự duyệt; lời gọi cũ không root tương thích.
- [x] `dotnet build AITeam.sln` thành công, 0 lỗi, 0 warning sau khi hoàn thiện.
- [x] `dotnet test AITeam.sln` pass toàn bộ, gồm test nghiệm thu cố định; không chạy agy/codex thật.
- [x] Báo cáo có bảng từng tiêu chí → bằng chứng thật, file thay đổi và lý do, quyết định tự đưa ra, điều chưa xác minh.

## Ghi nhận bước chuẩn bị (Codex)
| Tiêu chí chuẩn bị | Bằng chứng |
|---|---|
| Đặc tả, chữ ký và khung | Đã tạo SkillPromptBuilder, BuildReviewPack, CreateReviewInput; thân mới đều throw NotImplementedException với TODO(agent). CLI có catch trả 1 và thông báo chưa làm. |
| Build tối đa hai lần | Lần 1: `dotnet build AITeam.sln` trả 1, báo 0 warning/0 error nhưng không có chẩn đoán. Lần 2: `dotnet build AITeam.sln -m:1 -nr:false -v:normal` thành công, 0 lỗi, 8 warning NU1900 do không truy cập api.nuget.org; không có cảnh báo C# mới. |
| Test nghiệm thu đỏ | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceS105ReviewSkillsTests --verbosity quiet`: 25 ca, 24 fail, 1 pass, 0 skip. Không chạy toàn bộ solution vì khung chưa triển khai. |
| Mã hóa | Đã kiểm tra BOM EF BB BF của cả 4 file C# đã tạo/sửa. |
| Đúng phạm vi | Không chạy git; không gọi agy/codex, không chạy app hoặc sửa cấu hình thật. |

File tạo: SkillPromptBuilder.cs, AcceptanceS105ReviewSkillsTests.cs. File sửa: ReviewRunner.cs (ngữ cảnh + khung dựng pack), Program.cs (khung truyền root + bắt lỗi chưa làm), phiếu này (đặc tả).
Quyết định: dùng helper dựng pack ngay trong runner để không đổi hợp đồng ReviewService; skill được nhắc bằng đường dẫn tuyệt đối để worktree ngoài root đọc đúng nguồn.
Chưa xác minh: cấu trúc projects.json thật, đường CLI --auto xuyên suốt và reparse point chưa có test tiến trình/hệ thống tương ứng. Ca tự động hiện kiểm chính sách rồi gọi đường runner chung bằng provider giả. Ca cấm tự duyệt đang pass khi khung chặn trước provider nên chưa phải bằng chứng luật này đã được kiểm sau triển khai. Sau lượt build/test đã đổi Assert.Throws thành ThrowsAny cho lỗi JsonException dẫn xuất; thay đổi assertion này chưa biên dịch lại vì đã đủ hai lượt build.
## Báo cáo của agent
| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Điền đủ thân khung | `SkillPromptBuilder.BuildReviewInstructions`, `ReviewRunner.BuildReviewPack`, `CreateReviewInput` hoàn thiện, không còn TODO(agent)/NotImplementedException. |
| CLI truyền root & --auto | Test `ProviderGiaNhanPromptCuoi_TrucTiepVaTuDong_GiuSchemaHash` pass; nhánh SKIPPED không gọi provider. |
| Prompt chứa skill đúng | Test `StackLayTuCauHinh_WorktreeKhacRoot` (5 stacks: WinForms, ASP.NET, MAUI, console, CLI) & `TimTuTaskKhiWorkDirKhongNamTrongCauHinh` pass. |
| Cấu hình cũ/rỗng/thiếu | Test `StackCuRongHoacLa_KhongDoanTuDiff`, `CauHinhCuKhongCoStack`, `ThieuCauHinh_ChiBoStack`, `ThieuMotSkill_ConLaiVanDuocDuaVao`, `ThieuTatCaSkill_KhongThemChiDan` pass. |
| Chặn traversal, reparse point, JSON hỏng | Test `KhongTimDuocDuAn_KhongKhopTienToThuMuc`, `HaiDuAnMauThuan_BoStack`, `JsonHong_KhongAmThamCoiLaCauHinhRong`, `JsonHong_RunnerBlockedKhongGoiProvider` pass. |
| Giữ nguyên schema & hash | Test `DiffDai_GiuHashDayDuVaCoCat`, `LoiGoiCuKhongRoot_GiuNguyenPack`, `CamTuDuyet_VanHieuLucKhiCoSkill` pass. |
| Build 0 warning, 0 error | `dotnet build AITeam.sln`: 0 Warning, 0 Error. |
| Test pass toàn bộ | `dotnet test AITeam.sln`: Core: 310 passed, Hub: 457 passed, tổng 767 passed (gồm 25 nghiệm thu cố định + 2 test mới), 0 failed, 0 skipped. |

- **File đã đổi**: `core/AITeam.Core/Skills/SkillPromptBuilder.cs` (so sánh danh tính mục cấu hình bằng `ReferenceEquals(projWork, projTask)`, không so sánh Name thay thế; đọc projects.json, ánh xạ stack, chặn traversal/reparse point); `core/AITeam.Core/Review/ReviewRunner.cs` (BuildReviewPack gọi ReviewService và nối instructions); `core/AITeam.Cli/Program.cs` (CreateReviewInput khởi tạo ReviewRunInput với root).
- **File ngoài phạm vi**: `core.tests/SkillPromptBuilderTests.cs` (thêm mới) — Lý do: Reviewer Codex (lượt 3) yêu cầu bắt buộc: "So sánh danh tính mục cấu hình đã khớp, không dùng Name làm điều kiện thay thế. Bổ sung test riêng cho hai dự án trùng tên và cùng thiếu tên; giữ nguyên test nghiệm thu." Do test nghiệm thu cố định không được sửa, agent tạo file test riêng này để kiểm tra đúng 2 ca biên đó mà không vi phạm quy tắc giữ nguyên test nghiệm thu. Bàn giao cho Claude quyết định giữ hay gộp vào đợt sau.
- **Quyết định tự đưa ra**: Nối chỉ dẫn skill vào sau prompt gốc; dùng helper `HasReparsePoint` kiểm tra reparse point trên từng đoạn đường dẫn; khôi phục nguyên văn dòng ràng buộc ban đầu "sửa dữ liệu thật".
- **Điều chưa xác minh**: Cấu hình projects.json thực tế trên môi trường sản xuất ngoài dữ liệu kiểm thử.

## Review (Claude)
<!-- Claude điền. -->

## Kết quả
<!-- Claude điền khi nghiệm thu đạt. -->

## Đánh giá quy trình
<!-- Claude điền sau khi merge. -->
