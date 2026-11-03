# V9-10 · Ghi token đúng cho mỗi lượt

- **Agent:** C
- **Nhánh:** `agent-c/V9-10-token-moi-luot`
- **Yêu cầu:** R-012
- **Ngân sách:** ~200 dòng triển khai/test
- **Độ khó:** M

## Tóm tắt
- Phân biệt chưa đo được với 0 token; bảo toàn token thật của Codex qua CLI, log tương thích và metrics.

## Đọc
`plans/R-012/YEU-CAU-CODEX.md` B1; `core/AITeam.Core/Providers/`, `team.ps1`, `core.tests/ProviderTests.cs`, `core.tests/Fixtures/codex-success.jsonl`.

## Phạm vi
`core/AITeam.Core/Providers/CodexProvider.cs`, `ProviderModels.cs`, `DevRunner.cs`, `team.ps1`; tạo mới `core.tests/V910TokenMetricsTests.cs`, `core.tests/Fixtures/codex-exec-sample.jsonl`, `core.tests/Fixtures/codex-exec-sample-source.md`; task và bảng việc.

## Không làm
- Không sửa test cũ, H8-14/V9-04 hoặc sinh dữ liệu mẫu rồi nhận là chạy thật; không tự gộp O-02 chưa có trên main.

## Dữ liệu mẫu thật
- Trích nguyên sự kiện turn.completed từ codex-success.jsonl trên main 759ddaf; lưu trước khi sửa parser, bỏ dữ liệu không liên quan. Input 79889, output 934, reasoning 54; reasoning nằm trong output, không cộng lần hai.
- Không có mẫu OpenCode thật trong repo. Không tạo mẫu giả để vượt điều kiện nghiệm thu.

## Tiêu chí
- [x] Parser không biến usage thiếu/sai thành 0; có tokensKnown rõ ràng.
- [x] CLI xuất thinking/total; log Codex và metrics giữ null khi chưa đo, dùng long thay int.
- [x] Giữ input/output=-1 legacy của CLI để không phá nghiệm thu cũ, thêm total nullable và tokensKnown=false.
- [ ] OpenCode step_finish tích hợp, mẫu chạy thật và lượt mới >0.
- [ ] Kiểm chứng PowerShell 5.1 và test-verify trên Windows.

## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Codex thật | 5 test V910 mới đạt: mẫu lưu trữ thật cho tổng 80823, thinking 54; bốn dạng usage lỗi đều unknown/null. |
| Tương thích | Toàn bộ test DevRunner/Provider cũ giữ nguyên; JSON thêm trường, không đổi sentinel -1 legacy. |
| Build | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning, 0 error. |
| Test đầy đủ | Core 402/695 đạt, 293 lỗi nền Windows/4 tài liệu; Hub 776/790 đạt, 14 lỗi Windows; 0 skip. |
- Phần OpenCode và lượt mới thật chưa hoàn tất: O-02 chưa có trên main, chưa có mẫu OpenCode thật; lần gọi ChatGPT trước đã bị proxy cloud 403, không nhận mẫu cũ là lượt mới.
- Chưa chạy được powershell -File tools/test-verify.ps1 vì Linux không có PowerShell Windows; đường metrics PowerShell đã sửa nhưng chưa nhận đã nghiệm thu runtime.
- Giữ BOM/CRLF, không đổi assert. Nhánh riêng từ main 759ddaf, không merge main. Phút: chưa đo; token: chưa đo.
