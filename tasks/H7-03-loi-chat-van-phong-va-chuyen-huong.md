# H7-03 · Lõi chat Văn phòng và chuyển hướng

- **Agent:** B
- **Nhánh:** `agent-b/H7-03-loi-chat-van-phong-va-chuyen-huong`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~220 dòng
- **Yêu cầu:** R-009
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->


## Tóm tắt
- Thêm lõi gửi tin cho Chat giao việc ở Văn phòng: gửi yêu cầu mới hoặc bổ sung phiếu đã có.
- Mọi tin được đưa vào hộp thư hiện có để Claude xử lý; Hub không tự tạo hay sửa phiếu.
- Chặn nội dung trống, quá dài, dự án lạ và mã phiếu giả mạo trước khi gửi.
- Địa chỉ cũ `/Agents`, `/Inbox` chuyển về Văn phòng bằng HTTP 302, không chạy thao tác cũ.

## Đọc (CHỈ những file này)
- `AGENTS.md`
- `plans/R-009/brief.md` mục 1, 4 và ràng buộc.
- `hub/Services/OfficeChatService.cs`
- `hub/Program.cs`
- `hub/Services/Contracts.cs`
- `hub/Models/HubModels.cs`
- `hub/Services/RequestService.cs`
- `hub/Services/InboxService.cs`
- `hub.tests/AcceptanceH703OfficeChatTests.cs`

## Bối cảnh
Theo brief R-009, Chat giao việc được đưa về Văn phòng. IRequestService chỉ đọc phiếu;
IInboxService.Add là đường ghi yêu cầu duy nhất. Task này chỉ làm dịch vụ và chuyển hướng,
không dựng giao diện. Giữ Agent/Nhánh theo đầu phiếu đã cấp, dù kế hoạch gọi vai trò A.

## Mục tiêu
Hiện thực IOfficeChatService.Send và OfficeChatRedirectMiddleware.InvokeAsync trong khung.
Program đã đăng ký singleton và đặt middleware trước MapRazorPages; giữ cách nối này.

## Phạm vi (được sửa)
- `hub/Services/OfficeChatService.cs`
- `hub/Program.cs`
- `tasks/H7-03-loi-chat-van-phong-va-chuyen-huong.md` — chỉ cập nhật báo cáo và tiêu chí.

## Không làm
- Không sửa `hub.tests/AcceptanceH703OfficeChatTests.cs` (test nghiệm thu đã chuẩn bị).
- Không sửa Contracts.cs, HubModels.cs, RequestService, InboxService hay trang/handler cũ.
- Không làm form chat, menu, Map, CSS, CLI; không gọi agy/codex hay tiến trình ngoài.
- Không đổi cấu hình, trạng thái, định dạng file hay ghi trực tiếp vào requests/inbox.
- Không merge hoặc push main. Trong phiên chuẩn bị này không chạy git; hệ thống tự commit.

## Ràng buộc
- Giữ chữ ký và hợp đồng XML trong khung. Chỉ giữ trạng thái cục bộ mỗi lần gửi để singleton không lẫn tin.
- Send kiểm tra theo thứ tự: nội dung → dự án → phiếu → Add đúng một lần.
- text null/rỗng/toàn khoảng trắng: ArgumentException với Message chính xác `Nội dung yêu cầu không được để trống.`
- Độ dài text GỐC > 4000 (kể cả khoảng trắng): `Nội dung quá dài (tối đa 4000 ký tự).`
  Sau kiểm tra mới Trim để gửi; tiền tố bổ sung không tính vào giới hạn. Giữ nguyên xuống dòng bên trong để InboxService xử lý.
- Dùng FindProject(project); null: `Dự án không hợp lệ.`; gửi ProjectInfo.Name đã xác minh.
  Không bắt buộc ExistsLocally vì gửi inbox không cần thư mục dự án trên máy.
- requestId null/chuỗi rỗng là yêu cầu mới. Còn lại chỉ nhận toàn bộ `R-[0-9]{3}`,
  phân biệt hoa thường, không trim; chặn cả xuống dòng cuối, Unicode digit, tên file, đường dẫn tuyệt đối và traversal TRƯỚC Get.
- Bổ sung phải Get(requestId), kết quả không null và Id bằng requestId theo Ordinal;
  sai/mất/giả mạo: ArgumentException với Message chính xác `Phiếu không hợp lệ.` Không đọc GetAll.
- Quy ước chuẩn bị được chốt cho triển khai trong phiếu này: `[Bổ sung R-009] nội dung`.
  Mã trong tiền tố phải là mã đã xác minh; yêu cầu mới chỉ gửi text.Trim(). Không lấy FileName làm đường dẫn.
  Brief không cung cấp cú pháp tiền tố: Claude cần xác nhận quy ước này khi review; không suy diễn đó là định dạng cũ đã có.
- Lỗi đọc phiếu/inbox truyền nguyên ngoại lệ; không nuốt lỗi, không chuyển thành tin mới, không thử lại gây trùng.
- GET `/Agents`, `/Inbox` không phân biệt hoa thường, kể cả query handler: 302, Location `/`, không chạy next.
  Phương thức khác (kể cả HEAD), đường dẫn khác kể cả đường dẫn con: next đúng một lần, không tự sửa response.
- Không bỏ dịch vụ hoặc sửa handler cũ. Middleware không gọi dịch vụ, không ghi file/trạng thái khi GET.
- Không thêm thao tác file/tiến trình vào lõi nên không cần xử lý symlink hay thời hạn tiến trình tại đây.
- `.cs` giữ UTF-8 có BOM. Không có thay đổi giao diện hoặc CLI trong task này.

## Dữ liệu mẫu thật
Không đọc file phiếu trực tiếp: dùng hợp đồng RequestThread và IRequestService đã có.
Test dựng phiếu cũ có các mục rỗng; chưa xác minh trên dữ liệu thật hay quy ước tiền tố của quản lý.
Không dùng dữ liệu AITeam thật hoặc USERPROFILE; test không chạy tiến trình ngoài.

## Test nghiệm thu
`hub.tests/AcceptanceH703OfficeChatTests.cs`

## Tiêu chí hoàn thành
- [ ] Hai thân hàm TODO đã hiện thực đúng XML, không tự thêm đường ghi phiếu.
- [ ] Gửi mới/bổ sung đúng một mục inbox, giữ nội dung 4000 ký tự; lỗi trống/4001/dự án lạ đúng câu, không ghi.
- [ ] Chặn mã sai/traversal trước đọc; phiếu mất hoặc trả sai mã không gửi; phiếu cũ vẫn gửi được.
- [ ] Lỗi đọc/inbox truyền nguyên, không thử lại; dịch vụ không giữ trạng thái lần gửi.
- [ ] GET cũ trả 302 Location `/`, không chạy handler; request khác đi tiếp, DI và thứ tự middleware giữ đúng.
- [ ] `dotnet build AITeam.sln` 0 lỗi, 0 warning sau hiện thực.
- [ ] `dotnet test AITeam.sln` pass, gồm 39 ca nghiệm thu; không sửa/tắt test.

## Báo cáo của agent
Agent thực hiện điền sau khi hoàn tất; không coi build khung là bằng chứng code đã chạy đúng.

| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| Lõi gửi tin và kiểm tra lỗi | Chưa hiện thực |
| Phiếu cũ, giả mạo và traversal | Chưa hiện thực |
| Chuyển hướng và DI | Chưa kiểm tra nghiệm thu |
| Build | Agent điền số lỗi/warning thực tế |
| Test | Agent điền số pass/fail thực tế |

File đổi và lý do: agent điền.
Quyết định tự đưa ra: agent điền; quy ước tiền tố và giới hạn độ dài đã ghi rõ ở trên.
Điều chưa xác minh: dữ liệu thật; cú pháp tiền tố cần Claude xác nhận khi review.

### Kết quả chuẩn bị khung (Codex, bước 4)
- Đã soạn đặc tả, tạo khung OfficeChatService/middleware, nối DI và pipeline trong Program, tạo 39 ca nghiệm thu. Hai thân hàm vẫn ném NotImplementedException theo yêu cầu.
- File đổi: phiếu này (đặc tả); hub/Services/OfficeChatService.cs (khung); hub/Program.cs (nối khung); hub.tests/AcceptanceH703OfficeChatTests.cs (test bất biến khi thực hiện).

| Kiểm tra chuẩn bị | Bằng chứng thật |
|---|---|
| Build lần 1 | `dotnet build AITeam.sln`: exit 1; output chỉ có Build FAILED, 0 Warning(s), 0 Error(s), chưa xác định nguyên nhân |
| Build lần 2 | `dotnet build AITeam.sln --disable-build-servers -m:1 -v:normal`: exit 0; Build succeeded, 4 Warning(s) đều CS9113 tham số khung chưa dùng, 0 Error(s); 27,13 giây |
| Test nghiệm thu | `dotnet test hub.tests/AITeam.Hub.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~AcceptanceH703OfficeChatTests --logger "console;verbosity=quiet"`: exit 1; 38 fail, 1 pass, 0 skip, tổng 39; đang đỏ do khung chưa hiện thực |
| Giới hạn phiên | Đã dùng đủ 2 lượt build; không chạy git, không chạy app hoặc agy/codex; không chạy toàn bộ suite ở bước chuẩn bị |

Quyết định chuẩn bị: giữ nguyên Agent B/nhánh đã cấp; đề xuất tiền tố bổ sung nêu ở ràng buộc vì brief không có cú pháp chính thức; dùng pipeline trong bộ nhớ để test HTTP mà không mở máy chủ.
Điều chưa xác minh: cú pháp tiền tố quản lý; dữ liệu thật; toàn bộ suite. Các mục này không phải kết quả nghiệm thu sản phẩm.

## Review (Claude)
<!-- Claude điền sau nghiệm thu. -->

## Kết quả
<!-- Claude điền khi review đạt. -->

## Đánh giá quy trình
<!-- Claude điền sau nghiệm thu. -->