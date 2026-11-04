# K1-05 · Cổng KT hết báo nhầm: commit "(Claude)" sau khung, chữ Assert.True(true) trong dữ liệu

- **Agent:** C
- **Nhánh:** `agent-c/K1-05-cong-kt-bao-nham`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~60 dòng
- **Yêu cầu:** R-015
- **Nguồn:** hàng chờ R-015 mục 1
- **Thời gian tối đa:** 20 phút
- **Độ khó:** S
- **Review AI:** không (Claude review)

## Tóm tắt
- Không có chức năng mới cho người dùng.
- Cổng kiểm tra đang báo nhầm 2 kiểu, làm Claude phải kiểm tay:
  - F1-01: commit "(Claude)" thêm vào nhánh SAU commit đầu (đăng ký DI sẵn) bị tính là agent "sửa NGOÀI PHẠM VI" ⇒ cổng KHÔNG ĐẠT dù đúng.
  - F1-05: dữ liệu test có chữ `Assert.True(true)` (trích AGENTS.md) bị cảnh báo "test có thể luôn pass".
- Sau sửa: mọi commit "(Claude)" của quản lý được miễn như commit đầu; chỉ báo "luôn pass" khi đó là câu lệnh thật.

## Đọc (CHỈ những file này)
- `core.tests/AcceptanceK105GateFalsePositiveTests.cs` (test nghiệm thu, đọc TRƯỚC — dòng mẫu thật từ F1-05)
- `team.ps1`: hàm Test-AlwaysPassLine, Get-ClaudeOwnedFiles (khung, ngay trên Invoke-Verify); trong Invoke-Verify: mục "# 4. Phạm vi" và "# 5. Test có nhánh return sớm"; hàm Get-AcceptRef (mẫu nhận commit của quản lý)

## Mục tiêu
1. Test-AlwaysPassLine: true khi dòng khớp `^\s*if\s*\(.+\)\s*\{?\s*return\s*;` hoặc `^\s*Assert\.True\(\s*true\s*(,[^)]*)?\)\s*;`; còn lại false (chuỗi / chú thích / dữ liệu nhắc tới thì không báo).
2. Get-ClaudeOwnedFiles: với mỗi file trong Files, lấy commit cuối KHÔNG phải merge chạm tới file trong Base..HEAD (`git log -1 --no-merges --format=%H%x09%ae%x09%s Base..HEAD -- file`); thuộc Claude khi email = ManagerEmail (không phân biệt hoa thường) VÀ tiêu đề có `(Claude)` hoặc bắt đầu `Test nghiệm thu`, VÀ `git diff --quiet <commit> HEAD -- file` không khác. Trả mảng (có thể rỗng); git lỗi ⇒ coi như không thuộc Claude. Chạy git dưới `$ErrorActionPreference = 'Continue'` rồi trả lại (bài học K1-03).
3. Invoke-Verify:
   - mục 4: thay đoạn `$claudeCommit = git … --grep="^Test nghiệm thu" …` + bộ lọc bằng `Get-ClaudeOwnedFiles -Work $work -Base $base -Files $out -ManagerEmail "$($cfg.github.owner)@users.noreply.github.com"` (cùng cách Get-AcceptRef lấy email), loại các file đó khỏi `$out`;
   - mục 5: thay 2 regex trên dòng bằng `Test-AlwaysPassLine $line.Substring(1)`.

## Phạm vi (được sửa những gì)
- `team.ps1`
- `core.tests/K105GateExtraTests.cs` (tạo mới nếu cần)
- `tasks/K1-05-cong-kt-bao-nham.md`

## Không làm
- KHÔNG sửa `core.tests/AcceptanceK105GateFalsePositiveTests.cs` và các test đang có (`tools/test-verify.ps1` phải vẫn qua).
- Không đổi Get-AcceptRef, không đổi cách nhận test nghiệm thu.
- Không sửa hub, core (ngoài test mới).

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- Commit giả tiêu đề "(Claude)" nhưng email agent KHÔNG được miễn (có ca trong test nghiệm thu).
- File agent sửa lại SAU commit Claude vẫn tính cho agent.
- [x] Khung có trong commit đầu "Test nghiệm thu K1-05 + khung (Claude)": 2 hàm, KHÔNG đổi tên / tham số.
- `team.ps1` giữ UTF-8 **có BOM**.

## Dữ liệu mẫu thật
- Dòng F1-05 thật (raw string trích AGENTS.md) và chuỗi commit thật của F1-01 (khung DI "(Claude)" sau commit đầu) dựng lại trong test nghiệm thu.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceK105GateFalsePositiveTests.cs`

## Tiêu chí hoàn thành
- [ ] Build `AITeam.sln` với `-warnaserror` 0 warning
- [ ] `dotnet test AITeam.sln` xanh hết, gồm toàn bộ test nghiệm thu K1-05 (không sửa)
- [ ] `powershell -File tools/test-verify.ps1` qua
- [ ] `team.ps1` giữ BOM UTF-8
- [ ] Commit: `K1-05: cổng KT miễn mọi commit (Claude) của quản lý, chỉ báo test luôn pass khi là câu lệnh thật`

## Báo cáo của agent

## Review (Claude)

## Kết quả
