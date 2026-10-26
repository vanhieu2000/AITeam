# W1-01 · Helper nguồn thật và sandbox PowerShell

- **Agent:** A
- **Nhánh:** `agent-a/W1-01-helper-nguon-that-va-sandbox-powershell`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~350 dòng
- **Yêu cầu:** R-011
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Không thêm chức năng cho người dùng cuối; bổ sung hai helper dùng chung cho người viết test.
- Chép nguồn thật nguyên byte để test không đoán sai BOM, xuống dòng hoặc cấu trúc dữ liệu.
- Nạp đúng hàm PowerShell cần kiểm tra, không khởi chạy phần điều phối của script.
- Giả lập lệnh AI và git, ghi lại đối số/kết quả để test chạy an toàn và dễ kiểm chứng.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-011/brief.md` (mục tiêu 1; chỉ phần helper W1-01 theo kế hoạch đã duyệt trong yêu cầu giao việc).
- `core.tests/Support/TestShell.cs`
- `core.tests/Support/RealFile.cs`
- `core.tests/Support/TeamScript.cs`
- `core.tests/AcceptanceW101HelperTests.cs`
- `core.tests/AITeam.Core.Tests.csproj` (chỉ đọc).
- `team.ps1` (chỉ đọc nguồn thật và hàm Get-NextJournalRun).
- `scripts/SkillNotes.ps1`, `tools/workflow-common.ps1`, `templates/task.md` (chỉ đọc mẫu thật).

## Bối cảnh
Làm theo phần helper nguồn thật và sandbox trong kế hoạch W1-01 đã duyệt, ngân sách khoảng 350 dòng phần triển khai. TestShell đã xử lý UTF-8, CLIXML, timeout và dọn file chỉ đọc; không viết lại bộ chạy tiến trình. W1-01 ở đây là helper, không phải task trang Việc dù brief có nhắc ưu tiên đánh số giao diện.

## Mục tiêu
Giữ nguyên chữ ký public và hợp đồng XML trong hai file khung. Triển khai RealFile.Copy và TeamScript (FakeCommand, FakeCall, TeamScriptResult). Không có dịch vụ Hub nên không đăng ký DI.

## Phạm vi (được sửa)
- `core.tests/Support/RealFile.cs`
- `core.tests/Support/TeamScript.cs`
- `tasks/W1-01-helper-nguon-that-va-sandbox-powershell.md` (chỉ mục Báo cáo của agent).

## Không làm
- Không sửa test nghiệm thu mới hoặc acceptance cũ, TestShell, project, team.ps1, tools/, scripts/, templates/ hay hợp đồng Hub.
- Không di chuyển/chuyển đổi các test cũ sang helper trong task này.
- Không thêm UI, tài liệu QC, pipeline plan/prep, khóa hoặc giao dịch git.
- Không chạy agy/codex/git push thật. Không gọi exe bằng đường dẫn tuyệt đối trong script test.
- Không chạy lệnh git khi thực hiện lượt chuẩn bị này; hệ thống tự commit.

## Ràng buộc
- File .cs lưu UTF-8 có BOM; không đổi byte nguồn thật. Dùng File.Copy hoặc luồng byte, không decode/encode lại bản sao.
- RealFile tìm gốc từ AppContext.BaseDirectory đi ngược tới nơi có team.ps1 và templates/task.md; không dựa vào .git hoặc CWD.
- Copy tạo cây đích và ghi đè file cũ. File nguồn thiếu ném FileNotFoundException có “Không tìm thấy file nguồn.”.
- Chặn null/rỗng/trắng, thành phần .. (cả khi chuẩn hóa vẫn nằm trong gốc), đường dẫn tuyệt đối, ổ đĩa tương đối, UNC, ADS. Kiểm reparse point trên file/cây nguồn, cây đích và mọi cha trước khi ghi. Sai ném ArgumentException có “Đường dẫn bản sao không hợp lệ.”. Không nuốt lỗi I/O.
- TeamScript dùng TempRepo(git: false), không khởi tạo git. SourceRoot tùy chọn phục vụ fixture tạm; mặc định nguồn thật. team.ps1 bắt buộc; tools/scripts/templates thiếu thì bỏ qua, có thì chép đệ quy nguyên byte và chặn reparse.
- Nạp bằng Parser.ParseFile + AST, chỉ lấy định nghĩa hàm cấp script được chọn. Không thực thi dispatcher, param, dot-source hay khởi tạo ngoài hàm; phụ thuộc hàm phải được chọn tường minh.
- $Root trỏ sandbox; $PSScriptRoot trong hàm là thư mục script bản sao. Script truyền RunAsync là mã test tin cậy, không phải dữ liệu người dùng; đây không phải sandbox chống PowerShell độc hại.
- TestShell.PowerShell là bộ chạy duy nhất, timeout mặc định 60 giây; quá hạn truyền TimeoutException và dừng cây tiến trình. Không tự viết ProcessStartInfo.
- Cài fake sau bước nạp hàm; chặn agy/codex/git và biến thể .exe, không phân biệt hoa thường. Mọi git đều giả, kể cả status/push. Mặc định code 0 và output/error rỗng; cấu hình fake không trùng, chỉ ba tên chuẩn.
- Fake giữ nguyên từng đối số, thứ tự gọi, code/output/error; đặt LASTEXITCODE; stderr giả phải ghi trực tiếp stderr, không để ErrorActionPreference=Stop làm dừng fake code khác 0. Mỗi Run có tiến trình/log riêng.
- AST lỗi: Process.Code=1, Error chứa “LỖI: cú pháp PowerShell không hợp lệ.”; hàm thiếu/trùng: “LỖI: hàm PowerShell không hợp lệ.”. Lỗi đoạn test: Code=1, Error chứa “LỖI: ” + thông điệp và vẫn thu nhật ký fake.
- functionNames null/rỗng/tên trắng/trùng, fake sai, timeout <=0: ArgumentException có “Tham số sandbox không hợp lệ.”. File script thiếu: FileNotFoundException như RealFile.
- Dispose dọn file chỉ đọc, gọi lại được; lỗi khởi tạo phải dọn tài nguyên đã tạo. Không thay đổi nguồn hoặc hồ sơ người dùng.
- Không thêm cache, singleton hay xử lý đồng thời trong cùng instance. Không dùng dữ liệu dự án thật ngoài các nguồn chỉ đọc đã liệt kê.

## Dữ liệu mẫu thật
- Đã mở team.ps1 (param, dispatcher và Get-NextJournalRun), scripts/SkillNotes.ps1, tools/workflow-common.ps1 và templates/task.md.
- templates/task.md thật có BOM UTF-8 và CRLF. Test so byte trực tiếp với nguồn, giữ cây/tên.
- Ca tên có dấu dùng bản sao team.ps1 thành scripts/nguồn có dấu.ps1 trong thư mục tạm, nối hàm probe/dispatcher bẫy vào bản sao để kiểm AST.
- Không parse projects.json hay phiếu requests ở task này; không dựng giả các định dạng ấy.

## Test nghiệm thu
`core.tests/AcceptanceW101HelperTests.cs`

## Tiêu chí hoàn thành
- [ ] RealFile giữ nguyên byte/cây/tên, ghi đè bản sao cũ; báo lỗi thiếu file; chặn traversal/absolute/ADS/reparse trước khi ghi.
- [ ] Sandbox chép nguồn thật kèm ba cây tùy chọn, bỏ qua cây thiếu, cô lập và tự dọn cả file chỉ đọc.
- [ ] AST chỉ nạp hàm chọn, không chạy dispatcher; lỗi cú pháp/hàm thiếu/trùng rõ ràng; Unicode/BOM/CRLF/tên có dấu đúng.
- [ ] Fake thành công/thất bại/alias .exe và git push không gọi chương trình thật; log đúng đối số/code/output/error, không lẫn lần chạy.
- [ ] Timeout hữu hạn, lỗi sau fake vẫn giữ log; kiểm tham số và nguồn reparse đầy đủ.
- [ ] `dotnet build AITeam.sln` thành công, 0 warning khi triển khai xong.
- [ ] `dotnet test AITeam.sln` pass toàn bộ; không sửa/Skip test nghiệm thu.
- [ ] Báo cáo có bảng từng tiêu chí → bằng chứng thật, file đổi/lý do, quyết định và điều chưa xác minh.

## Kiểm chứng lượt chuẩn bị (Codex)
| Điều kiện giao việc | Bằng chứng thật |
|---|---|
| Khung và phạm vi | Hai file Support mới; mọi thân hàm đều ném NotImplementedException, chưa triển khai. Test nghiệm thu bất biến nằm ngoài phạm vi sửa của agent. |
| Build | Lần 1: dotnet build AITeam.sln trả mã 1, 0 warning/0 error nhưng không có chẩn đoán. Lần 2: dotnet build AITeam.sln -m:1 -v:normal thành công, 0 Warning(s), 0 Error(s), 33,06 giây. Đã dùng đủ 2 lượt build. |
| Test đỏ | dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceW101HelperTests --blame-hang-timeout 2m --logger 'console;verbosity=quiet': 33 ca, 0 pass, 33 fail, 0 skip. |
| Kiểm tra lỗi reparse | Chạy lại filter Copy_rejects_destination_junction_and_parent với logger normal: 2 ca đỏ do nhận NotImplementedException thay ArgumentException; tạo/dọn junction đã chạy thành công. |
| Mã hóa và nguồn thật | Kiểm byte ba file .cs mới và file task: đều có BOM EF BB BF. Nguồn team.ps1 có BOM; templates/task.md có BOM và CRLF. |
| Giới hạn | Chưa chạy toàn bộ suite; chỉ nhóm nghiệm thu mới. Không chạy git hoặc AI thật. |

File chuẩn bị: task này (đặc tả), RealFile.cs và TeamScript.cs (khung), AcceptanceW101HelperTests.cs (33 ca).
Quyết định: thêm sourceRoot tùy chọn để kiểm nguồn thiếu/reparse bằng thư mục tạm; fake toàn bộ git để không rò lệnh ra máy thật; giữ model mặc định vì task M không có các điều kiện bắt buộc đổi model.
Điều chưa xác minh: hành vi triển khai chưa có; test timeout hiện đỏ ngay từ constructor TODO nên chưa chứng minh dừng tiến trình. Sandbox không bảo vệ trước mã test độc hại gọi exe bằng đường dẫn tường minh.
## Báo cáo của agent
| Tiêu chí | Bằng chứng thật lượt này |
|---|---|
| Sao chép byte/cây/tên, ghi đè, thiếu file, chặn đường dẫn | Đạt: nhóm W1-01 43/43 pass, 0 fail, 0 skip; test nghiệm thu không sửa. |
| Sandbox, cây tùy chọn, cô lập/dọn | Đạt: cùng nhóm; 3 ca tools/scripts/templates từ chối junction hỏng/đích thành file, dữ liệu ngoài liên kết giữ nguyên. |
| AST, lỗi cú pháp/hàm, Unicode/BOM/CRLF | Đạt: cùng nhóm 43/43 pass. |
| Fake tiến trình con/alias/git, đối số/code/output/error/log | Đạt: SkillNotes gọi git giả code 19 giữ FakeCall; kiểm cả agy/codex/git và .exe, khác hoa thường, đối số rỗng/Unicode. |
| Timeout, lỗi sau fake, tham số, reparse | Đạt: cùng nhóm; kiểm reparse trước Directory.Exists và từ cha xuống; timeout hữu hạn, lỗi vẫn giữ log. |
| Build solution | `dotnet build AITeam.sln --no-restore -m:1`: thành công, 0 warning, 0 error, 29,44 giây. |
| Test solution | `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m --logger "console;verbosity=minimal"`: chưa đạt; Hub 673 pass/9 fail/0 skip; Core chưa có tổng kết, dừng bằng Ctrl+C khi tiến sát 3 phút, mã 1. |
| Báo cáo và mã hóa | Đã cập nhật bảng bằng kết quả lượt này; RealFile.cs, TeamScript.cs, task và test nghiệm thu đều có BOM EF BB BF. |
| Lệnh nhóm W1-01 | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceW101HelperTests\|FullyQualifiedName~TeamScriptCodexReviewTests" --blame-hang-timeout 2m --logger "console;verbosity=minimal"`: 43 pass/0 fail/0 skip, 23 giây; dấu \ trước ký tự phân cách chỉ để hiển thị bảng. |
| File/lý do | Giữ phần sửa dang dở RealFile.cs và TeamScript.cs (shim PATH, kiểm reparse); lượt này bổ sung assertion alias tiến trình con và dữ liệu ngoài liên kết trong TeamScript.cs; task: báo cáo. |
Quyết định: giữ shim exe tạm bằng Add-Type, PATH riêng của sandbox; dùng junction thay symlink cần đặc quyền; không sửa test nghiệm thu, không mở rộng phạm vi để sửa lỗi suite cũ.
Chưa xác minh: toàn suite xanh; lỗi đã thấy ngoài W1-01 gồm File.Replace bị từ chối quyền, git CreateFileMapping Win32 error 5, timeout và assertion S106. Không chạy app/chụp ảnh/lệnh git trực tiếp; hệ thống tự commit/push, chưa có commit được kiểm.

## Review (Claude)
<!-- Claude ghi kết quả sau triển khai. -->

## Kết quả
<!-- Claude điền sau review đạt. -->
