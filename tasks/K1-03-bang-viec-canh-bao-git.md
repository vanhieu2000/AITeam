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
> Vòng 2 (Claude sửa 04/11 14:00): thiết kế vòng 1 SAI — đã thử thật trên PowerShell 5.1, chỉ thêm `2>$null` dưới Stop vẫn ném lỗi khi git in cảnh báo.
1. Trong `Set-BoardStatus`, ngay trước `git -C … add -- $commitFiles`: lưu `ErrorActionPreference` cũ vào biến riêng rồi đặt `'Continue'`; ngay sau `git -C … commit …`: trả lại giá trị cũ. Giữ `2>$null` trên cả hai lệnh.
2. Giữ nguyên các dòng kiểm `LASTEXITCODE -ne 0` ⇒ `throw 'Không cập nhật được bảng việc.'` (đặt trong vùng Continue vẫn đúng: `throw` luôn dừng).
3. Không bọc thêm try/catch; không đổi các lệnh git khác.

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

| Tiêu chí | Bằng chứng / kết quả thật vòng 2 |
|---|---|
| Build 0 warning — đạt | `dotnet build AITeam.sln -warnaserror -nodeReuse:false -m:1`: thành công, 0 warning, 0 error. |
| K103 bất biến — đạt | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --filter "FullyQualifiedName~AcceptanceK103BoardGitWarningTests" --blame-hang-timeout 2m -m:1 -nodeReuse:false`: 2 pass, 0 fail, 0 skip; không sửa test. |
| Toàn solution — chưa đạt | `dotnet test AITeam.sln --no-build --blame-hang-timeout 2m`: không in kết quả, đã gửi Ctrl+C; chưa xác minh tiến trình dừng. Chạy lại thêm `-m:1 -nodeReuse:false`: Hub 914 pass, 10 fail, 0 skip; Core chưa tổng kết, có lỗi; đã dừng bằng Ctrl+C, mã thoát 1. |
| BOM — đạt | `[BitConverter]::ToString([IO.File]::ReadAllBytes($path)[0..2])`: `EF-BB-BF`. |
| Commit / push — hệ thống thực hiện | Không chạy lệnh Git theo chỉ dẫn Codex; chưa có commit được kiểm. |
- File đổi: `team.ps1` thêm 3 dòng lưu ErrorActionPreference vào biến riêng, đặt Continue trước add, khôi phục ngay sau commit; giữ stderr và LASTEXITCODE. File task cập nhật báo cáo.
- Quyết định: chạy lại test một node vì lệnh mặc định không in kết quả; không sửa lỗi ngoài phạm vi.
- Chưa xác minh: toàn bộ test xanh; lỗi ngoài K103 gồm Access denied tại ReplaceFile và Git sh không tạo được signal pipe (Win32 error 5); CIM tìm PID cũng bị Access denied. Không chạy app/chụp ảnh.
## Review (Claude)
### Vòng 1 — CẦN SỬA (04/11 13:58)
- Codex làm đúng phiếu (thêm `2>$null`), nhưng phiếu SAI: Claude thử thật trên PowerShell 5.1 — dưới Stop, `2>$null` vẫn ném khi git in cảnh báo (còn tệ hơn bản cũ).
- [TK] (chất lượng) thiết kế đoán hành vi PowerShell 5.1 mà không thử ⇒ sửa test nghiệm thu + mục tiêu, trả vòng 2.
### Vòng 2 — ĐẠT (04/11 14:15)
- Codex 364 s, 4 dòng / ngân sách 10. Lưu / đặt Continue / trả lại ErrorActionPreference quanh git add + commit; giữ kiểm LASTEXITCODE. Build 0 warning, 1949 test xanh.
## Kết quả
- Cảnh báo của git (CRLF) không còn làm cổng kiểm tra báo "Không ghi được nhật ký phiếu" và đánh KHÔNG ĐẠT oan.
- Kiểm chứng: thử thật PowerShell 5.1 cả gọi thẳng lẫn kèm 2>&1; 2/2 test K103.
