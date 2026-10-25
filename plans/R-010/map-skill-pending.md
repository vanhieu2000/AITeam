# Hàng chờ liên kết skill trên Map (R-009)

## Ánh xạ 11 nghiệp vụ
| Nghiệp vụ | ID skill |
|---|---|
| Tiếp nhận | `01-tiep-nhan` |
| Ý tưởng | `02-y-tuong` |
| Nghiên cứu | `03-research` |
| Kế hoạch | `04-ke-hoach` |
| Duyệt kế hoạch | `05-duyet-ke-hoach` |
| Chuẩn bị task | `06-chuan-bi-task` |
| Code | `07-code` |
| Kiểm thử | `08-kiem-thu` |
| QC | `09-qc` |
| Nghiệm thu | `10-nghiem-thu` |
| Publish Git | `11-publish-git` |

## Tiêu chí liên kết đọc an toàn
- Đây là hàng chờ chuẩn bị cho R-009, hiện tại chưa tạo trang Map.
- ID tra danh sách cho phép cố định, không nhận đường dẫn tùy ý.
- Chặn .. và đường dẫn tuyệt đối từ đầu vào, chuẩn hóa và kiểm tra nằm dưới skills.
- Chặn symlink và reparse point ở cả file lẫn thư mục cha.
- Trường hợp thiếu file thì không phát liên kết chết.
- Thiết kế chỉ đọc, không thực thi Markdown; luôn encode tên, nội dung và liên kết, không dùng Html.Raw.
- Không thay đổi quyền duyệt hoặc publish.
