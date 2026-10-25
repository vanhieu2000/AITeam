# V8-03 · Gói nghiệm thu và quyết định một lệnh

- **Agent:** A
- **Nhánh:** `agent-a/V8-03-goi-nghiem-thu-va-quyet-dinh-mot-lenh`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~390 dòng
- **Yêu cầu:** R-008
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
- **Model:** `gemini-4-argon-high`

## Tóm tắt
- Có chức năng mới cho quản lý: một lệnh xem mọi task đang chờ nghiệm thu của phiếu.
- Hiện cổng kiểm tra, QC, từng lỗi còn mở, ngân sách và trang giao diện cần xem cùng nhau.
- Một lệnh ghi quyết định ĐẠT, bổ sung kết quả và số liệu có nguồn rồi chuyển sang luồng merge hiện có.
- Khi cần sửa, ghi lý do, lưu tài liệu rồi giao lại đúng task; tránh ghi thành công khi push/merge thất bại.

## Đọc (CHỈ những file này)
- `AGENTS.md`, `plans/R-008/brief.md`, file task này.
- `team.ps1`, `tools/workflow-accept.ps1`, `templates/codex-accept.md`, `tools/test-verify.ps1`.
- `tools/workflow-common.ps1`, `tools/workflow-pipeline.ps1`: tái dùng khóa, nhật ký và giao dịch; KHÔNG sửa.
- `core.tests/Support/TestShell.cs`, `core.tests/AcceptanceV803AcceptTests.cs`.
- Ba mẫu tại `tests/fixtures/V8-03/`: `projects.json`, `R-008-day-viec-khoi-claude-day-chuyen-tu-chay.md`, `20261021203105-DESKTOP-PP14EFS-HDVTool-T-005-ui-layout-3.json`.

## Bối cảnh
Làm theo brief R-008 mục 3–4; phụ thuộc V8-01, V8-02. Hiện `review` chỉ hiển thị từng task,
chọn log theo tiền tố và chưa có lệnh nghiệm thu. `Get-RunRecord` đã biết loại commit khung;
`Invoke-Verify` còn loại file task và test nghiệm thu: dùng đúng phép đếm của Verify.
`Invoke-WorkflowCodexLock`, `Invoke-RepoTransaction`, `Update-WorkflowJournal`,
`Invoke-Merge`, `Assert-MergeApproval`, `Invoke-Assign` có sẵn; tái dùng, không làm cơ chế thứ hai.

## Phạm vi (được sửa)
- `team.ps1`
- `tools/workflow-accept.ps1`
- `templates/codex-accept.md`
- `tools/test-verify.ps1`
- File task này: chỉ điền Báo cáo của agent.

## Không làm
- Không đổi Hub, hợp đồng model/DI, pipeline, thuật toán khóa hoặc cách xác định lỗi hạ tầng.
- Không đổi `tools/workflow-common.ps1`, `tools/workflow-pipeline.ps1`, `core.tests/Support/TestShell.cs`.
- Không sửa test nghiệm thu và fixture `tests/fixtures/V8-03/`; đây là tài liệu bất biến do quản lý chuẩn bị.
- Không gọi agy/codex thật trong test; không thay đổi máy, dữ liệu thật hoặc cấu hình người dùng.
- Không bypass approval, force-push, reset --hard, clean, ghi đè sửa có sẵn hoặc tự diễn giải thiếu số liệu thành 0.
- Không thêm tiêu chí chạy app/chụp ảnh; quản lý xem giao diện thật tại bước 7.

## Đặc tả lệnh và khung
1. `accept-pack -Request R-xxx [-Project X]`: không Project dùng AITeam như Resolve-Project hiện có.
   Kiểm ID Request theo hợp đồng pipeline. Chỉ đọc plan đã duyệt, bảng việc và nhánh/log sẵn có;
   không fetch hoặc ghi heartbeat. Gom task đúng Yêu cầu, đã có báo cáo, đang chờ review/nghiệm thu,
   chưa merge; không đưa task đang chạy/chưa có file vào gói. Theo thứ tự plan, không chọn theo tiền tố.
   Mỗi task: KT, QC và từng finding mở (ID/mức/nguồn), Added/Budget, trang UI, DependsOn.
   Không có task: `Không có task chờ nghiệm thu.`; dữ liệu thiếu/hỏng: `không rõ`.
   Mục tiêu ≤40 dòng; nếu số finding làm vượt thì in đủ và ghi lý do vượt 40, tuyệt đối không cắt mất lỗi.
2. `Get-WorkflowAcceptEvidence(TaskId,Head,LogsPath,MetricsPath)` trả hợp đồng ghi trong khung.
   Thêm envelope MỚI `<TaskId>.accept-<run>.json` tại r.Logs khi verify/QC hoàn tất trong team.ps1:
   `{task,project,head,run,verify:{ok,source},qc:{verdict,findings,source},metrics:{source}}`.
   `head` là SHA 40 hex thực tế lúc thực hiện, `run` số nguyên dương; source là đường dẫn tương đối
   dưới LogsPath (verify/QC) hoặc MetricsPath (metrics). Envelope là chỉ mục provenance mới, không
   phải định dạng log cũ. Chỉ liên kết hai kết quả cùng task/lượt/SHA; cập nhật chỉ mục an toàn dưới khóa.
   Không hồi tố gắn HEAD hiện tại cho log cũ. Source phải tồn tại/đọc được; thiếu/hỏng từng phần thì
   phần đó không rõ. Chọn run lớn nhất cùng SHA, không chọn file mới nhất theo LastWriteTime.
   Metrics phải khớp project, task mã hoặc mã-slug đúng ranh giới, run và head; giữ field cũ,
   chỉ thêm head lúc tạo metric mới trong team.ps1. Không sửa/di trú metrics lịch sử.
   Phút=durationSec/60, token=totalTokens; thiếu/âm/không hợp lệ thì không rõ. Score chỉ từ nguồn
   đánh giá đã tồn tại, không suy từ SUCCESS/PASS. Findings chuẩn hóa từ kết quả QC thực tế,
   gồm id/severity/open/classification; không đoán từ câu văn hay nuốt BLOCKED thành PASS.
3. `accept -Project X -Task T [-Optional "ID: lý do; ID: lý do"] [-Note "…"]`:
   lấy đúng nhánh, giữ SHA đầu; KT đạt + QC PASS hợp lệ trên chính SHA đó, có nguồn.
   Blocker mở luôn chặn; Required mở cần ID phân loại Optional do quản lý truyền cùng lý do.
   Optional không khớp ID/thiếu lý do không được dùng để bỏ hết lỗi. Thiếu bằng chứng báo đúng
   `Chưa đủ bằng chứng nghiệm thu.` trước gọi Codex/ghi file. Không tin classification AI tự gán.
4. Dưới khóa Codex chung, truyền template và dữ liệu bằng file UTF-8, tham số rời, không nội suy
   Optional/Note/Reject thành dòng lệnh hay script. Mặc định thời hạn Codex ≤25 phút; đọc stdout/stderr
   song song, timeout dừng cây tiến trình. Không giữ khóa repo trong khi gọi AI.
   Decision có Round=max vòng trong RIÊNG Review+1, Verdict, Optional, Note, Reject, Evidence.
   Chỉ thêm vòng Review và điền Kết quả/Đánh giá quy trình. Mọi phút/token/lượt/QC kèm nguồn;
   thiếu điểm/số liệu ghi không rõ. Escape Markdown của ghi chú để không chèn heading/mục giả.
5. Snapshot sạch trước gọi AI; sau gọi kiểm toàn bộ diff và byte ngoài ba mục cho phép bằng
   `Test-WorkflowAcceptDocuments`. Codex thêm file/sửa code/test/báo cáo/heading ⇒ báo đúng
   `Codex sửa ngoài mục nghiệm thu cho phép.`, không stage/commit/push thay đổi đó.
   Không tự hoàn tác file có sẵn của người dùng. Kiểm lại SHA dưới giao dịch trước publish;
   SHA đổi báo `Chưa đủ bằng chứng nghiệm thu.`. Chỉ stage file task đã kiểm.
6. Publish callback trả `{Ok,Head}` sau commit/push tài liệu nhánh task; kiểm exit code từng bước.
   Approval phải khớp SHA MỚI sau commit tài liệu; pending/rejected/sai SHA dừng, phải xin duyệt lại,
   không chuyển approval cũ sang SHA mới. Sau duyệt gọi merge hiện có. Chỉ ghi nhật ký Merge ĐẠT/
   bảng ✅/thông báo thành công sau merge VÀ push thành công. Trong phạm vi team.ps1 cần sửa thứ tự
   kiểm lỗi của Invoke-Merge/Set-BoardStatus khi cần; giữ hành vi lệnh cũ và không thay giao thức approval.
   Publish/Merge false hoặc approval không đủ ⇒ `Không hoàn tất nghiệm thu.` (có thể kèm chi tiết ở stderr).
7. `accept … -Reject "lý do"`: không cần KT đạt, ghi `### Vòng N — CẦN SỬA`, commit/push rồi mới
   gọi assign cùng task với `-Resume -Feedback` giữ nguyên lý do tiếng Việt. Không đi qua approval/merge.
   Đã merge: trả ALREADY_MERGED trước callback, gọi lặp không thêm vòng hay merge lại.
   Mọi lần thất bại giữ trạng thái để chạy lại không tạo vòng trùng cho cùng quyết định/SHA.
8. Thân hàm khung hiện ném NotImplementedException; chỉ điền phần thân và wiring trong phạm vi.
   CLI catch hiện có phải in `LỖI: chưa làm`, exit 1 khi khung chưa triển khai; bắt cả lỗi khác.

## Ràng buộc
- PowerShell 5.1; `.ps1` UTF-8 BOM; bảo toàn BOM/CRLF của tài liệu. Không so BOM theo văn hóa.
- Chuẩn hóa đường dẫn, chặn .., tuyệt đối và reparse point ở file/các thư mục cha trước đọc/ghi nguồn.
- Tên task/request không dùng wildcard từ dữ liệu ngoài. Không ghi dữ liệu bí mật hoặc prompt chứa khóa.
- Giao dịch repo phải kiểm SHA trước ghi, không khóa repo trong thời gian gọi AI/đợi approval.
- Test chạy tiến trình chỉ qua TestShell.PowerShell/TestShell.Run/TempRepo. Chạy xUnit từ
  `tools/test-verify.ps1 -AcceptOnly`; lượt chạy đầy đủ cũng phải bao gồm lớp nghiệm thu mới.
- Không đổi hành vi lệnh cũ ngoài bổ sung metadata có nguồn và sửa kiểm lỗi merge/push bắt buộc.

## Dữ liệu mẫu thật
- Đã mở projects.json gốc: MẢNG ở gốc; bản sao giữ byte tại `tests/fixtures/V8-03/projects.json`.
- Đã mở phiếu `requests/R-008-day-viec-khoi-claude-day-chuyen-tu-chay.md`: tiêu đề `## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)`;
  bản sao cùng tên/BOM/CRLF tại `tests/fixtures/V8-03/`.
- Metrics thật: `20261021203105-DESKTOP-PP14EFS-HDVTool-T-005-ui-layout-3.json`: run dạng chuỗi,
  durationSec=7645, totalTokens=1642151, chưa có SHA. Test sao byte gốc; ca dữ liệu mới thêm head
  trong THƯ MỤC TẠM để kiểm liên kết, không coi mẫu cũ là bằng chứng hiện tại.
- Không đọc log máy thật theo giới hạn đề bài. Source KT/QC giả trong test chỉ kiểm hợp đồng envelope
  MỚI; adapter phải lấy từ kết quả thật của Invoke-Verify/Invoke-AutoReview, không đoán định dạng log cũ.

## Test nghiệm thu
`core.tests/AcceptanceV803AcceptTests.cs`

## Tiêu chí hoàn thành
- [ ] `dotnet build AITeam.sln` 0 lỗi, 0 warning mới; `dotnet test AITeam.sln` pass toàn bộ.
- [ ] `powershell -NoProfile -File tools/test-verify.ps1 -AcceptOnly` pass; không đổi test/fixture.
- [ ] Gói gom đủ task/finding, rỗng rõ ràng, không ghi file; đúng SHA/lượt/mã, dữ liệu cũ/hỏng không rõ.
- [ ] Added loại khung và test nghiệm thu đúng Verify; UI nhận diện trang cụ thể hoặc không rõ, có phụ thuộc.
- [ ] ĐẠT/reject đúng luồng; ký tự đặc biệt/tiếng Việt nguyên vẹn; không sửa ngoài ba mục; dùng khóa Codex chung.
- [ ] Approval giữ SHA mới; pending/từ chối/sai SHA không merge; push/merge lỗi không ghi thành công; đã merge gọi lặp không đổi.
- [ ] Mọi số liệu và điểm có nguồn hoặc không rõ; không nhận SUCCESS/PASS là điểm đánh giá.
- [ ] Báo cáo từng tiêu chí → bằng chứng thật, file đổi/lý do, quyết định tự đưa ra, điều chưa xác minh.

## Điều chưa chắc khi chuẩn bị
- `agy models` đã thử nhưng exit 1 vì chưa đăng nhập và mạng bị chặn; giữ model theo kế hoạch,
  chưa xác minh được mã còn khả dụng trên tài khoản này.
- Chưa có log KT/QC mẫu gắn SHA; hợp đồng envelope mới ở trên phải do writer tạo từ kết quả thực,
  không cho phép đoán SHA của log cũ. Không kiểm provider AI thật trong test.
- Ngân sách ~390 dòng là phần agent thêm, không tính khung/test/fixture chuẩn bị; nếu cần vượt ghi lý do.
- Kiểm tra khóa thật, adapter git và tình huống reparse point cần agent bổ sung test cô lập qua TestShell;
  không được coi các callback giả là bằng chứng cho tích hợp thật.
## Kiểm tra chuẩn bị (Codex)
| Điều kiện | Bằng chứng thật |
|---|---|
| Khung, wiring CLI, template | Các hàm nghiệp vụ chỉ ném NotImplementedException; catch CLI hiện có trả LỖI: chưa làm, mã 1. Không chạy git. |
| Build lần 1 | `dotnet build AITeam.sln`: exit 1, 0 warning/0 error được in, không có chẩn đoán; chưa coi là đạt. |
| Build lần 2 | `dotnet build AITeam.sln -m:1 -v:normal`: exit 0, 0 error, 8 warning NU1900 do mạng NuGet; không warning C#. Đã dùng đủ 2 lượt build. |
| Test đỏ | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --filter FullyQualifiedName~AcceptanceV803AcceptTests --blame-hang-timeout 2m`: exit 1; 41 failed, 0 passed, 0 skipped; 1 phút 16 giây. Lỗi khung NotImplementedException. |
| PowerShell và mã hóa | Parser: team.ps1, workflow-accept.ps1, test-verify.ps1 đều 0 lỗi. Ba script và file test C# đều UTF-8 BOM. |
| Dữ liệu thật | Ba fixture bằng byte với nguồn đã đọc; không tự dựng lại projects/phiếu/metrics cũ. |
| Chưa xác minh | Không chạy toàn bộ solution test; chưa xác minh model vì agy models yêu cầu đăng nhập; chưa kiểm tích hợp provider thật. |
## Báo cáo của agent
| Tiêu chí | Bằng chứng thật |
|---|---|
| Build/test toàn solution | `dotnet build AITeam.sln --no-restore -m:1 --nologo`: 0 lỗi, 0 warning; `dotnet test AITeam.sln --no-build --no-restore -m:1 --blame-hang-timeout 2m --nologo`: Hub 534 pass/9 fail, Core chưa có tổng kết, đã gửi ngắt sau 3 phút. |
| Test nghiệm thu bất biến | `powershell -NoProfile -File tools/test-verify.ps1 -AcceptOnly`: bản cuối 41 pass/0 fail/0 skip (1 phút 16 giây); không sửa test/fixture. |
| Gói, finding, SHA/lượt/mã, dữ liệu hỏng | Các ca Pack/Evidence trong 41 test đạt; adapter gom theo plan, nguồn mới gắn SHA, dữ liệu cũ không rõ; tích hợp git thật chưa xác minh. |
| Added, UI, phụ thuộc | Các ca Line_count/Pack trong 41 test đạt; adapter lấy seed như Verify, loại task/test nghiệm thu. |
| ĐẠT/reject, tiếng Việt, ba mục, khóa | Các ca Accept/Reject/Documents/Lock trong 41 test đạt; gọi Codex hữu hạn, dữ liệu UTF-8 qua file; chưa gọi AI thật. |
| Approval/push/merge/lặp | Các ca Approval/Already_merged/Head_changed trong 41 test đạt; thêm trạng thái publish chờ để tiếp tục, kiểm SHA mới và mã thoát push trước thành công; adapter thật chưa xác minh. |
| Số liệu có nguồn | Các ca Bound_evidence/Unsafe_metric trong 41 test đạt; phút/token kiểm project/task/run/head, điểm chưa có nguồn ghi không rõ. |
| Báo cáo | Bảng này ghi đủ tiêu chí, kết quả thực và giới hạn; chưa có commit được kiểm. |
File đổi: `team.ps1` (adapter/provenance/kiểm lỗi merge), `tools/workflow-accept.ps1` (điền 5 hàm), `templates/codex-accept.md` (escape/nguồn), file task (báo cáo); giữ BOM script và CRLF.
Quyết định: không chạy git theo chỉ dẫn Codex; hệ thống commit/push. Restore dùng `-m:1 --disable-parallel -p:NuGetAudit=false --ignore-failed-sources` vì NuGet mạng lỗi; không sửa cấu hình.
Chưa chắc: toàn bộ solution chưa đạt; 9 lỗi Hub có UnauthorizedAccessException tại File.Replace; chưa bổ sung test tích hợp git/khóa thật/reparse point qua TestShell; không có MCP aiteam_post_message. Lệnh taskkill testhost bị sandbox từ chối.
## Review (Claude)
<!-- Claude điền mỗi vòng: "### Vòng N — ĐẠT / CẦN SỬA", đã kiểm tra gì (kèm kết quả cổng kiểm tra), rồi:
**Blocker** / **Required**: file/vị trí · vấn đề · bằng chứng / cách tái hiện · hành vi mong muốn · test xác nhận.
**Optional**: ghi thành task riêng ở hàng chờ, không bắt sửa trong task này. -->

## Kết quả
<!-- Claude điền khi review ĐẠT, cho chủ dự án: 3–5 gạch đầu dòng — đã làm được gì (người dùng thấy gì), kiểm chứng ra sao, còn gì cần cải tiến thêm (đưa vào hàng chờ). -->

## Đánh giá quy trình
<!-- Claude điền ngay sau khi merge. Điểm 1–5 (5 = không vấn đề). Nguyên nhân: TK (task / thiết kế Claude) · AG (agent) · CK (công cụ / cổng kiểm tra) · NV (nghiệp vụ / chủ dự án) · HT (hạ tầng / provider). -->
<!-- Bài học kinh nghiệm: Với lỗi mới phát hiện (nguyên nhân TK / AG / CK), chọn skill đúng bước để cập nhật.
     Đối chiếu mục Lỗi hay gặp của skill, lỗi trùng không ghi lại.
     Chỉ ghi lỗi mới đã đối chiếu skill bằng lệnh (thay mã task thực tế, không chạy trực tiếp lệnh từ template):
     team.ps1 skill-note -Skill code-test -Text "Mô tả bài học mới" -Source S1-07 -->
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | | | | | | |
| 6. Review / QC | Cổng KT + reviewer | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
