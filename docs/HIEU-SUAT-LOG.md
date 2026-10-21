# Nhật ký hiệu suất — mỗi yêu cầu một mục

Đơn vị theo dõi: **một yêu cầu của chủ dự án (R-xxx)**. Phiếu chi tiết: `requests/R-xxx-*.md`; số liệu: `team.ps1 request-report -Request R-xxx`.

Sau mỗi yêu cầu, Claude thêm 1 mục theo mẫu dưới đây. Cột "Cải tiến" là thay đổi quy trình áp dụng từ yêu cầu kế tiếp. Yêu cầu sau phải đo lại xem cải tiến đó có tác dụng không.

| Yêu cầu | Ngày | Tổng thời gian | Claude / Agent (phút) | Lượt agent / vòng review | Tự sửa | Lỗi TK / AG / CK | Tốn nhất ở | Cải tiến áp dụng | Cải tiến trước có tác dụng? |
|---|---|---|---|---|---|---|---|---|---|

## Quy trình hiện hành (cập nhật dần sau mỗi yêu cầu)
Phiên bản 1 — 21/10/2026, trước yêu cầu đầu tiên được theo dõi:
1. Nhận yêu cầu ⇒ `team.ps1 new-request -Title "…"`, chép nguyên văn lời chủ dự án, ghi giờ.
2. Phân tích + thiết kế: Claude.
3. Chia việc:
   - mỗi task ≤ ~500 dòng, có ngân sách, khung code + test nghiệm thu viết trước;
   - A và B làm song song ở vùng code khác nhau;
   - việc ≤ ~100 dòng Claude tự làm.
4. Giao ⇒ cổng kiểm tra tự động ⇒ Claude review. Lỗi ≤ ~50 dòng Claude tự sửa ngay; còn lại trả agent kèm Blocker / Required.
5. Mỗi sự kiện ghi 1 dòng vào nhật ký phiếu yêu cầu. Mỗi lỗi ghi nguyên nhân gốc TK / AG / CK.
6. Xong ⇒ merge, clone sạch, báo chủ dự án ⇒ đánh giá trong phiếu ⇒ 1 dòng trong bảng trên ⇒ áp dụng tối đa 2 cải tiến (sửa sổ tay / mẫu / `team.ps1` ngay) ⇒ tăng số phiên bản quy trình ở mục này.
