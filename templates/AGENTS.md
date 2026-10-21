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

## Test nghiệm thu, báo cáo, cổng kiểm tra
- Mục **"Test nghiệm thu"** trong task là các file test Claude viết TRƯỚC khi giao. KHÔNG sửa, KHÔNG xóa, KHÔNG `Skip`; chỉ sửa code cho chúng pass. Thấy test sai thì ghi vào báo cáo kèm lý do, vẫn không sửa.
- Đọc cả thiết kế được trỏ tới trong task và dữ liệu mẫu thật trong `tests/fixtures/` (nếu task nêu).
- **Báo cáo của agent** phải có bảng **từng tiêu chí hoàn thành → bằng chứng** (lệnh đã chạy + kết quả thật: số test pass, số warning) — **TỐI ĐA 15 dòng**, không viết dài, file đã đổi và lý do, quyết định tự đưa ra khi không có ai hỏi, điều chưa xác minh.
- Sau khi push, **cổng kiểm tra tự động** (`team.ps1 verify`) chạy ngay: đã commit + push hết, BOM, build 0 warning, test pass, đúng phạm vi, test nghiệm thu còn nguyên, có báo cáo. Không đạt sẽ bị giao lại kèm danh sách lỗi.

## Lỗi hay gặp (đã xảy ra nhiều lần — bắt buộc tránh)
- **Giữ nguyên BOM UTF-8** của file đang có BOM khi sửa; script `.ps1` luôn lưu UTF-8 có BOM.
- **Không viết test luôn pass**: không `if (...) return;` sớm, không `Assert.True(true)`, không bắt exception rồi bỏ qua. Bỏ phần code mới đi thì test phải đỏ.
- Kiểm tra chuỗi lệnh đã gửi (tới thiết bị / tiến trình) bằng `Contains` phần cốt lõi, không `StartsWith`: lệnh thật thường có tiền tố.
- Không `catch { }` nuốt lỗi khi đọc dữ liệu sau đó sẽ ghi lại: dữ liệu hỏng thì báo lỗi, không coi là rỗng rồi ghi đè.
- **Dữ liệu từ bên ngoài là không tin cậy** (file người dùng mở, mục trong zip, manifest, form web, cấu hình, output thiết bị): chuẩn hóa đường dẫn và chặn `..` / đường dẫn tuyệt đối TRƯỚC khi ghi file; không ghép thẳng vào dòng lệnh; giới hạn kích thước khi tải.
- Định dạng dữ liệu (output lệnh, file) làm theo **mẫu thật** trong `tests/fixtures/` hoặc tài liệu được trỏ tới, không tự đoán. Không có mẫu thật thì ghi rõ trong báo cáo.
- Làm đủ **mọi luồng** thiết kế liệt kê (ví dụ bản cài / portable, lần đầu / dữ liệu cũ, null / rỗng / trùng).
- **Commit và push trước khi dừng.** Dừng giữa chừng mà chưa commit là mất việc.
- Không để code tạm (cờ chụp ảnh, debug, đường dẫn máy mình) trong sản phẩm.
- **Không tắt cuộn để "đạt" tiêu chí không cuộn** (`AutoScroll = false`, `BodyScrolls = false`): nội dung phải thật sự vừa (đổi bố cục, chia tab, thu gọn), không được bị cắt. Kiểm tra ảnh chụp: chữ hoặc thẻ bị cắt ở mép là KHÔNG ĐẠT.
- **KHÔNG chụp ảnh, KHÔNG chạy app**: Claude chụp bằng `tools/UiSnapshot` khi review. Giao diện làm đúng mục "Bố cục giao diện" của task và kiểm bằng test bố cục tự động có sẵn.
- Trong `FlowLayoutPanel`, control con KHÔNG dùng `Dock` (bị bỏ qua ⇒ co về rộng 0, không hiện). Cần xếp dọc giãn ngang thì dùng `TableLayoutPanel` 1 cột hoặc đặt `Width` theo panel cha khi resize.

## Tuyệt đối không
- Không chạy `git config` (tên / email commit đã được đặt sẵn cho thư mục làm việc của bạn; sửa sẽ lan sang repo của chủ dự án).
- Không đưa lên git: mật khẩu, khóa API, token, khóa ký, file `.pem` / `.pfx` / `.lic`, file `.env` thật, dữ liệu cá nhân.
- Không làm thay đổi máy thật khi test:
  - không tắt service, không gỡ phần mềm;
  - không ghi registry HKLM, không xóa file ngoài thư mục dự án;
  - không đổi cấu hình hệ thống.
- CSDL: chỉ dùng CSDL test hoặc CSDL tạm của dự án. Không sửa, không xóa dữ liệu thật trên máy.
- Không cài phần mềm toàn hệ thống. Thiếu công cụ thì ghi vào báo cáo để quản lý cài.
