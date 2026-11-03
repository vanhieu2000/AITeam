# V9-12 · Bản ghi lỗi có cấu trúc

- **Agent:** C
- **Nhánh:** `agent-c/V9-12-loi-co-cau-truc`
- **Yêu cầu:** R-012
- **Ngân sách:** ~300 dòng
- **Độ khó:** M

## Tóm tắt
- Chuẩn hóa chín mã nguyên nhân dùng chung cho metrics, staff-report và Hub.

## Đọc
`docs/MO-TA-HE-THONG.md` mục 7.2; yêu cầu B3; `hub/Services/InsightParsers.cs`; `hub.tests/AcceptanceH301Tests.cs`.

## Phạm vi
- `core/AITeam.Core/Metrics/FindingParser.cs` (tạo mới), `core/AITeam.Cli/Program.cs`, `team.ps1`, `hub/Services/InsightParsers.cs`, test mới `core.tests/V912FindingParserTests.cs`, fixture V912 từ năm review thật; task/bảng việc.

## Không làm
- Không sửa assert, không tự đưa NV thành mã thứ mười, không thay đổi H8-14/V9-04 hoặc merge main.

## Tiêu chí
- [ ] Chín mã TK/TN/AG/CK/HT/HM/CH/TO/DP; mã lạ là “khác”.
- [ ] Dùng chung ba nơi và 5 file task thật.

## Báo cáo của agent
| Điều kiện | Bằng chứng |
|---|---|
| Yêu cầu mới | B3 và mục 7.2 quy định chín mã; mã lạ ⇒ “khác”; dữ liệu cũ AG/TK/CK vẫn được nhận. NV không thuộc danh sách. |
| Test cũ | AcceptanceH301Tests.cs:203–207 yêu cầu ParseReviewIssues trả Cause="NV" với dòng - [NV] Lan dừng giữa chừng. |
| Mâu thuẫn | Cùng dữ liệu NV không thể vừa chuẩn hóa Cause="khác" vừa giữ Cause="NV" tại hợp đồng parser dùng cho Hub. |
- DỪNG trước khi đổi parser theo chỉ định chủ dự án; không sửa assert hoặc sinh mã thứ mười để qua test.
- Chưa chạy build/test mới vì chỉ thay tài liệu; chưa viết parser thay thế hoặc gắn vào metrics/staff-report. Phút: chưa đo; token: chưa đo.
- Cần quản lý chốt ánh xạ NV cũ hoặc cập nhật hợp đồng nghiệm thu trước khi tiếp tục; không merge main.

## Báo cáo của agent — đợt 2
| Tiêu chí | Bằng chứng |
|---|---|
| Cập nhật | Pull f268d9d của Claude và gộp main 9bc9a8e vào nhánh cũ; không sửa assert. |
| Parser chung | FindingParser giữ chín mã chuẩn, NV/mã lạ → khác; giữ nhóm, nội dung nguyên văn và vòng; bỏ comment/code fence/checkbox. Hub dùng adapter trình bày cũ bỏ dấu chấm cuối, còn Core/JSON giữ nguyên. |
| Ba nơi | CLI review-findings dùng Core; Get-ReviewFindings gọi CLI cho staff-report và metrics.findings; Hub InsightParsers cũng dùng Core. Bảng điểm mới thuộc D3, không đổi hợp đồng projection cũ ở mã này. |
| Test/fixture | 13 test mới đạt, có năm mục review trích từ task thật (Fixtures/V912/SOURCE.md); test Claude NV → khác đạt. |
| Build/toàn bộ | Build -warnaserror -nodeReuse:false 0 warning/0 error; Core 514/842 đạt, 328 lỗi nền; Hub 803/817 đạt, 14 lỗi nền. PS AST 0 lỗi; chưa chạy Windows. Log /tmp/r012-d2-v912-verified-tests. |
| Giới hạn | Đường gọi PowerShell chưa chạy Windows 5.1; không gọi AI. Giữ tương thích định dạng ReviewIssue cũ; JSON Core giữ văn bản gốc. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |
