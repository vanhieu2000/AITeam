# 11-publish-git

## Mục tiêu
Chỉ người có quyền publish mới đưa bản merge đã kiểm lên repository private; coder chỉ bàn giao nhánh, không tự merge.

## Đầu vào
Quyền publish được giao rõ ràng, repository đích và trạng thái private, duyệt merge gắn commit, nhánh bàn giao đã qua KT/QC/nghiệm thu áp dụng, lệnh build/test của dự án.

## Đầu ra (định dạng)
Gửi quản lý/chủ dự án biên bản Markdown: quyền và trạng thái repo, commit được duyệt, commit bản merge, nơi clone tạm, lệnh/mã thoát/số test/warning, commit đã push và kết quả push.
Kết luận đạt/cần sửa/chưa xác minh; bị chặn ghi lý do và chưa push, không nhận publish thành công khi chưa kiểm kết quả.

## Các bước
1. Kiểm quyền publish và repo đích private. Thiếu quyền hoặc repo public thì dừng, báo lý do, không push hay tự đổi quyền/trạng thái repo.
2. Kiểm duyệt merge đúng commit nguồn và nền đích; thiếu duyệt hoặc commit đổi thì dừng, xin duyệt lại đúng bản qua quản lý.
3. Người có quyền chuẩn bị bản merge theo duyệt, ghi commit kết quả; coder không thực hiện bước merge.
4. Clone sạch bản merge vào thư mục tạm riêng của dự án, chọn đúng commit kết quả; không dùng output build cũ hoặc dữ liệu cá nhân để kiểm.
5. Chạy build/test trên clone sạch theo lệnh task; lưu mã thoát, warning và số test. Kiểm lỗi hoặc chưa đủ bằng chứng thì dừng, báo lý do, không push.
6. Xác nhận commit nguồn/nền và bản merge vẫn khớp bản được duyệt/đã kiểm; nếu đổi thì dừng để duyệt và kiểm lại. Chỉ sau build/test đạt mới push đúng bản merge đã kiểm, không force-push.
7. Kiểm kết quả push trỏ đúng commit đã kiểm, báo bàn giao; lỗi push ghi chưa hoàn tất, không tự đổi đích hay ghi đè lịch sử để vượt lỗi.

## Checklist bắt buộc
- [ ] Có người có quyền publish được giao rõ ràng, repo đích private; coder chỉ bàn giao nhánh.
- [ ] Có duyệt merge đúng commit trước khi chuẩn bị bản merge; commit đổi thì dừng và duyệt lại.
- [ ] clone sạch bản merge vào nơi tạm → build và test đạt → push đúng commit đã kiểm, theo thứ tự này.
- [ ] Thiếu quyền, repo public, thiếu duyệt, kiểm lỗi hoặc commit đổi: dừng, báo lý do, không push.
- [ ] Không force-push main; không force-push để vượt xung đột/lỗi, không đưa bí mật hoặc dữ liệu cá nhân lên repo.
- [ ] Không báo pass khi chưa chạy; lưu kết quả build/test/push thật, phần chưa kiểm ghi chưa xác minh.

## Lỗi hay gặp
- Coder tự merge vượt quyền và bỏ bước review. Nguồn: AGENTS.md, mục Quy trình, bước 6–7 — agent không merge, Claude review.
- Kiểm bản làm việc cũ không chứng minh bản phát hành chạy được. Nguồn: AGENTS.md, mục Lỗi hay gặp — V4-08 clean-clone chạy nhầm bản cũ.
- Push công khai hoặc bỏ clone sạch sai quy trình publish. Nguồn: plans/R-010/brief.md, mục Skill theo bước — 11-publish-git: repo private, clone sạch, không force-push main.

## Ví dụ
- Tốt: người có quyền kiểm repo private, nhận duyệt đúng commit, tạo bản merge, clone sạch và build/test đạt rồi push chính commit đó; biên bản lưu mã thoát và số test.
- Xấu: coder tự merge rồi push trước, sau đó mới test trên thư mục cũ; dù test xanh cũng chưa chứng minh bản merge đã publish và phải báo vi phạm quy trình.

