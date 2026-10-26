# S1-07 · Nối nghiệm thu, IMP và hàng chờ Map

- **Agent:** A
- **Nhánh:** `agent-a/S1-07-noi-nghiem-thu-imp-va-hang-cho-map`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~140 dòng
- **Yêu cầu:** R-010
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Không thêm chức năng mới cho người dùng; chuẩn hóa hướng dẫn nghiệm thu và cải tiến.
- Mẫu IMP và đánh giá nhắc chọn skill đúng bước, ghi bài học mới có nguồn, tránh ghi trùng.
- Chuẩn bị 11 ánh xạ nghiệp vụ → skill cho Map; chưa tạo trang trước R-009.
- Phần tự sinh lời đọc skill nghiệm thu đang vướng hợp đồng helper; xem điều kiện giao việc bên dưới.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-010/brief.md`
- `CLAUDE.md`
- `templates/improvement.md`
- `templates/task.md`
- `plans/R-010/map-skill-pending.md`
- `scripts/SkillPrompts.ps1` (chỉ đọc, helper S1-04)
- `core.tests/AcceptanceS107WorkflowDocsTests.cs`
- `core.tests/Fixtures/S1-07/CLAUDE.md`
- `core.tests/Fixtures/S1-07/task.md`
- `core.tests/Fixtures/S1-07/improvement.md`

## Bối cảnh và điều kiện giao việc
Làm theo brief R-010, mục Nối vào công cụ và Tự cập nhật. Phụ thuộc S1-04, S1-06.
Helper Add-SkillPrompt hiện chỉ nhận Stage plan/prep/code; Stage khác ném ArgumentException("Bước skill không hợp lệ."). Không có Stage nghiệm thu. Không giao phần nối nghiệm thu cho coder trước khi quản lý chốt mở rộng phạm vi helper hoặc bổ sung hỗ trợ này ở S1-04. Không hướng dẫn gọi Stage không tồn tại; không thay bằng tự ghép đường dẫn thủ công.
Các mục IMP/đánh giá/Map dưới đây đã đủ đặc tả và có test đỏ. Phần nối nghiệm thu chưa có khung/test thực thi vì phụ thuộc hợp đồng chưa được duyệt; không nhận là đã chuẩn bị trọn task.

## Phạm vi (được sửa)
- `CLAUDE.md`
- `templates/improvement.md`
- `templates/task.md`
- `plans/R-010/map-skill-pending.md`
- `tasks/S1-07-noi-nghiem-thu-imp-va-hang-cho-map.md` (chỉ Báo cáo của agent)

## Không làm
- Không sửa team.ps1, scripts/SkillPrompts.ps1, scripts/SkillNotes.ps1, projects.json, team.json, skills hay hợp đồng Hub.
- Không tạo trang Map, route đọc file, dịch vụ, DI, CLI mới; không thay quyền duyệt hoặc publish.
- Không sửa test nghiệm thu và fixture. Không chạy agy/codex, app hay trình duyệt.
- Không sửa/xóa nội dung hướng dẫn cũ: chỉ bổ sung tại đúng mục liên quan, giữ thứ tự và nguyên văn mọi dòng cũ.

## Ràng buộc
- Ngân sách triển khai khoảng 140 dòng, không tính test và bản sao fixture.
- Markdown hiện có giữ BOM và CRLF; file mới UTF-8 có BOM. Không chèn đường dẫn máy cá nhân.
- Chưa có API mới trong kế hoạch nên không tạo lớp/interface giả chỉ để có khung. Khung tài liệu Map đã có hai mục TODO(agent), coder điền nội dung.
- Hợp đồng nghiệm thu mong muốn (chờ giải quyết phụ thuộc): một lời đọc 10-nghiem-thu và các skill stack tồn tại, đường dẫn tuyệt đối do helper lấy từ gốc AITeam, không từ dự án; WinForms → code-winforms, ASP.NET/Blazor → code-web, MAUI/console/CLI → code-app; hỗn hợp không trùng, null/rỗng/lạ không đoán stack, thiếu file bỏ qua, gọi lại không lặp, chặn reparse point. Giữ nguyên prompt cũ. Không chấp nhận đoạn minh họa chưa chạy được.
- Trong CLAUDE.md (nghiệm thu/đánh giá), templates/task.md (Đánh giá quy trình), templates/improvement.md (Vấn đề/Giải pháp): bổ sung nhắc “skill đúng bước”, “TK / AG / CK”, “lỗi trùng không ghi lại”. Chỉ ghi lỗi mới đã đối chiếu skill bằng lệnh skill-note; không chạy lệnh từ template.
- Ví dụ lệnh giữ tham số -Skill, -Text, -Source; nguồn mẫu task là S1-07, nguồn IMP là IMP-xxx. Thực tế thay bằng mã task/IMP đang đánh giá, không ghi cứng mã mẫu khi chạy. Ví dụ: team.ps1 skill-note -Skill code-test -Text "Mô tả bài học mới" -Source S1-07. Ví dụ IMP dùng -Source IMP-xxx.
- Giữ nguyên điều kiện thay đổi lớn cần chủ dự án duyệt ở mẫu IMP, Claude chốt nghiệm thu, kho private và quy tắc merge/push; không tăng quyền cho AI.

## Dữ liệu mẫu thật
- Fixture trong core.tests/Fixtures/S1-07 là bản sao byte nguyên vẹn từ CLAUDE.md, templates/task.md, templates/improvement.md trước triển khai; giữ BOM/CRLF gốc. Test bảo vệ toàn bộ hướng dẫn cũ, gồm duyệt/publish.
- Không đọc projects.json hay phiếu yêu cầu trong task này; không tự dựng JSON/phiếu giả. Không có dữ liệu người dùng/tiến trình ngoài cần mẫu.

## Đặc tả hàng chờ Map
Bảng Markdown đúng hai cột Nghiệp vụ | ID skill; ID đặt trong backtick, đúng 11 dòng:
Tiếp nhận → 01-tiep-nhan; Ý tưởng → 02-y-tuong; Nghiên cứu → 03-research; Kế hoạch → 04-ke-hoach; Duyệt kế hoạch → 05-duyet-ke-hoach; Chuẩn bị task → 06-chuan-bi-task; Code → 07-code; Kiểm thử → 08-kiem-thu; QC → 09-qc; Nghiệm thu → 10-nghiem-thu; Publish Git → 11-publish-git.
Mục an toàn phải ghi rõ: đây là hàng chờ R-009, “chưa tạo trang Map”; ID tra danh sách cho phép cố định, không nhận đường dẫn tùy ý; chặn .. và đường dẫn tuyệt đối từ đầu vào, chuẩn hóa và kiểm tra nằm dưới skills; chặn symlink/reparse point cả file lẫn thư mục cha; thiếu file thì không phát liên kết chết; chỉ đọc, không thực thi Markdown; encode tên/nội dung/liên kết, không Html.Raw. Không thay quyền duyệt/publish. Đây là tiêu chí cho R-009, không hiện thực endpoint trong S1-07.

## Test nghiệm thu
`core.tests/AcceptanceS107WorkflowDocsTests.cs`

## Tiêu chí hoàn thành
- [ ] Giải quyết phụ thuộc helper trước khi giao coder phần nối nghiệm thu; bổ sung khung/test thật cho đúng hợp đồng đã duyệt. Hiện CHƯA ĐẠT.
- [ ] Hướng dẫn IMP/đánh giá ở cả ba file có đúng bước, lỗi mới TK / AG / CK, nguồn task/IMP và chống trùng; giữ nguyên mọi dòng hướng dẫn cũ.
- [ ] Đủ 11 ánh xạ duy nhất và tiêu chí liên kết đọc an toàn; chưa tạo trang Map, không đổi hợp đồng Hub/quyền duyệt/publish.
- [ ] dotnet build AITeam.sln: 0 lỗi, 0 warning mới.
- [ ] dotnet test AITeam.sln: pass toàn bộ sau triển khai; không sửa test nghiệm thu.
- [ ] Báo cáo bảng từng tiêu chí → lệnh/kết quả thật, file đổi/lý do, quyết định tự đưa ra và điều chưa xác minh.

## Kiểm chứng bước chuẩn bị (Codex)
- Lần 1: dotnet build AITeam.sln trả mã 1, chỉ in Build FAILED với 0 warning/0 error; không nhận là đạt.
- Lần 2: dotnet build AITeam.sln -m:1 -v:normal thành công, 0 warning, 0 error, 55,25 giây. Compiler server báo không kết nối được rồi fallback biên dịch thành công. Đã dùng đủ giới hạn 2 lần build.
- dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --filter FullyQualifiedName~AcceptanceS107WorkflowDocsTests --blame-hang-timeout 2m: tổng 18, pass 3, fail 15, skip 0. Đỏ vì mẫu chưa có nhắc skill-note và Map chưa điền ánh xạ/an toàn; ba ca bảo toàn hướng dẫn cũ đang xanh.
- Chưa chạy toàn bộ solution test; chưa có test sinh prompt nghiệm thu do helper thiếu Stage tương ứng. Không đọc hợp đồng Hub để chụp snapshot vì ngoài danh sách đọc được duyệt; hồi quy hiện bảo vệ hướng dẫn cũ về quyền duyệt/publish, chưa kiểm byte hợp đồng Hub.
- Chưa sẵn sàng giao toàn bộ S1-07: cần chốt phạm vi helper rồi bổ sung khung và test prompt. Đã gửi câu hỏi về mở rộng scripts/SkillPrompts.ps1, chưa có trả lời. Không chạy git.
## Báo cáo của agent
| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Phụ thuộc helper nghiệm thu | Giữ nguyên trạng thái chưa đạt theo điều kiện giao việc (chờ chốt helper) |
| Mẫu IMP / Đánh giá quy trình / CLAUDE.md | Test HuongDanCu và MoiMau_NhacDungBuoc_NguonVaChongTrung pass 6/6 |
| Ánh xạ 11 nghiệp vụ và tiêu chí an toàn Map | Test Map_CoDungAnhXa (11/11) và Map_GhiRoHangChoVaRangBuocDocAnToan pass |
| dotnet build AITeam.sln | dotnet build AITeam.sln -m:1 → Build succeeded, 0 Warning, 0 Error (19.84s) |
| dotnet test AITeam.sln | dotnet test AITeam.sln → Pass 1042/1042 (Core: 515/515, Hub: 527/527) |
- **File đổi:** `CLAUDE.md`, `templates/task.md`, `templates/improvement.md` (bổ sung nhắc skill-note), `plans/R-010/map-skill-pending.md` (11 ánh xạ & an toàn Map).
- **Quyết định tự đưa ra:** Giữ nguyên UTF-8 BOM/không BOM và CRLF theo đúng file gốc; không sửa test nghiệm thu.
- **Điều chưa chắc:** Phần nối tự sinh prompt nghiệm thu cần quản lý mở rộng scripts/SkillPrompts.ps1 ở task sau.

## Review (Claude)
<!-- Claude điền. -->

### Vòng 1 — ĐẠT (sau khi Claude chốt helper + sửa)
- Cổng KT ĐẠT (1042 test). Codex QC BLOCKED đúng: kế hoạch thiếu hợp đồng helper cho nghiệm thu (TK).
- Quản lý chốt + Claude sửa: `Add-SkillPrompt -Stage accept` ⇒ `10-nghiem-thu` + skill stack; `team.ps1 review` in mục "Skill nghiệm thu". Sửa lỗi `switch` 1 phần tử bị mở thành chuỗi (bọc `@()`). Thêm 2 ca accept vào test S1-04.
- Claude sửa `tools/test-verify.ps1`: chép `scripts/` vào sandbox kịch bản; cho phép `$text` cục bộ (tham số `-Text` của S1-06 chỉ đọc ở switch cấp cao) — main đang hỏng kiểm tra tĩnh từ S1-06.
- Tài liệu (CLAUDE.md, mẫu task/IMP, hàng chờ Map 11 ánh xạ) đúng kế hoạch.
- [TK] (phạm vi) kế hoạch S1-07 không chốt hợp đồng helper cho bước nghiệm thu.
- [CK] (test) cổng KT không chạy `tools/test-verify.ps1` ⇒ S1-04/S1-06 làm hỏng kiểm tra tĩnh mà vẫn ĐẠT.
## Kết quả
- `team.ps1 review` tự in skill nghiệm thu đúng stack; mẫu task / IMP / sổ tay nhắc ghi bài học bằng `skill-note`; có danh sách 11 nghiệp vụ → skill cho trang Map (R-009).
- Kiểm chứng: 41 test S1-04 + S1-07, kiểm tra tĩnh + kịch bản `test-verify` ĐẠT.
<!-- Claude điền sau nghiệm thu. -->

## Đánh giá quy trình
<!-- Claude điền sau hoàn tất. -->