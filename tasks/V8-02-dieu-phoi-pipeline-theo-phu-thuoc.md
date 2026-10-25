# V8-02 · Điều phối pipeline theo phụ thuộc

- **Agent:** A
- **Nhánh:** `agent-a/V8-02-dieu-phoi-pipeline-theo-phu-thuoc`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~380 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
- **Model:** `claude-opus-5-5-medium`

## Tóm tắt
- Có chức năng mới: một lệnh `pipeline -Request R-xxx` chuẩn bị và giao các việc đủ điều kiện cho Antigravity.
- Hai việc độc lập có thể chạy cùng lúc; các lượt Codex chuẩn bị và kiểm tra dùng chung một khóa trên máy.
- Việc phụ thuộc phải chờ quản lý merge việc trước; pipeline không tự merge.
- Mỗi việc chỉ hiện một dòng kết quả, giữ log chi tiết riêng để giảm thao tác đọc của quản lý.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-008/brief.md`
- `plans/R-008/plan.md` (V8-02, rủi ro và quyết định quản lý)
- `team.ps1` (prep, assign, verify, QC, điều phối lệnh và catch cuối)
- `tools/workflow-common.ps1` (hợp đồng đã có từ V8-01; KHÔNG đổi)
- `tools/workflow-pipeline.ps1`
- `tools/test-verify.ps1`
- `core.tests/AcceptanceV802PipelineTests.cs`
- `core.tests/AITeam.Core.Tests.csproj` (wiring nghiệm thu; KHÔNG đổi)
- `core.tests/Fixtures/V802/plan.md`
- `core.tests/Fixtures/V802/board.md`

## Bối cảnh
Làm đúng V8-02 trong kế hoạch R-008 đã duyệt. V8-01 đã cung cấp khóa giao dịch repo và cổng thử lại lỗi máy; không viết lại. Invoke-Prep đã có nhánh tiếp tục worktree prep dở khi chưa có file task trong repo chính. Invoke-Assign hiện có cờ tự giao lại ở scope script và reset VerifyRetried sau QC; không dùng nguyên cách reset này cho pipeline.

## Phạm vi (được sửa)
- `team.ps1`
- `tools/workflow-pipeline.ps1`
- `tools/test-verify.ps1`

## Không làm
- Không merge, accept, accept-pack, thay Hub, định dạng cấu hình, mô hình dữ liệu hoặc chức năng V8-03.
- Không đổi `projects.json`, `tools/workflow-common.ps1`, fixture, test nghiệm thu hoặc csproj đã nối test.
- Không gọi AI thật khi test; không nhận diện harness trong sản phẩm.
- Không tự giao lại task đã có file trong repo chính, kể cả trạng thái lỗi/chưa giao.
- Không chạy ứng dụng, chụp ảnh hay kiểm trình duyệt.

## Ràng buộc
- PowerShell 5.1; `.ps1` UTF-8 có BOM. Giữ nguyên dữ liệu cũ, BOM/xuống dòng của dữ liệu đọc; không sửa plan khi phân tích.
- Giữ chữ ký và hợp đồng `///` trong khung. Chỉ điền TODO và nối adapter trong phạm vi. Đây là script PowerShell, không có dịch vụ Hub/DI hoặc lớp C# sản phẩm mới.
- `Read-WorkflowPipelinePlan`: heading `### MÃ · tên`, dòng `**Phụ thuộc:**` tách bằng `·`, `—` nghĩa không có. Duyệt chỉ tại mục `## Quyết định của quản lý…`: có `ĐÃ DUYỆT` và không có `TỪ CHỐI`. Chưa duyệt/từ chối: `Kế hoạch chưa được duyệt.`.
- Kiểm toàn bộ mã/đồ thị trước bất cứ prep/assign nào, kể cả lỗi ở task cuối; trùng mã không phân biệt hoa thường, thiếu phụ thuộc, tự phụ thuộc, chu trình và mã không hợp lệ: `Kế hoạch có phụ thuộc không hợp lệ.`.
- Request chỉ `^R-\d{3}$`; mã task chỉ `^[A-Za-z0-9]+-\d+[a-z]?$`. Không thực thi Markdown, không ghép nội dung ngoài vào lệnh. Chặn đường dẫn thoát và reparse point ở file/các thư mục cha, lỗi `Đường dẫn kế hoạch không an toàn.`.
- Adapter ReadBoard phải fetch thành công rồi đọc `origin/main:tasks/README.md`, không dùng bảng local. Chỉ hàng đúng mã có `✅ Đã merge` mới mở phụ thuộc; QC PASS/chờ review/tiền tố gần giống không đủ. Fetch/đọc hỏng phải dừng, không coi như bảng rỗng.
- Cố định hash plan trong lượt. Plan đổi thì dừng khởi chạy mới, chờ child đã chạy kết thúc và báo trạng thái các task còn lại.
- Khóa pipeline theo dự án chuẩn hóa + phiếu: lần trùng đang chạy trả rỗng, không prep/assign. Khóa Codex toàn máy dùng chung cho prep/QC (và accept ở task sau), nhả trong finally và xử lý abandoned mutex. Khóa repo V8-01 chỉ giữ trong giao dịch, không giữ khi chờ AI.
- Mỗi task một tiến trình riêng, tối đa hai tiến trình đang chạy. Mỗi task chỉ start một lần trong một lượt; bộ đếm agent/KT/QC tách riêng giữa task.
- Worker dùng Invoke-WorkflowPipelineTask. Prep gọi `prep -NoLaunch`, gồm khả năng tiếp tục worktree dở. Sau prep phải xác minh thành công thực tế trước assign, không suy mã 0 là thành công khi prep hiện chỉ return.
- Chạy Antigravity bằng `-Dev agy`, không fallback Codex viết code. Tách bước agent khỏi verify/QC bằng adapter (có thể dùng `assign -NoVerify`, rồi verify/QC); không chạy đồng thời hai cơ chế tự giao lại. Mọi lượt tiếp theo có Resume và Feedback đúng nguồn lỗi.
- KT trả lại tối đa một lần, QC trả lại tối đa một lần; không reset cờ KT khi QC trả lại. Tối đa ba lượt agent/task. Quota/lỗi dừng task, không ảnh hưởng task khác; QC chỉ sau KT đạt; BLOCKED/thiếu kết luận không được coi PASS.
- Không giữ khóa Codex suốt assign/chờ agent. Mỗi lượt prep/QC thực tế phải đi qua Invoke-WorkflowCodexLock, kể cả đường gọi trực tiếp bên ngoài pipeline; không khóa lồng gây deadlock.
- Callback trong lõi là adapter nghiệp vụ (ReadBoard/TaskExists/StartTask và Prep/Agent/Verify/Qc), không phải cờ test. StartTask trả Process đã chạy, stdout/stderr lưu log riêng; lõi chỉ trả `{Id,Status}`. CLI in một dòng `MÃ: trạng thái` mỗi task; không làm rò log dài ra console.
- Timeout hữu hạn khi chờ tiến trình, đọc stdout/stderr đồng thời, không để child mồ côi. Khóa Codex timeout báo `Hết thời gian chờ Codex.`. Lỗi CLI bắt ở catch ngoài cùng; TODO hiện phải in `LỖI: chưa làm`, mã 1.
- Test dùng thư mục tạm/tiến trình giả. `powershell.exe -File`, UTF-8 ở cả Console.OutputEncoding và ProcessStartInfo.StandardOutputEncoding; không EncodedCommand. Dọn ReadOnly trước xóa. Không dùng dữ liệu cá nhân hoặc hồ sơ người dùng.

## Dữ liệu mẫu thật
- `core.tests/Fixtures/V802/plan.md`: bản sao nguyên byte từ `plans/R-008/plan.md` tại lúc chuẩn bị, gồm quyết định duyệt thực tế; không đoán marker duyệt.
- `core.tests/Fixtures/V802/board.md`: bản sao nguyên byte bảng `tasks/README.md`, có hàng V8-01 merge thật. Test chỉ biến đổi bản sao tạm để dựng lỗi/trạng thái.
- Đã mở `projects.json` thật: mảng ở gốc; task này không phân tích lại cấu hình, tái dùng Resolve-Project. Không đọc/ghi phiếu trong V8-02 nên không dựng fixture phiếu giả.

## Test nghiệm thu
`core.tests/AcceptanceV802PipelineTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln`: 0 lỗi, 0 warning mới; không còn TODO trong khung V8-02.
- [ ] `dotnet test AITeam.sln` pass; toàn bộ AcceptanceV802PipelineTests bất biến pass.
- [ ] `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/test-verify.ps1` pass, gồm hồi quy lệnh cũ và nghiệm thu xUnit mới. Chạy riêng nghiệm thu bằng `-PipelineOnly` sau build.
- [ ] Plan chưa duyệt/từ chối/đồ thị sai không gọi worker; dữ liệu cũ không BOM/CRLF đọc đúng, không bị viết lại; đường dẫn xấu/reparse bị chặn.
- [ ] Hai task độc lập chồng thời gian, task thứ ba chờ; kết quả đúng một dòng mỗi task. Task có sẵn không prep/assign; phụ thuộc chỉ mở theo bảng origin/main đã merge, không tự merge.
- [ ] Hai pipeline cùng phiếu không chạy trùng; child lỗi và exception nhả khóa; prep/QC thực tế không chồng Codex toàn máy.
- [ ] Số lượt agent/verify/QC đúng từng task, không reset KT sau QC, không fallback khi hết quota; prep dở tiếp tục mà không gọi lại Codex tạo khung.
- [ ] Bổ sung test adapter CLI cho fetch lỗi/bảng local khác origin, tiếp tục prep dở và timeout child; test lõi hiện có không thay cho kiểm tra các adapter thật. Không đổi test nghiệm thu để hợp thức hóa adapter sai.

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng: lệnh + kết quả thật (lượt sửa cổng KT) |
|---|---|
| Build; không TODO V8-02 | `dotnet build AITeam.sln --no-restore --disable-build-servers -m:1 -v:minimal`: đạt, 0 lỗi/0 warning; parser 3 script: 0 lỗi, BOM EF-BB-BF; module không còn TODO. |
| Test solution | `dotnet test AITeam.sln --no-build --no-restore --disable-build-servers -m:1 --blame-hang-timeout 2m`: dừng Ctrl+C khi quá 3 phút, mã 1; Hub 518 pass/9 fail (Access denied), Core chưa tổng kết, S1-04 prep thiếu hàm khóa. |
| Hồi quy + nghiệm thu bất biến | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/test-verify.ps1`: 2 kiểm tra tĩnh đạt, dừng ở clone repo tạm (Win32 error 5); thêm `-PipelineOnly`: ít nhất 4 fail, dừng khi quá 3 phút, chưa tổng kết. |
| Duyệt/đồ thị/đường dẫn/dữ liệu cũ | Đã chạy `-PipelineOnly`, chưa có tổng kết hoàn chỉnh; không sửa plan, fixture hay test nghiệm thu. |
| Hai child/task cũ/phụ thuộc origin | `-PipelineOnly` chưa hoàn tất; chưa xác minh lại đầy đủ tiêu chí này trong lượt sửa. |
| Khóa trùng/nhả khóa/Codex prep-QC | `-PipelineOnly` chưa hoàn tất; tái hiện PowerShell 5.1 Start-Process có redirect + WaitForExit trả ExitCode null, giữ Handle trước chờ trả đúng 7. |
| Bộ đếm/không fallback/prep dở | PowerShell 5.1 gọi lõi worker trực tiếp: 4/4 ca verify, blocked, mixed, qc đạt đúng số agent/KT/QC; nghiệm thu tương ứng vẫn lỗi helper Stage (dòng 343–345). |
| Adapter CLI/fetch/prep dở/timeout | Script hồi quy bị chặn bởi Git trước các kịch bản adapter; chưa xác minh lại, không gọi AI thật. |
| Hồi quy skill-note sau sửa | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceS106SkillNotesTests.Unsafe_id_never_changes_data --blame-hang-timeout 2m`: 18/18 pass; lượt riêng duplicate + COM1: 2/2 pass (19 ca duy nhất). |
File đổi lượt này: `team.ps1` (nạp hai module workflow trong try/catch, lệnh skill-note không phụ thuộc chúng); task này (báo cáo); giữ BOM, không đổi test nghiệm thu.
Cần Claude xử lý harness: S1-04 trích riêng Invoke-Prep chưa nạp Invoke-WorkflowCodexLock; V8-02 Stage chưa giữ Handle trước WaitForExit. Không thêm mã nhận diện test/bỏ khóa để làm xanh.
Chưa đạt toàn bộ cổng KT; không chạy git trực tiếp, hệ thống tự commit/push; chưa có commit được kiểm. MCP báo tiến độ không khả dụng trong phiên.
## Ghi nhận bước chuẩn bị (Codex)
- Khung sản phẩm chỉ throw NotImplementedException; wiring import/lệnh/catch có sẵn, không viết thân nghiệp vụ.
- Các fixture là bản sao nguyên byte; test sửa bản tạm, không sửa dữ liệu thật. Không chạy git theo yêu cầu.
- Hợp đồng callback tách orchestration khỏi CLI để kiểm số tiến trình thật mà không gọi AI; coder chịu trách nhiệm nối adapter thật và test bổ sung nêu trên.
- Có 38 ca xUnit (12 Fact + 26 InlineData). Chưa xác minh discovery/thực thi do chưa build được.
- Build lần 1: `dotnet build AITeam.sln` trả mã 1, tổng kết 0 warning/0 error nhưng không có chẩn đoán, không coi là đạt.
- Build lần 2: `dotnet build AITeam.sln --no-restore --disable-build-servers -m:1 -v:minimal` trả mã 1, 5 lỗi NETSDK1004 thiếu project.assets.json, 0 warning. Đã dùng hết giới hạn 2 lần build; chưa xác minh biên dịch C#.
- `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --filter FullyQualifiedName~AcceptanceV802PipelineTests --blame-hang-timeout 2m` trả mã 0 nhưng không hiện discovery/kết quả test: KHÔNG tính là test pass hoặc đã chứng minh toàn bộ test đỏ.
- Parser PowerShell: ba script và 20 đoạn script trong test đều 0 lỗi; kiểm BOM bốn file mã đạt; hash hai fixture khớp nguồn. Smoke khung bằng powershell.exe -File trả mã 1 với LỖI: chưa làm như dự kiến, không báo thành công giả.
- Chưa chạy AI thật, toàn bộ hồi quy hoặc test adapter thật. Cần restore được dependency trước khi xác minh build và test nghiệm thu.

## Review (Claude)
<!-- Claude điền khi nghiệm thu. -->

## Kết quả
<!-- Claude điền khi review ĐẠT. -->

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 4. Giao việc | Codex | | | | | |
| 5. Thực hiện code | Antigravity | | | | | |
| 6. Review / QC | Cổng KT + reviewer | | | | | |
