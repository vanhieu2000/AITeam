---
name: 01-tiep-nhan
description: Tiếp nhận yêu cầu trước bước ý tưởng trong quy trình AITeam.
---

# 01-tiep-nhan

## Mục tiêu
Chuyển yêu cầu thô thành phiếu tiếp nhận có phạm vi và tiêu chí thành công kiểm được.
Chỉ chuyển sang ý tưởng khi đã ghi đủ điều chưa rõ và chỉ ra điểm nào chặn đề xuất.
Không tự chọn giải pháp hoặc coi phiếu tiếp nhận là quyết định duyệt triển khai.

## Đầu vào
- Yêu cầu gốc, người yêu cầu, nguồn và thời điểm; bản phiếu, lịch sử và quyết định cũ nếu có.
- Yêu cầu mới: tạo phiếu mới, phân biệt lời người dùng với diễn giải của người tiếp nhận.
- Yêu cầu cũ: giữ nguyên nguồn và quyết định trước; ghi phiên bản mới cùng phần thay đổi.
- Đầu vào rỗng/thiếu: liệt kê trường còn thiếu và câu hỏi xác nhận, không tự điền dữ kiện.
- Nội dung mâu thuẫn: ghi cả hai phát biểu và nguồn, đánh dấu CHƯA XÁC MINH; không tự chọn bên đúng.

## Đầu ra (định dạng)
Bàn giao phiếu cho người đề xuất ý tưởng với bố cục:
- Mã phiếu, nguồn yêu cầu, phiên bản, người yêu cầu và người xác nhận.
- Mục tiêu; phạm vi; ngoài phạm vi; tiêu chí thành công quan sát hoặc đo được.
- Lịch sử: phiên bản trước → thay đổi → lý do → quyết định cũ còn hiệu lực hay cần xác nhận lại.
- Bảng điểm thiếu: câu hỏi | nguồn | ảnh hưởng | người cần xác nhận | trạng thái.
- Trạng thái phiếu: đủ để đề xuất hoặc chờ xác nhận; ghi rõ blocker và phần có thể tiếp tục.

## Các bước
1. Trích yêu cầu gốc và nguồn; tách nhu cầu khỏi giải pháp người yêu cầu gợi ý.
2. Đối chiếu phiếu cũ nếu có, chỉ rõ thêm/bớt/đổi; không ghi đè lịch sử bằng diễn giải mới.
3. Viết mục tiêu và giới hạn; chuyển từ mơ hồ như “nhanh” thành câu hỏi về ngưỡng đo.
4. Gắn mỗi tiêu chí thành công với kết quả kiểm được; chưa có ngưỡng thì ghi cần xác nhận.
5. Liệt kê thiếu/rỗng/mâu thuẫn trước khi chuyển bước; giữ giả định ở trạng thái CHƯA XÁC MINH.
6. Chuyển phiếu cho bước ý tưởng cùng blocker; nếu thiếu mục tiêu cốt lõi thì trả người yêu cầu bổ sung.

## Checklist bắt buộc
- [ ] Mục tiêu, phạm vi và ngoài phạm vi phân biệt được; không mở rộng yêu cầu bằng suy đoán.
- [ ] Tiêu chí thành công có cách kiểm hoặc câu hỏi xác nhận ngưỡng còn thiếu.
- [ ] Yêu cầu cũ giữ nguồn, lịch sử, quyết định và phần thay đổi; giả định chưa thành xác nhận.
- [ ] Thiếu, rỗng và mâu thuẫn đều có câu hỏi, người xác nhận và ảnh hưởng tới chuyển bước.
- [ ] Người nhận biết phiếu đủ đề xuất hay đang chờ; không hiểu nhầm là đã duyệt thực hiện.

## Lỗi hay gặp
- AGENTS.md:56 — H-008 từng ghi cứng Agent “A”: khi nhận yêu cầu hiển thị, hỏi dữ liệu lấy từ đâu và thay đổi theo ai; không biến một ví dụ thành dữ liệu cố định.
- Ghi yêu cầu bổ sung đè lên quyết định cũ khiến mất căn cứ; giữ phiên bản và đánh dấu phần cần xác nhận lại.

## Ví dụ ngắn tốt / xấu
Tình huống minh họa: phiếu cũ chỉ xem trạng thái, yêu cầu mới nói “thêm nút dừng”.
- Tốt: Giữ quyết định cũ, đánh dấu nút dừng là phần bổ sung; hỏi ai được dừng và điều kiện thành công, nên bước ý tưởng biết phần chưa xác nhận.
- Xấu: Sửa phiếu cũ thành “mọi người được dừng” và ghi đã xác nhận, khiến bước sau thiết kế quyền ngoài yêu cầu.
