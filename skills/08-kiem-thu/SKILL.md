# 08-kiem-thu

## Mục tiêu
Cổng KT tự động kiểm điều kiện kỹ thuật và bằng chứng của nhánh; kết quả này là đầu vào riêng cho QC, chưa xác nhận nghiệm thu trực quan.

## Đầu vào
Task, phạm vi file, test nghiệm thu gốc, báo cáo agent, commit cần kiểm và quy trình commit/push đã giao; lệnh build/test của dự án.

## Đầu ra (định dạng)
Gửi agent và quản lý biên bản Markdown theo commit: hạng mục → lệnh/kiểm tra, mã thoát, số test pass/fail/skip, số warning và đường dẫn log.
Kết luận đạt/cần sửa/chưa xác minh cho cổng KT; ghi riêng phần UI chưa được nghiệm thu, không gộp thành kết luận UI đạt.

## Các bước
1. Xác định chính xác commit/nhánh nhận bàn giao; kiểm thay đổi đã commit/push theo quy trình hoặc ghi hệ thống chưa hoàn tất, chưa xác minh.
2. Đối chiếu file đổi với phạm vi, BOM với bản gốc, test nghiệm thu còn nguyên và báo cáo đủ bằng chứng từng tiêu chí.
3. Chạy build/test theo task trên cùng bản code; với AITeam dùng các lệnh checklist, ghi đầy đủ mã thoát, warning và số test, không chỉ lấy dòng “thành công”.
4. Có lỗi thì trả agent danh sách file/vị trí, lệnh tái hiện và kết quả mong muốn; sửa xong phải kiểm lại bản commit mới.
5. Chỉ chuyển sang QC độc lập khi điều kiện KT đạt; chuyển yêu cầu ảnh/thao tác UI cho bước nghiệm thu, không tự suy từ build.

## Checklist bắt buộc
- [ ] Kiểm BOM, phạm vi, test bất biến, báo cáo và commit/push theo quy trình được giao.
- [ ] `dotnet build AITeam.sln` đạt, 0 warning; ghi mã thoát và log, không bỏ qua warning mới.
- [ ] `dotnet test AITeam.sln --blame-hang-timeout 2m` đạt; ghi mã thoát và số test, gồm test nghiệm thu.
- [ ] Không báo pass khi chưa chạy; bị chặn phải ghi chưa xác minh và lý do.
- [ ] Không dùng build thay bằng chứng UI; cổng KT, QC và nghiệm thu trực quan là ba kết quả riêng.

## Lỗi hay gặp
- Bỏ qua test nghiệm thu bị sửa làm mất hợp đồng kiểm chứng. Nguồn: AGENTS.md, mục Test nghiệm thu, báo cáo, cổng kiểm tra — test do Claude viết trước, không sửa/xóa/Skip.
- Báo hoàn thành thiếu kết quả thật gây hiểu nhầm đã kiểm. Nguồn: AGENTS.md, mục Quy trình, bước 5 — chưa chạy thì ghi chưa chạy.
- BOM bị mất dù nội dung đúng vẫn vi phạm quy ước. Nguồn: AGENTS.md, mục Lỗi hay gặp — giữ nguyên BOM UTF-8.

## Ví dụ
- Tốt: cổng KT ghi commit đã kiểm, build mã 0/0 warning và test mã 0/19 pass; UI ghi chờ bước 7 có ảnh thực tế.
- Xấu: chỉ build mã 0 rồi ghi “mọi màn hình đạt”; chưa chạy test và không có bằng chứng thao tác UI.

