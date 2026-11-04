# R-015 · Rà soát Hub thành văn phòng AI đúng nghĩa (đề xuất chờ chủ dự án duyệt)

Ngày 04/11/2026 · Claude. Nguồn: rà từng trang Hub đang chạy (31 trang Razor, menu 9 mục), đọc mã, số liệu thật.

## 1. Chủ dự án quản lý gì trong một văn phòng AI
Một văn phòng AI phục vụ đúng 1 người ra quyết định (chủ dự án). Anh cần trả lời được 6 câu, mỗi câu là một màn hình:

| # | Câu hỏi của chủ dự án | Màn hình |
|---|---|---|
| 1 | Bây giờ đang có gì, có gì cần tôi không? | **Văn phòng** |
| 2 | Tôi đã yêu cầu gì, tới đâu rồi, kết quả ra sao? | **Yêu cầu** (mới vào menu) |
| 3 | Việc gì cần đúng tôi quyết? | **Cần anh duyệt** |
| 4 | Từng task đang ở đâu, ai làm? | **Theo dõi việc** |
| 5 | Tôi có những dự án nào, thông tin, tình trạng từng dự án? | **Dự án** (làm thật) |
| 6 | Đội AI của tôi: ai sẵn sàng, hạn mức, làm tốt hay kém? | **Đội AI** (gộp 5 trang) |
| + | Team làm có tốt dần lên không, bài học gì? | **Báo cáo & bài học** |
| + | Luật chơi: quy trình, vai trò, khóa bí mật | **Quy trình**, **Cài đặt** |

## 2. Hiện trạng (bằng chứng)
- **Trùng lặp:** 31 trang, menu chỉ dẫn 9. Trùng nhau: hiệu suất ×4 (`/Overview`, `/Performance`, `/Team`, `/Reports`), việc ×2 (`/Work`, `/Tracking`), AI ×5 (`/AiServices`, `/AiAccounts`, `/AI`, `/Accounts`, `/Providers`), quy trình ×3 (`/Workflow`, `/Process`, `/Map`), chat ×3 (`/Chat`, `/Research`, `/Assistant`).
- **Hỏng:** `/Inbox`, `/Agents` lỗi 500; `/Run` 404; `/p/AITeam` báo "Không đọc được tiến độ công việc".
- **Dự án:** `/Projects` chỉ là 3 dòng chữ, không thêm / sửa / lưu trữ; sửa thông tin dự án lại nằm ở tab Dự án trong Cài đặt.
- **Yêu cầu:** không có trong menu (`/Requests` tồn tại nhưng ẩn). 7 phiếu cũ R-006…R-013 vẫn "🟡 Đang làm", hiện 0/0 task ⇒ Văn phòng hiện sai.
- **Có giao diện, không có dữ liệu:** Bài học 0 (trong khi CLAUDE.md có ~30 bài học); "Claude đã tự quyết" 0 (Claude merge mỗi ngày); Tài khoản AI "Đăng nhập chưa kiểm · Hạn mức chưa đo" ở mọi dòng; Antigravity xuất hiện 2 lần.
- **Giao diện:** trang dài phải cuộn cả trang, khối trống / ô chọn rỗng (đã sửa Văn phòng ở UI-10).

## 3. Menu đề xuất (9 mục, 3 nhóm)
**Làm việc:** Văn phòng · Yêu cầu · Cần anh duyệt · Theo dõi việc
**Quản lý:** Dự án · Đội AI
**Đánh giá & cấu hình:** Báo cáo & bài học · Quy trình · Cài đặt (gồm Két bí mật)

**Bỏ hẳn (14 trang):** Overview, Performance, Team, Work, AI, AiAccounts, Accounts, Providers, Process, Map, Chat, Inbox, Agents, Run. Chức năng còn dùng được gộp vào trang giữ lại (bảng điểm nhân viên → Đội AI; đổi tài khoản Antigravity → Đội AI; chat → nút trợ lý nổi). `/Research` thay bằng chế độ "Hỏi cùng lúc" của trợ lý chat; bỏ thẻ "Phòng nghiên cứu" ở Văn phòng.

## 4. Dự án — giữ và làm thật
Văn phòng chạy nhiều dự án (hiện 3: AITeam, HDVTool, ChinaAndroidToolbox) nên CẦN quản lý dự án.

**Thông tin mỗi dự án** (nguồn `projects.json` dùng chung 2 máy + `team.local.json` riêng máy):
| Nhóm | Trường | Ghi chú |
|---|---|---|
| Nhận diện | Tên, mô tả 1 dòng, trạng thái (Đang làm / Tạm dừng / Lưu trữ) | Lưu trữ = ẩn khỏi giao việc, KHÔNG xóa repo |
| Mã nguồn | Repo GitHub (private), nhánh chính, thư mục trên máy này | Thư mục riêng từng máy → `team.local.json` |
| Kỹ thuật | Stack, lệnh build, lệnh test, lệnh chạy thử, vùng code (areas) cho A / B | Agent dùng khi giao việc |
| Đội | AI mặc định, người phụ trách vùng | |
| Số liệu (tự tính) | Yêu cầu đang mở, task đang chạy / chờ review, merge gần nhất, build gần nhất, phút AI 30 ngày | Không nhập tay |

**Màn hình:** trái = danh sách dự án (thẻ gọn: tên, trạng thái, số việc mở, merge gần nhất); phải = chi tiết dự án chọn, 3 thẻ: Thông tin (sửa tại chỗ), Việc & yêu cầu đang mở, Hoạt động gần đây. Nút: **+ Thêm dự án** (Mới / Từ GitHub / Từ thư mục có sẵn), Sửa, Tạm dừng / Lưu trữ, Mở thư mục, Chạy thử, Lấy bản mới (git pull).
**Bỏ** tab Dự án trong Cài đặt (chuyển về đây).

## 5. Yêu cầu — đưa vào menu
Danh sách phiếu R-xxx: trạng thái, dự án, ngày nhận, tiến độ task (x/y), thời gian, kết quả. Lọc theo trạng thái / dự án. Chi tiết phiếu: lời chủ dự án, phân tích, task, nhật ký, đánh giá, nút "Xem lại từng bước". Nút **+ Yêu cầu mới** (mở trợ lý chế độ Giao việc). Đóng / hủy phiếu.

## 6. Đội AI — gộp 5 trang
Mỗi AI 1 thẻ: vai trò, model, đăng nhập (đã kiểm lúc …), hạn mức / giờ hồi, đang làm gì, bảng điểm (đạt vòng 1, phút / task, lỗi AG 30 ngày), bật / tắt, kiểm tra kết nối, đổi tài khoản. Thêm AI = thêm 1 dòng danh bạ.

## 7. Phân việc
**Claude làm trực tiếp (giao diện):** menu mới; bỏ 14 trang + test của chúng; trang Dự án, Yêu cầu, Đội AI mới; mọi trang theo kiểu Văn phòng (vừa màn hình, mỗi nội dung 1 thẻ tự cuộn); dọn trạng thái 7 phiếu cũ.

**Giao AI (chức năng, có test nghiệm thu Claude viết trước):**
| Task | Việc | Ai |
|---|---|---|
| F1 | Dịch vụ dự án: thêm (gọi `team.ps1 add-project`), sửa trường, đổi trạng thái, lưu trữ; chặn đường dẫn / tên repo bẩn | Codex |
| F2 | Dịch vụ yêu cầu: đếm task đúng theo phiếu (sửa 0/0), đóng / hủy phiếu, tự đánh "Xong" khi mọi task merge | Codex |
| F3 | Đội AI: kiểm đăng nhập + hạn mức thật cho từng AI, gộp bảng điểm; bỏ dòng trùng Antigravity | Antigravity |
| F4 | Nhật ký "Claude tự quyết": `team.ps1` merge / chọn AI / tự sửa ghi 1 dòng | Codex |
| F5 | Nhập bài học có sẵn (CLAUDE.md "Bài học vận hành", AGENTS.md "Lỗi hay gặp") vào kho bài học | Antigravity |
| F6 | Bảng việc chịu được dòng trống / thiếu `|` (hôm nay mất 38 task vì 1 dòng trống) | Codex |

## 8. Thứ tự
1. Claude: menu + bỏ trang trùng + dọn phiếu cũ (không cần chức năng mới).
2. Song song: F1, F2, F4, F6 (Codex, tuần tự) · F3, F5 (Antigravity).
3. Claude làm giao diện Dự án / Yêu cầu / Đội AI khi F1–F3 xong; mockup gửi duyệt trước.
