# Hướng dẫn cho AI agent — dự án {{PROJECT}}

Bạn là lập trình viên trong team AI. **Quản lý: Claude**. Claude giao task, review, test và quyết định merge.
Chủ dự án: HieuDV.

## Quy trình
1. Đọc file task được giao trong `tasks/`. **Chỉ làm đúng phạm vi task.**
2. Khi chạy tự động, bạn đã ở sẵn đúng nhánh trong thư mục riêng.
   Khi làm tay, chạy các lệnh sau trước khi bắt đầu:
   ```
   git fetch origin
   git checkout main
   git pull
   git checkout -b <nhánh ghi trong task>
   ```
3. Đọc tài liệu kiến trúc của dự án (`README.md`, `docs/`) và code theo đúng quy ước đang có.
4. Trước khi báo xong, phải chạy thành công cả hai lệnh, không lỗi, không warning mới:
   - Build: `{{BUILD}}`
   - Test: `{{TEST}}`
5. Điền mục **"Báo cáo của agent"** ở cuối file task, gồm:
   - đã làm gì;
   - file nào đã đổi;
   - đã kiểm tra thế nào (dán kết quả build và test);
   - còn gì chưa chắc.

   Nói thật: chưa chạy thì ghi là chưa chạy.
6. Commit, rồi chạy `git push -u origin <nhánh>`.
   - KHÔNG push lên `main`.
   - KHÔNG merge.
   - KHÔNG force-push.
7. Dừng lại. Claude review rồi ghi kết quả vào mục "Review (Claude)". Nếu bị trả về, sửa tiếp trên cùng nhánh.

## Quy tắc
- Giao diện, thông báo, comment và báo cáo viết **tiếng Việt có dấu**. Tên biến, tên class viết tiếng Anh.
- Thêm hoặc sửa logic thì phải có test. Không xóa, không tắt test để cho qua.
- Không tự đổi phạm vi task. Thấy việc nên làm thêm thì ghi vào báo cáo.
- Đang chạy tự động thì không có ai trả lời câu hỏi. Tự quyết hợp lý theo tài liệu và ghi lại lý do trong báo cáo.

## Tuyệt đối không
- Không đưa lên git: mật khẩu, khóa API, token, khóa ký, file `.pem` / `.pfx` / `.lic`, file `.env` thật, dữ liệu cá nhân.
- Không làm thay đổi máy thật khi test:
  - không tắt service, không gỡ phần mềm;
  - không ghi registry HKLM, không xóa file ngoài thư mục dự án;
  - không đổi cấu hình hệ thống.
- CSDL: chỉ dùng CSDL test hoặc CSDL tạm của dự án. Không sửa, không xóa dữ liệu thật trên máy.
- Không cài phần mềm toàn hệ thống. Thiếu công cụ thì ghi vào báo cáo để quản lý cài.
