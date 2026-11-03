# AITeam — Mô tả hệ thống (bản gốc cho người và AI)

> Chủ dự án: HieuDV · Bản 1.0 · 03/11/2026 · Mọi AI (Claude, Codex, OpenCode, Antigravity…) làm việc trên AITeam ĐỌC FILE NÀY TRƯỚC.
> Khi thiết kế / task mâu thuẫn với file này: file này thắng, hoặc hỏi chủ dự án rồi sửa file này trước.

---

## 1. AITeam là gì — một câu
**AITeam là "văn phòng phần mềm do AI vận hành":** chủ dự án chỉ nói mình muốn gì; hệ thống tự phân tích, lập kế hoạch, chia việc, giao cho nhiều AI lập trình làm song song, tự kiểm tra, tự đánh giá, tự rút kinh nghiệm, và báo cáo lại. Chủ dự án chỉ **duyệt** những quyết định quan trọng.

Gồm 3 phần:
| Phần | Là gì | Ở đâu |
|---|---|---|
| **Bộ điều phối** | Lệnh PowerShell + thư viện C# chạy các AI, cổng kiểm tra, merge, đo đạc | `team.ps1`, `core/` (AITeam.Core, `aiteam` CLI) |
| **AITeam Hub** | Trang web nội bộ (ASP.NET Core Razor Pages + HTMX) để chủ dự án xem, giao việc, duyệt | `hub/`, mở bằng `CHAY-HUB.cmd` |
| **Dữ liệu** | File văn bản trên git (đồng bộ 2 máy nhà / công ty) | `requests/`, `tasks/`, `plans/`, `metrics/`, `approvals/`, `improvements/`, `staff/`, `.state/` (không lên git) |

## 2. Mục tiêu (đo được)
| # | Mục tiêu | Chỉ số | Hiện tại (đo 10/2026) | Đích |
|---|---|---|---|---|
| M1 | Chủ dự án tốn ít thời gian | Phút chủ dự án / yêu cầu (chỉ nói + duyệt) | chưa đo | ≤ 10 |
| M2 | Làm đúng ngay lần đầu | % task ĐẠT vòng 1 | 67 % | ≥ 80 % |
| M3 | Nhanh | Phút từ giao đến merge / task | 20–120 | ≤ 30 |
| M4 | Rẻ | Token / task; token Claude / yêu cầu | ~0,8–2,4 triệu | giảm 30 % mỗi quý |
| M5 | Không kẹt | Task "tạm dừng" > 24 giờ không ai xử lý | 5 (26–28/10) | 0 |
| M6 | Tự tốt lên | Lỗi cùng loại lặp lại sau khi đã ghi bài học | chưa đo | 0 |
| M7 | Minh bạch | Mọi việc / lượt / lỗi / token nhìn thấy trên Hub ≤ 1 phút sau khi xảy ra | một phần | 100 % |

## 3. Nguyên tắc bất biến
1. **Chủ dự án quyết sản phẩm, AI quyết kỹ thuật** trong khuôn khổ thiết kế đã duyệt.
2. **Một nguồn sự thật:** trạng thái nằm trong file trên git, không nằm trong trí nhớ của một AI hay tài khoản nào. Đổi máy / đổi AI vẫn làm tiếp được.
3. **Người viết code không tự chấm bài mình.** Code do AI X viết ⇒ người review khác X.
4. **Mọi lượt chạy đều được đo** (thời gian, token, dòng code, kết quả) và **mọi lỗi đều có nguyên nhân gốc** (mục 7).
5. **Dừng sớm thay vì vá vòng tròn:** cùng một lỗi sau 2 lượt ⇒ chuyển lên quản lý tìm gốc.
6. **An toàn:** không bí mật trong repo / log / prompt; AI chạy trong thư mục riêng (worktree) với quyền hẹp; repo private.
7. **Tiết kiệm:** việc rẻ cho AI rẻ, việc khó cho AI mạnh; không gọi AI cho việc máy làm được (build, test, đếm).

## 4. Vai trò (chức vụ) và điều phối
### 4.1 Bảng chức vụ
| Chức vụ | Ai đảm nhận (03/11) | Được làm | Không được làm | Chuyển giao khi |
|---|---|---|---|---|
| **Chủ dự án** | HieuDV | Đưa yêu cầu, duyệt thiết kế / mockup / merge quan trọng, đăng nhập tài khoản, bấm UAC, chọn gói trả phí | — | — |
| **Quản lý** | Claude | Phân tích, thiết kế, chốt task, chẩn đoán khi tắc, review, sửa nhỏ ≤ 50 dòng, chốt + merge, đánh giá, báo cáo | Giao task mơ hồ; giao lại cùng một lỗi lần 3 mà chưa tự tìm gốc | Claude vắng / hết hạn mức ⇒ chủ dự án giao thẳng Codex bằng `plans/R-xxx/YEU-CAU-CODEX.md` |
| **Lập trình viên chính** | Codex (ChatGPT) | Code việc khó, nhiều file, `team.ps1`, kiến trúc | QC, merge `main` | Hết hạn mức ⇒ OpenCode |
| **Lập trình viên 2** | OpenCode (CHỈ DeepSeek) | Code việc vừa / nhỏ, sửa theo chẩn đoán có sẵn | Dùng model khác; chạy repo private khi còn dùng bản free | Hết hạn mức ⇒ Codex |
| **Lập trình viên 3** | Antigravity | Chỉ khi tài khoản có model **Argon 4** | — | Không có Argon 4 ⇒ không giao |
| **Cổng kiểm tra** | Máy (`team.ps1 verify`) | Build 0 warning, toàn bộ test, BOM, phạm vi, ngân sách, quét bí mật | Đánh giá chất lượng thiết kế | — |
| **Người review** | Claude (Codex KHÔNG QC từ 03/11) | Đối chiếu thiết kế + tiêu chí, đọc diff, chạy thử giao diện | Duyệt code do chính mình viết > 50 dòng | — |

### 4.2 Luật chọn người làm (định tuyến tự động — mục tiêu của F3)
Theo thứ tự, dừng ở luật đầu tiên khớp:
1. Task ghi `Dev:` cụ thể ⇒ dùng đúng người đó (nếu đang nghỉ ⇒ báo, không tự đổi).
2. Dự án private và người làm chỉ có model free không cam kết riêng tư ⇒ loại người đó.
3. Người đang **nghỉ** (hết hạn mức, lỗi ≥ 3 lượt liên tiếp, đăng nhập hỏng) ⇒ loại.
4. Độ khó L / sửa `team.ps1` / > 300 dòng ⇒ Codex. Độ khó S–M ⇒ OpenCode. 
5. Cân tải: ưu tiên người đang ít việc 🟡 hơn; **không cho 2 người cùng sửa một file** (khóa theo Phạm vi).
6. Không còn ai ⇒ task vào "Chờ người làm", báo chủ dự án kèm lý do và giờ dự kiến có lại hạn mức.

## 5. Vòng đời: Yêu cầu → Task → Lượt
### 5.1 Ba cấp đối tượng
| Cấp | Mã | Là gì | Ví dụ |
|---|---|---|---|
| Yêu cầu | `R-xxx` | Một điều chủ dự án muốn (câu nói nguyên văn) | R-009 "sửa Văn phòng: thẻ tác nhân, chat bên phải…" |
| Task (job) | `<TIỀN TỐ>-xx` | Một việc nhỏ ≤ 15–30 phút AI, ≤ ~500 dòng, 1 nhánh | H7-04 "Thẻ tác nhân và chat giao việc" |
| Lượt (run) | `<Task>-n` | Một lần một AI chạy trên task | H7-04 lượt 10 (OpenCode, 5,3 phút) |

### 5.2 Trạng thái của Task (máy trạng thái — Hub phải hiển thị đúng các trạng thái này)
| Trạng thái | Biểu tượng | Nghĩa | Ai đẩy sang trạng thái sau | Giới hạn thời gian |
|---|---|---|---|---|
| Kế hoạch | ⚪ | Có trong plan, chưa có file task | Quản lý viết task | — |
| Chờ làm | 🟣 | File task đủ (Phạm vi, Tiêu chí, Test nghiệm thu), chờ người rảnh / phụ thuộc | Bộ điều phối giao | Báo nếu > 24 giờ |
| Đang làm | 🟡 | Một AI đang chạy lượt n | AI xong ⇒ Cổng KT | Thời gian tối đa ghi trong task (mặc định 30 phút) |
| Cổng KT | 🔵 | Đang build / test | Máy | 15 phút |
| Chờ review | 🔵 | Cổng KT ĐẠT | Quản lý | Báo nếu > 4 giờ |
| Cần sửa | 🔴 | Review / cổng KT không đạt, đã ghi lỗi | Bộ điều phối giao lại (tối đa 3 vòng) | — |
| Bị chặn | ⛔ | Thiếu thứ ngoài tầm AI (đăng nhập, UAC, thiết bị, quyết định) | Chủ dự án | Báo ngay |
| Tạm dừng | ⏸ | Quá số vòng / cùng lỗi 2 lần ⇒ chờ quản lý tìm gốc | Quản lý | Báo nếu > 24 giờ |
| Đã merge | ✅ | Vào `main`, clone sạch đạt | — | — |
| Hủy | ✖ | Không làm nữa (ghi lý do) | Quản lý / chủ dự án | — |

### 5.3 Các bước của một yêu cầu
| Bước | Việc | Ai | Đầu ra (file) |
|---|---|---|---|
| 1 | Tiếp nhận, chép nguyên văn, giờ nhận | Hub (chat) / Quản lý | `requests/R-xxx-*.md` |
| 2 | Phân tích + thiết kế (+ mockup nếu đổi giao diện lớn ⇒ chủ dự án duyệt) | Quản lý | `docs/design/*.md`, `docs/design/*-mockup.html` |
| 3 | Kế hoạch + chia task (có phụ thuộc, ngân sách, độ khó) | Quản lý | `plans/R-xxx/plan.md` |
| 4 | Viết task + test nghiệm thu + khung code | Quản lý | `tasks/<Mã>.md`, nhánh task có commit test đầu tiên |
| 5 | Giao (định tuyến mục 4.2), chạy song song | Bộ điều phối | Lượt `metrics/runs/*.json`, log |
| 6 | Cổng kiểm tra | Máy | `logs/<Mã>-verify-n.md` |
| 7 | Review + chốt | Quản lý | Mục "### Vòng N — ĐẠT/CẦN SỬA" trong task |
| 8 | Merge + clone sạch + bản chạy thử | Bộ điều phối | `main`, `ban-chay-thu/` |
| 9 | Tự đánh giá task + lượt (mục 7) | Máy + Quản lý | Mục "## Đánh giá" trong task, dòng `AGENTS.md` nếu là bài học |
| 10 | Đánh giá yêu cầu + báo cáo chủ dự án | Quản lý | Mục 4 của phiếu R, `docs/HIEU-SUAT-LOG.md` |

## 6. Mô hình dữ liệu (tối thiểu Hub phải đọc được)
| Đối tượng | Trường chính | Lưu ở |
|---|---|---|
| Request | id, tiêu đề, lời nguyên văn, giờ nhận, trạng thái, danh sách task, nhật ký có giờ, đánh giá | `requests/R-xxx-*.md` |
| Task | id, tiêu đề, dự án, yêu cầu, độ khó, ngân sách dòng, thời gian tối đa, phạm vi file, phụ thuộc, người làm, nhánh, trạng thái, tiêu chí, các vòng review, kết quả, đánh giá | `tasks/<Mã>.md` + dòng trong `tasks/README.md` |
| Run (lượt) | task, n, người làm (provider), model, máy, bắt đầu, kết thúc, phút, token vào / ra / suy nghĩ / tổng, dòng +/−, file đổi, trạng thái (SUCCESS / ERROR / QUOTA / TIMEOUT / STOPPED), cổng KT đạt?, số lỗi | `metrics/runs/*.json` (**thiếu: token của Codex / OpenCode đang = 0 — phải sửa, F9**) |
| Finding (lỗi) | task, vòng, loại nguyên nhân (mục 7.2), nhóm (bảo mật / giao diện / test / phạm vi / chất lượng / hiệu năng / dữ liệu / BOM / tài liệu), mô tả, bằng chứng, ai sửa, phút sửa | Dòng `- [Mã] (nhóm) mô tả` trong mục review của task |
| Provider (người làm AI) | id, loại, model, đã cài?, đăng nhập?, trạng thái (sẵn sàng / bận / nghỉ đến giờ X / lỗi), hạn mức đã dùng / còn lại / thời điểm hồi, link quản lý tài khoản | `.state/providers.json`, `.state/accounts.json` (theo máy) |
| Approval | id, loại (merge / decision / improvement), nội dung, trạng thái, người duyệt, giờ | `approvals/A-xxx.json` |
| Improvement | chu kỳ, chỉ số trước / sau, vấn đề lặp, giải pháp, có tác dụng không | `improvements/IMP-xxx.md` |

## 7. Tự đánh giá sau mỗi lượt / task (F7 — trọng tâm)
### 7.1 Khi nào
- **Sau mỗi lượt** (máy tự làm, 0 token): so số đo với ngưỡng, gắn cờ.
- **Sau mỗi task merge** (quản lý, ≤ 5 phút): xác nhận nguyên nhân gốc, ghi bài học.
- **Sau mỗi yêu cầu / mỗi 5 task** (quản lý): chu kỳ cải tiến, sửa công cụ / mẫu.

### 7.2 Phân loại nguyên nhân gốc (dùng đúng mã này ở mọi nơi)
| Mã | Nguyên nhân | Dấu hiệu tự phát hiện (máy) | Sửa ở đâu |
|---|---|---|---|
| **TK** | Task / thiết kế viết thiếu, sai, mơ hồ | Agent hỏi lại; sửa ngoài Phạm vi; tiêu chí không kiểm được | Mẫu task, thiết kế |
| **TN** | Test nghiệm thu sai (dựng môi trường sai, assert sai, cảnh báo trong test) | Test đỏ cùng stack trace ở mọi lượt dù code đổi; đỏ trước cả khi agent sửa | Test nghiệm thu, `AGENTS.md` |
| **AG** | AI làm sai dù task rõ | Cổng KT trượt vì lỗi trong Phạm vi; review bắt bug | Lời giao việc, `staff/<AI>.md` "Lưu ý riêng" |
| **CK** | Cổng kiểm tra để lọt / chặn nhầm | Lỗi lộ ra sau merge; cổng báo lỗi mà người kiểm thấy đúng | `team.ps1 verify` |
| **HT** | Hạ tầng / môi trường (quyền, sandbox, khóa file, mạng, cấu hình CLI) | "Access denied", treo không token, lỗi 1223, timeout không có output | Sổ tay vận hành `QUY-TRINH.md`, công cụ |
| **HM** | Hạn mức / tài khoản | 429, `quota`, `RESOURCE_EXHAUSTED`, QuotaExhausted | Định tuyến, chủ dự án mua gói / đổi tài khoản |
| **CH** | Chậm (không sai nhưng tốn thời gian) | Phút lượt > 2 × thời gian tối đa; lượt > 3 | Chia task nhỏ hơn, chỉ định file cần đọc |
| **TO** | Tốn token (không sai nhưng tốn) | Token lượt > 2 × trung bình cùng độ khó; đọc file ngoài danh sách | Danh sách "Đọc", model rẻ hơn |
| **DP** | Điều phối (giao trùng, 2 AI cùng sửa 1 file, sai thứ tự phụ thuộc) | Xung đột merge; task chạy khi phụ thuộc chưa xong | Bộ điều phối |

### 7.3 Đầu ra bắt buộc của đánh giá
- Trong task: mục `## Đánh giá` — mỗi lỗi 1 dòng `- [MÃ] (nhóm) mô tả · bằng chứng · đã sửa ở đâu`; 1 dòng tổng: phút, lượt, vòng, token, dòng / ngân sách.
- Lỗi có thể lặp ở task khác ⇒ 1 dòng vào **cuối** `AGENTS.md`.
- Lỗi do công cụ ⇒ sửa công cụ ngay hoặc tạo task V9-xx.
- Hub hiển thị: lỗi theo mã nguyên nhân theo tuần (biểu đồ cột chồng), top 5 bài học mới.

## 8. Các chức năng (F1 … F14)
Mỗi chức năng: **Mục đích · Ai dùng · Làm gì · Dữ liệu · Màn hình · Hiện trạng 03/11 · Xong khi**.

### F1. Tiếp nhận yêu cầu (chat giao việc)
- **Mục đích:** chủ dự án nói một câu là hệ thống nhận việc, không cần biết lệnh.
- **Làm gì:** ô chat trên trang Văn phòng; chọn dự án (hoặc "dự án mới"); gửi ⇒ tạo phiếu R-xxx, chép nguyên văn, giờ nhận; bổ sung vào phiếu cũ; hiện trả lời của quản lý trong cùng luồng.
- **Hiện trạng:** ĐÃ CÓ (H7-03, H7-04 merge 03/11). Quản lý chỉ nhận khi bắt đầu phiên.
- **Xong khi:** phiếu mới hiện trên Hub ≤ 5 giây; quản lý (hoặc bộ điều phối tự động F3) nhận trong ≤ 20 phút khi có người trực.

### F2. Lập kế hoạch và chia task tự động
- **Mục đích:** thay chủ dự án / quản lý chia việc; mỗi task đủ rõ để AI làm đúng lần đầu.
- **Làm gì:** từ phiếu R ⇒ `plan.md` (task, phụ thuộc, độ khó S/M/L, ngân sách, người đề xuất); từ plan ⇒ file task theo `templates/task.md` + test nghiệm thu + khung code; **tự kiểm chất lượng task** trước khi giao: có Phạm vi đường dẫn thật, Tiêu chí kiểm được, test nghiệm thu build được và đỏ đúng chỗ, không có cảnh báo trong test (bài học H7-04), dựng trang đúng cách (bài học H7-06).
- **Hiện trạng:** CÓ lệnh `plan` / `prep` (Codex). Thiếu "tự kiểm chất lượng task". Codex hiện không dùng cho bước này.
- **Xong khi:** ≥ 80 % task đạt vòng 1; 0 lỗi TN trong 10 task liên tiếp.

### F3. Điều phối và giao việc tự động (thay chủ dự án)
- **Mục đích:** việc chờ được tự giao cho đúng AI, chạy song song, không ai phải ngồi gõ lệnh.
- **Làm gì:** hàng đợi "Chờ làm" ⇒ áp luật mục 4.2 ⇒ tạo worktree `<a|b|c>-<mã>`, merge `origin/main`, giao; khóa file theo Phạm vi để 2 AI không cùng sửa; theo dõi giờ; hết hạn mức ⇒ chuyển người; xong ⇒ cổng KT ⇒ "Chờ review"; cùng lỗi 2 lượt ⇒ "Tạm dừng" + báo quản lý; tôn trọng phụ thuộc (task B chờ task A merge).
- **Hiện trạng:** CÓ `assign`, `pipeline`, tự chuyển agy ⇒ Codex. THIẾU: OpenCode, khóa file, hàng đợi tự chạy, bỏ agy khi không có Argon 4; Codex chạy được chỉ khi ghi đè cấu hình (xem `QUY-TRINH.md` v5).
- **Xong khi:** chủ dự án duyệt plan xong không phải gõ lệnh nào cho tới khi có "Chờ review"; 0 xung đột do 2 AI sửa cùng file.

### F4. Giám sát thời gian thực (Văn phòng)
- **Mục đích:** nhìn 1 màn hình biết ai đang làm gì, bao lâu, có kẹt không.
- **Làm gì:** mỗi AI là một thẻ: tên, model, trạng thái (rảnh / đang làm Mã / nghỉ đến giờ X / lỗi), task + lượt đang chạy, phút đã chạy / thời gian tối đa (thanh tiến độ), token lượt này, dòng cuối log; cảnh báo đỏ khi quá giờ, không có output > 10 phút, lỗi liên tiếp; nút Dừng (xác nhận).
- **Hiện trạng:** CÓ thẻ + nút điều khiển (V5-04/05, H7-04). Thiếu: token trực tiếp, cảnh báo "không có output", thẻ OpenCode.
- **Xong khi:** từ lúc AI bắt đầu lượt đến lúc thẻ đổi ≤ 10 giây; mọi trường hợp kẹt 26–28/10 đều đã hiện cảnh báo.

### F5. Bảng công việc: tổng quan → chi tiết (chờ / đang / đã làm)
- **Mục đích:** xem toàn cảnh rồi bấm sâu tới từng lượt, từng lỗi.
- **Làm gì — 5 cấp bấm sâu:**
  1. **Toàn hệ thống:** số task theo trạng thái (mục 5.2) cho mọi dự án; việc cần chủ dự án (Bị chặn, Chờ duyệt) lên đầu.
  2. **Dự án:** cột Kanban Chờ làm · Đang làm · Chờ review · Cần sửa / Tạm dừng · Đã merge (7 ngày); lọc theo yêu cầu, người làm.
  3. **Yêu cầu R-xxx:** lời nguyên văn, dây chuyền 10 bước (mục 5.3) với bước hiện tại, danh sách task + trạng thái, tổng phút / token, nhật ký có giờ.
  4. **Task:** Tóm tắt, Tiêu chí (tích), dòng thời gian các lượt (Gantt), các vòng review, lỗi theo mã, Kết quả, Đánh giá; link nhánh / diff / log.
  5. **Lượt:** người làm, model, phút, token, dòng +/−, kết quả cổng KT, log cuối, báo cáo của AI.
- **Hiện trạng:** có từng phần (Dự án, Task, Requests, Map, W1-07 trang Việc). Thiếu cấp 1 gộp mọi dự án, Kanban, cấp 5.
- **Xong khi:** từ trang chủ tới log của một lượt bất kỳ ≤ 3 cú bấm; số liệu khớp `metrics/runs`.

### F6. Cổng kiểm tra, review và chốt
- **Mục đích:** không để code hỏng vào `main`; người chốt nhìn đủ bằng chứng trong 1 trang.
- **Làm gì:** cổng KT tự chạy (mục 4.1); gói review (`team.ps1 review`): diff theo file, báo cáo AI, kết quả cổng, cảnh báo; nút ĐẠT / CẦN SỬA kèm lý do có mã nguyên nhân; merge kèm clone sạch.
- **Hiện trạng:** CÓ. Thiếu: cho phép sửa harness có kiểm soát (V9-07); build song song gây khóa file.
- **Xong khi:** 0 lỗi lọt sau merge trong 20 task; review 1 task ≤ 10 phút.

### F7. Tự đánh giá — xem mục 7.

### F8. Đánh giá AI (bảng điểm người làm)
- **Mục đích:** biết AI nào giỏi việc gì, đáng tiền không, để định tuyến tốt hơn.
- **Làm gì:** cho mỗi provider + model, theo tuần và toàn thời gian: số task, % đạt vòng 1, vòng TB, phút TB / task theo độ khó, token TB / task, lỗi AG theo nhóm, số lần hết hạn mức, chi phí ước tính; xếp hạng theo loại việc (giao diện / PowerShell / C# lõi / test); gợi ý "nên giao loại việc X cho Y".
- **Hiện trạng:** có `staff-report` cho Minh / Lan / Codex; trang Hiệu suất cũ. Chưa có OpenCode, chưa chia theo model và loại việc.
- **Xong khi:** bảng điểm cập nhật sau mỗi lượt; luật định tuyến (4.2) đọc được gợi ý này.

### F9. Token và hạn mức
- **Mục đích:** biết đã tiêu bao nhiêu, còn bao nhiêu, bao giờ hết / bao giờ hồi, để không bị dừng giữa chừng.
- **Làm gì:** cho mỗi tài khoản AI:
  - **Đã dùng:** token vào / ra / suy nghĩ theo lượt, task, ngày, tuần (đọc từ output CLI: Codex `exec --json` sự kiện token, OpenCode `step_finish.part.tokens`, agy log).
  - **Còn lại:** lấy từ nhà cung cấp khi CLI có trả (ví dụ phần trăm hạn mức / giờ hồi trong sự kiện của Codex — **phải kiểm chứng bằng output thật trước khi dùng**); nếu nhà cung cấp không cho biết ⇒ hiển thị "không rõ" + ước tính từ hạn mức khai báo trong cấu hình (ví dụ OpenCode Go 12 USD / 5 giờ, 30 USD / tuần, 60 USD / tháng).
  - **Dự báo:** với tốc độ 24 giờ qua thì bao giờ hết; giờ hồi gần nhất.
  - **Claude:** không đo trực tiếp được ⇒ ghi số phiên / phút làm việc; mục tiêu giảm qua quy trình.
- **Hiện trạng:** THIẾU phần lớn. `metrics/runs` ghi token = 0 cho Codex; không có "còn lại".
- **Xong khi:** mọi lượt có token > 0 (hoặc ghi rõ "không đo được"); trang Tài khoản AI hiện đã dùng / còn lại / giờ hồi cho từng tài khoản; cảnh báo khi còn < 20 %.

### F10. Tài khoản AI và liên kết
- **Mục đích:** một chỗ quản lý mọi dịch vụ AI: đăng nhập chưa, gói gì, link tới trang quản lý.
- **Làm gì:** danh sách tài khoản (Codex / ChatGPT, OpenCode + DeepSeek, Antigravity, Claude, Gemini…): đã cài CLI? phiên bản, đăng nhập?, model được phép (OpenCode: chỉ DeepSeek), gói (free / trả phí), **link** trang hạn mức / thanh toán / tài liệu; nút "Kiểm tra lại" (chạy `doctor` cho dịch vụ đó). Không bao giờ hiện / lưu mật khẩu, khóa API.
- **Hiện trạng:** CÓ trang Tài khoản AI (V5-06). Thiếu OpenCode, link, trạng thái tách "đăng nhập" / "đang chạy".
- **Xong khi:** chủ dự án biết cần đăng nhập / nạp tiền ở đâu trong ≤ 1 phút.

### F11. Đơn duyệt
- **Mục đích:** gom mọi việc cần chủ dự án quyết vào 1 bảng.
- **Làm gì:** bảng 6 cột (Mã, Loại, Dự án/task, Tóm tắt, Lúc tạo, Trạng thái), nút Duyệt / Từ chối + ghi chú ngay trong dòng; merge tự chặn nếu chưa duyệt / nhánh đổi sau duyệt.
- **Hiện trạng:** CÓ (H7-06 merge 03/11). Dữ liệu giả chưa có đơn chờ để xem.

### F12. Cải tiến quy trình
- **Mục đích:** hệ thống tự tốt lên theo số đo, không theo cảm tính.
- **Làm gì:** mỗi 5 task / mỗi tuần: so chỉ số mục 2 với kỳ trước, top nguyên nhân (mục 7.2), tối đa 2 cải tiến, áp dụng ngay vào công cụ / mẫu, kỳ sau đo có tác dụng không.
- **Hiện trạng:** CÓ `improvements/IMP-001…006`, trang Quy trình & cải tiến.

### F13. Báo cáo cho chủ dự án
- **Làm gì:** sau mỗi yêu cầu và cuối ngày: xong gì, đang làm gì, kẹt gì + cần anh làm gì, token đã dùng / còn lại, 1–3 cải tiến vừa áp dụng. Ngắn, gạch đầu dòng, tiếng Việt.

### F14. Bảo mật và an toàn
- Bí mật chỉ ở kho DPAPI / `.env.local`; quét bí mật trước commit; AI chạy trong worktree, quyền hẹp (OpenCode: chỉ sửa file + git/dotnet; Codex: sandbox), không `--auto` / bỏ qua sandbox; Hub chỉ nghe localhost, mọi POST có token chống giả mạo; dữ liệu hiển thị luôn được mã hóa HTML; repo private (03/11 kho AITeam đang public — chủ dự án quyết).

## 9. Hub — menu và mỗi trang
| Menu | Trang | Trả lời câu hỏi | Chức năng |
|---|---|---|---|
| Văn phòng `/` | Thẻ AI + chat giao việc + phòng nghiên cứu | "Ai đang làm gì? Tôi muốn giao việc" | F1, F4 |
| Đơn duyệt `/Approvals` | Bảng việc chờ quyết | "Tôi cần quyết gì?" | F11 |
| Dự án `/Projects` → `/Project` | Danh sách dự án → Kanban + yêu cầu + task | "Dự án X tới đâu?" | F5 cấp 1–4 |
| Hiệu suất `/Performance` | Chỉ số mục 2, bảng điểm AI, lỗi theo nguyên nhân | "Team làm tốt không? AI nào tốt?" | F7, F8 |
| Quy trình & cải tiến `/Process` | Chu kỳ IMP, bài học mới | "Đang cải tiến gì?" | F12 |
| Tài khoản `/AI` | Tài khoản, token, hạn mức, link | "Còn bao nhiêu? Cần nạp / đăng nhập đâu?" | F9, F10 |
| Map `/Map` | Phân nhiệm vụ theo vai trò | "Ai phụ trách bước nào?" | mục 4 |
| Cài đặt `/Settings` | Cấu hình, dự án, đường dẫn máy | — | — |

Chuẩn giao diện chung: tiếng Việt; rộng tối thiểu 1000 px không tràn ngang; chuỗi dài xuống dòng; mỗi danh sách có trạng thái rỗng / đang tải / lỗi; số liệu có giờ cập nhật.

## 10. Thuật ngữ
| Từ | Nghĩa |
|---|---|
| Worktree | Thư mục làm việc riêng của một nhánh, để nhiều AI làm song song không đè nhau |
| Cổng kiểm tra (cổng KT) | Bước máy tự kiểm build / test / phạm vi sau mỗi lượt |
| Test nghiệm thu | Test quản lý viết TRƯỚC, mô tả hành vi phải đạt; AI không được sửa assert |
| Harness / fixture | Phần dựng môi trường trong test (tạo trang, dữ liệu giả) |
| Hạn mức (quota) | Lượng token / lượt nhà cung cấp cho phép trong một khoảng thời gian |
| Provider | Một AI làm việc (Codex, OpenCode, Antigravity…) |
| Lượt (run) | Một lần một AI chạy trên một task |
