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
<!-- Lập trình viên điền bảng tiêu chí → bằng chứng, file đổi/lý do, quyết định, điều chưa xác minh. -->

### Ghi nhận bước chuẩn bị (Codex, 26/10/2026)
| Điều kiện | Bằng chứng / trạng thái |
|---|---|
| Đặc tả và khung | WorkflowRoleDispatch, RunAssignedAsync, CLI WorkflowRole, hai helper PowerShell; phần thân mới đều throw NotImplementedException, chưa nối logic sản phẩm. |
| Test và mẫu thật | 68 ca (Theory mở rộng + Fact); AcceptanceH702WorkflowDispatchTests, Fixtures/H702; dùng TestShell, stub, không tiến trình agent thật trong test. |
| Kiểm tra tĩnh | PowerShell parser: 0 lỗi; 5 file mã đổi/thêm có UTF-8 BOM; fixture team.json khớp nguyên byte nguồn. |
| Build lần 1 | `dotnet build AITeam.sln`: mã 1; in 0 warning/0 error nhưng Build FAILED, chưa chứng minh biên dịch. |
| Build lần 2 | `dotnet build AITeam.sln -m:1 -nr:false`, CLI home cục bộ: mã 1, 0 warning, 9 lỗi NU1301; NuGet repository-signatures bị proxy 127.0.0.1:9 từ chối. Đã dừng ở giới hạn 2 lần. |
| Test | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --filter FullyQualifiedName~AcceptanceH702WorkflowDispatchTests --blame-hang-timeout 2m`: mã 0, không output/số ca. CHƯA xác minh discovery hoặc trạng thái đỏ; không tính là pass. |
| Model | Đặt claude-opus-4-6-thinking theo yêu cầu sửa team.ps1; agy models yêu cầu đăng nhập, mạng bị chặn nên chưa xác minh model. Quản lý kiểm tra trước giao; Opus hết lượt riêng thì chọn Gemini Argon High theo chỉ định. |

File chuẩn bị: task này (đặc tả), team.ps1 (khung helper), Program.cs (CLI khung), ReviewRunner.cs (QC cố định khung), WorkflowRoleDispatch.cs (hợp đồng), test và fixture (nghiệm thu bất biến).
Quyết định: giữ nguyên Agent B/nhánh ở đầu dù kế hoạch ghi A; thiếu mục hiểu theo H7-01; claude/gate/owner thủ công theo khả năng runtime hiện có. Không chạy git theo chỉ định phiên này.
Điều chưa chắc: thư mục tạm `.dotnet-h702` còn trong worktree vì kiểm duyệt tự động từ chối lệnh dọn với lý do “blocked by policy”; không đưa thư mục này vào commit. Build/test và model chưa xác minh; chưa thử Map lưu thật. Test AST không chứng minh toàn giao dịch prep/verify; agent bổ sung test tích hợp stub cho các luồng và CLI review trực tiếp. Chưa đủ điều kiện giao khi build chưa đạt.

## Review (Claude)
<!-- Claude điền. -->

## Kết quả
<!-- Claude điền sau review. -->

## Đánh giá quy trình
<!-- Claude điền sau nghiệm thu. -->