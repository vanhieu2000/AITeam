---
name: 04-ke-hoach
description: Chia kế hoạch thành task trước khi duyệt trong quy trình AITeam.
---

# 04-ke-hoach

## Mục tiêu
Chuyển phương án cùng bằng chứng thành các task nhỏ, có hợp đồng bàn giao và test nghiệm thu.
Chuyển sang duyệt khi phụ thuộc, thứ tự, rủi ro và ngân sách có thể đánh giá được.
Lập kế hoạch không đồng nghĩa đã được giao code hoặc được duyệt thực hiện.

## Đầu vào
- Phiếu tiếp nhận, các phương án, đề xuất ưu tiên và bảng research có nguồn/trạng thái.
- Phạm vi file dự kiến, hợp đồng hiện có, ràng buộc và quyết định trước đó.
- Đầu vào rỗng/thiếu mục tiêu: trả tiếp nhận; thiếu phương án: trả ý tưởng; thiếu bằng chứng: trả research.
- Nguồn cũ/mâu thuẫn hoặc giả định CHƯA XÁC MINH phải giữ nguyên trạng thái và ảnh hưởng.
- Không đưa điều CHƯA XÁC MINH thành kết luận chắc chắn; nếu trọng yếu thì ghi blocker trước giao việc.

## Đầu ra (định dạng)
Bàn giao người duyệt bản kế hoạch gắn phiên bản đầu vào và phương án đề nghị:
| ID | Mục tiêu | Phạm vi file | Hợp đồng/đầu ra | Phụ thuộc | Rủi ro | Test nghiệm thu | Ngân sách dòng | Thời gian |
|---|---|---|---|---|---|---|---|---|
- Mỗi task ≤400 dòng, ≤15 phút; ghi ước lượng cụ thể, vượt một ngưỡng phải chia nhỏ trước khi duyệt.
- Kèm thứ tự thực hiện, nhóm chạy song song, file dùng chung và task phải chạy tuần tự.
- Bảng điểm thiếu: giả định | nguồn | trạng thái | ảnh hưởng | bước cần bổ sung; nêu blocker rõ ràng.
- Mỗi hợp đồng chỉ rõ đầu vào/đầu ra và cách task sau dùng kết quả, không tạo API giả để đủ bảng.

## Các bước
1. Gắn mỗi tiêu chí thành công với task và test nghiệm thu quan sát được; loại việc ngoài phạm vi.
2. Chia theo đầu ra nhỏ; ước lượng dòng và thời gian, không chia chỉ để đổi tên mà vẫn phụ thuộc mơ hồ.
3. Vượt 400 dòng hoặc 15 phút thì tách task với hợp đồng rõ, tính lại phụ thuộc và ngân sách.
4. Liệt kê ID phụ thuộc có thật; phát hiện thiếu/vòng và sắp xếp thứ tự trước khi gán nhóm.
5. Chỉ cho song song khi đầu vào sẵn sàng và không sửa trùng file; file chung phải có thứ tự bàn giao.
6. Gắn rủi ro với tác động, cách giảm và test; giữ giả định trọng yếu chưa xác minh thành blocker.
7. Gửi bản kế hoạch cùng bằng chứng cho bước duyệt, chưa chuẩn bị task triển khai khi chưa ĐẠT.

## Checklist bắt buộc
- [ ] Mỗi task có đủ 9 trường, phạm vi file rõ và đầu ra được task nhận tiếp hiểu được.
- [ ] Từng task ≤400 dòng và ≤15 phút; phần vượt đã chia nhỏ và ước lượng lại.
- [ ] Phụ thuộc không thiếu/vòng; thứ tự và nhóm song song không sửa trùng file.
- [ ] Tiêu chí thành công được phủ bằng test nghiệm thu; rủi ro có biện pháp và bằng chứng liên quan.
- [ ] Giả định CHƯA XÁC MINH vẫn được nêu đúng trạng thái; blocker không bị giấu trong ước lượng.

## Lỗi hay gặp
- AGENTS.md:48 — V4-08: test tiến trình dùng bin/Debug đã chạy nhầm bản cũ; task có tiến trình con phải ghi ProjectReference và đường dẫn AppContext.BaseDirectory vào hợp đồng/test, không dựa vào bản build máy cá nhân.
- Hai task khác mục tiêu vẫn xung đột nếu sửa cùng file; kiểm tập file và phụ thuộc trước khi cho song song.

## Ví dụ ngắn tốt / xấu
Tình huống minh họa: dự kiến một task 600 dòng, 25 phút và hai phần cùng sửa một trang.
- Tốt: Chia thành các task 250/350 dòng, 10/15 phút, ghi hợp đồng và thứ tự sửa trang; người duyệt kiểm được ngân sách và xung đột.
- Xấu: Giữ task 600 dòng, gọi hai phần là song song dù cùng sửa trang, khiến vượt ngân sách và không có thứ tự giải quyết xung đột.
