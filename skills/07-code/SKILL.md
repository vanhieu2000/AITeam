# 07-code

## Mục tiêu
Triển khai đúng phần thân được giao, giữ hợp đồng và tạo bằng chứng để quản lý review đúng bản code.

## Đầu vào
Task đã chuẩn bị, khung, test nghiệm thu bất biến và thiết kế/mẫu thật được task cho phép đọc; quy trình nhánh và quyền hạn được giao.

## Đầu ra (định dạng)
Gửi quản lý báo cáo Markdown: bảng từng tiêu chí → lệnh đã chạy/kết quả thật, số test, warning và commit được kiểm; liệt kê file đổi/lý do, quyết định tự đưa ra và rủi ro.
Mỗi tiêu chí ghi đạt/cần sửa/chưa xác minh; nếu hệ thống chịu trách nhiệm commit thì ghi rõ chưa có commit được kiểm, không bịa mã commit.

## Các bước
1. Đọc task và các file được chỉ định; xác định phạm vi, chữ ký bất biến và test không được sửa.
2. Đọc skill theo stack được chỉ định nếu có; thiếu skill thì ghi nhận và tiếp tục theo task, không mở rộng nhiệm vụ để tạo skill.
3. Điền phần thân theo khung, giữ hợp đồng và BOM đang có; thêm test cho logic trong các trường hợp tiêu chí yêu cầu, không nhân bản test tương tự.
4. Chạy test liên quan khi sửa; cuối lượt chạy build/test bắt buộc của dự án, lưu lệnh và kết quả thật. Lỗi test nghiệm thu đáng nghi phải báo quản lý, không sửa test để qua.
5. Điền báo cáo, bàn giao nhánh theo quy trình commit/push đã giao; nếu không được chạy git thì để hệ thống thực hiện, ghi rõ giới hạn kiểm chứng.

## Checklist bắt buộc
- [ ] Chỉ đổi trong phạm vi, giữ chữ ký và hợp đồng; việc phát hiện thêm đưa vào báo cáo.
- [ ] Không sửa test nghiệm thu, không xóa/tắt/Skip hay né assertion để qua.
- [ ] Không tự merge hoặc publish; commit và bàn giao nhánh theo quyền được giao.
- [ ] Không báo pass khi chưa chạy; thiếu công cụ/dữ liệu/bằng chứng ghi chưa xác minh.
- [ ] Báo cáo gắn kết quả với commit được kiểm hoặc ghi chưa xác minh commit; không dùng kết quả bản cũ cho bản đã sửa.

## Lỗi hay gặp
- Mất BOM khi sửa file tạo lỗi cổng kiểm tra; giữ mã hóa ban đầu. Nguồn: AGENTS.md, mục Lỗi hay gặp — giữ nguyên BOM UTF-8.
- Test trả về sớm khiến code chưa làm vẫn xanh. Nguồn: AGENTS.md, mục Lỗi hay gặp — Không viết test luôn pass.
- Tự sửa hợp đồng để tiện triển khai làm lệch thiết kế. Nguồn: AGENTS.md, mục Riêng dự án này — không tự đổi HubModels.cs/Contracts.cs.

## Ví dụ
- Tốt: coder giữ chữ ký, chạy test tiêu chí, báo “12 pass, 0 fail” cùng commit đã kiểm; phần chưa có dữ liệu thật ghi chưa xác minh.
- Xấu: coder đổi test nghiệm thu để xanh rồi tự merge; báo cáo không còn chứng minh yêu cầu gốc và vượt quyền bàn giao.

