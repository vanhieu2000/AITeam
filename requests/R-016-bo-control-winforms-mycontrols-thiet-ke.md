# R-016 · Bo control WinForms MyControls (thiet ke + giao viec + nghiem thu)

- **Nhận lúc:** 13:37 09/11/2026 · máy HIEUDV
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > thư mục này là dự án phát triển control custom lại cho winform. mục đích thay thế hiển thị hoặc chức năng của control gốc rất sơ sài hoặc khi muôn lấy truyền nhân data vào control lại phải code lại. tôi muốn xây dựng 1 bộ control cần thiếu để design tốt hơn. cài đặt trực tiếp qua cài đặt design của visual chứ không cần viết lại code. Lên ý tưởng thiết kế cho tôi: về giao diện, chức năng , event ,.. mục đích thiếu kê giao diẹn mềm mại , có bo góc , hiện đại,.. . có các nhóm như, loai menu, dọc hay ngang, dang tree hay list ,... , loại control cha như panel, group,.. thêm lạo thẻ card như web..., các con trol nhập liệu như textbox, label , combobox,.. grid,..  đại khái là tôi thấy devxpress control khá ổn nhưng dll rất nặng. tôi muốn custom theo phong cách cá nhân. Tham khảo thêm cả control bạn đã tại bên dự án CIM_MES. bạn lên ý tưởng và check giao diện chức năng  còn giao cho AI khác làm . bạn lên ý tưởng và quản lí phát yêu cầu và keiemr cuối nghiệm thu
  >
  > (bổ sung) tên các control đặt theo quy chuẩn gồm: my + tên gốc , ví dụ : mytextbox,mylabel,.. các control đóng vào lib gen thành dll để cài dll vào projecct khác có thể dùng control ngay
  >
  > (duyệt thiết kế) Duyệt, làm đi

## 1. Phân tích (Claude)
- Mục tiêu hiểu được: thư viện control WinForms bo góc hiện đại `MyControls.dll` (tên My + tên gốc), chỉnh hoàn toàn trong Designer, có theme + data binding/validation không cần code, cài DLL vào project khác dùng ngay.
- Dự án / phần bị ảnh hưởng: dự án mới MyControls (`D:\99. Test\IOT_Control`, trước là IotScadaControls + IotScadaDemo).
- Quyết định đã tự chọn (không hỏi): .NET Framework 4.7.2, không thư viện ngoài, icon = font Segoe Fluent/MDL2, font mặc định Segoe UI 10, csproj SDK-style để `dotnet build/test` chạy được, 6 giai đoạn ~22 task.
- Điều cần chủ dự án quyết (nếu có): đã duyệt thiết kế + mockup + quy tắc tên (13:3x 09/11).
- Thiết kế: `docs/DESIGN.md`, mockup `docs/design/mockup.html` (repo MyControls)

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| GĐ0 | MyControls | Claude | — | — | Khung dự án, đổi tên, cấu trúc |
| U-01 Theme + lớp nền | MyControls | A | ~450 dòng | mặc định | Lõi Core |
| U-02 DataBinder + Validator | MyControls | B | ~450 dòng | mặc định | Lõi Data, Review AI: có |

Mỗi file task phải có dòng `- **Yêu cầu:** R-016` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| ~11:30 | Nhận yêu cầu | Lên ý tưởng, viết DESIGN.md + mockup |
| ~12:00 | Chủ dự án bổ sung quy tắc tên My* + đóng gói DLL | Cập nhật thiết kế mục 4, 11 |
| 13:37 | Chủ dự án duyệt thiết kế | Bắt đầu GĐ 0 (Claude) |
| ~13:45 | add-project MyControls (kho private vanhieu2000/MyControls) | |
| ~14:05 | Xong GĐ0 (Claude): đổi tên My*, csproj SDK-style net472, sln, test xUnit, BUILD-DLL, AGENTS.md luật riêng | build 0 warning, 1/1 test |
| 13:49 | Xong task + khung + test nghiệm thu U-01 (25 ca), U-02 (16 ca) | Giao A: U-01, B: U-02 (Antigravity) |
| 13:5x | Giao lần 1–2 thất bại trước khi chạy: Set-BoardStatus đòi `<dự án>/requests` (Claude sửa 1 dòng team.ps1, 8a46d33); TaskLinter chặn mục Phạm vi | [CK] + [TK] |
| 14:00 | Giao U-01 → Minh (agy), U-02 → Codex | |
| 14:05 | U-02 Codex xong 277 s; cổng KT lỗi "Không ghi được nhật ký phiếu" (phiếu phải nằm trong repo dự án) ⇒ thêm `requests/R-016…` vào MyControls, chạy lại verify ĐẠT 21/21 | [CK] |
| 14:08 | Review U-02 vòng 1 ĐẠT, merge | 476/450 dòng; 2 Optional |
| 14:17 | U-01 lượt 2 xong (lượt 1 không push ⇒ cổng KT tự giao lại) ; cổng KT ĐẠT 37/37 | [AG] không push |
| 14:20 | Review U-01 vòng 1 ĐẠT, merge | 4 Optional; ngân sách ước thấp [TK] |
| 14:25 | Clone sạch: build 0 warning, 57/57 test; BUILD-DLL ⇒ MyControls.dll 87 KB; project net48 trống tham chiếu DLL build + chạy OK | GĐ1 (U-01, U-02) xong |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-016`, rồi điền:

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian (nhận → báo xong) | |
| Thời gian Claude (phân tích + thiết kế + review + tự sửa) | |
| Thời gian agent (cộng mọi lượt) | |
| Thời gian chờ không ai làm (vd. chờ hạn mức, chờ chủ dự án) | |
| Số lượt agent / số vòng review | |
| Dòng code / ngân sách | |
| Claude tự sửa: số lần / tổng dòng | |
| Lỗi theo nguyên nhân: TK / AG / CK | |

**Đánh giá nhân viên** (chi tiết ghi thêm vào `staff/<mã>-<tên>.md`):

| Nhân viên | Task | Lượt · phút | Vòng | Dòng / ngân sách | Làm tốt | Lỗi AG | Hướng cải thiện |
|---|---|---|---|---|---|---|---|

**Team đang bất ổn ở đâu** (mỗi mục ghi bằng chứng từ nhật ký):
- Kỹ thuật (công cụ, build, cổng kiểm tra, môi trường, model):
- Quy trình (thiết kế, chia task, giao việc, review, phối hợp 2 máy):
- Quản lý (Claude: lỗi TK, quyết định sai, chậm review):
- **Tốn thời gian nhất ở đâu:**
- **Cái gì làm tốt (giữ lại):**
- **Cải tiến quy trình áp dụng cho yêu cầu sau** (tối đa 2, cụ thể, sửa file nào):
  1.
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có / chưa
