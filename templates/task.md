# {{TASK}} · {{TITLE}}

- **Agent:** {{AGENT}}
- **Nhánh:** `{{BRANCH}}`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~400 dòng
- **Yêu cầu:** R-xxx
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Bối cảnh
<!-- Vì sao cần làm, hiện trạng. Trỏ tới thiết kế: "Làm theo docs/design/<file>.md mục X". Liệt kê code có sẵn để tái dùng. -->

## Mục tiêu
<!-- Kết quả mong muốn nhìn từ phía người dùng + các lớp / hàm / màn hình cần có. -->

## Phạm vi (được sửa những gì)
<!-- Ghi đường dẫn trong `…` để cổng kiểm tra đối chiếu, ví dụ `src/Foo/`, `tests/`. -->
-

## Không làm
-

## Ràng buộc (bảo mật / dữ liệu / tương thích)
<!-- Claude điền TRƯỚC khi giao, đừng để tới lúc review mới nêu. Ví dụ:
- Dữ liệu không tin cậy: <file người dùng mở / mục trong zip / form / output thiết bị> → chặn `..`, đường dẫn tuyệt đối, giới hạn kích thước.
- Luồng phải chạy được: <bản cài + portable / lần đầu + dữ liệu cũ / null + rỗng>.
- Không đổi định dạng / hợp đồng: <...>. Giữ tương thích với dữ liệu cũ: <...>. -->
-

## Dữ liệu mẫu thật
<!-- Với dữ liệu đọc từ thiết bị / file / API: đường dẫn mẫu THẬT trong `tests/fixtures/...` và nguồn.
Chưa có mẫu thật → ghi "Chưa xác minh trên máy thật, theo <nguồn>". Xóa mục nếu không liên quan. -->
-

## Giao diện
<!-- Xóa mục nếu task không có giao diện. Mockup / mô tả đã được chủ dự án duyệt: docs/design/... mục ...
- Kích thước tối thiểu phải đúng: <ví dụ 1100×700>, DPI 100 / 125 / 150 %.
- Chuỗi dài nhất phải thử: <...>.
- Ảnh phải nộp vào `docs/screenshots/{{TASK}}/`: <trạng thái 1>, <trạng thái 2>, <trạng thái lỗi / rỗng>. -->
-

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
<!-- Claude viết và commit lên main TRƯỚC khi giao: các ca biên, lỗi, bảo mật, dữ liệu cũ, định dạng thật.
Ghi đường dẫn trong `…`, ví dụ `tests/Foo.Tests/AcceptanceT003Tests.cs`. Lúc giao các test này đang đỏ (hoặc chưa biên dịch được vì chưa có lớp). -->
-

## Tiêu chí hoàn thành
- [ ] Build `{{BUILD}}` 0 warning
- [ ] Test `{{TEST}}` pass hết, gồm toàn bộ test nghiệm thu (không sửa) + test mới của agent cho phần đã làm
- [ ]

## Báo cáo của agent
<!-- Agent điền:
| Tiêu chí | Bằng chứng (lệnh + kết quả thật / ảnh) |
|---|---|
File đã đổi và lý do · Quyết định tự đưa ra · Điều chưa xác minh / rủi ro. -->

## Review (Claude)
<!-- Claude điền mỗi vòng: "### Vòng N — ĐẠT / CẦN SỬA", đã kiểm tra gì (kèm kết quả cổng kiểm tra), rồi:
**Blocker** / **Required**: file/vị trí · vấn đề · bằng chứng / cách tái hiện · hành vi mong muốn · test xác nhận.
**Optional**: ghi thành task riêng ở hàng chờ, không bắt sửa trong task này. -->
