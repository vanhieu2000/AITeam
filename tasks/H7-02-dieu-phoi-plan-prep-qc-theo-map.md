# H7-02 · Điều phối plan/prep/QC theo Map

- **Agent:** B
- **Nhánh:** `agent-b/H7-02-dieu-phoi-plan-prep-qc-theo-map`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~350 dòng
- **Yêu cầu:** R-009
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
- **Model:** `claude-opus-4-6-thinking`

## Tóm tắt
- Có chức năng mới: plan, chuẩn bị task và QC dùng người quản lý đã chọn trên Map.
- Cấu hình cũ giữ cách chạy hiện tại; không ghi lại team.json.
- QC chặn tự duyệt, không tự chuyển người khi người được giao gặp lỗi.
- Vai cần thao tác thủ công thì dừng và báo rõ.

## Đọc (CHỈ những file này)
- `AGENTS.md`, `plans/R-009/brief.md` mục 6, task này.
- `team.ps1`, `core/AITeam.Cli/Program.cs`, `core/AITeam.Core/Review/`.
- `hub/Services/WorkflowRolesService.cs` (hợp đồng H7-01, chỉ đọc).
- `core/AITeam.Core/Providers/ProviderConfig.cs`, `core/AITeam.Core/Providers/ProviderModels.cs`, `core/AITeam.Core/Routing/RoutingModels.cs`, `core/AITeam.Core/Routing/Routing.cs`.
- `core.tests/Support/TestShell.cs`, `core.tests/AcceptanceH702WorkflowDispatchTests.cs`, `core.tests/Fixtures/H702/`.

## Bối cảnh
Theo brief R-009 mục 6; H7-01 đã có trong WorkflowRolesService. Hiện plan/prep ghi cứng codex, QC dùng router có fallback. Core không tham chiếu Hub. Khung chưa nối vào các luồng: phải nối thật, không chỉ hoàn thiện helper.

## Mục tiêu
- Hoàn thiện WorkflowRoleDispatch.Resolve và WorkflowAssignment đúng XML doc.
- CLI `workflow-role --root <d> --role plan|prep|qc`: stdout duy nhất JSON camelCase gồm role, actor, providerId, isExplicit, readOnly, manual; mã 0. Sai role: stderr đúng XML doc, mã 2. Main bắt mọi ngoại lệ; khung chưa làm in “LỖI: chưa làm”, mã 1.
- Hoàn thiện Resolve-WorkflowAssignment và Invoke-WorkflowRoleRun; nối Invoke-Plan, Invoke-Prep, Invoke-AutoReview. Invoke-Qc riêng đang gọi AutoReview nên phải dùng cùng quyết định.
- Hoàn thiện ReviewRunner.RunAssignedAsync. CLI review nhận `--reviewer-provider <id>` nội bộ và đối chiếu với Map; CLI review trực tiếp cũng đọc Map dù không có tùy chọn này. Không dùng tham số để lách Map. Kiểm cấu hình trước nhánh --auto SKIPPED.

## Phạm vi (được sửa)
- `team.ps1`
- `core/AITeam.Cli/Program.cs`
- `core/AITeam.Core/Review/`
- `tasks/H7-02-dieu-phoi-plan-prep-qc-theo-map.md` (chỉ Báo cáo của agent).

## Không làm
- Không đổi Hub/DI, schema H7-01, team.json/team.local.json/projects.json, tools/, skill/, người được giao code, cơ chế khóa hoặc giao dịch git.
- Không sửa/xóa/Skip test nghiệm thu và fixture; các file này ngoài phạm vi triển khai.
- Không đổi hợp đồng run/review, nhật ký, pipeline, verify, tên log, review policy hoặc các bước khác.
- Không chạy git/agy/codex thật trong test; không viết harness tiến trình riêng.

## Ràng buộc
- Schema H7-01: workflow object, roles MẢNG đúng 11 object {code,actor,note}; phân biệt hoa thường. Code: receive, idea, research, plan, approve_plan, prep, code, gate, qc, accept, publish. Actor: claude, codex, minh, lan, gate, owner. Note chuỗi không null, tối đa 500 UTF-16, không thực thi.
- Thiếu file/workflow/roles: plan/prep codex; QC giữ router cũ và fallback cũ. Có roles nhưng thiếu/trùng dòng, null/sai kiểu/giá trị lạ là cấu hình sai; không fallback. “Thiếu mục” được hiểu theo H7-01, không chấp nhận mảng phân vai dở dang.
- Ánh xạ cố định: codex→codex, minh/lan→agy. Claude/gate/owner cần thủ công vì runtime hiện chưa hỗ trợ chạy tự động danh tính đó. Dừng chính xác “Tác nhân được gán cần thực hiện thủ công.”, mã khác 0; không tự chọn người khác.
- Parser báo chính xác theo XML doc: “Phân vai không hợp lệ.” / “Không đọc được cấu hình phân vai.” / “Vai trò không được hỗ trợ.”. CLI thêm tiền tố LỖI, mã khác 0. Không phản chiếu payload ngoài vào lỗi.
- Đọc UTF-8 có/không BOM; chặn reparse point của file và mọi tổ tiên. Không ghi team.json khi thành công/lỗi/thiếu file. Không dùng actor/note làm đường dẫn hoặc shell-eval/Invoke-Expression.
- Chọn vai TRƯỚC tác dụng phụ của plan/prep/QC: lỗi/thủ công không tạo worktree, prompt, git fetch, commit hay gọi provider. Chỉ thay đoạn chọn/chạy provider, giữ giao dịch prep hiện có.
- Plan luôn --read-only; prep Develop; QC dùng ReviewService ReadOnly, giữ schema, SHA-256, OutcomeJson. Giữ timeout hiện tại: plan/prep 20 phút, QC mặc định 15 phút; không thêm retry.
- Invoke-WorkflowRoleRun nhận mảng Arguments, giữ nguyên đường dẫn có dấu/khoảng trắng, --out/--root/timeout/LASTEXITCODE. Run thêm --provider; review thêm --reviewer-provider chỉ khi IsExplicit. Chỉ bọc khóa chung Codex khi ProviderId=codex; agy/thủ công không lấy khóa này.
- QC đã gán: provider thiếu/tắt/không có vai Reviewer → ExitConfig 2; tự duyệt (so không phân biệt hoa thường)/đang nghỉ/thiếu sandbox chỉ đọc/lỗi/quota/timeout → Blocked 3. Chặn trước factory nếu biết không hợp lệ. Chỉ một lần chạy; không router/fallback. Giữ ghi state/kết quả như cũ.
- QC tự động và riêng giữ --dev thật; Minh/Lan cùng provider agy vẫn bị chặn tự duyệt. Cấu hình hợp lệ không cần review giữ --auto SKIPPED; cấu hình sai không được SKIPPED thành công.
- Không để NotImplementedException văng khỏi Main/entrypoint PowerShell; giữ catch hiện có. Khung trong phạm vi; không cần DI vì không thêm dịch vụ Hub. `.cs`/`.ps1` UTF-8 BOM.
- Test chỉ dùng TempRepo(false), TestShell.PowerShell/Run và stub, không dữ liệu máy thật. Test AST chứng minh wiring, không thay bằng chứng giao dịch tích hợp.

## Dữ liệu mẫu thật
- `core.tests/Fixtures/H702/team.json`: sao nguyên byte từ team.json của worktree, giữ BOM/xuống dòng, cấu hình cũ chưa có workflow.roles. Các biến thể Map sửa trên bản sao theo schema H7-01.
- `core.tests/Fixtures/H702/H7-02-dieu-phoi-plan-prep-qc-theo-map.md`: bản sao task thật lần chuẩn bị này, dùng cho ReviewService; không đọc phiếu yêu cầu/projects.json trong task này.
- Chưa có mẫu team.json đã lưu thật từ Map; schema đối chiếu trực tiếp H7-01, chưa xác minh trên máy thật.

## Test nghiệm thu
`core.tests/AcceptanceH702WorkflowDispatchTests.cs`

## Tiêu chí hoàn thành
- [ ] Nối cả plan/prep/AutoReview/QC riêng và CLI review trực tiếp; chỉ ba vai plan/prep/qc thay đổi.
- [ ] Mặc định cũ, từng ánh xạ, thủ công, cấu hình sai, không ghi file và chặn reparse point đúng đặc tả.
- [ ] Stub nhận đúng provider/cờ chỉ đọc/khóa Codex; tham số có dấu/khoảng trắng được giữ nguyên.
- [ ] QC đã gán chặn tự duyệt/provider thiếu/tắt/sai vai/đang nghỉ, không fallback khi quota/timeout/lỗi; JSON/hash giữ nguyên.
- [ ] `dotnet build AITeam.sln` 0 lỗi/0 warning; `dotnet test AITeam.sln` pass hết; không sửa test nghiệm thu.
- [ ] Báo cáo bảng từng tiêu chí → lệnh/kết quả thật, file đổi/lý do, quyết định và điều chưa xác minh.

## Báo cáo của agent
B = `dotnet build AITeam.sln -warnaserror -m:1 -nr:false`; T = `dotnet test AITeam.sln --blame-hang-timeout 3m --no-build --no-restore -m:1 -nr:false`.
F = `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~AcceptanceS104SkillPromptsTests|FullyQualifiedName~AcceptanceH702WorkflowDispatchTests" --blame-hang-timeout 3m -m:1 -nr:false`.
| Tiêu chí | Bằng chứng lượt Codex 02/11/2026 |
|---|---|
| Nối plan/prep/AutoReview/QC/CLI | F: 91/91 pass, 0 skip (H7-02 và toàn bộ S1-04); không đổi mã sản phẩm. |
| Mặc định/ánh xạ/thủ công/schema/reparse/không ghi | F pass; test H7-02 và fixture giữ nguyên. |
| Provider/cờ/khóa/tham số | F pass; harness S1-04 nạp hai hàm thật, giả workflow-role và khóa, không dùng khóa máy. |
| QC chặn tự duyệt/cấu hình/lỗi, không fallback | F pass; không đổi logic QC. |
| Build/test toàn solution | B: 0 lỗi/0 warning; T: Hub 712 pass/9 fail, Core 753 pass/5 fail, 0 skip; chưa đạt toàn xanh. |
| Kiểm tra bổ sung | `powershell -NoProfile -ExecutionPolicy Bypass -File tools\test-verify.ps1`: fail ở git clone (signal pipe, Win32 error 5); `git diff --check` sạch; BOM S1-04 còn nguyên. |
Ngoại lệ harness: `core.tests/AcceptanceS104SkillPromptsTests.cs:168,199–207` thiếu hàm AST và stub workflow-role/khóa; ban đầu ProviderGiaNhanPromptThat_MoiNhanh(plan/prep) 2 fail/3 pass: “Resolve-WorkflowAssignment is not recognized”; sửa dựng môi trường tối thiểu 8 dòng thêm/1 dòng đổi, không sửa assert. File task chỉ đổi báo cáo.
Còn đỏ: V805SafeMergeTests.Merge_checks_local_clone_before_push_and_rolls_back_failure (2), V808AutoCommitTests.Merge_commits_only_allowed_changes_and_blocks_dirty_code (2), Board_inserts_missing_task_once_then_updates_status (1): Git signal pipe/CreateFileMapping Win32 error 5; Hub 9 lỗi File.Replace Access denied/lưu Map không thành công; không sửa ngoài phạm vi.
Quyết định: restore tuần tự khắc phục thiếu assets; build/test dùng một MSBuild; lượt build trùng test từng bị khóa DLL, build cuối đã sạch. Không sửa test lần hai; không chạy app/browser, không push.
Chưa xác minh main mới: fetch bị Permission denied FETCH_HEAD; merge bị ORIG_HEAD.lock; add/commit bị index.lock. Không có commit mới; HEAD acbece9, bản sửa còn trong worktree; chưa kiểm trên main đã cập nhật/máy thật.
## Review (Claude)
<!-- Claude điền. -->

## Kết quả
<!-- Claude điền sau review. -->

## Đánh giá quy trình
<!-- Claude điền sau nghiệm thu. -->