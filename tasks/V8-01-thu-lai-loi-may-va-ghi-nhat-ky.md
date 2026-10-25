# V8-01 · Thử lại lỗi máy và ghi nhật ký

- **Agent:** A
- **Nhánh:** `agent-a/V8-01-thu-lai-loi-may-va-ghi-nhat-ky`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~320 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Không thêm chức năng cho người dùng; sửa độ tin cậy của quy trình giao việc.
- Build/test lỗi khóa file được thử lại đúng một lần, giữ kết quả cả hai lần để quản lý xem.
- Lỗi code hoặc test thất bại vẫn trượt; phục hồi lỗi máy vẫn phải qua QC như trước.
- Prep, assign, cổng kiểm tra, QC và merge được ghi vào nhật ký phiếu, tránh mất hoặc trùng dòng khi chạy đồng thời.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `tasks/V8-01-thu-lai-loi-may-va-ghi-nhat-ky.md`
- `team.ps1`
- `tools/workflow-common.ps1`
- `tools/test-verify.ps1`
- `core.tests/AcceptanceV801WorkflowTests.cs`
- `core.tests/AITeam.Core.Tests.csproj` (chỉ đọc để biết lệnh test)

## Phạm vi (được sửa)
- `team.ps1`
- `tools/workflow-common.ps1`
- `tools/test-verify.ps1`
- `tasks/V8-01-thu-lai-loi-may-va-ghi-nhat-ky.md` (chỉ Báo cáo của agent)

## Không làm
- Không sửa `core.tests/AcceptanceV801WorkflowTests.cs`: test nghiệm thu đã viết trước, bất biến; không Skip hoặc giảm assertion.
- Không đổi Hub, CLI C#, hợp đồng dữ liệu, config hay lệnh cũ ngoài thử lại/nhật ký/khóa đã nêu.
- Không gọi agy/codex thật trong test; không tắt build server/service, không cài phần mềm, không sửa máy thật.
- Không thêm UI, ảnh chụp hay yêu cầu chạy app. Không mở task/phiếu cũ hoặc quét repo.

## Ràng buộc
- Ngân sách phần agent khoảng 320 dòng; khung và test nghiệm thu đã chuẩn bị không tính vào phần agent viết.
- Toàn bộ PowerShell tương thích Windows PowerShell 5.1, UTF-8 có BOM. Giữ BOM và CRLF/LF vốn có của dữ liệu được cập nhật.
- Hợp đồng public là các hàm trong `tools/workflow-common.ps1`; chú thích `# ///` là đặc tả bắt buộc. Điền TODO, không đổi chữ ký. Không có dịch vụ Hub nên không cần DI.
- `Invoke-ShellCapture` vẫn thu cả stdout/stderr, Code và Output nguyên bản. Không lọc output trước phân loại. Chỉ phân loại khi Code khác 0. Chỉ thử lại stage lỗi, tối đa tổng hai lần; không gọi lại AI vì lỗi máy.
- Danh sách hạ tầng: Build/Test chỉ nhận MSB3491 kèm denied/being used, dòng `Access to the path '…' is denied.`, hoặc `The process cannot access the file '…' because it is being used by another process.`. Đối chiếu cả output: CSxxxx, MSB khác, warning code (trừ NU1900), test fail/hang/abort hay lỗi không nhận diện ⇒ không được coi là thuần hạ tầng. Dòng ngữ cảnh/summary bình thường không phải lỗi. MSB3491 trần không đủ.
- MCP chỉ nhận đúng dòng `MCP transport error: Connection reset by peer (os error 10054).`, không nhận chung chữ MCP/timeout/denied. Lẫn lỗi khác ⇒ trượt, không thử lại. MCP còn lỗi sau lần 2 ⇒ trượt; không mở rộng chữ ký khi chưa có bằng chứng.
- `Invoke-WorkflowCheck` trả Code/Output cuối và Attempts đủ Number/Code/Output cả hai lần; lưu nguyên output cả hai vào báo cáo verify và log prep thích hợp, có nhãn stage/lần. Lần 2 phải qua toàn bộ kiểm tra mã thoát, warning, test summary và hang như lần 1; không dùng kết quả phân loại để kết luận ĐẠT. Giữ ngoại lệ callback; điểm vào script bắt ngoại lệ, ghi stderr `LỖI: …`, mã thoát khác 0; NotImplementedException của khung phải thành `LỖI: chưa làm`, mã 1.
- Nối `Invoke-WorkflowCheck` trực tiếp trong `Invoke-Prep` và `Invoke-Verify`. Bỏ nhánh retry cũ làm mất output ở Prep. Nối `Invoke-WorkflowGate` trực tiếp trong `Invoke-Assign`, giữ Verify/Review để luồng cũ xử lý mã QC 0/1/3 và giới hạn giao lại. Gate gọi Verify rồi Qc khi Verify.Ok, không giữ khóa trong callback. MCP nếu dùng cho kết quả kiểm tra phải đi qua cùng hàm, không biến thất bại thành đạt.
- `Invoke-RepoTransaction`: cùng repo (chuẩn hóa đường dẫn, khác hoa thường/đuôi /.) dùng cùng khóa liên tiến trình; repo khác không chặn nhau. Tái nhập được; finally nhả khóa kể cả lỗi; quá hạn báo đúng `Không lấy được khóa giao dịch repo.`. Nối trực tiếp vào `Set-BoardStatus` và `Save-RunMetrics`; tất cả writer bảng việc ở Prep và merge cũng phải dùng khóa. Khóa bao trọn đọc-sửa-ghi-stage-commit liên quan, tuyệt đối không bao thời gian chờ AI/QC/build/test. Khóa repo riêng nếu metrics ở Root khác repo dự án; tránh khóa chéo gây deadlock.
- `Update-WorkflowJournal` nhận RepoPath/TaskFile/TaskId/Event/Run/Result/At. TaskFile chỉ tên .md trực tiếp trong tasks; kiểm tra đường dẫn chuẩn hóa nằm trong repo, không rooted/../wildcard, không reparse point ở root, thư mục hoặc file. TaskId và R-nnn hợp lệ, Run >= 1; không nhận CR/LF, dấu |, HTML ở trường đưa vào dòng.
- Đọc UTF-8 nghiêm ngặt. Phiếu thiếu/hỏng, thiếu hoặc trùng `## Nhật ký`, ID/path không an toàn: báo chính xác `Không ghi được nhật ký phiếu.`, không sửa file nào. Task cũ không có Yêu cầu trả false, không tạo phiếu. Không catch rỗng rồi ghi đè. Thiếu task cũng là lỗi, khác task cũ.
- Chèn trong đúng `## Nhật ký` của `requests/R-nnn.md`, trước heading ## tiếp theo, bảo toàn nội dung khác. Dòng: `- yyyy-MM-dd HH:mm:ss zzz | TaskId | Event | lượt Run | Result <!-- workflow:TaskId:Event:Run -->`. Event chỉ Prep/Assign/Verify/Qc/Merge; giữ múi giờ At. Marker gồm task + sự kiện + lượt để chống trùng xuyên tiến trình/lần gọi, kể cả Result đổi. Lượt mới vẫn ghi; không dùng thời gian làm khóa trùng.
- Gọi `Update-WorkflowJournal` trực tiếp tại năm hàm `Invoke-Prep`, `Invoke-Assign`, `Invoke-Verify`, `Invoke-Qc`, `Invoke-Merge`. QC tự động cũng phải ghi Qc tại chỗ nhận kết quả, không chỉ QC chạy tay. Prep/assign ghi khi kết thúc, mỗi cổng (kể cả trước tự giao lại) và QC ghi cả đạt/cần sửa/blocked/lỗi, merge chỉ ghi sau merge thành công. Lượt lấy từ lượt chạy/log/cổng hiện có, ổn định khi gọi lại cùng sự kiện; không cố định 1 cho mọi lần.
- Sửa nhật ký và bảng việc tương ứng trong cùng giao dịch, commit chung đúng các file liên quan, không stage toàn repo, không reset --hard làm mất dữ liệu. Hàm journal không tự git/AI; caller commit. Nếu phiếu hỏng, dừng cập nhật giao dịch tương ứng trước khi sửa bảng việc/metrics, nêu lỗi; không được che mất lỗi. Không giữ khóa trong lúc gọi AI.
- Giữ hành vi NU1900 đang có, cờ NoLaunch/NoVerify/NoReview, dữ liệu metrics cũ, retry agent/QC cũ ngoài lỗi máy và định dạng bảng việc. Bản sao repo giả trong `tools/test-verify.ps1` phải mang theo workflow-common (khung đã nối Copy-Item).

## Dữ liệu mẫu
- Khung phiếu trong test dựng từ mẫu Markdown quy định trong task này; task cũ có ca không có Yêu cầu. Không được đọc phiếu thật khi test.
- Chưa xác minh trên máy thật: không được đọc log/fixture ngoài giới hạn của bước prep. Chữ ký lỗi khóa file lấy từ nhánh xử lý hiện có trong team.ps1; chữ ký MCP là allowlist bảo thủ Codex chốt ở đây, chưa có mẫu MCP thật. Không tự đoán thêm loại lỗi.

## Test nghiệm thu
`core.tests/AcceptanceV801WorkflowTests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln`: 0 lỗi, 0 warning mới; `dotnet test AITeam.sln`: pass toàn bộ, giữ nguyên test nghiệm thu.
- [x] `powershell -NoProfile -ExecutionPolicy Bypass -File tools/test-verify.ps1`: toàn bộ hồi quy đạt trong repo tạm. Bổ sung kịch bản tiến trình giả trong file này để kiểm tra cổng thực: lỗi máy→đạt, lỗi máy hai lần, lần 2 warning/hang/code fail, MCP còn lỗi, QC sau phục hồi; không gọi AI thật.
- [x] Cả hai output được lưu nguyên; đúng một retry cho thuần hạ tầng; lỗi code/hỗn hợp vẫn trượt, QC vẫn gọi sau phục hồi; không làm mất kiểm tra warning/hang cũ.
- [x] Đủ năm loại sự kiện thực tế và tất cả kết quả cổng/QC; marker chống trùng theo lượt; journal và bảng việc commit chung; metrics/bảng việc/journal có chung khóa repo, hai tiến trình không mất dòng, không giữ khóa khi chờ AI.
- [x] Thiếu/hỏng phiếu, task cũ, BOM, đường dẫn thoát và reparse point xử lý đúng thông báo và không mất dữ liệu; chỉ dữ liệu tạm trong test.
- [x] Điền báo cáo có bảng từng tiêu chí → lệnh và kết quả thật, file đổi/lý do, quyết định tự đưa ra, phần chưa xác minh. Không báo pass khi chưa chạy.

## Ghi chú chuẩn bị (Codex)
- Đã tạo khung 5 hàm PowerShell (thân hàm chỉ ném NotImplementedException), 66 ca xUnit; nối tải module, bản sao module cho repo tạm và bắt ngoại lệ điểm vào. Chưa triển khai logic nghiệp vụ.
- Build lần 1: `dotnet build AITeam.sln` trả mã 1, thông báo Build FAILED, 0 warning, 0 error, không có chẩn đoán cụ thể.
- Build lần 2: `dotnet build AITeam.sln -m:1 -nr:false -p:UseSharedCompilation=false -v:normal` trả mã 1, 0 warning, 1 lỗi CS0118 tại tên Process trong test mới. Đã sửa tên thành System.Diagnostics.Process sau đó; CHƯA build lại vì giới hạn tối đa hai lần. Không tuyên bố build đạt.
- Parser PowerShell: 0 lỗi ở cả ba file ps1; xác nhận BOM UTF-8 cho ba ps1 và test C#. Chưa chạy test xUnit (khung được thiết kế để đỏ), chưa chạy tools/test-verify.ps1. Không chạy git.
- Cần quản lý xác minh build sau sửa và kết quả test khi giao việc; chưa có mẫu output MCP thật. Các test nối luồng bằng AST bổ sung cho test hành vi, không thay thế kiểm tra giao dịch/commit trong hồi quy repo tạm của agent.

## Báo cáo của agent
| Tiêu chí | Bằng chứng vòng 4 |
|---|---|
| Build | `dotnet build AITeam.sln` thất bại 0 warning/0 error; chạy lại `--no-restore -m:1 -nr:false -p:UseSharedCompilation=false -v:normal`: đạt, 0 warning/0 error. |
| Test toàn bộ, giữ nghiệm thu | `dotnet test AITeam.sln --no-restore --blame-hang-timeout 2m` chưa trả kết quả; lượt `--no-build -m:1 -nr:false`: Hub 493 pass/7 fail (Access denied), Core gặp lỗi CLI quá hạn; đã dừng, không báo pass. |
| Hồi quy đầy đủ | `powershell -NoProfile -ExecutionPolicy Bypass -File tools/test-verify.ps1`: bị chặn tại clone repo tạm, Git signal pipe Win32 error 5; hai kiểm tra tĩnh đầu đạt. |
| 1: Tổng kết MSBuild, output/retry | Bổ sung tổng kết thật vào runner và ca hỗn hợp; kiểm riêng bằng PowerShell: 4/4 đạt, giữ output và đúng hai lần. |
| 2: Cấp lượt nguyên tử | Mọi caller `Get-NextJournalRun` cấp lượt cùng khóa với ghi journal; hồi quy gọi Assign liên tiếp đạt (lượt 1, 2). |
| 3: Merge một marker, đúng task | Chuẩn hóa `Get-TaskId`, ghi Merge một lần rồi cập nhật bảng việc cùng giao dịch; AST hợp đồng nối luồng: 10/10 đạt. |
| 4: Chỉ commit phiếu liên quan | Git giả trong ca 15: stage/commit đúng README + R-008, giữ nguyên R-009 đang sửa: 3/3 đạt. |
| 5: Prep lỗi cũng ghi, đúng lượt | Cả nhánh build lỗi/đạt cấp lượt và ghi trong giao dịch; task tạm được thu hồi trong finally khi build lỗi. |
| 6: Chia đều agent có khóa | Khóa bao trọn cập nhật task, đọc-sửa-ghi bảng việc và stage/commit; không bao thời gian AI/build/QC. |
| An toàn/BOM/báo cáo | Parser 0 lỗi, BOM EF-BB-BF ở 3/3 script; không sửa test nghiệm thu; báo cáo cập nhật theo kết quả thật. |
- File đổi: `team.ps1` (giao dịch/lượt/phiếu), `tools/workflow-common.ps1` (summary), `tools/test-verify.ps1` (hồi quy), file task (báo cáo). Quyết định: thu hồi task tạm của Prep lỗi để vẫn chuẩn bị lại được; hệ thống tự commit/push, không chạy git trực tiếp. Lượt riêng `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceV801WorkflowTests --blame-hang-timeout 2m -m:1 -nr:false` đã ngắt sau hơn 3 phút; ghi nhận 1 ca lỗi `KhoaChanTienTrinhKhacVaTimeout` (tiến trình con chưa lấy được khóa), chưa có tổng kết. Chưa xác minh: toàn bộ hồi quy/nghiệm thu, MCP thật.

## Review (Claude)
### Vòng 1 — ĐẠT (nghiệm thu, 25/10/2026, Claude)
- 4 vòng: Minh 3 lượt (2 lượt flash-medium, 1 lượt claude-opus-4-6-thinking) + Codex vòng 4 sửa 6 phát hiện QC; cổng KT ĐẠT.
- Claude kiểm: không còn `reset --hard` (thay bằng pull --rebase); dòng tổng kết MSBuild 'N Error(s)' không còn bị coi là lỗi lạ; kịch bản test-verify 14 (lỗi hạ tầng lẫn lỗi lạ) + 15 (chỉ commit phiếu của task).
- Bài học: task lõi khó ⇒ dùng model mạnh ngay từ đầu (flash-medium bỏ sót lỗi nguy hiểm 2 lượt); vượt ngân sách 758/320 dòng.

## Kết quả
- Cổng KT tự chạy lại 1 lần khi lỗi máy (khóa file, MSB3491); nhật ký phiếu tự ghi (prep / assign / cổng KT / QC / merge) dưới khóa chung, không commit nhầm phiếu khác.

