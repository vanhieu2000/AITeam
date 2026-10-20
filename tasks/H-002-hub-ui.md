# H-002 · AITeam Hub: giao diện khung + Tổng quan / Dự án / Task (dữ liệu giả)

- **Agent:** B
- **Nhánh:** `agent-b/H-002-hub-ui`
- **Ưu tiên:** cao

## Bối cảnh
Thiết kế đầy đủ nằm ở `docs/HUB-DESIGN.md`, đọc kỹ mục 2 và 4.
Claude đã dựng khung và hợp đồng:
- record: `hub/Models/HubModels.cs`;
- interface: `hub/Services/Contracts.cs`.

Agent A đang làm dịch vụ thật **song song** trên cùng hợp đồng. Bạn làm giao diện với **dữ liệu giả**.

## Mục tiêu
Giao diện đẹp, gọn, tiếng Việt, có chế độ sáng / tối, cho 3 trang:
- **Tổng quan** `/`
- **Dự án** `/p/{tên}`
- **Task** `/p/{tên}/t/{mã}`

Trang dùng các interface trong `Contracts.cs` qua DI, để khi có dịch vụ thật thì chạy ngay, không phải sửa trang.

## Phạm vi (được sửa)
- `hub/Pages/`: `_ViewImports`, `_ViewStart`, `Shared/_Layout.cshtml`, `Index`, `Project`, `Task`. Dùng Razor Pages, mỗi trang có PageModel.
- `hub/wwwroot/`:
  - `css/hub.css`;
  - `lib/htmx.min.js`: tải bản htmx 2.x từ unpkg / jsdelivr về **đặt cục bộ**, ghi phiên bản vào đầu file. Không dùng CDN lúc chạy.
- `hub/DevData/`: các lớp giả hiện thực interface. Dữ liệu giống thật: 2 dự án, đủ 4 trạng thái task, có agent đang chạy, có lần chạy lỗi.
- `hub/Program.cs`: CHỈ thêm khối đăng ký dữ liệu giả khi `HUB_FAKE=1`.
  - Agent A cũng sửa file này, nên chỉ thêm đúng 1 khối `if`, gọi `DevData.FakeServices.Register(builder.Services)`.

## Yêu cầu giao diện
- Bố cục:
  - thanh bên trái rộng 220px: logo "AITeam", menu 8 mục theo thiết kế; các mục chưa làm (Agent, Cài đặt, Tài khoản, Bí mật, Hộp thư) vẫn hiện, trang tạm "Đang xây dựng";
  - vùng nội dung tối đa 1200px;
  - nút sáng / tối, lưu vào localStorage.
- Màu theo thiết kế mục 4: nhấn `#2563eb`, trạng thái 🟡 / 🔵 / 🔴 / ✅. Font hệ thống (Segoe UI). Bo góc 8px, viền mảnh, không bóng nặng.
- **Tổng quan**:
  - 4 thẻ số liệu;
  - lưới thẻ dự án;
  - khối "Agent đang chạy": thời gian chạy dạng "12 phút", tự làm mới mỗi 15 giây bằng HTMX;
  - khối "Tình trạng máy": icon ✓ / ! / ✗ kèm câu `Fix`.
- **Dự án**:
  - kanban 4 cột;
  - thẻ task: mã, tiêu đề, nhãn agent, số lần chạy, lần chạy cuối;
  - tab "Kế hoạch" hiển thị markdown.
- **Task**:
  - các mục hiển thị markdown;
  - tiêu chí dạng checkbox chỉ xem;
  - bảng lần chạy, nút "Xem kết quả" mở `Response` trong khung thu gọn (`<details>`);
  - link GitHub nhánh.
- Markdown → HTML: viết bộ chuyển đổi nhỏ trong `hub/Pages/Shared/` (heading, đậm, code, danh sách, bảng, link, checkbox). **Mã hóa HTML trước** (chống XSS). Không thêm thư viện NuGet.
- Responsive: dưới 900px thì thanh bên thu gọn, kanban xếp dọc.

## Không làm
- Không sửa `Contracts.cs`, `HubModels.cs`, `hub/Services/`. Thiếu dữ liệu thì ghi vào báo cáo.
- Không làm các trang của giai đoạn 2.

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln` 0 warning, `dotnet test AITeam.sln` pass. Thêm test cho bộ chuyển markdown, gồm cả trường hợp chèn `<script>` phải bị mã hóa.
- [ ] Chạy thử `HUB_FAKE=1 dotnet run --project hub`, mở `http://127.0.0.1:5057`: cả 3 trang hiển thị đúng, không lỗi console. Ghi vào báo cáo cách đã kiểm tra.
- [ ] Không gọi mạng lúc chạy (htmx là file cục bộ).

## Báo cáo của agent
<!-- Agent điền -->

## Review (Claude)
<!-- Claude điền -->
