# H8-14 · ChatGPT tại Hub, cấu hình model và ghi nhớ SQLite

- **Agent:** Codex
- **Nhánh:** `agent-c/H8-14-chatgpt-local`
- **Ưu tiên:** cao
- **Ngân sách:** ~1500 dòng triển khai/test/tài liệu; mở rộng theo yêu cầu trực tiếp bổ sung chat + DB.
- **Yêu cầu:** R-012
- **Độ khó:** L

## Tóm tắt
- Trò chuyện với ChatGPT ngay tại Hub; lưu lịch sử, model và ghi nhớ chung trong SQLite trên máy.
- Có tạo/xóa hội thoại, gửi/dừng, kiểm tra đăng nhập, sao chép và tải lịch sử JSON.
- Chủ dự án cho phép triển khai và push nhánh một lượt, không chờ duyệt mockup; không merge main.

## Đọc
`docs/MO-TA-HE-THONG.md`, `plans/R-012/YEU-CAU-CODEX.md`, `QUY-TRINH.md`, `AGENTS.md`, `hub/Services/HubPaths.cs`, `core/AITeam.Core/Process/ProcessRunner.cs`, `core/AITeam.Core/Providers/CodexProvider.cs`.

## Phạm vi
- Tạo mới: `hub/Services/LocalChatStore.cs`, `hub/Services/LocalChatClient.cs`, `hub/Pages/Chat.cshtml`, `hub/Pages/Chat.cshtml.cs`, `hub/Pages/Shared/_LocalChat.cshtml`, `hub/wwwroot/css/chat.css`, `hub/wwwroot/js/local-chat.js`, `hub.tests/LocalChat*Tests.cs`.
- Đăng ký/đường vào: `hub/AITeam.Hub.csproj`, `hub/Program.cs`, `hub/Pages/Office.cshtml`, `hub/Pages/Settings.cshtml`, `hub/Pages/AI.cshtml`, `hub/Pages/Shared/_Layout.cshtml`.
- Sửa nhỏ kèm theo yêu cầu V9-04: `core/AITeam.Core/Review/ReviewRoundCounter.cs`, tạo mới `core.tests/ReviewTabHeadingTests.cs`.
- Dẫn chiếu tài liệu bị lệch từ upstream: `skills/02-y-tuong/SKILL.md`, `skills/03-research/SKILL.md`, `skills/04-ke-hoach/SKILL.md`, `skills/05-duyet-ke-hoach/SKILL.md`.
- Tài liệu: `docs/CHAT-LOCAL.md`, `docs/design/R-012-hub-offline-mockup.html`, `docs/screenshots/H8-14/`, file task này, `tasks/README.md`, nhật ký R-012, bài học cuối `AGENTS.md`.

## Không làm
- Không tự merge main, không sửa test nghiệm thu có sẵn, không đọc/copy credential CLI.
- Không giả là nhúng chatgpt.com hoặc đã làm xong toàn bộ R-012; không biến nội dung AI thành lệnh chạy tự động.

## Tiêu chí
- [x] Lưu/đọc lại SQLite sau khi khởi động; có test dữ liệu ở thư mục tạm.
- [x] Chat hai lượt, có lịch sử/ngữ cảnh; model/ghi nhớ giữ qua reload.
- [x] Request chống gửi trùng; dừng và phản hồi muộn không ghi đè; queued dùng cấu hình lúc gửi.
- [x] Phản hồi AI được hiển thị dạng text, POST có antiforgery; host/ID giả bị chặn.
- [x] HUB_FAKE không gọi CLI hoặc tạo DB thật.
- [x] Không tràn ngang ở 1000/375 px, có ảnh và hướng dẫn dùng Windows.
- [ ] Phản hồi ChatGPT thật trên mạng cho phép đích chatgpt.com.
- [ ] Toàn bộ test Windows / chạy máy công ty.

## Test nghiệm thu
Test mới trong `hub.tests/LocalChatTests.cs`, `hub.tests/LocalChatClientTests.cs`, `hub.tests/LocalChatHttpTests.cs`, `core.tests/ReviewTabHeadingTests.cs`. Không đổi assert/test cũ.

## Báo cáo của agent
- Đợt sửa theo yêu cầu: tách chat đầy đủ khỏi Văn phòng, menu trái “Trò chuyện AI” ngay sau “Văn phòng”; Văn phòng chỉ có thẻ hội thoại gần nhất sau workspace, không tải script/polling chat.
- File sửa: Office.cshtml/.cs, _Layout.cshtml, _LocalChat.cshtml, Chat.cshtml.cs, LocalChatStore.cs, chat.css, local-chat.js; thêm LocalChatRevisionTests.cs và thay hai ảnh yêu cầu; giữ BOM UTF-8, không sửa test cũ.
| Tiêu chí | Bằng chứng |
|---|---|
| 1 · Menu/thẻ nhỏ/không polling | Test HTTP Razor mới đạt; Chromium xác nhận thẻ sau workspace và 0 request State trên Văn phòng. |
| 2 · Không cắt/tràn 375/1000 | Chromium: #local-chat 349/349 và 750/750; mọi nút scrollWidth ≤ clientWidth, cả khi mở/đóng cài đặt; ảnh docs/screenshots/H8-14/chat-375.png và chat-1000.png. |
| 3 · Tổng token/ghi rõ lượt chưa đo | Test 102 lượt, ngoài giới hạn hiển thị 200 tin: tổng 1.010 token, 1 lượt chưa đo; header cập nhật theo hội thoại; đã thêm nguyên văn dòng 13.000 token trong cài đặt. |
| 4 · CodexPath | Test file tồn tại/PATH/npm, file thiếu/ngoài thư mục và cấu hình CLI đã bị gỡ đạt; báo lỗi rõ lúc lưu, vẫn đọc được lịch sử. |
- Build `dotnet build AITeam.sln -warnaserror -nodeReuse:false`: 0 warning, 0 error.
- Test `dotnet test AITeam.sln --no-build`: Hub 787/810 đạt, 23 lỗi (14 lỗi Windows cũ + 9 ca menu cũ); Core 402/691 đạt, 289 lỗi Windows cũ; 0 skip. Cả 20 test LocalChat đạt, gồm 4 test mới.
- Test menu cũ AcceptanceH706NavigationTests.cs:252 khóa danh sách không có /Chat, xung đột trực tiếp yêu cầu mới; giữ nguyên, không né hoặc sửa assert.
- Chưa xác minh phản hồi ChatGPT thật do proxy cloud 403 và nghiệm thu Windows; không merge main. Phút/token phiên sửa chưa có số đo tin cậy.
