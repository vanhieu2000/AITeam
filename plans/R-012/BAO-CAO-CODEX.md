# R-012 — Báo cáo đợt Codex ngày 03/11/2026 (UTC)

Đã đọc `docs/MO-TA-HE-THONG.md` và `plans/R-012/YEU-CAU-CODEX.md`. Đã đi lần lượt A1 → A3, B1 → B4, C1 → C5, D1 → D6. Không làm lại H8-14/V9-04. Đây là báo cáo công việc đã push và giới hạn còn lại, **không phải tuyên bố R-012 hoàn tất nghiệm thu**.

- 26 nhánh riêng, mỗi nhánh xuất phát từ `origin/main` tại `759ddafaf15c3b12d365884f476f6741af172331`; V9-03 gộp đúng nhánh B4. Không merge main, không force push.
- Đối chiếu SHA bằng `git ls-remote --heads origin`: cả 26 nhánh khớp GitHub; main trên GitHub vẫn là commit trên. Kiểm BOM `.cs/.cshtml/.ps1` và đối chiếu các dòng `Assert.` của file test cũ với main: không có sai khác.
- Mỗi mã đã commit/push ngay sau khi xử lý. Báo cáo chi tiết và bằng chứng nằm trong file task của chính nhánh đó.
- Phút/token chưa có bộ đo tin cậy cho đợt này: ghi **chưa đo**, không thay bằng 0. Các số token dựng trong test không được tính là token đã dùng thật.

## Bảng cuối đợt

| Mã | Nhánh | Trạng thái | phút | token |
|---|---|---|---|---|
| H7-02 | [agent-c/H7-02-map-plan-prep-qc](https://github.com/vanhieu2000/AITeam/tree/agent-c/H7-02-map-plan-prep-qc) | Đã push tích hợp; chờ nghiệm thu Windows | chưa đo | chưa đo |
| V6-02 | [agent-c/V6-02-chan-giao-lai](https://github.com/vanhieu2000/AITeam/tree/agent-c/V6-02-chan-giao-lai) | Dừng: harness cũ thiếu biến, có bằng chứng trong task | chưa đo | chưa đo |
| H7-05 | [agent-c/H7-05-nhung-phong-nghien-cuu](https://github.com/vanhieu2000/AITeam/tree/agent-c/H7-05-nhung-phong-nghien-cuu) | Dừng: assert đọc markup trực tiếp xung đột trích partial | chưa đo | chưa đo |
| W1-04 | [agent-c/W1-04-cong-kiem-tra-qc](https://github.com/vanhieu2000/AITeam/tree/agent-c/W1-04-cong-kiem-tra-qc) | Chặn: W1-03 chưa trên main | chưa đo | chưa đo |
| V9-10 | [agent-c/V9-10-token-moi-luot](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-10-token-moi-luot) | Codex đã có số đo; thiếu fixture OpenCode thành công/chạy thật | chưa đo | chưa đo |
| V9-11 | [agent-c/V9-11-han-muc-va-gio-hoi](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-11-han-muc-va-gio-hoi) | Dừng: test cũ cho ghi đè state hỏng; thiếu số liệu quota thật | chưa đo | chưa đo |
| V9-12 | [agent-c/V9-12-loi-co-cau-truc](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-12-loi-co-cau-truc) | Dừng: assert giữ mã NV trái quy tắc mã lạ → khác | chưa đo | chưa đo |
| V9-13 | [agent-c/V9-13-tu-gan-co-luot](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-13-tu-gan-co-luot) | Đã push cờ và hook; thiếu log H7-06 thật/Windows | chưa đo | chưa đo |
| O-02 | [agent-c/O-02-opencode-deepseek](https://github.com/vanhieu2000/AITeam/tree/agent-c/O-02-opencode-deepseek) | 8 test mới đạt; chạy model thật bị proxy 403, chờ Windows | chưa đo | chưa đo |
| V9-06 | [agent-c/V9-06-codex-on-dinh](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-06-codex-on-dinh) | 5 test mới đạt; chưa kiểm máy công ty | chưa đo | chưa đo |
| V9-14 | [agent-c/V9-14-dinh-tuyen-theo-luat](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-14-dinh-tuyen-theo-luat) | 9 test quyết định đạt; chưa nối assign/nhật ký | chưa đo | chưa đo |
| V9-15 | [agent-c/V9-15-hang-doi-khoa-file](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-15-hang-doi-khoa-file) | Chặn: cần hợp đồng B2/B4/C3 trên main | chưa đo | chưa đo |
| V9-16 | [agent-c/V9-16-kiem-chat-luong-task](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-16-kiem-chat-luong-task) | Dừng: task tối giản của harness S1-04 phải bị lint chặn | chưa đo | chưa đo |
| H8-10 | [agent-c/H8-10-tong-quan-kanban](https://github.com/vanhieu2000/AITeam/tree/agent-c/H8-10-tong-quan-kanban) | Mockup đã push; DỪNG chờ chủ dự án duyệt | chưa đo | chưa đo |
| H8-11 | [agent-c/H8-11-chi-tiet-yeu-cau-task-luot](https://github.com/vanhieu2000/AITeam/tree/agent-c/H8-11-chi-tiet-yeu-cau-task-luot) | Mockup đã push; DỪNG chờ chủ dự án duyệt | chưa đo | chưa đo |
| H8-12 | [agent-c/H8-12-hieu-suat-bang-diem](https://github.com/vanhieu2000/AITeam/tree/agent-c/H8-12-hieu-suat-bang-diem) | Mockup đã push; DỪNG chờ chủ dự án duyệt | chưa đo | chưa đo |
| H8-13 | [agent-c/H8-13-tai-khoan-han-muc](https://github.com/vanhieu2000/AITeam/tree/agent-c/H8-13-tai-khoan-han-muc) | Mockup đã push; DỪNG chờ chủ dự án duyệt | chưa đo | chưa đo |
| H8-01 | [agent-c/H8-01-research-prg](https://github.com/vanhieu2000/AITeam/tree/agent-c/H8-01-research-prg) | Dừng: assert PageResult xung đột POST-Redirect-GET | chưa đo | chưa đo |
| H8-03 | [agent-c/H8-03-review-khong-lap](https://github.com/vanhieu2000/AITeam/tree/agent-c/H8-03-review-khong-lap) | Đã push code, 2 test mới và ảnh; chờ review | chưa đo | chưa đo |
| H8-04 | [agent-c/H8-04-hieu-suat-man-hinh-hep](https://github.com/vanhieu2000/AITeam/tree/agent-c/H8-04-hieu-suat-man-hinh-hep) | Đã push CSS và ảnh thật 1000/1280; chờ review | chưa đo | chưa đo |
| H8-05 | [agent-c/H8-05-log-viet-va-gioi-han-job](https://github.com/vanhieu2000/AITeam/tree/agent-c/H8-05-log-viet-va-gioi-han-job) | Dừng: assert 20 job trái yêu cầu 200 | chưa đo | chưa đo |
| H8-06 | [agent-c/H8-06-don-duyet-mau](https://github.com/vanhieu2000/AITeam/tree/agent-c/H8-06-don-duyet-mau) | Đã push, test HTTP/không ghi file và ảnh đạt; chờ review | chưa đo | chưa đo |
| V9-01 | [agent-c/V9-01-xoa-cache-model](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-01-xoa-cache-model) | Đã push; 2 test mới cần powershell.exe, chưa xác minh | chưa đo | chưa đo |
| V9-02 | [agent-c/V9-02-dong-bo-test-pipeline](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-02-dong-bo-test-pipeline) | Đã push đồng bộ Together/đồng hồ logic; còn Delay giả, chờ Windows | chưa đo | chưa đo |
| V9-03 (gộp B4) | [agent-c/V9-13-tu-gan-co-luot](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-13-tu-gan-co-luot) | 4 test C# đạt; đường agy trực tiếp chưa nối counter | chưa đo | chưa đo |
| V9-05 | [agent-c/V9-05-dispose-khi-build-loi](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-05-dispose-khi-build-loi) | Đã push; test singleton lỗi Build đạt, chờ review | chưa đo | chưa đo |
| V9-07 | [agent-c/V9-07-cong-kiem-tra-build-harness](https://github.com/vanhieu2000/AITeam/tree/agent-c/V9-07-cong-kiem-tra-build-harness) | 6 test policy đạt; chưa kiểm khóa build bằng PowerShell 5.1 | chưa đo | chưa đo |

## Kiểm thử và giới hạn

Các nhánh có thay đổi mã đã chạy `dotnet build AITeam.sln -warnaserror -nodeReuse:false`, kết quả cuối 0 warning/0 error, rồi `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m`. Tài liệu/mockup riêng không chạy lại build/test không liên quan.

Trên main trong môi trường Linux: Hub đạt 776/790, 14 lỗi nền; Core đạt 397/690, 293 lỗi nền (chủ yếu thiếu Windows/PowerShell, kèm 4 lỗi trích dẫn dòng skill đã có). Không ghi các lỗi đó là đã khắc phục. Các nhánh có thêm lỗi/xung đột được ghi riêng: H7-02 có 13 ca H702 cần Windows; H7-05 có thêm 1 assert markup xung đột; V9-11 có thêm 1 assert yêu cầu ghi đè state hỏng; V9-01 có 2 ca mới không khởi chạy được powershell.exe. Không xóa/Skip/đổi assert để làm xanh.

B4 cuối cùng có Core 408/701 đạt, 293 lỗi nền; Hub 776/790 đạt, 14 lỗi nền. Gồm 7 test V913 và 4 test V903 mới đạt. Chưa có verify log/stack H7-06 ngày 26–28/10 để nghiệm thu phát hiện TN trên dữ liệu thật; chưa có runtime PowerShell 5.1 để nghiệm thu hook/journal/khóa build. V9-03 mới bao phủ đường provider C#; đường agy trực tiếp cần tích hợp cùng định tuyến/khóa model Argon.

OpenCode 1.19.5 đã được kiểm phiên bản và gọi model DeepSeek trong thư mục tạm với nội dung công khai tối thiểu; proxy từ chối mở tunnel với HTTP 403. Không có lượt thành công hay token đo được; fixture commit chỉ giữ lỗi thật đã bỏ ID. Không suy diễn đây là lỗi đăng nhập/hết quota và không thử model khác.

C3 hiện là hàm quyết định đã kiểm bằng bảng; chưa nối assign và nhật ký. C4 chưa triển khai queue thiếu các nền B2/B4/C3 trên main. Mỗi nhánh giữ độc lập, không tự nhập nhánh chưa được quản lý review.

## Mockup chờ duyệt

Theo đúng yêu cầu chủ dự án, D1–D4 đã push HTML tĩnh và dừng trước sửa UI sản phẩm. Số liệu trong mockup đều ghi rõ là minh họa. Chromium thật tại 1000×800: document scrollWidth = clientWidth = 1000 và mọi nút không tràn; có ảnh trong từng nhánh.

- H8-10: [HTML](https://github.com/vanhieu2000/AITeam/blob/agent-c/H8-10-tong-quan-kanban/docs/design/H8-10-mockup.html) · [Ảnh 1000 px](https://github.com/vanhieu2000/AITeam/blob/agent-c/H8-10-tong-quan-kanban/docs/screenshots/H8-10/mockup-1000.png).
- H8-11: [HTML](https://github.com/vanhieu2000/AITeam/blob/agent-c/H8-11-chi-tiet-yeu-cau-task-luot/docs/design/H8-11-mockup.html) · [Ảnh 1000 px](https://github.com/vanhieu2000/AITeam/blob/agent-c/H8-11-chi-tiet-yeu-cau-task-luot/docs/screenshots/H8-11/mockup-1000.png).
- H8-12: [HTML](https://github.com/vanhieu2000/AITeam/blob/agent-c/H8-12-hieu-suat-bang-diem/docs/design/H8-12-mockup.html) · [Ảnh 1000 px](https://github.com/vanhieu2000/AITeam/blob/agent-c/H8-12-hieu-suat-bang-diem/docs/screenshots/H8-12/mockup-1000.png).
- H8-13: [HTML](https://github.com/vanhieu2000/AITeam/blob/agent-c/H8-13-tai-khoan-han-muc/docs/design/H8-13-mockup.html) · [Ảnh 1000 px](https://github.com/vanhieu2000/AITeam/blob/agent-c/H8-13-tai-khoan-han-muc/docs/screenshots/H8-13/mockup-1000.png).

## Các quyết định cần quản lý xử lý trước nghiệm thu

Giải quyết các xung đột test được chỉ đúng file/dòng trong từng task; cung cấp môi trường Windows PowerShell 5.1/máy công ty và mạng model không bị proxy 403; review và tích hợp nền theo thứ tự. Chủ dự án duyệt mockup D1–D4 trước khi triển khai bố cục. Không cần cấp bí mật qua báo cáo này; không có khóa/token đăng nhập được đưa vào Git.
