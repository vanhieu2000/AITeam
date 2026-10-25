# 06-chuan-bi-task

## Mục tiêu
Chuyển kế hoạch đã duyệt thành task đủ đặc tả, khung và test đỏ trước khi giao coder điền phần thân.

## Đầu vào
Kế hoạch đã duyệt, hợp đồng hiện có, mẫu task và dữ liệu mẫu thật khi kế hoạch yêu cầu; người quản lý chịu trách nhiệm chuẩn bị.

## Đầu ra (định dạng)
Bàn giao cho coder và reviewer file task Markdown: mục tiêu, phạm vi, không làm, ràng buộc, file được đọc/sửa, tiêu chí và đường dẫn test bất biến; kèm khung code, test và commit chuẩn bị.
Ghi trạng thái đạt/cần sửa/chưa xác minh cho từng điều kiện giao việc; thiếu mẫu thật phải ghi chưa xác minh và nguồn đang dùng.

## Các bước
1. Đối chiếu kế hoạch đã duyệt, chia task vừa ngân sách; ghi rõ hợp đồng không được đổi và luồng bắt buộc.
2. Đọc mẫu thật được trỏ tới; thiết kế ca lỗi, biên, dữ liệu cũ và bảo mật theo dữ liệu đó, không đoán định dạng.
3. Tạo khung giữ chữ ký; đăng ký DI/trang nếu thiết kế yêu cầu. Phần chưa làm phải thất bại rõ ràng, không trả kết quả thành công giả.
4. Viết test nghiệm thu trước phần thân; ghi đường dẫn bất biến vào task, chạy và lưu kết quả đỏ cùng nguyên nhân dự kiến.
5. Kiểm mọi file khung đều nằm trong phạm vi; ghi lệnh build/test, điều kiện giao diện và người nghiệm thu nếu có, rồi bàn giao commit chuẩn bị theo quy trình.

## Checklist bắt buộc
- [ ] Có khung code và test nghiệm thu trước khi coder viết phần thân; chữ ký và phạm vi khớp kế hoạch.
- [ ] Phần thân khung dùng `throw new NotImplementedException();`; chú thích `// TODO(agent): điền phần thân` chỉ nằm trong mã khung, không thay đặc tả.
- [ ] Nếu có CLI, Main bắt mọi exception từ khung, ghi `LỖI: chưa làm` ra stderr với lỗi chưa triển khai, trả mã 1; lỗi khác cũng được báo và trả mã khác 0.
- [ ] Có mẫu thật khi được yêu cầu; thiếu mẫu ghi chưa xác minh, không nhận đã kiểm dữ liệu thật.
- [ ] Đường dẫn test nghiệm thu, ca cần pass và kết quả đỏ được ghi cụ thể; không dùng test luôn pass.

## Lỗi hay gặp
- Khung ngoài phạm vi khiến coder không thể hoàn thiện: đối chiếu danh sách file trước giao. Nguồn: AGENTS.md, mục Quy trình, bước 1 — chỉ làm đúng phạm vi task.
- CLI để exception văng ra ngoài có thể mở hộp lỗi Windows; Main phải chặn lỗi. Nguồn: AGENTS.md, mục Lỗi hay gặp — Main bắt MỌI ngoại lệ (R-006).
- Đoán định dạng khi thiếu mẫu làm sai dữ liệu. Nguồn: AGENTS.md, mục Lỗi hay gặp — làm theo mẫu thật trong tests/fixtures/.

## Ví dụ
- Tốt: task đọc manifest kèm mẫu thật, chữ ký hàm và test đường dẫn `..` đang đỏ; coder nhận đúng danh sách file để triển khai.
- Xấu: giao “đọc manifest” không mẫu, khung trả danh sách rỗng và test xanh; chưa có bằng chứng tính năng được triển khai.

