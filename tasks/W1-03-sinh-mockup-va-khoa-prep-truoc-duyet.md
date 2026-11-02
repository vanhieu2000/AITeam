# W1-03 · Sinh mockup và khóa prep trước duyệt

- **Agent:** A
- **Nhánh:** `agent-a/W1-03-sinh-mockup-va-khoa-prep-truoc-duyet`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~320 dòng
- **Yêu cầu:** R-011
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->

- **Model:** `gemini-4-argon-high`

## Tóm tắt
- Có chức năng mới: sinh file mockup tĩnh để chủ dự án xem và duyệt trước khi giao việc.
- Kế hoạch nói rõ task có giao diện hay chỉ lõi; chưa rõ thì yêu cầu bổ sung.
- Chặn chuẩn bị khi chưa duyệt hoặc mockup thay đổi; giữ duyệt cũ cho plan chỉ lõi.

## Đọc (CHỈ những file này)
- `AGENTS.md`, `plans/R-011/brief.md`, `plans/R-011/plan.md`.
- `team.ps1` (ba hàm khung, Invoke-Plan, Invoke-Prep, catch ngoài cùng).
- `templates/codex-plan.md`, `templates/codex-prep.md`, `hub/wwwroot/css/hub.css`.
- `core.tests/AcceptanceW103PlanApprovalTests.cs`, `core.tests/Support/TestShell.cs`, `core.tests/Support/TeamScript.cs`, `core.tests/Support/RealFile.cs` (chỉ đọc).

## Bối cảnh
Theo brief R-011 mục 2; phụ thuộc W1-01 đã có helper. Ba hàm PowerShell đã có chữ ký, chú thích hợp đồng và thân ném NotImplementedException. Đã nối Write-PlanMockup vào Invoke-Plan trước lưu plan; Assert-PrepPlanApproval ngay sau đọc plan trong Invoke-Prep. Catch CLI hiện có in `LỖI: chưa làm`, mã 1; giữ nguyên.

## Phạm vi (được sửa)
- `team.ps1`
- `templates/codex-plan.md`
- `templates/codex-prep.md`
- `tasks/W1-03-sinh-mockup-va-khoa-prep-truoc-duyet.md` (chỉ báo cáo).

## Không làm
- Không sửa test nghiệm thu/helper W1-01, `tools/workflow-pipeline.ps1`, hợp đồng Hub, nhật ký, dữ liệu thật.
- Không tự duyệt, không tự ghi hash vào quyết định; không thực thi HTML hay mở trình duyệt/app.
- Không sửa retry/provider, git transaction, khóa pipeline; ngân sách ~320 dòng phần triển khai, không tính test/đặc tả.

## Ràng buộc
- PowerShell 5.1; UTF-8 BOM. Chú thích hợp đồng dùng `# ///`. Giữ tên/chữ ký ba hàm khung.
- `Read-PlanMockup(PlanText)` trả `{ HasUi; Html }`. Mỗi mục task `### MÃ · …` mới phải có `- **Giao diện:** Có` hoặc `Không`. Thiếu/chưa rõ báo `Chưa xác định loại task; cần bổ sung Giao diện: Có/Không.`. Không suy ra từ tiêu đề. Một task Có => plan UI.
- UI cần đúng một mục `## Mockup`, đúng một khối html đóng kín, 1..120 dòng nội dung (kể cả dòng trống, không tính hàng rào); nhiều khối html kể cả ngoài mục đều sai. Lõi không cần mockup.
- Chỉ HTML/CSS tĩnh: CSS nội tuyến và link stylesheet đúng `../../hub/wwwroot/css/hub.css`; liên kết khác chỉ `href="#"`. Chặn script (mọi hoa/thường), on*, javascript/data/URL ngoài/protocol-relative, src/srcset/action, CSS url/@import/expression (kể cả mã hóa/escape), iframe/object/embed/form/base/meta refresh/SVG/MathML. Nên whitelist tag/thuộc tính. Lỗi `Mockup không hợp lệ.`; không thực thi nội dung.
- `Write-PlanMockup(RequestId, PlanText)` kiểm ^R-\d{3}$ trước ghi; chặn thoát root và reparse point ở file/mọi cha, lỗi `Đường dẫn mockup không hợp lệ.`. Lõi trả null, không tạo thư mục. UI ghi `Root/docs/design/REQUEST-mockup.html`, UTF-8 BOM CRLF; thay href CSS Hub thành URI file tuyệt đối `Root/hub/wwwroot/css/hub.css`. Trả `{ Path; Fingerprint }`, SHA-256 hex thường trên mọi byte file. Kiểm toàn bộ trước ghi, không tự duyệt; Invoke-Plan có thể in path/hash.
- `Assert-PrepPlanApproval(RequestId, PlanText)` chỉ đọc. Loại HTML comment và fenced code trước tìm mục `## Quyết định của quản lý` (cho hậu tố tên/ngày), kết thúc tại heading ## kế. Chỉ dòng bắt đầu `- ĐÃ DUYỆT.` (cho văn bản sau dấu chấm) trong mục này hợp lệ; thiếu báo `Kế hoạch chưa được duyệt.`. Không nhận ví dụ/phủ định/duyệt ở mục khác.
- UI cần thêm dòng `- Mockup SHA256: <64 hex>` trong cùng mục quyết định thật. Hash khớp mọi byte file; file phải khớp kết quả xuất HTML hiện tại (so trong bộ nhớ, không ghi lại). Thiếu/sai file/hash, HTML đổi, file đổi, reparse point => `Mockup chưa được duyệt hoặc đã thay đổi.`. Hash trong comment/code không hợp lệ.
- Tương thích: plan cũ KHÔNG khai báo loại và KHÔNG Mockup vẫn nhận duyệt hiện có; có Mockup phải kiểm UI, không lách bằng xóa trường. Có khai báo chưa rõ phải bổ sung.
- Guard trước Select-FreeAgent, tạo logs, fetch/worktree, provider, kể cả NoLaunch/resume. Từ chối không gọi chúng, không sửa file. Resolve-Project/Get-PlanDir/đọc plan là thao tác đọc cho phép.
- Mẫu codex-plan thêm trường Giao diện/mục Mockup, phân biệt giới hạn phần kế hoạch và giới hạn HTML để không tự mâu thuẫn. Mẫu codex-prep nhắc guard, không tự duyệt và helper chung. Không bỏ hướng dẫn cấm git của bước chuẩn bị.

## Dữ liệu mẫu thật
- Đã mở plan R-011, mẫu codex-plan và CSS Hub thật. Test RealFile.Copy giữ nguyên tên/byte/BOM/CRLF rồi biến đổi bản sao sandbox. Không phân tích projects/phiếu trong task này.
- Plan R-011 thật duyệt mockup riêng khác khối HTML và chưa có hash: quản lý cần chọn lại mockup rồi duyệt hash, không tự di trú. Chỉ plan cũ lõi được nhận duyệt cũ.

## Test nghiệm thu
`core.tests/AcceptanceW103PlanApprovalTests.cs`

## Tiêu chí hoàn thành
- [ ] Plan lõi/UI/chưa rõ đúng hợp đồng; xuất CSS mở tại máy, HTML 120 nhận/121 từ chối.
- [ ] Chặn nhiều khối, mã chủ động/URL/CSS nguy hiểm, duyệt giả trong comment/code, sửa sau duyệt.
- [ ] Hai luồng thật nối helper; prep từ chối trước tác dụng phụ, lõi cũ giữ duyệt hợp lệ.
- [ ] `dotnet build AITeam.sln` 0 lỗi, không warning mới; `dotnet test AITeam.sln` pass hết, không sửa test nghiệm thu.
- [ ] Không yêu cầu agent chạy app/chụp ảnh; Claude kiểm mockup thật ở bước 7.
- [ ] Báo cáo từng tiêu chí → lệnh/kết quả thật, file/lý do, quyết định tự đưa ra, điều chưa xác minh.

## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Đỏ ban đầu | verify-1 (HEAD ebef924) báo 6 đỏ; chạy lại sau merge tại nhánh này còn 2: `AcceptanceW103…Ui_exports…` (Path trả `\` thay vì `docs/design`) và `AcceptanceS104…ProviderGiaNhanPromptThat_MoiNhanh(prep,codex)` (`Assert-PrepPlanApproval` không được nạp). |
| Sửa trong Phạm vi | `team.ps1:1151`: trả `Path` dạng `Root\docs/design/<R>-mockup.html` bằng `[IO.Path]::Combine`; vẫn dùng đường dẫn chuẩn hóa để kiểm reparse/ghi file. |
| Sửa môi trường test (ngoại lệ, không sửa assert) | `core.tests/AcceptanceS104SkillPromptsTests.cs:168` thêm `Assert-PrepPlanApproval` vào danh sách hàm nạp; `:182` thêm mục `## Quyết định của quản lý (giả)` + `- ĐÃ DUYỆT.` cho plan fixture. Lý do: harness chỉ nạp 5 hàm và fixture thiếu duyệt nên `Invoke-Prep` không thể chạy đúng hợp đồng guard mới; assert giữ nguyên. |
| Build | `dotnet build AITeam.sln -warnaserror` lần đầu lỗi MSB3491 khóa file tạm trong `bin` (môi trường); chạy `-m:1 -nr:false`: 0 warning, 0 lỗi. |
| Test | `dotnet test AITeam.sln --blame-hang-timeout 3m --no-build`: Hub 721 đạt/0 đỏ, Core 737 đạt/0 đỏ. Lọc W1-03 + S104: 70 đạt/0 đỏ. |
| Hợp đồng | Giữ tên/chữ ký 3 hàm; guard ngay sau đọc plan, trước Select-FreeAgent/logs/fetch/provider; không sửa assert, không tự duyệt. |
| Chưa xác minh | Không chạy được `powershell -File tools\test-verify.ps1` (quyền công cụ chỉ cho git/dotnet); chưa mở mockup thật — Claude kiểm ở bước 7. |
## Review (Claude)
<!-- Claude điền. -->

## Kết quả
<!-- Claude điền sau nghiệm thu. -->
