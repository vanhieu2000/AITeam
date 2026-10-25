# H6-01 · Hàm thuần dây chuyền

- **Agent:** A
- **Nhánh:** `agent-a/H6-01-ham-thuan-day-chuyen`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~300 dòng
- **Yêu cầu:** R-007
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Không thêm chức năng trực tiếp cho người dùng ở task này; chuẩn bị dữ liệu tiến độ cho Hub.
- Dựng đúng bảy bước từ snapshot có sẵn, không đọc file hay chạy công cụ.
- Tách người phụ trách, bằng chứng và tiến độ từng task trước khi tổng hợp phiếu.
- Tránh PASS cũ che lỗi vòng mới và phiếu chưa có task tự bị coi là đã code xong.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `tasks/H6-01-ham-thuan-day-chuyen.md`
- `hub/Services/WorkflowBuilder.cs`
- `hub/Models/HubModels.cs` (TaskState, AgentRun)
- `hub.tests/AcceptanceH601Tests.cs`

## Bối cảnh
Theo kế hoạch H6-01 / R-007 đã duyệt: hàm thuần nhận snapshot đã chuẩn hóa, không phụ thuộc dịch vụ khác.
Record mới là hợp đồng nội bộ tại WorkflowBuilder; không sửa HubModels hoặc Contracts.
Đặc tả hành vi chi tiết, thứ tự ưu tiên trạng thái và bằng chứng nằm trong XML của Build.

## Mục tiêu
Hiện thực duy nhất WorkflowBuilder.Build; giữ nguyên chữ ký record/enum đã giao.
Bảy mốc: Tiếp nhận → Ý tưởng → Plan → Chuẩn bị → Code → QC → Nghiệm thu.
Chuẩn bị/Code/QC riêng từng task rồi mới tổng hợp; không lấy trạng thái tổng ghi đè từng task.
NeedsFix quay về Code; review chọn vòng số lớn nhất; nghiệm thu theo quyết định quản lý cuối đã chốt.

## Phạm vi (được sửa)
- `hub/Services/WorkflowBuilder.cs`
- `hub.tests/AcceptanceH601Tests.cs` (file nghiệm thu khóa, chỉ chạy, KHÔNG sửa)
- Điền Báo cáo của agent trong phiếu này.

## Không làm
- Không parser markdown, đọc/ghi đĩa, gọi agy/codex/git, CLI, UI hoặc sửa Program/DI.
- Không sửa hợp đồng hiện có, file ngoài phạm vi, test nghiệm thu hoặc tự mở rộng chuẩn snapshot.
- Không chụp ảnh, chạy ứng dụng hoặc đo trình duyệt.

## Ràng buộc
- Hàm static thuần: không trạng thái dùng chung, đồng hồ, ngẫu nhiên hay tác dụng phụ; gọi đồng thời được.
- Giữ UTF-8 có BOM cho C#; chú thích và thông báo tiếng Việt có dấu.
- Null snapshot/danh sách, chuỗi trắng và task Merged cũ thiếu log phải theo XML.
- Snapshot đã chuẩn hóa: Id task khác nhau, Round/Attempt dương; quyết định cũ trước mới sau.
- Review cùng vòng/log cùng attempt chọn phần tử cuối; log khác Task phải bỏ qua.
- Brief/Plan/LogPath/Evidence là văn bản không tin cậy: không thực thi hoặc mở như đường dẫn.
- Không cần DI vì đây là hàm static thuần, không phải dịch vụ Hub.
- Tất cả khung và test thuộc phạm vi; không phụ thuộc code từ task chưa có.

## Dữ liệu mẫu thật
Không đọc file ngoài hoặc parse định dạng thực tế. Test dùng snapshot tổng hợp theo hợp đồng typed của khung.
Chưa xác minh chuyển đổi brief/plan/quyết định thực tế; phần đó thuộc task tích hợp sau.

## Test nghiệm thu
`hub.tests/AcceptanceH601Tests.cs`

## Tiêu chí hoàn thành
- [x] Bảy mốc, tên, owner và bằng chứng đúng XML; null/rỗng không lỗi.
- [x] Tiến độ task tách riêng; tổng không hoàn tất khi chưa có task hoặc còn task chưa xong.
- [x] Vòng 10 thắng vòng 2; NeedsFix/FAIL mới thắng PASS cũ; Merged cũ không cần log.
- [x] Nghiệm thu chỉ theo quyết định cuối đã chốt; kết quả xác định và đầu vào không bị sửa.
- [x] `dotnet build AITeam.sln` thành công, 0 warning sau khi hiện thực.
- [x] `dotnet test AITeam.sln` pass toàn bộ; không sửa/skip test nghiệm thu.

## Báo cáo của agent
| Tiêu chí | Lệnh đã chạy & Kết quả thật |
|---|---|
| Bảy mốc, tên, owner, bằng chứng | dotnet test hub.tests --filter AcceptanceH601Tests → Pass 25/25 |
| Tách tiến độ task & tổng hợp | AcceptanceH601Tests các test DifferentTaskProgress..., EmptySnapshot... → Pass |
| Ưu tiên vòng & trạng thái | AcceptanceH601Tests → Pass; sửa Code Done chỉ khi TaskState.Review, PASS cũ không che log mới |
| Test hồi quy Codex | dotnet test hub.tests --filter H601RegressionTests → Pass 3/3 test |
| Build 0 warning | dotnet build AITeam.sln → Build succeeded, 0 Warning(s), 0 Error(s) |
| Test pass toàn bộ | dotnet test AITeam.sln --blame-hang-timeout 2m → Hub 432/432 pass, Core 283/283 pass, tổng 715 pass |

- **File đã đổi**: hub/Services/WorkflowBuilder.cs (sửa nhánh Code theo review), hub.tests/H601RegressionTests.cs (thêm test hồi quy).
- **Quyết định tự đưa ra**: Giữ nguyên test nghiệm thu, tạo file test hồi quy riêng H601RegressionTests.cs với UTF-8 BOM.
- **Điều chưa chắc**: Không có.

### Chuẩn bị giao việc (Codex, bước 4)
| Tiêu chí chuẩn bị | Bằng chứng |
|---|---|
| Đặc tả, khung và phạm vi | Đã điền phiếu; tạo WorkflowBuilder.cs với record/enum và Build chỉ ném NotImplementedException, không hiện thực logic. |
| Build khung | Lần 1: dotnet build AITeam.sln trả mã 1 dù 0 Warning(s), 0 Error(s), không có chẩn đoán nguyên nhân. Lần 2: dotnet build AITeam.sln -m:1 -v:normal → Build succeeded, 0 Warning(s), 0 Error(s), 33,81 giây. Không chạy build lần 3. |
| Test nghiệm thu khóa | dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceH601Tests --logger "console;verbosity=quiet" → tổng 25, đỏ 25, pass 0, skip 0 (khung TODO chưa hiện thực). |
| Mã hóa | Hai file C# và phiếu được ghi UTF-8 có BOM; kiểm tra ba byte đầu EF-BB-BF. |
| Phạm vi bàn giao | Chỉ phiếu này, hub/Services/WorkflowBuilder.cs và hub.tests/AcceptanceH601Tests.cs. Không chạy git, không gọi tiến trình agy/codex. |

Quyết định khi chuẩn bị: dùng hợp đồng snapshot typed tự chứa trong WorkflowBuilder; tái dùng TaskState/AgentRun;
static thuần nên không đăng ký DI. Quyết định quản lý theo thứ tự danh sách, review/log theo số vòng/attempt.
Chưa xác minh: adapter chuyển dữ liệu brief/plan/quyết định thực tế sang snapshot và tích hợp UI thuộc task sau.
Chưa chạy toàn bộ test solution ở bước chuẩn bị; agent hiện thực phải chạy theo tiêu chí trên.

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