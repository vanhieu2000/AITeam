---
name: code-web
description: Viết Razor Pages và HTMX theo thiết kế; hướng dẫn API hoặc Blazor khi dự án có dùng.
---
# code-web

## Mục tiêu
Viết Web theo stack hiện có, dữ liệu thật và markup an toàn; tránh lặp lỗi Hub.
Razor Pages/HTMX là luồng chính; API/Blazor chỉ áp dụng khi dự án dùng.

## Đầu vào
- Task, thiết kế trang, PageModel/model, handler và partial trong phạm vi.
- Layout/CSS, cơ chế antiforgery, fixture và test render hiện có.
- Khi scaffold dữ liệu: model, DbContext/provider được yêu cầu; không tạo context trùng.
- Thiếu mẫu thật/hợp đồng: ghi thiếu, không đoán cấu trúc dữ liệu.

## Đầu ra (định dạng)
- Razor markup, handler và CSS đúng phạm vi, kèm test hành vi/render cần thiết.
- Báo cáo tiêu chí → test/kết quả thật; nêu bố cục cần nghiệm thu.
- Không đổi hợp đồng model/service hay thêm API, Blazor, migration ngoài task.

## Các bước
1. Giữ cấu trúc Razor Pages/style hiện có; tách handler xử lý khỏi markup.
   Scaffold phần được giao, tái dùng DbContext và provider đúng dự án.
2. POST có antiforgery token; handler vẫn kiểm quyền, dữ liệu và trạng thái phía server.
   HTMX gửi token theo cơ chế dự án; không tắt antiforgery để test pass.
3. Đặt hx-confirm trên form phát request; HTMX 2 bỏ qua nó trên button submit trong form.
   Kiểm full page và partial sau request HTMX, kể cả lỗi validation.
4. Để Razor encode dữ liệu; không Html.Raw với query/form/file vào script.
   Tham số script chỉ nhận số đã parse hoặc giá trị trong danh sách cho phép.
   Viết thẻ Razor trực tiếp, không ghép chuỗi C# chứa HTML rồi in bằng biểu thức Razor.
5. Partial dựng tay phải nhận ViewData, nhất là khi dùng TagHelper/antiforgery.
   Với PartialViewResult, truyền ViewDataDictionary có Model đúng và giữ ModelState;
   dựa trên ViewData hiện có để giữ ngữ cảnh, không tạo dictionary rỗng làm mất dữ liệu.
6. CSS khớp thẻ render: .seg a khác .seg span; lấy dữ liệu model, không ghi cứng Agent.
   Co cột, cho chuỗi dài xuống dòng; rộng 1000px phải không tràn ngang.
7. Test render thật bằng hạ tầng dự án, không chỉ tìm chữ trong file nguồn.
   Assert markup, token, form hx-confirm, dữ liệu encode và ViewData của partial.
   Kiểm chuỗi dài, model rỗng, validation, test bố cục sẵn; ảnh do bước nghiệm thu.
8. API khi dự án dùng: giữ một kiểu endpoint, DTO tách entity; truyền CancellationToken.
   Status code đúng: tạo mới 201 + Location; dùng ProblemDetails cho lỗi phù hợp.
   TypedResults cần Results<T1,T2> rõ khi nhiều nhánh; API theo TFM thực tế.
9. Blazor khi dự án dùng: Parameter đi xuống, EventCallback<T> đi lên; không sửa Parameter.
   State riêng ở OnParametersSet; có trạng thái tải/rỗng/lỗi và @key cho danh sách.
   Await async, hủy CTS/gỡ subscription lúc dispose; không .Result/.Wait() chặn UI.

## Quy ước của dự án
- OfficeModel inject IOfficeService/ICommandService; OnGet nạp dữ liệu và hiện lỗi, OnGetDesks trả `_OfficeDesks`; trang `/` tự gọi handler mỗi 10 giây, không đọc file trong Razor (`hub/Pages/Office.cshtml.cs:17`, `hub/Pages/Office.cshtml.cs:44`, `hub/Pages/Office.cshtml.cs:84`, `hub/Pages/Office.cshtml:22`).
- OfficeService.Get tổng hợp staff/board/running/rests rồi OfficeBuilder.Build; ParseStaff đọc tên/role/provider. Hiện có catch rỗng khi đọc từng nguồn; không lấy làm mẫu đọc rồi ghi lại (`hub/Services/OfficeService.cs:41`, `hub/Services/OfficeService.cs:88`, `hub/Services/OfficeService.cs:96`; yêu cầu: `AGENTS.md:57`).
- OnPostCommand chỉ nhận tên HubCommand hợp lệ, tách target rồi gọi dịch vụ. Self tạo ViewDataDictionary với ModelState và Model=this, nhưng không sao chép toàn bộ ViewData trang; nếu cần dữ liệu ngữ cảnh phải giữ thêm (`hub/Pages/Office.cshtml.cs:97`, `hub/Pages/Office.cshtml.cs:158`).
- Form `_OfficeControl` dùng asp-page-handler/hx-post, target `#office-control`, swap outerHTML, hx-confirm trên form; form Dừng trong `_OfficeDesks` cũng vậy. Giữ antiforgery của POST Form TagHelper; token sinh ra cần kiểm bằng render (`hub/Pages/Shared/_OfficeControl.cshtml:19`, `hub/Pages/Shared/_OfficeDesks.cshtml:96`).
- ApprovalsModel có ValidateAntiForgeryToken, OnPostDecide whitelist approve/reject rồi RedirectToPage (PRG); form Decide có AntiForgeryToken rõ ràng. Form này không có hx-confirm/HTMX, không dùng làm bằng chứng xác nhận HTMX (`hub/Pages/Approvals.cshtml.cs:9`, `hub/Pages/Approvals.cshtml.cs:41`, `hub/Pages/Approvals.cshtml:61`).
- d.Name/t.Title/m.Text và Model.Message qua biểu thức Razor encode; màu/trạng thái dùng switch cố định. Không suy ra MarkdownRenderer.Render an toàn khi chưa đọc helper (`hub/Pages/Shared/_OfficeDesks.cshtml:10`, `hub/Pages/Shared/_OfficeDesks.cshtml:43`, `hub/Pages/Shared/_OfficeDesks.cshtml:75`, `hub/Pages/Approvals.cshtml:13`, `hub/Pages/Approvals.cshtml:54`).
- Selector khớp thẻ thật: bàn dùng span.pill; tab Approvals dùng span.tab, script tìm id tab. Không lấy `.seg a` làm mô tả trang này (`hub/Pages/Shared/_OfficeDesks.cshtml:47`, `hub/Pages/Approvals.cshtml:29`, `hub/Pages/Approvals.cshtml:124`; bài học H-008: `AGENTS.md:56`).
- PageTests.Index_running_agents_handler_returns_partial dựng PageModel với fake, assert PartialViewResult/Model; AcceptanceV504OfficeTests.Desks_handler_returns_partial tương tự, còn Razor_markup_follows_layout đọc file nguồn. Hai file không gọi hạ tầng render, chưa chứng minh token/HTML render thật (`hub.tests/PageTests.cs:14`, `hub.tests/PageTests.cs:33`, `hub.tests/AcceptanceV504OfficeTests.cs:169`, `hub.tests/AcceptanceV504OfficeTests.cs:177`).

## Checklist bắt buộc
- [ ] Razor Pages/HTMX giữ antiforgery và kiểm quyền; hx-confirm trên form phát request.
- [ ] Không đưa query/form/file qua Html.Raw vào script; chỉ số/danh sách cho phép.
- [ ] Markup Razor là thẻ thật, không in chuỗi HTML C#; dữ liệu không ghi cứng.
- [ ] Partial dựng tay truyền ViewData; test render thật kiểm markup và HTMX/full page.
- [ ] Rộng 1000px không tràn, CSS khớp thẻ, chuỗi dài/rỗng đọc được.
- [ ] Đường dẫn ngoài chuẩn hóa, chặn .., tuyệt đối và symlink/reparse point trước ghi.
- [ ] Giới hạn tải; dữ liệu hỏng báo lỗi, không nuốt rồi ghi đè; test dùng dữ liệu tạm.
- [ ] API/Blazor chỉ khi dự án dùng; giữ hợp đồng Hub, không thêm dịch vụ ngoài yêu cầu.
- [ ] Giữ BOM UTF-8 cshtml/cs đang có BOM, không debug hoặc đường dẫn máy mình.

## Lỗi hay gặp
- `AGENTS.md:52` / V5-05: hx-confirm trên nút không xác nhận request form HTMX 2.
- `AGENTS.md:55` / H-007: query qua Html.Raw vào script tạo XSS; whitelist/parse trước dùng.
- `AGENTS.md:56` / H-008: ghi cứng Agent hoặc CSS sai thẻ khiến dữ liệu/bố cục sai.
- AGENTS.md: chuỗi C# chứa span bị encode thành chữ; viết span trong Razor.
- plans/R-010/brief.md: partial thiếu ViewData, test chỉ đọc nguồn không chứng minh render.

## Ví dụ
Tốt — POST có token và xác nhận trên form gửi HTMX.
```cshtml
<form method="post" asp-page-handler="Stop" hx-post="?handler=Stop"
      hx-confirm="Dừng lượt chạy này?" hx-target="#run-status">
    @Html.AntiForgeryToken()
    <input type="hidden" name="id" value="@Model.RunId" />
    <span class="agent-name">@Model.AgentName</span>
    <button type="submit">Dừng chạy</button>
</form>
```
Xấu — xác nhận sai phần tử; tên Agent ghi cứng.
```cshtml
<form method="post" hx-post="?handler=Stop">
    <span>Agent A</span>
    <button type="submit" hx-confirm="Dừng?">Dừng chạy</button>
</form>
```

## Nguồn
- Đã đọc `%USERPROFILE%/.claude/skills/create-datadriven-aspnetcore/SKILL.md`, Workflow:
  xác định scaffolder, tái dùng DbContext, khảo sát layout, Blazor tương tác chỉ khi cần.
- Đã đọc `%USERPROFILE%/.claude/skills/dotnet-webapi/SKILL.md`, DTO/endpoints/error handling.
- Đã đọc `%USERPROFILE%/.claude/skills/author-component/SKILL.md`, Core Rules/Async/Disposal.
- `AGENTS.md`, Lỗi hay gặp V5-05/H-007/H-008; `plans/R-010/brief.md`, mục code-web.
- Quy tắc Hub lấy từ task/AGENTS.md; task tài liệu không kiểm render/ảnh app thật.
