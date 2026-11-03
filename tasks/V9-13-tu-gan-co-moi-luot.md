# V9-13 · Tự gắn cờ sau mỗi lượt

- **Agent:** C
- **Nhánh:** `agent-c/V9-13-tu-gan-co-luot`
- **Yêu cầu:** R-012
- **Ngân sách:** ~400 dòng triển khai/test
- **Độ khó:** L

## Tóm tắt
- Máy gắn CH/TO/HT/HM, gợi ý TN khi cùng test/stack lặp hai lượt liên tiếp; ghi cờ vào metrics và nhật ký R, dừng tự giao lại.

## Đọc
`docs/MO-TA-HE-THONG.md` mục 7, yêu cầu B4; `team.ps1` Get-RunRecord/Save-RunMetrics/Invoke-Assign/Invoke-Verify; `core/AITeam.Cli/Program.cs`.

## Phạm vi
`team.ps1`, `core/AITeam.Cli/Program.cs`; tạo `core/AITeam.Core/Metrics/RunDiagnostics.cs`, `core.tests/V913RunFlagsTests.cs`, task/bảng việc.

## Không làm
- Không sửa test cũ, không đổi dữ liệu metrics lịch sử để làm bằng chứng, không gọi AI hoặc merge main.

## Tiêu chí
- [x] Luật CH/TO/HT/HM có ngưỡng, không khớp nhầm token 429.
- [x] TN dựa vào tên test + stack, cùng task/project và hai run liên tiếp; vẫn nhận khi các test khác thay đổi.
- [x] Chỉ lưu fingerprint/cờ, không sao chép log thô/chi tiết lỗi nhạy cảm vào metrics.
- [x] Hook Save-RunMetrics và điểm dừng trước cập nhật bảng/review tiếp; nhật ký R có dấu nhận diện chống trùng.
- [ ] Chạy lại log thật H7-06 ngày 26–28/10 và nghiệm thu PowerShell Windows.

## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Chẩn đoán | 7 test V913 mới đạt, gồm từng luật và cùng một test lỗi dù các lỗi khác đổi; dữ liệu test dựng, không nhận là log H7-06 thật. |
| CLI | diagnose-run chạy thật trả CH/HT/HM trên đầu vào kiểm tra; không gọi AI. |
| Build/cú pháp | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning, 0 error. Parser PowerShell 7.6 cục bộ: 0 lỗi cú pháp; không thay powershell.exe. |
| Toàn bộ | Core 404/697 đạt, 293 lỗi nền Windows/4 trích dẫn lệch; Hub 776/790 đạt, 14 lỗi Windows; 0 skip. |
- Chưa có verify log/stack trace thật 26–28/10 trong checkout: metrics đếm lỗi không đủ để suy ra stack giống nhau, không tự gắn TN cho dữ liệu cũ khi thiếu bằng chứng.
- Chưa chạy được powershell -File tools/test-verify.ps1 và hook/journal end-to-end trên Windows; không nhận nghiệm thu hoàn tất. Runtime sẽ chỉ tạm dừng khi có stack thật khớp.
- Không sửa assert; các thay đổi đúng Phạm vi, BOM/CRLF nguyên. Phút: chưa đo; token: chưa đo. Không merge main.
