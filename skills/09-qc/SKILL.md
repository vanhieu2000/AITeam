# 09-qc

## Mục tiêu
Reviewer độc lập đánh giá đúng yêu cầu, bảo mật và hồi quy trên commit đã qua cổng KT; không thay cổng KT hoặc nghiệm thu trực quan.

## Đầu vào
Commit cần review, diff so với bản gốc, task/thiết kế, test bất biến và bằng chứng KT gắn đúng commit; dữ liệu mẫu được task chỉ định.

## Đầu ra (định dạng)
Gửi quản lý và coder báo cáo Markdown ghi reviewer, commit và kết luận đạt/cần sửa/chưa xác minh.
Mỗi phát hiện gồm Blocker/Required/Optional, file:vị trí, lý do, cách tái hiện hoặc bằng chứng, hành vi mong muốn và test xác nhận; Optional đưa vào hàng chờ riêng.

## Các bước
1. Xác nhận reviewer không phải coder của thay đổi; kiểm commit trong diff trùng commit của log KT và yêu cầu được giao.
2. Đọc diff, đối chiếu từng tiêu chí và luồng lỗi/biên; tìm đổi ngoài phạm vi, đổi hợp đồng và test bị làm yếu.
3. Kiểm dữ liệu ngoài: đường dẫn, tham số tiến trình, giới hạn tải và bí mật; đánh giá hồi quy theo các luồng thiết kế, không suy đoán từ tên test.
4. Chạy kiểm tra cần thiết trong phạm vi và ghi kết quả thật; nếu chỉ đọc code thì ghi rõ chỉ review tĩnh, phần chưa chạy là chưa xác minh.
5. Blocker là lỗi chặn an toàn/khả năng dùng; Required là sai yêu cầu bắt buộc; trả coder sửa hai nhóm này. Optional không chặn phạm vi hiện tại, đưa hàng chờ.
6. Khi coder sửa, review lại diff và bằng chứng của commit mới; bàn giao nhận xét cho quản lý quyết định bước tiếp theo, không tự merge.

## Checklist bắt buộc
- [ ] Reviewer độc lập với coder, kết luận gắn đúng commit và bằng chứng cùng bản.
- [ ] Phân loại Blocker, Required, Optional với vị trí, lý do và cách tái hiện cụ thể.
- [ ] Không báo pass khi chưa chạy; tách nhận xét tĩnh, kiểm tra đã chạy và phần chưa xác minh.
- [ ] Không dùng build thay bằng chứng UI; QC không tự nhận là KT hay nghiệm thu trực quan.
- [ ] Kiểm phạm vi, bảo mật, hồi quy và test bất biến; Optional được ghi hàng chờ, không ép mở rộng task.

## Lỗi hay gặp
- Ghép đường dẫn từ file ngoài có thể thoát thư mục đích. Nguồn: AGENTS.md, mục Lỗi hay gặp — dữ liệu từ bên ngoài không tin cậy, chuẩn hóa/chặn đường dẫn trước ghi.
- Nuốt lỗi đọc rồi ghi lại có thể phá dữ liệu. Nguồn: AGENTS.md, mục Lỗi hay gặp — không catch rỗng khi đọc dữ liệu sẽ ghi lại.
- Review lẫn quyết định merge vượt vai trò. Nguồn: AGENTS.md, mục Quy trình, bước 7 — Claude review và quyết định bước tiếp theo.

## Ví dụ
- Tốt: reviewer độc lập ghi Required tại hàm đọc zip, tái hiện mục `../outside.txt` thoát thư mục, yêu cầu chặn trước ghi và test đỏ rồi xanh.
- Xấu: coder tự xác nhận QC đạt vì build xanh; chưa đối chiếu yêu cầu, chưa đánh giá đường dẫn ngoài và không có reviewer độc lập.

