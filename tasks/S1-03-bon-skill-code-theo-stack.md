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
### Chuẩn bị bước 4 (Codex)
| Tiêu chí chuẩn bị | Bằng chứng |
|---|---|
| Đặc tả và khung | Task này + bốn SKILL.md có tám mục và TODO(agent); chưa viết nội dung thực hiện. Không có lớp/hàm sản phẩm mới nên không cần khung C#/DI. |
| Build | Lần 1 `dotnet build AITeam.sln`: mã 1, báo 0 warning/0 error, không có chẩn đoán cụ thể. Lần 2 `dotnet build AITeam.sln -m:1 -nodeReuse:false -v:normal`: thành công, 0 warning, 0 error, 28,35 giây. Không build thêm. |
| Test nghiệm thu đỏ | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceS103StackSkillsTests --logger 'console;verbosity=quiet'`: 16 fail, 0 pass, 0 skip (khung chưa có nội dung). Chưa chạy toàn bộ suite; không chạy tiến trình agent thật. |

File chuẩn bị: task này (đặc tả); bốn SKILL.md trong Phạm vi (khung); `core.tests/AcceptanceS103StackSkillsTests.cs` (16 ca xUnit kiểm nội dung tài liệu, không phải logic ứng dụng).
Quyết định: không thêm API giả vào task chỉ có tài liệu; nguồn gốc chưa đọc do giới hạn đầu vào, yêu cầu người thực hiện ghi nguồn thực tế và báo nếu thiếu. Test kiểm hợp đồng văn bản, reviewer vẫn phải đánh giá chất lượng tóm lược và đối chiếu nguồn.
Điều chưa xác minh: nguồn skill ngoài repo và nội dung MAUI; nội dung hoàn chỉnh do Antigravity thực hiện. Không chạy git theo yêu cầu.

### Thực hiện (Antigravity điền)
<!-- Antigravity điền sau khi hoàn thiện nội dung. -->
| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Nội dung / nguồn / giới hạn dòng | Chưa thực hiện |
| Build và test | Chưa thực hiện |

File đã đổi và lý do: chưa điền.
Quyết định tự đưa ra: chưa điền.
Điều chưa xác minh: bước chuẩn bị chưa đọc skill gốc hoặc tài liệu MAUI; không khẳng định nguồn ngoài có sẵn trên máy agent.

## Review (Claude)
<!-- Claude điền sau bước thực hiện. -->

## Kết quả
<!-- Claude điền khi review đạt. -->

