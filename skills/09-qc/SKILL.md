# 09-qc

## Mục tiêu
Reviewer độc lập đánh giá đúng yêu cầu, bảo mật và hồi quy trên commit đã qua cổng KT; không thay cổng KT hoặc nghiệm thu trực quan.

## Đầu vào
Commit cần review, diff so với bản gốc, task/thiết kế, test bất biến và bằng chứng KT gắn đúng commit; dữ liệu mẫu được task chỉ định.

## Đầu ra (định dạng)
Trả JSON đúng lược đồ cố định, không thêm khóa: `verdict` (`PASS` | `CHANGES_REQUESTED` | `BLOCKED`), `findings[]` (`severity` blocker/required/optional, `file`, `line` là số, `evidence`, `fix`), `unverified[]` là chuỗi.
Mỗi phát hiện ghi reviewer, commit, file:vị trí, lý do, cách tái hiện hoặc bằng chứng, hành vi mong muốn và test xác nhận; Optional đưa vào hàng chờ riêng.
Không tự nhận `PASS` khi chưa chạy: mọi phần chưa chạy hoặc chưa xác minh phải ghi vào `unverified`; chỉ `PASS` khi đã chạy và không còn blocker/required.

## Các bước
1. Xác nhận reviewer không phải coder của thay đổi; kiểm commit trong diff trùng commit của log KT và yêu cầu được giao.
2. Đọc diff, đối chiếu từng tiêu chí và luồng lỗi/biên; tìm đổi ngoài phạm vi, đổi hợp đồng và test bị làm yếu.
3. Kiểm dữ liệu ngoài: đường dẫn, tham số tiến trình, giới hạn tải và bí mật; đánh giá hồi quy theo các luồng thiết kế, không suy đoán từ tên test.
4. Chạy kiểm tra cần thiết trong phạm vi và ghi kết quả thật; nếu chỉ đọc code thì ghi rõ chỉ review tĩnh, phần chưa chạy là chưa xác minh.
5. Blocker là lỗi chặn an toàn/khả năng dùng; Required là sai yêu cầu bắt buộc; trả coder sửa hai nhóm này. Optional không chặn phạm vi hiện tại, đưa hàng chờ.
6. Khi coder sửa, review lại diff và bằng chứng của commit mới; bàn giao nhận xét cho quản lý quyết định bước tiếp theo, không tự merge.

## Checklist bắt buộc
- [ ] Đúng commit: reviewer độc lập với coder, kết luận gắn đúng commit và bằng chứng cùng bản, không review commit cũ.
- [ ] Phạm vi: chỉ thay đổi trong task; không đổi hợp đồng dữ liệu, không thêm việc ngoài phạm vi.
- [ ] Test bất biến: test nghiệm thu còn nguyên; không sửa, xóa hay `Skip` assert để cho qua.
- [ ] Dữ liệu thật: dùng file mẫu/fixtures và dữ liệu được task chỉ định; không dựng số liệu giả che lỗi.
- [ ] Bảo mật: không lộ bí mật/token/đường dẫn máy; dữ liệu ngoài được chuẩn hóa, chặn `..`/tuyệt đối trước khi ghi.
- [ ] Lỗi / biên / hồi quy: xét null/rỗng/trùng, lỗi I/O, giới hạn và luồng cũ; phân loại Blocker, Required, Optional kèm vị trí, lý do, cách tái hiện.
- [ ] Không báo pass khi chưa chạy: tách nhận xét tĩnh, kiểm tra đã chạy và phần chưa chốt ghi vào `unverified`.
- [ ] Không dùng build thay bằng chứng UI; QC không tự nhận là KT hay nghiệm thu trực quan; Optional ghi hàng chờ, không ép mở rộng task.

## Lỗi hay gặp
- Ghép đường dẫn từ file ngoài có thể thoát thư mục đích. Nguồn: AGENTS.md, mục Lỗi hay gặp — dữ liệu từ bên ngoài không tin cậy, chuẩn hóa/chặn đường dẫn trước ghi.
- Nuốt lỗi đọc rồi ghi lại có thể phá dữ liệu. Nguồn: AGENTS.md, mục Lỗi hay gặp — không catch rỗng khi đọc dữ liệu sẽ ghi lại.
- Review lẫn quyết định merge vượt vai trò. Nguồn: AGENTS.md, mục Quy trình, bước 7 — Claude review và quyết định bước tiếp theo.

## Ví dụ
- Tốt: reviewer độc lập ghi Required tại hàm đọc zip, tái hiện mục `../outside.txt` thoát thư mục, yêu cầu chặn trước ghi và test đỏ rồi xanh.
- Xấu: coder tự xác nhận QC đạt vì build xanh; chưa đối chiếu yêu cầu, chưa đánh giá đường dẫn ngoài và không có reviewer độc lập.

