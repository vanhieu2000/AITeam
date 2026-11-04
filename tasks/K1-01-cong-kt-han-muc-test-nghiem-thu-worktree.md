# K1-01 · Cổng kiểm tra: lỗi mạng ≠ hết hạn mức, nhận bản sửa test của quản lý, tìm đúng worktree

- **Agent:** C
- **Nhánh:** `agent-c/K1-01-cong-kt-han-muc-test-nghiem-thu-worktree`
- **Ưu tiên:** cao
- **Ngân sách:** ~150 dòng
- **Yêu cầu:** R-014 (3 lỗi [CK] ghi trong nhật ký)
- **Thời gian tối đa:** 30 phút
- **Độ khó:** M
- **Review AI:** không (Claude review)

## Tóm tắt
- Không có chức năng mới cho người dùng. Sửa 3 lỗi của cổng kiểm tra tự động trong `team.ps1` đã gây mất thời gian ở R-014.
- Lỗi mạng (mất DNS) không còn bị báo nhầm "HẾT HẠN MỨC" (V10-02: agent viết chữ "429" trong báo cáo ⇒ cổng tưởng hết lượt).
- Khi quản lý (Claude) sửa test nghiệm thu giữa chừng, cổng nhận bản sửa đó làm bản gốc, không bắt agent khôi phục test cũ (W2-02).
- Cổng tìm đúng thư mục làm việc của nhánh dù thư mục đặt tên khác (V10-03), và không tự đổi Agent của task đã chuẩn bị nhánh sẵn.

## Đọc (CHỈ những file này)
- `core.tests/AcceptanceK101GateFixesTests.cs` (test nghiệm thu, đọc TRƯỚC)
- `team.ps1`: `Test-QuotaExhausted` (~dòng 1220), đoạn gọi nó trong `Invoke-Assign` (~dòng 885–905), `Invoke-Assign` đoạn chia đều `Select-FreeAgent` (~dòng 678–708) và đoạn tìm worktree (~dòng 734–748), `Get-TaskContext` (~dòng 2103), `Invoke-Verify` mục "3. Test nghiệm thu" (~dòng 2288–2326), `Set-ManagerIdentity` (để biết email quản lý).

## Bối cảnh
- **Lỗi 1 (V10-02).** `Test-QuotaExhausted` dò từ khóa trên TOÀN BỘ log agy, gồm cả `response` (báo cáo agent). Log thật V10-02: `response` có "429 → QuotaExhausted" (agent mô tả code nó viết), `error` là `dial tcp: lookup …: no such host`. Kết quả: báo nhầm HẾT HẠN MỨC, và với `-Dev` trống thì `assign` còn tự chuyển sang Codex.
- **Lỗi 2 (W2-02).** Mục 3 của `Invoke-Verify` lấy bản gốc test nghiệm thu = `base` (nếu file có trên main) hoặc commit ĐẦU thêm file trên nhánh. Claude sửa test sau đó (commit thứ 3) ⇒ cổng báo "Agent đã SỬA test nghiệm thu" ⇒ agent khôi phục test sai cũ.
- **Lỗi 3 (V10-03).** `Invoke-Assign` đã dò `git worktree list` theo tên nhánh, nhưng `Get-TaskContext` (dùng cho `verify`) chỉ đoán `<Work>\<agent>-<task>` ⇒ worktree ở `rv\V10-03` thì cổng báo "Chưa có thư mục làm việc". Ngoài ra đoạn chia đều chỉ kiểm thư mục mặc định, nên với nhánh Claude đã đẩy sẵn (test nghiệm thu) nó vẫn đổi Agent trên main ⇒ file task trên main và trên nhánh lệch nhau ⇒ xung đột.

## Mục tiêu
1. **Hạn mức / lỗi mạng**
   - Hàm nội bộ mới `Get-AgentErrorText([string]$logText, [string]$errText)`: nếu `$logText` parse được JSON (`ConvertFrom-Json`, bọc try/catch) thì trả `"<error>`n$errText"` (chỉ trường `error`, KHÔNG lấy `response`); không phải JSON ⇒ trả `"$logText`n$errText"` như cũ.
   - `Test-QuotaExhausted`: giữ luật "SUCCESS ⇒ false" và regex cũ (`\b429\b` đứng riêng), nhưng dò trên `Get-AgentErrorText`.
   - Hàm mới `Test-NetworkError([string]$logText, [string]$errText)`: true khi văn bản lỗi (cùng nguồn trên) khớp `(?i)no such host|dial tcp|connection (refused|reset)|could not be made|deadline exceeded|Client\.Timeout|network is unreachable|TLS handshake timeout`; SUCCESS ⇒ false.
   - Trong `Invoke-Assign`, ngay sau khối báo hết hạn mức: nếu `Test-NetworkError` ⇒ `Say "LỖI MẠNG khi agent chạy (mất kết nối tới máy chủ AI) — kiểm tra mạng rồi giao lại với -Resume; KHÔNG phải hết hạn mức." Yellow`. Không đổi luồng khác.
2. **Bản gốc test nghiệm thu**
   - Hàm mới `Get-AcceptRef -Work <dir> -Base <sha> -Path <file tương đối> -ManagerEmail <email>` trả SHA bản gốc, hoặc `$null`:
     - Ưu tiên: commit MỚI NHẤT trong `Base..HEAD` có chạm `Path`, tác giả có email = `ManagerEmail` (so không phân biệt hoa thường) VÀ dòng tiêu đề chứa `(Claude)` hoặc bắt đầu bằng `Test nghiệm thu`.
     - Không có ⇒ như cũ: `Base` nếu file có ở `Base`; không thì commit đầu tiên thêm file (`--diff-filter=A --reverse`); không có ⇒ `$null`.
     - Đọc tác giả + tiêu đề bằng `git log --format=%H%x09%ae%x09%s`, tách theo tab.
   - `Invoke-Verify` mục 3 thay đoạn tự tìm `$ref` bằng `Get-AcceptRef`, với `ManagerEmail = "$($cfg.github.owner)@users.noreply.github.com"` (giống `Set-ManagerIdentity`). Phần còn lại (harness được phép, thông báo lỗi) giữ nguyên.
3. **Worktree + chia đều**
   - Hàm mới `Resolve-TaskWork($r, [string]$branch, [string]$defaultWork)`: đọc `git -C $r.Path worktree list --porcelain`; có worktree đang giữ `refs/heads/<branch>` ⇒ trả đường dẫn đó (đổi `/` thành `\`); không ⇒ `$defaultWork`.
   - Hàm mới `Test-BranchPrepared($r, [string]$branch)`: true nếu có worktree giữ nhánh, hoặc `git -C $r.Path ls-remote --heads origin <branch>` có kết quả.
   - `Get-TaskContext`: `Work = Resolve-TaskWork $r $branch (Join-Path $r.Work ("$agentId-$Task".ToLower()))`.
   - `Invoke-Assign`: thay đoạn dò worktree inline (`$existing` / `$m`) bằng `Resolve-TaskWork` (giữ dòng `Say "Nhánh … đã có thư mục làm việc → dùng lại …"` khi khác mặc định). Điều kiện chia đều thêm `-and -not (Test-BranchPrepared $r $branch)`.

## Phạm vi (được sửa những gì)
- `team.ps1`
- `tasks/K1-01-cong-kt-han-muc-test-nghiem-thu-worktree.md`

## Không làm
- KHÔNG sửa `core.tests/AcceptanceK101GateFixesTests.cs`.
- Không sửa `hub/**`, `core/**` (kể cả `ErrorClassifier.cs`, `RunDiagnostics.cs`).
- Không đổi cách tính phạm vi / ngân sách (`$claudeCommit`, `$seed`) — chỉ mục 3 test nghiệm thu.
- Không đổi `Select-FreeAgent`, `Set-ManagerIdentity`.
- Không thêm tham số dòng lệnh mới cho `team.ps1`.

## Ràng buộc (bảo mật / dữ liệu / tương thích)
- Chỉ commit có ĐÚNG email quản lý **và** đánh dấu `(Claude)` / `Test nghiệm thu` mới được làm bản gốc. Agent đổi tên commit thành "(Claude)" mà email là của agent ⇒ không được nhận (test có ca này). Commit tự động `"<task>: codex viết code"` (team.ps1 commit hộ bằng danh tính repo) ⇒ không được nhận.
- Tên nhánh / đường dẫn đưa vào regex phải qua `[regex]::Escape`.
- Log agy có thể rất lớn hoặc hỏng: `ConvertFrom-Json` bọc try/catch, lỗi ⇒ quay về dò toàn văn.
- `team.ps1` giữ UTF-8 **có BOM**; PowerShell 5.1.
- [x] Gọi tiến trình ngoài (git): dùng `git -C`, lỗi git không làm vỡ cổng (trả mặc định / `$null`).
- [x] Hàm dùng (`Get-Config`, `Resolve-Project`, `Say`) đã có trên main.
- [x] Không có file khung; commit đầu của nhánh chỉ có test nghiệm thu.

## Dữ liệu mẫu thật
- Log agy V10-02 thật (`AITeam-work\AITeam\logs\V10-02-1.json`, 04/11), rút gọn vào hằng `V1002Log` trong test nghiệm thu: `"status":"ERROR"`, `response` có "429 … QuotaExhausted", `error` = `… dial tcp: lookup daily-cloudcode-pa.googleapis.com: no such host`.
- Danh tính quản lý thật: `HieuDV <vanhieu2000@users.noreply.github.com>` (git log main).

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `core.tests/AcceptanceK101GateFixesTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln -warnaserror -nodeReuse:false` 0 warning
- [ ] `dotnet test AITeam.sln --no-build` xanh hết, gồm 5/5 test K101 (không sửa)
- [ ] Không còn đoạn dò worktree inline trùng lặp trong `Invoke-Assign`
- [ ] `team.ps1` còn BOM
- [ ] Commit: `K1-01: cổng KT phân biệt lỗi mạng, nhận bản sửa test của quản lý, tìm đúng worktree`

## Báo cáo của agent

| Tiêu chí | Bằng chứng thật |
| --- | --- |
| Build 0 warning | `dotnet build AITeam.sln -warnaserror -nodeReuse:false -m:1 --no-restore`: thành công, 0 warning, 0 error; lần đầu không `-m:1` thất bại không nêu lỗi. |
| Toàn bộ test xanh | `dotnet test AITeam.sln --no-build --blame-hang-timeout 2m -m:1`: chưa đạt; Hub 914 pass, 10 fail, 0 skip (lỗi quyền ghi/ReplaceFile); Core chưa có tổng kết, đã ngắt Ctrl+C sau hơn 3 phút, gặp lỗi Git Win32 error 5. |
| K101 5/5, giữ nguyên test | Lệnh test riêng `core.tests/AITeam.Core.Tests.csproj --no-build --filter FullyQualifiedName~AcceptanceK101GateFixesTests --blame-hang-timeout 2m`: 4 pass, 1 fail, 0 skip; ca worktree lỗi dựng repo tạm tại `git push`, trước khi gọi code cần kiểm. Không sửa test. |
| Bỏ dò worktree trùng | Đã thay `$existing`/`$m` trong `Invoke-Assign` bằng `Resolve-TaskWork`; `Get-TaskContext` dùng chung; chặn chia lại agent khi nhánh đã chuẩn bị. |
| BOM | Kiểm byte `team.ps1`: `EF-BB-BF`; PowerShell Parser: không lỗi cú pháp. |
| Commit theo yêu cầu | Không chạy git theo chỉ dẫn Codex dự phòng; hệ thống tự commit/push, chưa có commit được kiểm. |
- File đổi: `team.ps1` (phân loại lỗi, bản gốc nghiệm thu, worktree/chia đều) và file task này (báo cáo).
- Quyết định: dùng `-m:1` sau build mặc định thất bại; không sửa lỗi ngoài phạm vi hoặc né test bị sandbox chặn.
- Chưa xác minh: toàn bộ test xanh và ca worktree K101 trong môi trường cho phép Git tạo signal pipe.
- Xung đột harness ngoài phạm vi: `AcceptanceS104SkillPromptsTests.cs:149` (các ca assign dòng 58) báo không nhận ra hàm mới `Resolve-TaskWork`; cần Claude cập nhật phần nạp hàm của harness theo hợp đồng K101, không sửa test hoặc thêm mã nhận diện test để né lỗi.

## Review (Claude)

## Kết quả
