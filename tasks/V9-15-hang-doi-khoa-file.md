# V9-15 — Hàng đợi tự chạy và khóa file
- Nhánh: `agent-c/V9-15-hang-doi-khoa-file`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Trạng thái | Dừng triển khai hàng đợi do thiếu hợp đồng nền trên origin/main 759ddaf. |
| Phụ thuộc | R-012 C4 yêu cầu định tuyến C3, hạn mức B2 và tạm dừng B4. Các nhánh V9-14/V9-11/V9-13 mới push, chưa được merge; main chưa có DeveloperSelector/QuotaObservation/RunDiagnostics. |
| Rủi ro cụ thể | Auto chạy song song khi thiếu khóa/quota/pause thống nhất có thể giao lại task đã tạm dừng hoặc tiếp tục dùng provider hết hạn mức. Không viết vòng lặp bỏ qua các điều kiện bắt buộc. |
| Chưa làm | Không thêm auto/locks.json tạm và không tự merge/cherry-pick nhánh khác. Cần tích hợp nền trước để kiểm 2 task cùng file, phụ thuộc đã merge và đổi người khi hết hạn mức. |
| Kiểm tra | Đọc mã main và yêu cầu C4; thay đổi chỉ tài liệu, không chạy lại build/test, không sửa assert cũ. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |

## Báo cáo của agent — đợt 2
| Tiêu chí | Bằng chứng |
|---|---|
| Phụ thuộc | Đã fetch main 9bc9a8e; DeveloperSelector/RunDiagnostics đã có, nhưng QuotaObservation.cs của V9-11 chưa tồn tại trên main. |
| Trạng thái | Chờ đúng mục 3 GIAO-CODEX-DOT-2.md: chỉ làm hàng đợi sau khi V9-11 vào main. V9-11 hiện còn test V911 cũ xung đột đã báo, không tự merge nhánh đó. |
| Kiểm tra | Chỉ cập nhật nhánh và báo cáo, chưa sửa queue/lock; không chạy build/test không liên quan. Chưa chạy Windows. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |

## Báo cáo của agent — OpenCode
| Tiêu chí | Bằng chứng |
|---|---|
| Khóa phạm vi file | `core/AITeam.Core/Queue/ScopeLocks.cs` + `ScopeLockStore` (`.state/locks.json`, khóa liên tiến trình, ghi tmp+move, sao lưu `locks.json.hong-*` nguyên byte, chặn `..`/tuyệt đối/reparse). |
| `Invoke-Assign` không chạy trùng vùng | `team.ps1` hàm lồng `Invoke-ScopeLockFile`, lấy khóa sau `Assert-NotClaimedElsewhere`, nhả trong `finally`; gọi lại trong cùng lượt không khóa lại (`$script:ScopeLockOwn`). |
| Hàng đợi auto | `TaskQueue` (đọc bảng, phụ thuộc chỉ đạt khi ✅ Đã merge) + `team.ps1 auto -Project -Max`, `.state/auto.stop`/`auto.pid`, `stop -All`; CLI `queue-ready`, `lock-acquire`, `lock-release`. |
| Test | `core.tests/V915QueueTests.cs` 17 ca: trùng file/thư mục, đổi người cùng task, nhả khi xong, file hỏng, đường dẫn xấu, bảng/phụ thuộc, auto 0 task, stop -All, assign chờ khóa và nhả khóa khi lỗi. |
| Build/test | `dotnet build AITeam.sln -warnaserror -nodeReuse:false` → 0 warning, 0 error; `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m` → 822/822 Hub + 929/929 Core đạt. |
| Tự quyết | Task không khai `Phạm vi` thì bỏ qua khóa (lint đã chặn luồng thật) để không phá harness S104; xung đột khóa không đổi trạng thái bảng (giữ 🟣 để auto giao lại sau); `auto` chỉ giao lại cùng task sau 60 giây. |
| Chưa xác minh | Chưa chạy `auto`/`stop -All` trên tiến trình thật và chưa đo phút/token; hàng đợi chưa dùng `QuotaObservation` vì V9-11 chưa có trên main (định tuyến V9-14 đã dùng trong `Invoke-Assign`). |
| File đổi | `core/AITeam.Core/Queue/ScopeLocks.cs` (mới), `core/AITeam.Core/Queue/TaskQueue.cs` (mới), `core/AITeam.Cli/Program.cs`, `team.ps1`, `core.tests/V915QueueTests.cs` (mới), file task này. |

## Báo cáo của agent — OpenCode (vòng 2)
| Tiêu chí | Bằng chứng |
|---|---|
| Một nguồn logic | `team.ps1`: xóa hàm lồng `Invoke-ScopeLockFile`, `Get-QueueReadyTasks`, `Test-QueuePending`; thêm hàm cấp cao nhất `Invoke-ScopeLock` (`-Action acquire|release`) gọi `lock-acquire`/`lock-release` của CLI C#. |
| `Invoke-Assign` | Gọi `Invoke-ScopeLock -Action acquire -TaskId -Provider -TaskFile -BoardFile -ProjectPath`; giữ `$script:ScopeLockOwn`, nhả trong `finally`. |
| `Invoke-AutoQueue` | Đọc danh sách bằng `& $exe queue-ready --board … --tasks …` rồi lọc `ready`; không còn logic PowerShell song song. |
| `lock-release` | `Program.cs` truyền `--project` vào `ScopeLockStore` như `lock-acquire`; thêm test CLI roundtrip trong `V915QueueTests.cs`. |
| Harness cũ | `AcceptanceS104SkillPromptsTests.cs` thêm `function Invoke-ScopeLock { $true }` ở phần dựng harness, giữ nguyên assert (V602/V914 không chạy tới nhánh khóa). |
| Test | `V915QueueTests`: thay ca PowerShell cũ bằng CLI `queue-ready`, `lock-acquire`/`lock-release` thật; ca assign dùng CLI thật qua `aiteam.dll`. |
| Build/test | `dotnet build AITeam.sln -warnaserror -nodeReuse:false` → 0 warning, 0 error; `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m` → 835 Hub + 937 Core đạt, 0 hỏng. |

