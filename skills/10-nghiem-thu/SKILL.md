# 10-nghiem-thu

## Mục tiêu
Ở bước 7, Claude hoặc người được giao xác nhận kết quả trực quan theo thiết kế và ghi bài học; coder không phải chạy app để thay bước này.

## Đầu vào
Commit đã qua KT/QC, yêu cầu và bố cục đã duyệt, danh sách trạng thái UI/kích thước/DPI phải xem, log cùng commit và người được giao nghiệm thu.

## Đầu ra (định dạng)
Gửi quản lý/chủ dự án biên bản Markdown: tiêu chí → thao tác, kết quả, đường dẫn ảnh và commit; kết luận đạt/cần sửa/chưa xác minh.
Không có UI ghi “không áp dụng” cho kiểm trực quan; thiếu ảnh/thao tác bắt buộc ghi chưa xác minh. Kèm lỗi phân loại TK/AG/CK và nguồn thật nếu có bài học mới.

## Các bước
1. Xác nhận đúng commit đã qua KT/QC và thiết kế dùng đối chiếu; nếu bản code đổi thì kiểm lại bằng chứng liên quan trước kết luận.
2. Nếu có UI, Claude/người được giao chạy app để xem các trạng thái, thao tác và kích thước/DPI mà task yêu cầu; lưu ảnh có đường dẫn, kiểm chữ dài, lỗi/rỗng và dữ liệu thật theo thiết kế.
3. Đối chiếu kết quả với thiết kế đã duyệt, ghi lỗi có vị trí, thao tác tái hiện và ảnh; không bắt coder tự chạy app hoặc chụp ảnh thay người nghiệm thu.
4. Nếu không có UI thì ghi không áp dụng; phần nghiệp vụ chưa được kiểm vẫn phải ghi chưa xác minh, không coi không áp dụng là mọi thứ đã đạt.
5. Phân nguyên nhân lỗi mới: TK (task/thiết kế), AG (agent), CK (công cụ/cổng kiểm tra); chỉ ghi bài học đã có bằng chứng vào skill tương ứng, kèm nguồn thật, tránh trùng.
6. Dùng cú pháp skill-note bên dưới khi được giao cập nhật; biên bản nghiệm thu không tự cấp quyền merge/publish.

## Checklist bắt buộc
- [ ] Người được giao kiểm trực quan ở bước 7, ảnh/thao tác gắn đúng commit; coder không phải chạy app.
- [ ] Không báo pass khi chưa chạy; không có UI ghi không áp dụng, thiếu bằng chứng ghi chưa xác minh.
- [ ] Không dùng build thay bằng chứng UI; kết quả KT/QC được dẫn riêng.
- [ ] Lỗi mới TK/AG/CK có nguồn thật trước khi ghi `skill-note`; chọn `-Skill` tương ứng, nội dung qua `-Text`, bằng chứng qua `-Source`.
- [ ] Chỉ soạn/đọc ví dụ trong task tài liệu; không tự thực thi skill-note hay publish khi chưa được giao hành động đó.

## Lỗi hay gặp
- Xác nhận HTMX sai vị trí khiến nút không hỏi lại. Nguồn: AGENTS.md, mục Lỗi hay gặp — hx-confirm phải trên form phát request (V5-05).
- Lỗi mới không đưa về skill dễ lặp lại. Nguồn: plans/R-010/brief.md, mục Tự cập nhật — lỗi TK/AG/CK mới ghi bằng skill-note.

## Ví dụ
- Tốt: người nghiệm thu tái hiện lỗi AG xác nhận HTMX, lưu ảnh cùng commit; ghi bài học vào code-web bằng ví dụ `./team.ps1 skill-note -Skill code-web -Text "Đặt hx-confirm trên form phát request." -Source "AGENTS.md — Lỗi hay gặp: V5-05"` khi được giao cập nhật.
- Xấu: thiếu ảnh và chưa chạy app nhưng ghi UI đạt vì build xanh, rồi thêm bài học TK không có nguồn; phải đổi kết luận thành chưa xác minh.

