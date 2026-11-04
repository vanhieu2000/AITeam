# K1-03 · Cổng kiểm tra: cảnh báo của git không còn làm hỏng ghi bảng việc / nhật ký phiếu

- **Agent:** C
- **Nhánh:** `agent-c/K1-03-bang-viec-canh-bao-git`
- **Ưu tiên:** cao
- **Ngân sách:** ~10 dòng
- **Yêu cầu:** R-014 (lỗi [CK] phát hiện khi giao K1-01, K1-02)
- **Thời gian tối đa:** 15 phút
- **Độ khó:** S
- **Review AI:** không (Claude review)

## Tóm tắt
- Không có chức năng mới cho người dùng.
- Sau mỗi lần agent chạy xong, `team.ps1` báo "LỖI: Không ghi được nhật ký phiếu" và cổng kiểm tra bị đánh KHÔNG ĐẠT dù task tốt (K1-01, K1-02 ngày 04/11).
- Nguyên nhân: trong `Set-BoardStatus`, lệnh `git add` không chuyển hướng stderr; git in cảnh báo "LF will be replaced by CRLF" ⇒ PowerShell 5.1 (ErrorActionPreference = Stop) coi là lỗi.
- Sau sửa: cảnh báo của git bị bỏ qua, lỗi thật vẫn bắt bằng `$LASTEXITCODE`.

## Đọc (CHỈ những file này)
- `core.tests/AcceptanceK103BoardGitWarningTests.cs` (test nghiệm thu, đọc TRƯỚC)
- `team.ps1`: hàm `Set-BoardStatus` (~dòng 2617–2700).

## Mục tiêu
1. Trong `Set-BoardStatus`, mọi lệnh `git -C … add` / `git -C … commit` có `2>$null` (giống các lệnh git khác trong cùng hàm).
2. Giữ nguyên các dòng kiểm `$LASTEXITCODE -ne 0` ⇒ `throw 'Không cập nhật được bảng việc.'`.

## Phạm vi (được sửa những gì)
- `team.ps1`
- `tasks/K1-03-bang-viec-canh-bao-git.md`

## Không làm
- KHÔNG sửa `core.tests/AcceptanceK103BoardGitWarningTests.cs`.
- Không sửa hàm khác ngoài `Set-BoardStatus`; không đổi `.gitattributes`, `core.autocrlf`.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- `team.ps1` giữ UTF-8 **có BOM**; PowerShell 5.1.
- [x] Không có file khung; commit đầu của nhánh chỉ có test nghiệm thu.

## Dữ liệu mẫu thật
- Log thật 04/11 13:11 và 13:20: `git : warning: in the working copy of 'tasks/README.md', LF will be replaced by CRLF … At D:\HIEUDV\AITeam\team.ps1:2617 char:13 + git -C $r.Path add -- $commitFiles` ⇒ "LỖI: Không ghi được nhật ký phiếu."

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceK103BoardGitWarningTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln -warnaserror -nodeReuse:false` 0 warning
- [ ] `dotnet test AITeam.sln --no-build` xanh hết, gồm 2/2 test K103 (không sửa)
- [ ] `team.ps1` còn BOM
- [ ] Commit: `K1-03: git add/commit trong Set-BoardStatus bỏ qua cảnh báo stderr`

## Báo cáo của agent

| Tiêu chí | Bằng chứng / kết quả thật |
|---|---|
| Build 0 warning | `dotnet build AITeam.sln --no-restore -warnaserror -nodeReuse:false -m:1 -v:normal`: thành công, 0 warning, 0 error. |
| Test toàn solution | `dotnet test AITeam.sln --no-build --blame-hang-timeout 2m`: quá 3 phút chưa có tổng kết; chưa xác minh xanh hết. Đã gửi Ctrl+C; CIM bị Access denied khi tìm PID để dừng. |
| Test K103 bất biến | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --filter "FullyQualifiedName~AcceptanceK103BoardGitWarningTests" --blame-hang-timeout 2m`: 2 pass, 0 fail, 0 skip; không sửa test. |
| BOM | `[IO.File]::ReadAllBytes('team.ps1')`: ba byte đầu `EF-BB-BF`. |
| Commit / push | Không chạy git theo chỉ dẫn Codex; hệ thống tự commit/push, chưa xác minh commit. |
- File đổi: `team.ps1` thêm `2>$null` đúng một lệnh add trong Set-BoardStatus; commit đã có chuyển hướng, giữ nguyên hai kiểm tra LASTEXITCODE và throw. File task này bổ sung báo cáo.
- Quyết định: dùng build một node sau khi lệnh build ban đầu trả mã 1 nhưng chỉ in 0 warning/0 error, không có chẩn đoán.
- Chưa xác minh: bộ test toàn solution và phiên test đã dừng hoàn toàn; không chạy app/chụp ảnh; không sửa ngoài phạm vi.

## Review (Claude)

## Kết quả
