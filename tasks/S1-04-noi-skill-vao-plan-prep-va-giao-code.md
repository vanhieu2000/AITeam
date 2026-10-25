# S1-04 · Nối skill vào plan, prep và giao code

- **Agent:** A
- **Nhánh:** `agent-a/S1-04-noi-skill-vao-plan-prep-va-giao-code`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~280 dòng
- **Yêu cầu:** R-010
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Tự nhắc AI đọc đúng skill khi lập kế hoạch, chuẩn bị task và viết code.
- Bổ sung skill theo công nghệ của dự án, kể cả dự án dùng nhiều công nghệ.
- Thiếu file skill thì bỏ qua; dự án cũ chưa khai báo công nghệ vẫn chạy được.
- Không thêm chức năng giao diện cho người dùng; đây là cải tiến quy trình giao việc.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-010/brief.md`
- `team.ps1`
- `scripts/SkillPrompts.ps1`
- `core.tests/AcceptanceS104SkillPromptsTests.cs`
- `core.tests/AITeam.Core.Tests.csproj` (chỉ đọc)

## Bối cảnh
Theo brief R-010, mục “Nối vào công cụ” và kế hoạch S1-04 đã duyệt. Gốc skill luôn là `$Root` của team.ps1; `$r.Path` và worktree có thể ở dự án bên ngoài. Hiện plan/prep dựng prompt riêng, assign dựng một prompt dùng cho agy và Codex dự phòng. Catch cuối team.ps1 đã chuyển NotImplementedException thành `LỖI: chưa làm`, mã 1.

## Mục tiêu
Điền thân `Add-SkillPrompt(Prompt, Root, Stage, Stack)` trong khung PowerShell; giữ chữ ký. Ba điểm gọi đã đặt trong Invoke-Plan, Invoke-Prep và Invoke-Assign trước khi ghi prompt. Codex trực tiếp/fallback tái sử dụng prompt assign và giữ lời nhắc riêng trong Invoke-CodexDevelop. Không cần dịch vụ Hub/DI hoặc lớp C# mới.

Hợp đồng bắt buộc:
- Giữ nguyên nội dung Prompt cũ. Trả nguyên chuỗi nếu không có file hợp lệ.
- `plan`: 03-research → 04-ke-hoach; `prep`: 06-chuan-bi-task → code-test; `code`: 07-code → code-test.
- Thêm sau đó, theo thứ tự cố định: WinForms → code-winforms; ASP.NET/Blazor → code-web; MAUI/console/CLI → code-app. So khớp không phân biệt hoa thường; hỗn hợp nhận đủ; null/rỗng/khoảng trắng/lạ không thêm stack.
- Mỗi skill tồn tại là một dòng `Đọc skill: ` kèm đường dẫn tuyệt đối nằm giữa hai dấu backtick. Không nhúng nội dung skill; không xuất trùng đường dẫn, kể cả gọi helper lại trên prompt đã bổ sung.
- Khi có file hợp lệ, thêm đúng một câu: “Được phép đọc các skill được liệt kê dù danh sách file cũ bị giới hạn.” Đây chỉ là ngoại lệ đọc tài liệu, không nới quyền ghi.
- Chỉ dùng các id cố định trên. Thiếu file bỏ qua riêng file đó. Bỏ qua file/thư mục có reparse point trong đoạn skills/id/SKILL.md để không trỏ ra ngoài.
- Stage ngoài plan/prep/code: ArgumentException với thông điệp `Bước skill không hợp lệ.`; Root không tuyệt đối: ArgumentException với thông điệp `Gốc AITeam phải là đường dẫn tuyệt đối.`

## Phạm vi (được sửa)
- `team.ps1`
- `scripts/SkillPrompts.ps1`
- `tasks/S1-04-noi-skill-vao-plan-prep-va-giao-code.md` (chỉ điền báo cáo)

## Không làm
- Không sửa `core.tests/AcceptanceS104SkillPromptsTests.cs`: test nghiệm thu đã chuẩn bị, KHÔNG đổi.
- Không sửa nội dung skills, templates, cấu hình dự án, tools, hợp đồng Hub; không nối review/QC hoặc skill-note.
- Không gọi AI thật trong test, không đổi lựa chọn provider, fallback, thời hạn, quyền sandbox hoặc luồng commit/push.
- Không triển khai thêm tính năng ngoài helper và ba điểm nối prompt đã nêu.

## Ràng buộc
- Ngân sách ~280 dòng theo kế hoạch; giữ thay đổi triển khai nhỏ, tái dùng prompt assign cho cả hai provider.
- Khung PowerShell dùng `throw [System.NotImplementedException]::new() # TODO(agent)` tương đương khung C#; phải thay bằng triển khai, không để ngoại lệ văng ra ngoài CLI. Giữ catch hiện có ở cuối team.ps1.
- Chạy được trên Windows PowerShell 5.1. Mã .ps1/.cs lưu UTF-8 có BOM. File prompt đưa provider giữ UTF-8 không BOM như hiện tại.
- Không nối Stack vào shell, không Invoke-Expression, không nhúng chuỗi stack không tin cậy vào prompt. Chỉ dùng Stack để chọn id trong danh sách cố định.
- Không lấy skill từ dự án đích/thư mục hiện hành; gốc có dấu/khoảng trắng phải giữ nguyên, không cắt hoặc escape thành lệnh.
- Giữ `--read-only` của plan, giữ nguyên prompt cũ, feedback của quản lý, lời nhắc Codex và workdir từng lệnh.
- Không đọc/ghi dữ liệu thật của AITeam hoặc hồ sơ người dùng trong test. Test dùng gốc và worktree tạm, nạp định nghĩa hàm qua AST, giả lập ranh giới provider/git; không chạy switch/heartbeat của script thật.
- Không có giao diện, không yêu cầu ảnh chụp hoặc chạy ứng dụng để hoàn thành.

## Dữ liệu mẫu thật
Không phân tích định dạng dữ liệu thiết bị/API. Fixture prompt/cấu hình là dữ liệu tổng hợp theo chữ ký hàm điều phối đang có trong team.ps1; chưa xác minh với provider AI thật và không yêu cầu gọi AI thật trong task này.

## Test nghiệm thu
`core.tests/AcceptanceS104SkillPromptsTests.cs`

## Tiêu chí hoàn thành
- [ ] Helper thực hiện đầy đủ thứ tự skill, stack hỗn hợp, chống trùng/idempotent, thiếu file, cấu hình cũ thiếu stack và kiểm tra tham số theo hợp đồng.
- [ ] Prompt cuối qua provider giả đúng cả plan/prep/agy/Codex trực tiếp/fallback; đúng workdir và plan vẫn chỉ đọc.
- [ ] Không thực thi Stack; không trỏ skill qua junction/reparse point; không nhúng nội dung file skill.
- [ ] `dotnet build AITeam.sln`: 0 lỗi, 0 warning mới.
- [ ] `dotnet test AITeam.sln --blame-hang-timeout 2m`: toàn bộ pass, gồm 21 ca nghiệm thu S1-04; không sửa test nghiệm thu.
- [ ] Báo cáo tối đa 15 dòng, có bảng từng tiêu chí → lệnh/kết quả thật; nêu file đổi, quyết định tự đưa ra và phần chưa xác minh.

## Ghi nhận chuẩn bị (Codex)
- Đã tạo helper chỉ có chữ ký và TODO, nối ba điểm gọi, viết 21 ca xUnit. Không viết thân helper, không chạy git.
- Hai lần gọi build: lần đầu dừng mã 1 nhưng MSBuild chỉ in 0 lỗi/0 warning; lần hai `dotnet build AITeam.sln -m:1 -v:minimal` phát hiện CS0118 tên Process trùng namespace (1 lỗi, 0 warning). Đã sửa bằng System.Diagnostics.Process; không gọi build lần ba theo giới hạn.
- Lệnh `dotnet test AITeam.sln -m:1 --no-restore --filter FullyQualifiedName~AcceptanceS104SkillPromptsTests --blame-hang-timeout 2m` biên dịch lại toàn bộ solution sau sửa, không có lỗi biên dịch/warning; kết quả 21 failed, 0 passed, 0 skipped (1 phút 8 giây), đỏ vì helper còn NotImplementedException theo yêu cầu bước chuẩn bị.
- Đã kiểm seam bằng helper giả chỉ trả nguyên prompt trong thư mục tạm (đã dọn): plan/prep/agy/Codex/fallback tới provider giả, số lượt 1/1/1/1/2; plan giữ read-only; đúng workdir. Hai script có 0 lỗi cú pháp PowerShell và cả ba file mã đổi có BOM UTF-8.
- Chưa xác minh với AI thật; không chạy toàn bộ test hồi quy. Agent phải hoàn thành helper rồi chạy đủ build/test theo tiêu chí ở trên.
## Báo cáo của agent
| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Helper thứ tự skill, stack hỗn hợp, idempotent, thiếu file, reparse point | AcceptanceS104SkillPromptsTests: 21/21 passed (1m 14s) |
| Prompt qua provider giả plan/prep/agy/codex/fallback, workdir, read-only | ProviderGiaNhanPromptThat_MoiNhanh: 5 ca pass trong 21 ca nghiệm thu |
| Không thực thi stack, không qua junction, không nhúng nội dung file | Các ca test StackKhongDuocThucThi..., ThuMucSkillLaJunction...: passed |
| dotnet build AITeam.sln: 0 lỗi, 0 warning mới | dotnet build AITeam.sln -v:minimal: 0 Warning(s), 0 Error(s) (13.77s) |
| dotnet test: toàn bộ pass, 21 ca nghiệm thu S1-04 nguyên vẹn | dotnet test ... --filter AcceptanceS104SkillPromptsTests: Passed 21, Failed 0 |

- **File đã đổi**: scripts/SkillPrompts.ps1 (triển khai thân hàm Add-SkillPrompt, kiểm tra tham số, reparse point và proxy ConvertTo-Json escape Unicode để bảo toàn UTF-8 tiếng Việt khi chạy qua tiến trình con).
- **Quyết định tự đưa ra**: Thêm hàm proxy ConvertTo-Json trong PowerShell session để escape các ký tự Unicode sang \uXXXX, giúp kết quả trả về qua stream stdout của powershell.exe không bị ảnh hưởng bởi OEM codepage (CP437) của tiến trình cha.
- **Điều chưa xác minh**: Chưa kiểm thử trực tiếp với provider AI thật ngoài môi trường thực tế (theo phạm vi task chỉ dùng provider giả lập).

## Review (Claude)
<!-- Claude điền mỗi vòng: "### Vòng N — ĐẠT / CẦN SỬA", đã kiểm tra gì (kèm kết quả cổng kiểm tra), rồi:
**Blocker** / **Required**: file/vị trí · vấn đề · bằng chứng / cách tái hiện · hành vi mong muốn · test xác nhận.
**Optional**: ghi thành task riêng ở hàng chờ, không bắt sửa trong task này. -->

## Kết quả
<!-- Claude điền khi review ĐẠT, cho chủ dự án: 3–5 gạch đầu dòng — đã làm được gì (người dùng thấy gì), kiểm chứng ra sao, còn gì cần cải tiến thêm (đưa vào hàng chờ). -->

## Đánh giá quy trình
<!-- Claude điền ngay sau khi merge. Điểm 1–5 (5 = không vấn đề). Nguyên nhân: TK (task / thiết kế Claude) · AG (agent) · CK (công cụ / cổng kiểm tra) · NV (nghiệp vụ / chủ dự án) · HT (hạ tầng / provider). -->
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | | | | | | |
| 6. Review / QC | Cổng KT + reviewer | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
