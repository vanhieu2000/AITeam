# S1-02 · Skill chuẩn bị đến publish

- **Agent:** B
- **Nhánh:** `agent-b/S1-02-skill-chuan-bi-den-publish`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~360 dòng
- **Yêu cầu:** R-010
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Không thêm chức năng mới cho người dùng; chuẩn hóa hướng dẫn từ chuẩn bị task đến publish.
- Hoàn thiện sáu skill để các AI cùng tuân thủ phạm vi, bằng chứng và quyền hạn.
- Tách cổng KT, QC độc lập và nghiệm thu trực quan thành ba bước riêng.
- Ngăn báo pass khi chưa chạy và ngăn coder tự merge hoặc publish trái quyền.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `templates/task.md`
- `plans/R-010/brief.md`
- File task này và sáu file skill trong Phạm vi.
- `core.tests/AcceptanceS102SkillWorkflowTests.cs` (chỉ đọc).

## Bối cảnh
Theo `plans/R-010/brief.md`: skill dùng chung mọi dự án, mỗi file ≤150 dòng.
Đây là task nội dung, không có giao diện, dữ liệu runtime, API hay dịch vụ DI.
Khung Markdown đã có mục; agent phải thay TODO bằng nội dung, không thêm lớp C# giả.

## Phạm vi (được sửa)
- `skills/06-chuan-bi-task/SKILL.md`
- `skills/07-code/SKILL.md`
- `skills/08-kiem-thu/SKILL.md`
- `skills/09-qc/SKILL.md`
- `skills/10-nghiem-thu/SKILL.md`
- `skills/11-publish-git/SKILL.md`
- File task này: chỉ điền Báo cáo của agent.

## Không làm
- Không sửa test `core.tests/AcceptanceS102SkillWorkflowTests.cs`.
- Không thêm API, CLI, DI, UI hoặc sửa hợp đồng dữ liệu.
- Không sửa `team.ps1`, `CLAUDE.md`, `team.json`, `projects.json`, `skill/`, `tools/`, mẫu prompt hay skill khác.
- Không thực thi ví dụ publish, không gọi agy/codex thật, không tự merge.

## Ràng buộc
- Tổng sáu skill ≤360 dòng, từng file 1–150 dòng, kể cả dòng trắng; tiếng Việt có dấu.
- Giữ bảy tiêu đề cấp 2 đúng thứ tự: Mục tiêu; Đầu vào; Đầu ra (định dạng); Các bước; Checklist bắt buộc; Lỗi hay gặp; Ví dụ.
- Mỗi mục có nội dung thực chất, bỏ hết TODO(agent); Ví dụ có hai gạch `- Tốt:` và `- Xấu:` với tình huống/kết quả cụ thể.
- Lỗi hay gặp có `Nguồn:` trỏ `AGENTS.md` kèm tên mục/lỗi thật, hoặc `plans/R-010/brief.md` kèm mục. Không bịa nguồn hay chép dài.
- Đầu ra nêu định dạng, người nhận, trạng thái đạt/cần sửa/chưa xác minh.
- Checklist bắt buộc giữ các cụm từ hợp đồng dưới đây; có thể thêm giải thích. Test bảo vệ cụm chính sách, reviewer kiểm nghĩa toàn văn.
- Không có file giao CLI, tiến trình, dữ liệu người dùng hay singleton mới; các kiểm tra hợp đồng tương ứng không áp dụng.
- Test chỉ đọc sáu tài liệu nguồn của checkout, không đọc dữ liệu runtime AITeam hoặc USERPROFILE.

## Nội dung phải soạn
- **06-chuan-bi-task:** từ kế hoạch đã duyệt, dựng đặc tả + khung + test trước phần thân; nêu phạm vi và test bất biến. Checklist có `khung code`, `test nghiệm thu`, `NotImplementedException`, `TODO(agent)`, `LỖI: chưa làm`, `stderr`, `mã 1`. TODO(agent) được phép ở ví dụ mã khung, không dùng làm nội dung chờ viết. Main bắt exception khung, in lỗi và trả 1; đọc mẫu thật khi kế hoạch yêu cầu, thiếu mẫu ghi chưa xác minh.
- **07-code:** giữ phạm vi/hợp đồng, đọc skill theo stack nếu có, thiếu skill ghi nhận chứ không tự mở rộng task. Checklist có `phạm vi`, `Không sửa test nghiệm thu`, `Không tự merge`, `commit`, `chưa xác minh`, `Không báo pass khi chưa chạy`. Đầu ra bảng từng tiêu chí → lệnh/kết quả thật, file/lý do, quyết định và rủi ro, commit được kiểm.
- **08-kiem-thu:** cổng KT tự động kiểm BOM, phạm vi, test bất biến, báo cáo, commit/push theo quy trình được giao. Checklist có `dotnet build AITeam.sln`, `dotnet test AITeam.sln`, `0 warning`, `commit`, `Không báo pass khi chưa chạy`, `Không dùng build thay bằng chứng UI`. Báo lệnh, mã thoát, số test và commit; lỗi trả agent, không suy ra UI đạt.
- **09-qc:** review độc lập với coder, xem diff, bảo mật/hồi quy, đối chiếu yêu cầu và bằng chứng đúng commit. Checklist có `độc lập`, `commit`, `Blocker`, `Required`, `Optional`, `Không báo pass khi chưa chạy`, `Không dùng build thay bằng chứng UI`. Kết luận kèm vị trí/lý do/cách tái hiện, Optional vào hàng chờ; không trộn QC với KT hay nghiệm thu trực quan.
- **10-nghiem-thu:** Claude/người được giao kiểm trực quan ở bước 7 nếu có UI, coder không phải chạy app. Checklist có `trực quan`, `commit`, `TK`, `AG`, `CK`, `skill-note`, `-Skill`, `-Text`, `-Source`, `Không báo pass khi chưa chạy`, `Không dùng build thay bằng chứng UI`. Ví dụ cú pháp skill-note dùng nguồn thật, lỗi TK/AG/CK mới ghi vào skill tương ứng. Không có UI ghi không áp dụng; thiếu bằng chứng ghi chưa xác minh. Chỉ soạn hướng dẫn, không chạy lệnh này trong task.
- **11-publish-git:** chỉ người có quyền publish; checklist có `người có quyền`, `private`, `duyệt merge`, `clone sạch`, `build`, `test`, `push`, `commit`, `Không force-push main`, `Không báo pass khi chưa chạy`. Thứ tự: kiểm quyền/private → duyệt merge đúng commit → chuẩn bị bản merge → clone sạch bản merge vào nơi tạm → build/test đạt → push bản đã kiểm. Thiếu quyền, repo public, thiếu duyệt, kiểm lỗi hoặc commit đổi thì dừng, báo lý do, không push. Coder chỉ bàn giao nhánh, không tự merge. Không chứa bí mật/dữ liệu cá nhân trong ví dụ.

## Test nghiệm thu
`core.tests/AcceptanceS102SkillWorkflowTests.cs`

## Tiêu chí hoàn thành
- [ ] Sáu skill đủ bảy mục có nội dung, nguồn thật, ví dụ tốt/xấu; không còn placeholder.
- [ ] Chính sách riêng đúng Nội dung phải soạn, phân quyền và thứ tự publish rõ ràng.
- [ ] Từng file ≤150 dòng, tổng ≤360 dòng; không sửa ngoài phạm vi.
- [ ] `dotnet build AITeam.sln` thành công, 0 warning mới.
- [ ] `dotnet test AITeam.sln` pass, gồm 19 ca nghiệm thu không sửa.
- [ ] Báo cáo có bằng chứng thực chạy, commit được kiểm, điều chưa xác minh; không coi chưa chạy là đạt.

## Ghi chú chuẩn bị (Codex)
- Đã tạo sáu khung Markdown và 19 ca xUnit; chưa soạn nội dung skill, không thêm API ngoài kế hoạch.
- Build lần 1: `dotnet build AITeam.sln` trả 1, 0 warning/0 error, không nêu nguyên nhân.
- Build lần 2: `dotnet build AITeam.sln -m:1 -v:normal -nodeReuse:false` trả 1, 0 warning/1 error CS0030 ở test dòng 87.
- Đã sửa CS0030: TheoryData<string> duyệt ra string, truyền trực tiếp vào ReadSkill; chưa build lại vì giới hạn hai lần. Chưa xác nhận build thành công, chưa chạy test.
- Test kiểm cấu trúc/cụm chính sách; nghĩa nội dung, nguồn dẫn cụ thể và thứ tự publish cần reviewer kiểm độc lập sau khi agent hoàn thiện.
- Không chạy git theo yêu cầu; hệ thống tự commit. Các file tạo mới lưu UTF-8 BOM.

## Báo cáo của agent
<!-- Antigravity điền sau khi hoàn thiện:
| Tiêu chí | Bằng chứng (lệnh + kết quả thật) |
|---|---|
Ghi số test pass/fail, số warning, commit được kiểm; file đã đổi/lý do, quyết định, điều chưa xác minh.
-->


## Review (Claude)
<!-- Claude điền mỗi vòng: "### Vòng N — ĐẠT / CẦN SỬA", đã kiểm tra gì (kèm kết quả cổng kiểm tra), rồi:
**Blocker** / **Required**: file/vị trí · vấn đề · bằng chứng / cách tái hiện · hành vi mong muốn · test xác nhận.
**Optional**: ghi thành task riêng ở hàng chờ, không bắt sửa trong task này. -->

## Kết quả
<!-- Claude điền khi review ĐẠT, cho chủ dự án: 3–5 gạch đầu dòng — đã làm được gì (người dùng thấy gì), kiểm chứng ra sao, còn gì cần cải tiến thêm (đưa vào hàng chờ). -->

## Đánh giá quy trình
<!-- Claude điền ngay sau khi merge. Điểm 1–5 (5 = không vấn đề). Nguyên nhân: TK (task / thiết kế Claude) · AG (agent) · CK (công cụ / cổng kiểm tra) · NV (nghiệp vụ / chủ dự án) · HT (hạ tầng / provider). -->
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | | | | | | |
| 6. Review / QC | Cổng KT + reviewer | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
