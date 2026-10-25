# S1-03 · Bốn skill code theo stack

- **Agent:** A
- **Nhánh:** `agent-a/S1-03-bon-skill-code-theo-stack`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~380 dòng
- **Yêu cầu:** R-010
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Không thêm chức năng cho người dùng; chuẩn hóa cách AI viết code theo stack.
- Hoàn thiện bốn skill WinForms, Web, App và Test, mỗi file không quá 150 dòng.
- Hướng dẫn có nguồn và ví dụ tốt/xấu, tránh lặp lỗi R-001 và AGENTS.md.
- Antigravity điền nội dung vào khung tài liệu và làm test nghiệm thu pass.

## Đọc (CHỈ những file này)
- `AGENTS.md`, `plans/R-010/brief.md` (mục Skill code).
- Bốn file SKILL.md trong Phạm vi và `core.tests/AcceptanceS103StackSkillsTests.cs` (KHÔNG sửa test).
- Nguồn gốc, chỉ đọc phần cần tóm lược: `%USERPROFILE%/.claude/skills/winforms-expert/SKILL.md`, `%USERPROFILE%/.claude/skills/create-datadriven-aspnetcore/SKILL.md`, `%USERPROFILE%/.claude/skills/dotnet-webapi/SKILL.md`, `%USERPROFILE%/.claude/skills/author-component/SKILL.md`, `%USERPROFILE%/.claude/skills/scaffold-dotnet-test-project/SKILL.md`.
- MAUI: các mục app-lifecycle, data-binding, dependency-injection, shell/navigation tại `https://learn.microsoft.com/dotnet/maui/fundamentals/`. Nếu dùng skill maui-* có sẵn, ghi chính xác đường dẫn đã đọc.
- Không quét repo hoặc đọc task cũ. Nguồn không có thì ghi chưa xác minh, không bịa nội dung/đường dẫn/dòng đã đọc.

## Bối cảnh
Theo brief R-010, skill dùng chung mọi dự án, ngắn và cập nhật được sau lỗi thực tế. Khung Markdown là toàn bộ khung của task tài liệu này; không có API, thân hàm sản phẩm hay dịch vụ Hub mới.

## Mục tiêu
Mỗi skill có tám mục cấp 2: Mục tiêu; Đầu vào; Đầu ra (định dạng); Các bước; Checklist bắt buộc; Lỗi hay gặp; Ví dụ; Nguồn. Giữ tiêu đề để test và skill-note tìm được mục. Ví dụ tốt/xấu nằm trong khối code, có nhãn Tốt/Xấu ngay trước khối.

## Phạm vi (được sửa)
- `skills/code-winforms/SKILL.md`
- `skills/code-web/SKILL.md`
- `skills/code-app/SKILL.md`
- `skills/code-test/SKILL.md`
- `tasks/S1-03-bon-skill-code-theo-stack.md` (chỉ Báo cáo của agent).

## Không làm
- KHÔNG sửa `core.tests/AcceptanceS103StackSkillsTests.cs`; test nghiệm thu do bước chuẩn bị tạo.
- Không sửa công cụ, cấu hình, hợp đồng Hub, project hay skill khác; không nối skill vào prompt/tool.
- Không viết ứng dụng minh họa, gọi agy/codex, cài workload hoặc đổi cấu hình máy.
- Không triển khai UI. UiSnapshot chỉ nhắc như công cụ của bước nghiệm thu do Claude thực hiện.

## Ràng buộc
- Tổng nội dung khoảng 380 dòng; mỗi skill 1–150 dòng, không còn TODO(agent)/placeholder. Viết tiếng Việt có dấu, tóm lược bằng lời của mình, không sao chép nguyên văn dài.
- Mỗi mục có nội dung thực chất; Checklist dùng `- [ ]`; Lỗi hay gặp ghi nguồn R-001/AGENTS.md hoặc mã lỗi liên quan; Nguồn ghi đường dẫn skill gốc/file:dòng hoặc URL MAUI. Thiếu nguồn phải báo, không tự nhận đã đọc.
- WinForms: TableLayoutPanel/FlowLayoutPanel, DPI 100–200% (gồm 125/150), MinimumSize, chuỗi dài, Designer, STA/UiThread. Checklist R-001 chống cắt chữ/chồng lấn; test nền dựng ẩn, CreateControl/PerformLayout và LayoutChecker; không gọi Show trong hướng dẫn test nền. UiSnapshot thuộc nghiệm thu.
- Web: Razor Pages/HTMX, antiforgery, hx-confirm trên form phát request; không Html.Raw với query/form/file vào script, chỉ số/danh sách cho phép; viết markup Razor thay chuỗi HTML. Partial dựng tay truyền ViewData; test render thật + markup, yêu cầu rộng 1000px không tràn. Không ghi cứng dữ liệu, CSS khớp thẻ. API/Blazor chỉ áp dụng khi dự án dùng.
- Ví dụ Web tốt có `<form ... hx-confirm="...">`, không đặt xác nhận trên button và không dùng Html.Raw. Ví dụ Test có string.Concat + new string để ghép bí mật lúc chạy. Hướng dẫn kiểm lệnh nêu rõ Contains thay StartsWith.
- App: MAUI lifecycle/binding/DI/navigation. CLI Main bắt mọi ngoại lệ, stderr `LỖI: …`, mã khác 0; khung chưa làm bắt NotImplementedException, in `LỖI: chưa làm`, mã 1. Ví dụ tốt có try/catch, Console.Error.WriteLine, return 1; tiến trình dùng ArgumentList, UseShellExecute = false, timeout và đọc output không chặn, không ghép shell.
- Test: xUnit, không return sớm/Assert.True(true)/nuốt exception; kiểm lệnh bằng Contains phần cốt lõi. DOS names: CON, PRN, AUX, NUL, COM1, LPT1, cả phần mở rộng/khác hoa thường; bí mật ghép lúc chạy bằng string.Concat. Tiến trình con dùng ProjectReference + AppContext.BaseDirectory, không bin/Debug; không chạy agy/codex thật.
- Checklist test/bảo mật: dữ liệu ngoài chuẩn hóa đường dẫn, chặn `..`, đường dẫn tuyệt đối và symlink/reparse point trước ghi; giới hạn tải; dữ liệu hỏng báo lỗi, không nuốt rồi ghi đè. Dùng thư mục tạm/HubPaths, không hồ sơ người dùng/dữ liệu thật; null/rỗng/trùng, lần đầu/dữ liệu cũ, bản cài/portable; fixture thật, thiếu thì báo. Giữ BOM UTF-8, không để debug/đường dẫn máy mình.
- Các quy tắc trên là nội dung hướng dẫn, không thêm kiểm tra runtime. Test chỉ đọc bốn tài liệu tĩnh của checkout, không đọc cấu hình/dữ liệu AITeam hay hồ sơ người dùng. Không có hợp đồng file/CLI ngoài hoặc DI mới.

## Test nghiệm thu
`core.tests/AcceptanceS103StackSkillsTests.cs`

## Tiêu chí hoàn thành
- [ ] Bốn skill đủ tám mục, nguồn, checklist và ví dụ tốt/xấu thực chất; mỗi file ≤150 dòng, tổng khoảng 380 dòng.
- [ ] Bao phủ quy tắc từng stack, R-001 và các lỗi AGENTS.md trong Ràng buộc; hướng dẫn đủ mọi luồng liệt kê.
- [ ] `dotnet build AITeam.sln`: 0 lỗi, 0 warning mới.
- [ ] `dotnet test AITeam.sln`: pass, gồm 16 ca AcceptanceS103StackSkillsTests không sửa.
- [ ] Báo cáo có bảng từng tiêu chí → lệnh + kết quả thật, số test/warning; file đổi và lý do; quyết định tự đưa ra; nguồn chưa xác minh.

## Báo cáo của agent
| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Bốn skill, tám mục, nguồn/ví dụ/checklist | PowerShell ReadAllLines/regex: WinForms 84, Web 90, App 102, Test 97 dòng; tổng 373, mỗi file 8 mục, không còn chỗ trống, BOM UTF-8 giữ nguyên. |
| Quy tắc stack, R-001, AGENTS.md và mọi luồng | Đối chiếu Ràng buộc; `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceS103StackSkillsTests --blame-hang-timeout 2m -m:1 -nodeReuse:false`: 16 pass, 0 fail/skip; không sửa test nghiệm thu. |
| Build | `dotnet build AITeam.sln` thất bại mã 1, 0 warning/0 error không chẩn đoán; lần `--no-restore -m:1 -nodeReuse:false -v:normal` báo 5 NETSDK1004 do thiếu assets; `dotnet build AITeam.sln -m:1 -nodeReuse:false -v:minimal` restore/build thành công, 0 lỗi, 0 warning, 47,77 giây. |
| Test toàn solution | `dotnet test AITeam.sln --no-restore --blame-hang-timeout 2m -m:1 -nodeReuse:false`: 756 pass (Hub 457, Core 299), 0 fail, 0 skip. |
| Báo cáo và phạm vi | Chỉ sửa bốn SKILL.md và mục báo cáo task; không sửa code sản phẩm/test/cấu hình; không chạy git theo chỉ dẫn Codex dự phòng. |
File đổi: `skills/code-{winforms,web,app,test}/SKILL.md` — điền khung, nguồn thực đọc và ví dụ; task này — báo cáo kết quả.
Quyết định: thêm name/description cho bốn skill; dùng Microsoft Learn cho MAUI; chạy lại build tuần tự sau lỗi/thiếu assets, không đổi cấu hình dự án.
Nguồn: đã đọc năm skill gốc được chỉ định và bốn mục MAUI; quy ước R-001/Hub lấy từ brief, task và AGENTS.md, không tự nhận đã đọc code dự án ngoài.
Chưa xác minh: runtime/Designer/ảnh giao diện và code HDVTool/ChinaAndroidToolbox ngoài phạm vi; không chạy app. `quick_validate.py` không chạy được do thiếu Python (mã 9009); đã kiểm cấu trúc/BOM bằng PowerShell và 16 test nghiệm thu.
Lưu ý kiểm tra: lần test riêng đầu dùng --no-build trả 0 nhưng không in số test, không dùng làm bằng chứng pass; số 16 ở trên là lần chạy sau build thành công.
## Review (Claude)
<!-- Claude điền sau bước thực hiện. -->

## Kết quả
<!-- Claude điền khi review đạt. -->

