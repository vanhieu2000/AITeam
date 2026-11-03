---
name: 02-y-tuong
description: Đề xuất phương án sau tiếp nhận, trước research trong quy trình AITeam.
---

# 02-y-tuong

## Mục tiêu
Đề xuất các phương án đáp ứng phiếu tiếp nhận, chỉ rõ ưu/nhược và đánh đổi.
Chuyển sang research với câu hỏi có thể kiểm chứng; không coi đề xuất là quyết định đã duyệt.
Không triển khai thử hay mở rộng phạm vi để làm phương án hấp dẫn hơn.

## Đầu vào
- Phiếu tiếp nhận gồm mục tiêu, phạm vi, ngoài phạm vi, tiêu chí thành công và điểm thiếu.
- Lịch sử thay đổi, quyết định cũ và bằng chứng đã có kèm nguồn, trạng thái xác minh.
- Phiếu rỗng/thiếu mục tiêu: trả bước tiếp nhận; thiếu chi tiết phụ thì ghi điều kiện của phương án.
- Yêu cầu hoặc quyết định mâu thuẫn: giữ cả hai nguồn, nêu câu hỏi cần xác nhận trước khi chọn.
- Bằng chứng thiếu nguồn hoặc quá cũ không được dùng làm sự thật; ghi CHƯA XÁC MINH.

## Đầu ra (định dạng)
Bàn giao bản phương án cho người research với bố cục:
- Mục tiêu và giới hạn kế thừa từ phiếu; liên kết phiên bản phiếu tiếp nhận.
- Bảng phương án: ID | cách làm | ưu điểm | nhược điểm | đánh đổi | điều kiện áp dụng.
- Bảng giả định: ID | phát biểu CHƯA XÁC MINH | ảnh hưởng nếu sai | câu hỏi research.
- Bảng bằng chứng riêng: phát hiện | nguồn file:dòng | trạng thái | giới hạn áp dụng.
- Đề xuất ưu tiên có điều kiện, lý do và điểm chưa rõ; ghi rõ chưa phải quyết định duyệt.

## Các bước
1. Rút tiêu chí so sánh từ phiếu; không tự thêm mục tiêu người yêu cầu chưa xác nhận.
2. Nêu các cách làm khác biệt thực chất; nếu chỉ có một cách khả thi thì giải thích căn cứ.
3. So sánh ưu/nhược trên cùng tiêu chí: phạm vi, công sức, rủi ro và trải nghiệm liên quan.
4. Ghi đánh đổi cụ thể: lợi ích nào đạt được, chi phí hoặc hạn chế nào phải chấp nhận.
5. Tách giả định khỏi bằng chứng; chưa kiểm thì ghi CHƯA XÁC MINH, kể cả điều nghe có vẻ hiển nhiên.
6. Chuyển giả định ảnh hưởng lựa chọn thành câu hỏi research, ưu tiên câu có thể loại phương án.
7. Gửi phương án và câu hỏi cho research; mâu thuẫn yêu cầu cốt lõi trả về tiếp nhận.

## Checklist bắt buộc
- [ ] Mỗi phương án có ưu/nhược, đánh đổi và điều kiện áp dụng gắn tiêu chí thành công.
- [ ] Giả định CHƯA XÁC MINH nằm riêng với bằng chứng có nguồn.
- [ ] Quyết định cũ được dẫn lại đúng; phương án thay thế chưa được trình bày như đã duyệt.
- [ ] Điểm thiếu/rỗng/mâu thuẫn có hướng trả lại hoặc câu hỏi, không bị lấp bằng suy đoán.
- [ ] Người research nhận đủ câu hỏi và ảnh hưởng của câu trả lời tới lựa chọn.

## Lỗi hay gặp
- AGENTS.md:53 — V5-05: HTMX 2 bỏ qua xác nhận đặt trên nút submit; khi đề xuất thao tác nguy hiểm, ghi việc xác minh phần tử phát request thay vì giả định có nút là đủ an toàn.
- Chỉ kể lợi ích của phương án yêu thích khiến so sánh lệch; ghi cả hạn chế và điều kiện làm nó không phù hợp.

## Ví dụ ngắn tốt / xấu
Tình huống minh họa: cần xác nhận trước khi gửi yêu cầu dừng.
- Tốt: So sánh xác nhận trên form với hộp thoại riêng, ghi giả định tương thích CHƯA XÁC MINH và câu hỏi research; chưa chọn thay người duyệt.
- Xấu: Tuyên bố đặt xác nhận trên nút chắc chắn hoạt động và đã được chọn, khiến kế hoạch bỏ qua việc kiểm chứng hành vi submit.
