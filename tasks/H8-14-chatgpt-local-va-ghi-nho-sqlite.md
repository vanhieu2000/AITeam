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
- Đăng ký/đường vào: `hub/AITeam.Hub.csproj`, `hub/Program.cs`, `hub/Pages/Office.cshtml`, `hub/Pages/Settings.cshtml`, `hub/Pages/AI.cshtml`.
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
- Đã triển khai chat + SQLite + cấu hình model/ghi nhớ; sửa V9-04 và 4 trích dẫn dòng AGENTS sai từ upstream; giữ BOM/CRLF.
- Build `dotnet build AITeam.sln -warnaserror -nodeReuse:false`: 0 warning, 0 error.
- Tiêu chí lưu/đọc, gửi trùng, hủy, snapshot/khóa DB → 16 test LocalChat đạt; parser tab → test mới đạt.
- Tiêu chí HTTP/bảo mật → test host thật: CSRF, ID, Host; Chromium kiểm phản hồi không render HTML.
- Tiêu chí UI → 2 tin, reload giữ lịch sử/model, 1000/375 không tràn; ảnh `docs/screenshots/H8-14/`.
- CLI đăng nhập ChatGPT sẵn có; gọi thật bị proxy 403, chưa xác minh phản hồi AI thật; không ghi credential/log nhạy cảm vào nhánh.
- Test đầy đủ: báo số chính xác ở nhật ký R-012; còn lỗi Windows, không nhận toàn suite xanh.
- Quyết định: dùng SQLite local và đăng nhập CLI chính thức, không tự điều khiển website; chủ dự án đã bỏ bước hỏi duyệt mockup.
- Chưa xong: nghiệm thu Windows, gọi ChatGPT thật và các nhóm R-012 còn lại trong `docs/CHAT-LOCAL.md`.
- Phút/token của phiên Codex: chưa có số đo tin cậy, không ghi 0 giả.
