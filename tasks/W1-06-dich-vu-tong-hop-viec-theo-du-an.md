# W1-06 · Dịch vụ tổng hợp việc theo dự án

- **Agent:** B
- **Nhánh:** `agent-b/W1-06-dich-vu-tong-hop-viec-theo-du-an`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~300 dòng
- **Yêu cầu:** R-011
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->

## Tóm tắt
- Chuẩn bị dữ liệu để người dùng xem mọi yêu cầu và task theo dự án trên trang Việc.
- Yêu cầu chưa chia task vẫn xuất hiện; nhiều task được đếm riêng theo từng bước.
- Ghép tiến độ có sẵn với tên người theo cấu hình, không ghi cứng Agent A/B.
- Bước này chưa thêm màn hình hoặc nút mới; chỉ bổ sung dịch vụ chỉ đọc cho trang dùng sau.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-011/brief.md`
- `hub/Services/WorkProgressService.cs`
- `hub/Program.cs`
- `hub/Services/WorkProgressParser.cs`
- `hub/Services/Contracts.cs`
- `hub/Models/HubModels.cs`
- `hub/Services/HubPaths.cs`
- `hub/Services/WorkflowRolesService.cs` (hợp đồng GetRoles và TeamFileLock.HasReparsePoint)
- `hub.tests/AcceptanceW106WorkProgressServiceTests.cs`
- `hub.tests/Fixtures/W106/` (chỉ những bản sao được liệt kê trong mục Dữ liệu mẫu thật)
- `core.tests/Support/TestShell.cs`

## Bối cảnh
Làm theo brief R-011 mục 3 và kế hoạch W1-06 đã duyệt. W1-05 đã có `WorkProgressParser` và chín bước trong cùng file; tái dùng Parse/Calculate, không viết lại máy trạng thái. Dữ liệu repo dự án ở ProjectInfo.Path; logs thuộc ProjectInfo.WorkDir, có thể hoàn toàn khác HubPaths.AITeamRoot. ITeamConfigService áp đường dẫn máy trước khi trả ProjectInfo.

## Mục tiêu
Điền phần thân hai phương thức của `WorkProgressService`, giữ nguyên interface/record và chữ ký constructor trong khung. Dịch vụ trả snapshot độc lập qua `GetProject(project)` và `GetRequest(project, requestId)`; không tạo endpoint mới. DI singleton đã đăng ký trong Program cho cả chế độ thật/giả/rỗng.

## Phạm vi (được sửa)
- `hub/Services/WorkProgressService.cs`
- `hub/Program.cs`

## Không làm
- Không sửa `hub/Services/Contracts.cs`, `hub/Models/HubModels.cs`, parser W1-05 hoặc dịch vụ cấu hình/phân vai.
- Không sửa test nghiệm thu, `hub.tests/AITeam.Hub.Tests.csproj`, `core.tests/Support/TestShell.cs` hoặc bản sao `hub.tests/Fixtures/W106/`; phần liên kết TestShell chỉ là hạ tầng đã chuẩn bị, KHÔNG đổi.
- Không sửa team.ps1, tools/, skill/, projects.json, team.json hoặc dữ liệu thật.
- Không migrate, ghi nguồn, chạy git/agy/codex, thêm package, giao diện, thao tác điều khiển hay bộ cache.
- Không đọc log thật hoặc tự đoán schema kết quả verify/review; log chỉ là nguồn chẩn đoán nguyên văn trong khung hiện tại.

## Ràng buộc
- Theo chính xác XML summary của IWorkProgressService. Phần thân TODO là nơi agent triển khai; giữ API để W1-07 dùng.
- Nối task bằng metadata `- **Yêu cầu:** R-xxx`, không tìm R-xxx trong nội dung tùy ý. Task thiếu liên kết hoặc trỏ yêu cầu chưa có phiếu nằm trong UnlinkedTasks. Metadata có liên kết nhưng mã sai thì báo lỗi nguồn.
- Tên phiếu thật dạng `requests/R-xxx-<slug>.md`, nhật ký `## 3. Nhật ký (…)`, có CRLF/BOM tùy nguồn; giữ nguyên byte, chỉ giải mã khi đọc. Không dùng File.ReadAllText với fallback UTF-8 thay ký tự lỗi.
- ID yêu cầu chỉ `^R-\d+$`; mã task theo quy tắc W1-05. So ID không phân biệt hoa thường. Tên dự án là khóa tra cấu hình, không ghép vào đường dẫn. null/rỗng/không tìm thấy: InvalidDataException với đúng `Việc không hợp lệ.`.
- Lỗi đọc/giải mã/JSON, trùng ID, reparse, giới hạn, metadata sai: IOException với `Không đọc được tiến độ công việc. Nguồn: <tên an toàn>`. Tên chỉ basename hoặc nhãn requests/plans/tasks/logs/cấu hình; không lộ đường dẫn máy, nội dung đầu vào hoặc inner exception có bí mật.
- Kiểm đường dẫn chuẩn hóa và mọi tổ tiên/file trước đọc; từ chối symlink/junction/reparse point dù đích vẫn ở trong repo. Không theo liên kết từ metadata. Giới hạn mỗi file 2 MiB, tối đa 2000 file mỗi thư mục nguồn. Dữ liệu hỏng phải báo lỗi, không coi là rỗng.
- Repo tồn tại nhưng không có requests/plans/tasks/logs là bình thường; repo không tồn tại là lỗi nguồn. Không bắt buộc .git, không fallback sang repo AITeam khi chọn dự án khác.
- Tính progress bằng parser W1-05 cùng journal/plan/board. Không tự sửa thứ tự ưu tiên hay suy diễn ĐẠT từ văn xuôi. Requests/Tasks/UnlinkedTasks xếp ID OrdinalIgnoreCase; StepCounts đủ 9 khóa, tổng bằng số task.
- Tiến độ đại diện yêu cầu: task chưa merge đầu tiên theo ID; nếu mọi task merge lấy task đầu; chưa có task thì Calculate bằng ID yêu cầu với plan/journal. Giữ yêu cầu rỗng trong danh sách.
- Người theo IWorkflowRolesService.GetRoles và ITeamConfigService.GetSettings().Developers; code ưu tiên DeveloperInfo có Id khớp metadata Agent. Mã bước ánh xạ và tên dự phòng chuẩn ghi ở XML. Chỉ gán tên cho Done/Failed/Current; Waiting và giờ thiếu giữ `Chưa có dữ liệu`.
- Log chỉ từ WorkDir/logs: `<task>-verify-N.md`, `<task>.review-N.json`, N nguyên dương; không nhận tên khác. Thiếu log trả rỗng; review có mặt phải là JSON object hợp lệ. Đọc UTF-8 nghiêm ngặt, Logs xếp theo tên, Content nguyên văn. Không dùng JSON mẫu tự chế để suy luận gate.
- Dịch vụ singleton chỉ đọc; nhiều lời gọi đồng thời không lẫn snapshot, không tạo thư mục hay sửa byte nguồn. Không cần khóa ghi/giao dịch vì không có thao tác ghi.
- File .cs lưu UTF-8 CÓ BOM, giữ xuống dòng CRLF. Không thêm cờ nhận biết đang chạy test.

## Dữ liệu mẫu thật
Các đường dẫn dưới đây đều tương đối với `hub.tests/Fixtures/W106/`. Codex đã sao chép nguyên byte và so SHA-256 với nguồn trước khi giao:
- `projects.json`: mảng ở gốc; test dùng config giả trả ProjectInfo với đường dẫn tạm, không dùng đường dẫn máy trong bản sao.
- `requests/R-010-bo-skill-cho-tung-buoc-day-chuyen-tiep-n.md`
- `requests/R-009-hub-v61-theo-gop-y-van-phong-the-nghien.md`
- `requests/R-008-day-viec-khoi-claude-day-chuyen-tu-chay.md`
- `tasks/README.md`
- `tasks/V8-02-dieu-phoi-pipeline-theo-phu-thuoc.md`
- `tasks/S1-07-noi-nghiem-thu-imp-va-hang-cho-map.md`
- `tasks/H7-07-giao-dien-map.md`
- `plans/R-009/plan.md`
- `plans/R-010/plan.md`
R-009 thật đã có H7-07 Merge, không được ghi cứng mọi task R-009 chưa làm. Bảng hiện tại ghi V8-02 đã merge; ca thiếu bảng đối chiếu trực tiếp parser W1-05 với nhật ký thật, không khóa kỳ vọng theo trạng thái cũ trong brief. Ca lỗi/biên biến đổi bản sao trong thư mục tạm. Không đọc AITeam thật hoặc USERPROFILE lúc chạy test.

## Test nghiệm thu
`hub.tests/AcceptanceW106WorkProgressServiceTests.cs`

## Tiêu chí hoàn thành
- [x] Hai API triển khai đúng XML, DI phân giải được; không đổi hợp đồng ngoài phạm vi.
- [x] Bản sao R-010/R-009/R-008 nối đúng S1-07/H7-07/V8-02; R-009 nhận H7-07 Merge và dữ liệu chưa có task/nhiều task/task cũ được giữ đúng.
- [x] Repo khác/rỗng không đọc nhầm AITeam; mất log không lỗi, nguồn hỏng báo đúng câu và tên nguồn an toàn.
- [x] Chặn ID sai, traversal, reparse và quá giới hạn; không rò bí mật/đường dẫn trong exception.
- [x] Tên người từ cấu hình, không bịa giờ; đồng thời/đọc lại không đổi byte và không cache cũ.
- [x] `dotnet build AITeam.sln` đạt 0 lỗi, 0 warning khi đã triển khai (CS9113 chỉ được chấp nhận ở khung prep).
- [ ] `dotnet test AITeam.sln` pass hết; riêng 35 ca nghiệm thu chạy bằng `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --filter FullyQualifiedName~AcceptanceW106WorkProgressServiceTests --blame-hang-timeout 2m`.

## Kiểm tra chuẩn bị (Codex)
- Khung/DI: đã tạo; hai hàm đều chỉ throw NotImplementedException, không có thân triển khai.
- Hạ tầng: hub.tests liên kết file chung TestShell.cs, không viết ProcessStartInfo riêng; TempRepo(git:false), junction tạo bằng TestShell.PowerShell trong thư mục tạm.
- Build lượt 1: `dotnet build AITeam.sln` trả mã 1, chỉ in `Build FAILED; 0 Warning(s); 0 Error(s)`; chưa xác định nguyên nhân.
- Build lượt 2: `dotnet build AITeam.sln -m:1 -v:normal` đạt, mã 0, 0 lỗi, 4 warning, 25,00 giây: 3 CS9113 của tham số khung và 1 xUnit2031. Đã sửa xUnit2031 bằng overload Assert.Single có predicate; chưa build lại sau chỉnh assertion vì giới hạn tối đa 2 lượt. Không nhận đã xác minh 0 warning cho bản cuối.
- `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --filter FullyQualifiedName~AcceptanceW106WorkProgressServiceTests --blame-hang-timeout 2m --logger "console;verbosity=quiet"`: tổng 35, fail 35, pass 0, skip 0 (đỏ chờ triển khai).
- Chạy lại 5 ca (dữ liệu thật và 4 junction) với `--no-build --filter "FullyQualifiedName~Real_requests_join_tasks|FullyQualifiedName~Reparse_source_directory" --blame-hang-timeout 2m --logger "console;verbosity=normal"`: 5 fail vì NotImplementedException từ GetProject; junction tạo được bằng helper, không phải lỗi harness.
- Chưa chạy toàn bộ solution test ở bước prep vì thân khung cố ý chưa triển khai. Không chạy git.

| Điều kiện giao việc | Bằng chứng thật | Trạng thái |
|---|---|---|
| Đặc tả/phạm vi/API/DI | Chỉ hai file sản phẩm trong Phạm vi; Program đăng ký singleton, hai phương thức throw TODO | Đạt |
| Dữ liệu thật | 10 bản sao trong Fixtures/W106; Get-FileHash so từng nguồn/bản sao đều trùng SHA-256 | Đạt |
| Test biên/lỗi/bảo mật/cũ | 35 ca được runner khám phá, 35 đỏ, 0 skip; 5 ca kiểm lại xác nhận NotImplementedException | Đạt trạng thái prep |
| Biên dịch | Build lượt 2 mã 0, 0 lỗi; 3 CS9113 được phép ở khung | Đạt 0 lỗi |
| Cảnh báo assertion | Sửa xUnit2031 theo overload predicate sau build; chưa build lại do giới hạn | Chưa xác minh bản cuối |
| BOM/không triển khai thân | Kiểm byte đầu cả WorkProgressService.cs, Program.cs, AcceptanceW106WorkProgressServiceTests.cs: BOM=True | Đạt |

File đã tạo: WorkProgressService.cs, AcceptanceW106WorkProgressServiceTests.cs và 10 fixture nguyên byte. File đã sửa: Program.cs (DI), AITeam.Hub.Tests.csproj (liên kết TestShell chung), task này (đặc tả/bằng chứng). Quyết định: dùng snapshot thật hiện tại cho R-009/V8-02; log chỉ giữ nguyên văn vì chưa được phép đọc schema thật.

## Điều chưa chắc
- Không đọc logs/ theo giới hạn đầu vào; chưa xác minh schema log thật. Task này chỉ trả nội dung log và kiểm JSON object, chưa diễn giải schema thành tiến độ mới.
- W1-05 chọn lượt mới nhất theo số; Merge lượt 1 có thể không thắng Verify lượt lớn hơn nếu thiếu ✅ ở bảng. Giữ hợp đồng W1-05, không sửa parser trong W1-06.
- `RealFile.Copy` chưa có trong Support tại worktree này; fixture được chép nguyên byte khi prep và hạ tầng Fixtures sẵn có chép ra output, test chỉ đọc bản sao output.

## Báo cáo của agent
B: `dotnet build AITeam.sln --no-restore -m:1 -v:minimal`; N: `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --filter "FullyQualifiedName~AcceptanceW106WorkProgressServiceTests|FullyQualifiedName~W106WorkProgressReviewTests" --blame-hang-timeout 2m --logger "console;verbosity=minimal"`; T: `dotnet test AITeam.sln --no-build -m:1 --blame-hang-timeout 2m --logger "console;verbosity=minimal"`.
| Tiêu chí | Bằng chứng QC lượt 2, 26/10/2026 |
|---|---|
| Hai API, DI, hợp đồng | N: 47 pass, 0 fail, 0 skip (35 nghiệm thu + 12 bổ sung); giữ nguyên API/DI. |
| Mẫu thật, yêu cầu rỗng/nhiều task/task cũ | N: 35/35 nghiệm thu pass; không sửa test nghiệm thu/fixture. |
| Repo khác/rỗng, log thiếu/nguồn hỏng | N: các ca nghiệm thu liên quan pass trong tổng 47/47. |
| ID, traversal, reparse, giới hạn | N: các ca nghiệm thu liên quan pass trong tổng 47/47. |
| Người, giờ thiếu, đồng thời/đọc lại | N: các ca nghiệm thu liên quan pass trong tổng 47/47. |
| REQUIRED 1: cấu hình nghiêm ngặt | N: 9 ca JSON/UTF-8/quá 1 MiB trên 3 file với TeamConfigService thật pass; 2 ca cấu hình hợp lệ/có hoặc thiếu file tùy chọn pass; lỗi IOException nguồn cấu hình, không inner exception. |
| REQUIRED 2: snapshot độc lập | N: 1 ca sửa Areas sau GET pass; snapshot cũ giữ dữ liệu, GET mới nhận thay đổi; ProjectInfo và dictionary đều sao chép mới. |
| Build, BOM, ngân sách | B: mã 0, 0 warning, 0 error, 19,16 giây; kiểm byte BOM/CRLF=True; service 270 dòng, test bổ sung 96 dòng. |
| Test toàn solution | T: Hub 644 pass, 9 fail, 0 skip; Core thấy 2 fail V8-05 (Git signal pipe Win32 error 5), chưa tổng kết; ngắt gần 3 phút theo giới hạn chống treo, mã 1. |
File đổi: WorkProgressService.cs (2 REQUIRED); W106WorkProgressReviewTests.cs (test hồi quy được quản lý yêu cầu); task này (báo cáo). Tự quyết: kiểm root JSON đúng array/object, tái dùng Read với giới hạn 1 MiB cho cấu hình.
Chưa xác minh toàn solution xanh: 7 lỗi quyền File.Replace ở cấu hình/phân vai và 2 assertion Map; Core chưa hoàn tất. Không sửa ngoài phạm vi; không chạy lệnh git/app/chụp ảnh; hệ thống tự commit/push, chưa xác minh commit.
## Review (Claude)
<!-- Claude điền sau cổng kiểm tra. -->

### Vòng 3 — ĐẠT (Claude)
- Cổng KT lượt 7: toàn bộ test đạt (V8-04 chạy lại 2 test chập chờn S1-04/V8-02); mục 'ngoài phạm vi' `hub.tests/W106WorkProgressReviewTests.cs` là test quản lý yêu cầu thêm ⇒ CHẤP NHẬN.
- QC lượt 1–2 (4 required) đã sửa: reparse + UTF-8/JSON/1MB cho projects.json/team.json/team.local.json trước dịch vụ cấu hình; plans là file ⇒ IOException; snapshot sao chép Areas. Claude kiểm dòng 72–81, 200 + 47/47 test W1-06.
- QC lượt 3 không chạy được (Codex hết hạn mức tới 04:14) ⇒ quản lý nghiệm thu trực tiếp.

## Kết quả
- Có dịch vụ gom yêu cầu + task + bước hiện tại theo dự án cho trang Việc (`hub/Services/WorkProgressService.cs`), chỉ đọc, chặn liên kết / cấu hình hỏng.
- Kiểm chứng: test nghiệm thu + test bổ sung W1-06, toàn bộ test.
<!-- Claude điền khi review đạt. -->

## Đánh giá quy trình
<!-- Claude điền sau merge. -->