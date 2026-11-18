# R-017 · MyControls: gop y demo - ghi nho dang nhap, cai tien grid, form khung, menu da cap

- **Nhận lúc:** 10:17 18/11/2026 · máy HIEUDV
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > demo tôi góp ý 1 vài vấn dề: 1. login thiếu phần ghi nhớ mật khẩu => lưu tên đăng nhập và mật khẩu vào file ini dự án 2. phần bảng grid demo tôi chưa thấy chức năng bật/tắt chọn multi dòng dữ liêu jtrar ra danh sách ID, dữ liệu dòng , căn giữa dòng , header từng cột như devexpress có chưa,.. đã có gom nhóm và tổng phần tử tỏng nhóm bên cạnh chỗ gom chưa ,.. nghiên cứu kĩ ohaanf grid này cải tiến cho tôi , phần demo tập trung cho tôi phần login=> quản lí người dùng => đăng kí chi tiết người dùng áp dụng tất các control nhập liệu bao gồm cả tải ảnh đại diện.-Phát triển thêm chức năng: 3. thanh title gồm : thông tin đăng nhập, menu đăng nhập, đăng xuất , đổi mâtk khẩu , đổi avatar, thông báo . và tên tab đang chọn. cái này áp dụng được cho rất nhiều phần mềm nên tôi cần phát triển 1 form sẵn 4. control menu : dạng dọc , ngang để làm danh sách chức năng áp dụng cho nhiều phần mềm sử dụng được , phải tạo được chức năng to=>nhỏ hơn => nhỏ hơn=> cho phép chỉnh caption, ảnh -icon phân hệ chức năng
  > hãy giao cho Ai khác làm bạn nghiệm thu

## 1. Phân tích (Claude)
- Mục tiêu hiểu được:
- Dự án / phần bị ảnh hưởng:
- Quyết định đã tự chọn (không hỏi):
- Điều cần chủ dự án quyết (nếu có):
- Thiết kế: `docs/design/...`

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| | | A / B / Claude | ~N dòng | mặc định | |

Mỗi file task phải có dòng `- **Yêu cầu:** R-017` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 10:17 | Nhận yêu cầu, chủ dự án duyệt kế hoạch 9 task | Thiết kế: docs/design/R-017-thiet-ke.md |
| trước 11:31 (không ghi giờ chính xác) | Xong thiết kế và mockup, gửi chủ dự án duyệt giao diện | docs/design/R-017-mockup.html |
| trước 11:31 (không ghi giờ chính xác) | Chủ dự án duyệt mockup; cho phép đẩy task lên main từ thư mục chính | Hook chặn push main từ worktree |
| 11:31 | Giao U-123 (agy) | 28 test nghiệm thu đỏ trước khi giao, build khung 0 warning |
| 11:33 | Giao U-124 (agy) | 30 test nghiệm thu đỏ, không có NullReference |
| 11:37 | Giao U-128 (agy) | 50 test nghiệm thu, 49 đỏ |
| 11:45 | Giao U-129 (agy) | 31 test nghiệm thu đỏ |
| 13:15 | Merge U-123 vòng 1 | Claude sửa 3 điểm nhỏ khi review |
| 13:32 | Merge U-124 vòng 2 | [AG] hiệu năng ô chọn tất cả; Claude sửa 2 điểm nhỏ |
| 13:49 | Merge U-128 vòng 2 ở lần thử thứ 3 | 2 lần trước clone sạch bị sập testhost ở hai test khác nhau; đang điều tra, chưa rõ nguyên nhân |
| 14:43 | Merge U-129 vòng 2 | [AG] dữ liệu sai kiểu bị đổi thành rỗng; đã sửa |
| 14:49 | Giao U-125 (agy) | 21 test nghiệm thu, 20 đỏ |
| 14:55 | Giao U-130 (agy) | 25 test nghiệm thu, 24 đỏ; lần đầu hook quét bí mật chặn commit khung, đã bỏ hằng mật khẩu mẫu |
| 17:05 | Review U-126 vòng 1 CẦN SỬA | [CK] Cổng kiểm tra coi lỗi thật là "chập chờn": lần thử lại chạy dll bin\Debug cũ nên đạt giả (U88 RefreshData); [AG] SetData chép danh sách làm RefreshData mất dòng mới |
| 13:50 | [CK] Cổng kiểm tra: thử lại test đỏ tìm bin\Debug trong clone sạch chỉ build Release nên lần thử lại luôn báo không thấy dll | Chưa sửa trong team.ps1 |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

| 16:05 18/11/2026 | Cờ tự động MyControls/U-125 lượt 2 | CH |
| 19:52 18/11/2026 | Cờ tự động MyControls/U-127 lượt 4 | CH |
| 00:27 19/11/2026 | Cờ tự động MyControls/U-132 lượt 1 | TO |
## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-017`, rồi điền:

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian (nhận → báo xong) | |
| Thời gian Claude (phân tích + thiết kế + review + tự sửa) | |
| Thời gian agent (cộng mọi lượt) | 1143,5 phút |
| Thời gian chờ không ai làm (vd. chờ hạn mức, chờ chủ dự án) | |
| Số lượt agent / số vòng review | 49 lượt / vòng TB 1,70; đạt vòng 1: 3/10 task |
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
- **Tốn thời gian nhất ở đâu:** U-131: test tạo lại 12 tài khoản cho mỗi ca nên chạy 8–9 phút; Claude tự sửa 4 vòng sau review thay vì giao thẳng Codex; chạy lại toàn bộ test nhiều lần.
- **Cái gì làm tốt (giữ lại):** Thử kéo chuột trên lưới thật tìm ra lỗi test nghiệm thu không bắt được; đọc file dump xác định "treo" chỉ là test chậm; giao Codex rà soát cả nhóm lỗi xong trong 1 lượt (U-132, 16 phút).
- **Cải tiến quy trình áp dụng cho yêu cầu sau** (tối đa 2, cụ thể, sửa file nào):
  1. Có phát hiện sau review thì giao thẳng Codex kèm danh sách rà soát theo nhóm lỗi, không sửa từng vòng.
  2. Test cần dữ liệu mẫu đắt (băm mật khẩu) phải dùng thư mục mẫu dùng chung ngay từ khung test nghiệm thu.
- **Chưa xác minh khi báo xong:** chưa thử kéo thả bằng chuột thật; sau U-132 chỉ chạy toàn bộ test 1 lần (10/10 lần là ở bản trước U-132).
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có / chưa
