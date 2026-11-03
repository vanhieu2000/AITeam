# ChatGPT ngay trong AITeam Hub

## Chức năng đã có trên nhánh H8-14
- Chat đầy đủ tại `/Chat`, mở bằng menu **Trò chuyện AI** ngay dưới **Văn phòng**. Văn phòng `/` chỉ có thẻ nhỏ hội thoại gần nhất sau thẻ tác nhân/Chat giao việc, không polling chat 2 giây. Cài đặt vẫn mở từ `/AI` và `/Settings`.
- Tạo nhiều hội thoại, gửi tin nhắn, theo dõi phản hồi nền, dừng, sao chép, xuất toàn bộ hội thoại JSON và xóa có xác nhận.
- SQLite lưu tin nhắn, trạng thái từng lượt, model, token nếu biết, ghi nhớ chung và cấu hình kết nối. Không cần cài SQL Server/PostgreSQL.
- Khởi động lại giữ lịch sử; lượt bị ngắt được báo rõ, không tự gửi lại gây tốn hạn mức.
- Chống bấm gửi trùng, giới hạn hàng đợi, không ghi đè phản hồi đã hủy, mỗi lượt giữ cấu hình lúc gửi.
- Đây là hội thoại ChatGPT qua **Codex CLI chính thức** dùng tài khoản ChatGPT trên máy, không phải nhúng hay điều khiển tab chatgpt.com. Lịch sử web ChatGPT không tự đồng bộ sang Hub; các model phải được Codex CLI/tài khoản hỗ trợ.

## Chạy trên máy Windows
1. Cài .NET SDK 10 nếu chưa có; cài Codex CLI theo hướng dẫn chính thức: `npm install -g @openai/codex`.
2. Chạy `codex login` một lần trên máy và đăng nhập tài khoản ChatGPT. Không gửi token/mật khẩu vào Hub hoặc commit.
3. Từ checkout của nhánh, chạy:
   ```powershell
   dotnet restore AITeam.sln
   dotnet build AITeam.sln -warnaserror -nodeReuse:false
   Remove-Item Env:HUB_FAKE -ErrorAction SilentlyContinue
   Remove-Item Env:HUB_EMPTY -ErrorAction SilentlyContinue
   dotnet run --project hub --no-build
   ```
4. Mở Hub đang chạy trên máy, chọn **Cài đặt → Mở cài đặt chat** (hoặc vào Văn phòng).
5. Để trống model để dùng mặc định CLI; nếu cấu hình CLI cũ trỏ model không được cấp quyền, nhập model tài khoản đang hỗ trợ. Đường dẫn CLI có thể để trống để tự tìm.
6. Bấm **Lưu cấu hình**, rồi **Kiểm tra kết nối / đăng nhập**. Nút kiểm tra dùng cấu hình đã lưu, không gọi một lượt AI và không tiêu token hội thoại.
7. Gửi hai câu liên tiếp, tải lại Hub để xem lịch sử. Dùng **Ghi nhớ chung / lời hướng dẫn** để lưu những điều muốn AI nhớ ở mọi hội thoại.

## Dữ liệu và bộ nhớ
- Database: `%USERPROFILE%\.aiteam\hub\chat.db`; Linux: `~/.aiteam/hub/chat.db`.
- Cấu hình tài khoản AI hiện có vẫn ở vị trí cũ; SQLite mới chỉ quản lý chat và cấu hình chat. Không tự sửa `team.json`, phân vai hoặc tự giao task từ lời AI.
- Lịch sử hiển thị 200 tin gần nhất; xuất JSON lấy toàn bộ (tối đa 1.000 lượt/hội thoại). Giới hạn 500 hội thoại.
- Mỗi câu hỏi gửi lại tối đa 40 tin gần nhất + câu hỏi hiện tại, giới hạn 48.000 ký tự, cùng ghi nhớ chung. Không khẳng định AI nhớ vô hạn hoặc toàn bộ lịch sử cũ.
- Tin gửi tới ChatGPT đi qua Internet; lịch sử vẫn nằm trên máy để xem khi offline. Chưa có model suy luận hoàn toàn offline.
- Không lưu cookie/token đăng nhập CLI trong DB. Các mẫu bí mật thông dụng được che trước khi lưu/gửi nhưng không bảo đảm nhận diện mọi thông tin nhạy cảm; không nhập bí mật vào chat.
- SQLite **chưa mã hóa toàn bộ nội dung**. Bảo vệ tài khoản Windows/ổ đĩa và file sao lưu. Không commit DB hoặc JSON xuất chứa nội dung cá nhân.
- Sao lưu toàn bộ: dừng Hub rồi sao chép `chat.db` cùng các file `-wal`/`-shm` nếu còn. Khôi phục lúc Hub đã dừng. Chỉ một Hub được sở hữu database này tại một thời điểm.
- Cấu hình cũ sai/hỏng không bị thay bằng mặc định rồi ghi đè âm thầm; lỗi lưu không xóa hội thoại cũ.

## Chạy minh họa
```powershell
$env:HUB_FAKE = "1"
$env:HUB_PORT = "5058"
dotnet run --project hub --no-build
```
Banner nói rõ **Minh họa · không gọi AI thật**. SQLite ở bộ nhớ, không chạy Codex/đọc phiên đăng nhập/ghi DB thật. Dữ liệu biến mất khi tiến trình Hub dừng, vẫn còn khi chỉ tải lại trình duyệt.

## Kiểm chứng trên cloud Linux
- Build `-warnaserror -nodeReuse:false`: 0 warning, 0 error.
- 16 test chat: persistence, migration/recovery, hủy, gửi trùng, khóa DB, snapshot model, CLI arguments/parser, HTTP + antiforgery, host/ID giả; không gọi mạng trong test.
- Trình duyệt Chromium: gửi 2 tin liên tiếp; lịch sử/model còn sau reload; không thực thi HTML trong phản hồi; 1000/375 px không tràn; ảnh ở `docs/screenshots/H8-14/`.
- Sửa thêm V9-04: parser nhận `##\tReview (Claude)` và dừng ở heading cấp 2 có tab. Có test hồi quy mới.
- Sửa 4 dẫn chiếu dòng AGENTS bị lệch từ upstream, giữ nguyên test nghiệm thu.
- Test đầy đủ cuối: Hub **792/806 đạt, 14 lỗi**; Core **402/691 đạt, 289 lỗi**; tổng **1.194 đạt / 303 lỗi / 0 skip**. Các lỗi Windows (PowerShell 5.1/cmd/DPAPI/đường dẫn/khóa file) chưa thể nghiệm thu trên Linux. Sau lần siết validation model, build lại 0 warning/error và chạy lại 16 test chat.
- CLI cloud có trạng thái đã đăng nhập ChatGPT. Thử gọi thật bằng đúng tham số của connector bị proxy từ chối `403` tới `chatgpt.com`, sau đó dừng retry. Chưa có bằng chứng một phản hồi AI thật thành công trên cloud. Trên máy cá nhân cần mạng cho phép đích này; không tắt TLS/sandbox để né lỗi.

## Phần R-012 chưa được coi là hoàn thành
Nhánh này triển khai bổ sung chat và SQLite, cấu hình model, V9-04 và sửa tham chiếu tài liệu. Mockup D1–D4 được giữ để tham khảo, không phải các màn hình đã triển khai. Chưa gộp các nhánh H7-02/V6-02/W1-03; W1-04 còn phụ thuộc W1-03 vào main. Chưa triển khai toàn bộ B1–B4/C1–C5/D1–D4 hoặc xác minh chạy thật trên máy công ty. Không đánh dấu những việc này “đạt” chỉ từ bản demo chat.

| Mã | Nhánh | Trạng thái | Phút | Token |
|---|---|---|---|---|
| H8-14 | `agent-c/H8-14-chatgpt-local` | Đã triển khai; chờ kiểm chứng ChatGPT thật/Windows và chủ dự án kiểm tra | Chưa có số đo tin cậy | Chưa có số đo tin cậy |
| V9-04 | Cùng nhánh H8-14 theo đợt sửa trực tiếp | Parser tab + test hồi quy đã đạt; chưa merge main | Chưa đo riêng | Chưa đo riêng |

## Đợt chỉnh theo review H8-14
- Đầu hội thoại cộng token đã đo của toàn bộ lượt trong DB, không giới hạn 200 tin hiển thị; số lượt chưa đo ghi riêng.
- Cài đặt ghi rõ chi phí khoảng 13.000 token đầu vào/tin nhắn từ hướng dẫn hệ thống Codex CLI. Đây là chú thích ước lượng, không được cộng giả vào số token thực đo.
- CodexPath tùy chọn phải là file tồn tại trong một thư mục PATH hoặc cây `%APPDATA%\npm`. Để trống để tự phát hiện. Nếu CLI bị gỡ, vẫn xem lịch sử/sửa cấu hình được; lưu hoặc chạy với đường dẫn sai sẽ báo lỗi.
- Giữ nguyên các test menu H7-06 cũ dù chúng không chấp nhận mục /Chat mới; kết quả test mới và đầy đủ được ghi trong file task H8-14.
