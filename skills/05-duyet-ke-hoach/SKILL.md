---
name: 05-duyet-ke-hoach
description: Review và quyết định duyệt kế hoạch trước khi chuẩn bị task AITeam.
---

# 05-duyet-ke-hoach

## Mục tiêu
Đánh giá kế hoạch theo yêu cầu và bằng chứng để quyết định ĐẠT hoặc CẦN SỬA có căn cứ.
Chỉ bàn giao chuẩn bị task khi ĐẠT; không tự giao code hay coi review là bằng chứng thực nghiệm.
Không duyệt khi còn giả định trọng yếu thiếu bằng chứng, dù phương án có vẻ hợp lý.

## Đầu vào
- Kế hoạch theo phiên bản, phiếu tiếp nhận, phương án, bảng research và quyết định cũ.
- Danh sách task với phạm vi file, hợp đồng, phụ thuộc, rủi ro, test nghiệm thu và ngân sách.
- Kế hoạch rỗng/thiếu trường: CẦN SỬA, chỉ rõ phần phải bổ sung ở bước kế hoạch.
- Đầu vào mâu thuẫn: giữ các nguồn/phiên bản, không âm thầm thay quyết định trước bằng giả định mới.
- Bằng chứng thiếu/cũ hoặc CHƯA XÁC MINH: kiểm mức ảnh hưởng; trọng yếu thì chặn duyệt và trả research.

## Đầu ra (định dạng)
Bàn giao biên bản cho người lập kế hoạch; nếu ĐẠT thì gửi người chuẩn bị task:
- Phiên bản kế hoạch, nguồn đã đối chiếu, người quyết định và thời điểm.
- Quyết định ĐẠT hoặc CẦN SỬA; lý do cụ thể gắn từng tiêu chí và bằng chứng file:dòng.
- Bảng vấn đề: ID | blocker/tác động | nguồn | điều kiện sửa | bước nhận lại | người xử lý.
- Điểm chưa rõ, mức ảnh hưởng, lý do được phép để mở nếu không trọng yếu; không đổi thành sự thật.
- Điều kiện chuyển bước: ĐẠT mới chuẩn bị task; CẦN SỬA phải sửa và duyệt lại đúng phiên bản.

## Các bước
1. So mục tiêu và phạm vi với phiếu tiếp nhận; phần ngoài phạm vi chưa xác nhận trả tiếp nhận.
2. Kiểm lựa chọn và đánh đổi; thiếu phương án hợp lý trả ý tưởng, thiếu bằng chứng trả research.
3. Đối chiếu bằng chứng với nguồn và phiên bản; không nhận giả định hay lời khẳng định làm bằng chứng.
4. Kiểm đủ task, hợp đồng và test nghiệm thu; từng task ≤400 dòng và ≤15 phút, vượt thì trả kế hoạch chia nhỏ.
5. Duyệt đồ thị phụ thuộc: ID thiếu hoặc vòng đều là blocker; kiểm thứ tự tạo đầu ra trước khi dùng.
6. Kiểm rủi ro có biện pháp, nhóm song song không sửa trùng file và không bỏ qua phụ thuộc.
7. Còn blocker hoặc giả định trọng yếu chưa có bằng chứng: CẦN SỬA, ghi điều kiện sửa và bước nhận lại.
8. Hết blocker và đủ tiêu chí: ghi ĐẠT cùng người quyết định; bàn giao chuẩn bị task đúng bản đã duyệt.

## Checklist bắt buộc
- [ ] Phạm vi, tiêu chí thành công và quyết định trước được đối chiếu, không tự mở rộng.
- [ ] Bằng chứng đủ và còn áp dụng; không có giả định trọng yếu CHƯA XÁC MINH được duyệt như sự thật.
- [ ] Phụ thuộc không thiếu/vòng; nhóm song song có đầu vào sẵn và không sửa trùng file.
- [ ] Rủi ro, test nghiệm thu, hợp đồng và ngân sách ≤400 dòng, ≤15 phút/task đã kiểm.
- [ ] Quyết định có lý do, blocker, điều kiện sửa, người quyết định và bước nhận lại rõ ràng.
- [ ] Chỉ bản ĐẠT được bàn giao chuẩn bị task; bản CẦN SỬA phải qua duyệt lại.

## Lỗi hay gặp
- AGENTS.md:53 — V5-05: xác nhận trên nút submit bị HTMX 2 bỏ qua; không duyệt kế hoạch chỉ dựa câu “đã có xác nhận”, phải kiểm căn cứ về phần tử phát request và tiêu chí nghiệm thu tương ứng.
- Đánh dấu ĐẠT có điều kiện dù còn giả định trọng yếu khiến bước sau hiểu là được làm; ghi CẦN SỬA và điều kiện duyệt lại.

## Ví dụ ngắn tốt / xấu
Tình huống minh họa: task B phụ thuộc ID không tồn tại và giả định tương thích vẫn chưa kiểm.
- Tốt: Quyết định CẦN SỬA, trả kế hoạch sửa ID phụ thuộc và research bổ sung bằng chứng tương thích, ghi người quyết định; chưa bàn giao chuẩn bị task.
- Xấu: Ghi ĐẠT vì “chắc sẽ tương thích”, bỏ qua ID thiếu rồi giao chuẩn bị task, khiến lập trình bắt đầu với đầu vào không tồn tại.
