# H8-15 · Sửa nhỏ giao diện Hiệu suất và Tài khoản AI

- **Agent:** O
- **Nhánh:** `agent-c/H8-15-sua-nho-hieu-suat-tai-khoan`
- **Yêu cầu:** R-012 (Optional sau review H8-12, H8-13)
- **Ngân sách:** ~60 dòng
- **Độ khó:** S

## Tóm tắt
- Không có chức năng mới. Gọn giao diện: gộp cảnh báo đọc file, chữ trạng thái vừa ô, bỏ chú thích lặp.

## Báo cáo của agent — OpenCode
- Đã làm: gộp cảnh báo đọc nguồn ở `/Performance` thành một `<details>` thu gọn (không lỗi thì không hiện); thêm class `perf-value--text` cho giá trị chữ trạng thái; bỏ câu chú thích lặp trong từng thẻ `/AiAccounts`.
- File đã đổi: `hub/Pages/Performance.cshtml`, `hub/wwwroot/css/performance.css`, `hub/Pages/AiAccounts.cshtml`, `hub.tests/H812PerformanceTests.cs`, `hub.tests/H813AccountQuotaTests.cs`, file task này.
- Test thêm: `Page_groups_multiple_read_warnings_into_one_collapsed_details` (2 lỗi đọc ⇒ đúng 1 `<details>` chứa đủ 2 lỗi); `Missing_data_note_is_shown_once_on_the_page` (câu chú thích xuất hiện đúng 1 lần). Không sửa assert cũ.
- Kiểm: `dotnet build AITeam.sln -warnaserror -nodeReuse:false` → Build succeeded, 0 warning, 0 error.
- Kiểm: `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m` → Hub 837/837, Core 919/919, tổng 1756/1756 đạt, 0 hỏng, 0 bỏ qua.
- Chưa chắc / chưa xác minh: chưa xem ảnh giao diện thật ở 1000 px (luật không mở trình duyệt); đã kiểm bằng HTML render trong test.
