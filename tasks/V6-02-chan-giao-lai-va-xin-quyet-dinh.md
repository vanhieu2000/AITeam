# V6-02 · Chặn giao lại và xin quyết định

- **Agent:** A
- **Nhánh:** `agent-a/V6-02-chan-giao-lai-va-xin-quyet-dinh`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~330 dòng
- **Yêu cầu:** R-006
- **Thời gian tối đa:** 15 phút
- **Review AI:** có
- **Độ khó:** M
- **Model:** `claude-opus-4-6-thinking`

## Tóm tắt
- Có chức năng mới: chủ dự án nhận yêu cầu quyết định khi task đã tới giới hạn review.
- Dừng giao lại từ vòng thứ ba theo mặc định, tránh agent sửa lặp vô hạn.
- Chủ dự án chọn giao tiếp, Claude tự sửa hoặc bỏ task; hệ thống chỉ xin ý kiến.
- Bảng việc hiện rõ đang chờ quyết định; gọi lại không sinh thêm yêu cầu đang chờ.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-006/brief.md`
- `team.ps1`
- `tools/test-verify.ps1`
- `core/AITeam.Cli/Program.cs` (hợp đồng review-rounds và bắt lỗi)
- `core.tests/Support/TestShell.cs`
- `core.tests/AcceptanceV602ReviewGateTests.cs`
- `core.tests/Fixtures/V602/team.json`
- `core.tests/Fixtures/V602/projects.json`
- `core.tests/Fixtures/V602/V6-02-chan-giao-lai-va-xin-quyet-dinh.md`
- File task này.

## Phạm vi (được sửa)
- `team.ps1`
- `tools/test-verify.ps1`
- `tasks/V6-02-chan-giao-lai-va-xin-quyet-dinh.md` (chỉ báo cáo)

## Không làm
- Không đổi CLI/lõi V6-01, Hub, cấu hình, khóa, giới hạn verify riêng hoặc thêm Force/NoVerify để vượt chốt.
- Không sửa/xóa/Skip `core.tests/AcceptanceV602ReviewGateTests.cs`, `core.tests/Fixtures/V602/`, bộ TestShell hay kịch bản verify cũ. Đây là test/dữ liệu Codex chuẩn bị, KHÔNG đổi.
- Không tự chọn hoặc thực hiện ba lựa chọn thay chủ dự án; không đọc log sản xuất, không chạy agy/codex thật trong test.

## Ràng buộc
- V6-01 đã có: CLI `review-rounds --task-file <f> --logs <d> --task <T>` in một số nguyên không âm, mã 0. Đây là nguồn đếm DUY NHẤT; không dùng regex đếm lại trong PowerShell.
- Giữ chữ ký ba helper đã có khung. PowerShell dùng `throw [NotImplementedException]::new() # TODO(agent)` tương đương khung C# chưa triển khai; chỉ agent điền thân hàm.
- `Get-ReviewRoundLimit($cfg)` dùng cấu hình hợp nhất từ `Get-Config`, nhận số nguyên dương (kể cả chuỗi số nguyên), còn lại mặc định 3; không sửa cơ chế hợp nhất.
- `Get-ReviewRoundCount($r, [string]$taskFile, [string]$taskId)` gọi `Get-AITeamExe`; truyền đối số rời, đường dẫn có khoảng trắng vẫn đúng. CLI thiếu, mã khác 0, stdout âm/thập phân/lẫn chữ => lỗi `Không xác định được số vòng review; dừng giao lại.`. Không đổi lỗi vận hành thành 0; dữ liệu hỏng/cũ đã do CLI V6-01 xử lý.
- `Stop-ReviewReassignment($r, [string]$taskFile, [string]$taskId, $cfg)` trả false khi tổng < ngưỡng; khi tổng >= ngưỡng tạo/tái dùng decision, ghi bảng rồi trả true. CLI lỗi: báo nguyên câu trên và dừng, không gọi agent hay tạo decision dựa trên số giả.
- `Invoke-Assign`: kiểm mẫu `^[A-Za-z0-9][A-Za-z0-9-]{0,63}$` trước `Find-TaskFile`, mã sai báo `LỖI: mã task không hợp lệ.`. Với Resume, kiểm chốt sau tìm task nhưng TRƯỚC trạng thái đang làm, claim, fetch/worktree, prompt, gọi agent. False giữ luồng cũ. Force/NoVerify vẫn bị chặn.
- Trong nhánh review AI Code=1, gọi chốt với `Join-Path $work tasks/<tên file>` và logs vừa ghi, TRƯỚC kiểm ReviewRetried/feedback; true phải return khỏi Invoke-Assign để bảng không bị ghi đè. Chặn cả feedback rỗng và đã retry.
- Title đúng `Cần anh duyệt · <Task> · quá N vòng review`; detail có dự án, mã task, `Tổng/ngưỡng: N/M` và nguyên ba lựa chọn `giao tiếp`, `Claude tự sửa`, `bỏ task`.
- Tái dùng pending decision cùng dự án/mã task, không phụ thuộc N đang tăng; decision hiện có task=null nên đối chiếu thêm tiền tố title có ranh giới ` · `. Không tái dùng decision khác dự án/task hoặc đã quyết.
- Dùng cơ chế `Invoke-ApprovalRequest` hiện có (`Kind=decision`, `Title`, `Prompt`, `Project` ở scope script); lưu/khôi phục biến khi tạm đổi. Bảng đúng `⏸ Chờ anh quyết · quá N vòng review`. Lỗi ghi approval/bảng phải báo rõ, không nuốt.
- UTF-8 BOM cho .ps1/.cs, giữ xuống dòng. Không thêm tiến trình/harness riêng; xUnit dùng TestShell/TempRepo, mọi dữ liệu và remote ở thư mục tạm.

## Dữ liệu mẫu thật
- `core.tests/Fixtures/V602/` chép nguyên byte từ `team.json`, `projects.json` và khung task hiện tại trước khi điền đặc tả; giữ tên/BOM/CRLF. projects là MẢNG ở gốc, không phải đối tượng projects.
- Không đụng phiếu requests trong tính năng này. Không mở log thật theo giới hạn đọc của bước chuẩn bị; JSON verdict trong ca 2+1 dựng theo hợp đồng đang dùng ở `Invoke-AutoReview`, chưa đối chiếu mẫu log thật. Ghi rõ giới hạn này trong báo cáo.

## Test nghiệm thu
- `core.tests/AcceptanceV602ReviewGateTests.cs`
- `tools/test-verify.ps1`

## Tiêu chí hoàn thành
- [ ] Điền đủ ba helper và nối cả Resume + nhánh review AI; 0/2 đi tiếp, 3/4 dừng, ngưỡng hợp lệ/tự về 3 đúng.
- [ ] Cộng 2 vòng Claude + 1 log AI mới, không phụ thuộc feedback/retry; lỗi CLI và mã task không hợp lệ không đi vào luồng agent.
- [ ] Kịch bản 20 duy nhất mới trong test-verify gọi test xUnit repo/remote/root tạm: Resume task 3 vòng không gọi agent, pending đúng nội dung, bảng thật đúng trạng thái, lần hai không nhân đôi.
- [ ] Giữ các kịch bản verify cũ; không sửa test bất biến để qua cổng.
- [ ] `dotnet build AITeam.sln`: 0 lỗi, 0 warning; `dotnet test AITeam.sln --no-build --blame-hang-timeout 2m`: pass toàn bộ.
- [ ] `powershell -NoProfile -File tools/test-verify.ps1`: pass; không yêu cầu chạy app hay chụp ảnh.

## Báo cáo của agent
<!-- Agent điền tối đa 15 dòng: bảng từng tiêu chí → lệnh + số pass/fail/skip/warning thật, file đổi/lý do, quyết định tự đưa ra, điều chưa xác minh. -->

## Điều chưa chắc khi chuẩn bị
- `agy models` đã chạy nhưng thất bại do chưa đăng nhập/mạng bị chặn; chưa xác minh model có trong danh sách tài khoản. Model ghi theo kế hoạch được duyệt, không tự thay mã.
- Chưa có mẫu log thật trong danh sách được phép đọc; dùng hợp đồng verdict của code, không nhận đã xác minh log thật.
- Chuẩn bị chỉ build và chạy test đỏ không dùng git; không chạy kịch bản repo/remote hoặc toàn bộ script verify vì lệnh hiện tại cấm chạy git. Agent phải chạy đủ khi triển khai.

## Review (Claude)
<!-- Claude điền sau khi agent hoàn thành. -->

## Kết quả
<!-- Claude điền khi nghiệm thu. -->
