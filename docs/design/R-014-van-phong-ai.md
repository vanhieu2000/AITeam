# R-014 · Văn phòng AI — thiết kế tổng thể (bản đề xuất, chờ chủ dự án duyệt)

Mockup tĩnh: `docs/design/R-014-mockup.html` (6 màn hình, mở bằng trình duyệt).
Nguồn học: skill **ui-ux-pro-max** (MIT) — dữ liệu `products.csv`, `colors.csv`, `typography.csv`, `ui-reasoning.csv`, `quick-reference.md`. Không cài skill vào repo, chỉ học quy tắc.

## 1. Mục tiêu và vai trò
- **Chủ dự án (HieuDV)** — người quyết cuối, chỉ phải xử lý **việc nhạy cảm**: bảo mật, mật khẩu / khóa / chuỗi kết nối CSDL, dữ liệu nhạy cảm, hành động ra ngoài không hoàn tác (đăng công khai, gửi thư, thanh toán, đổi repo sang public), và quyết định sản phẩm khi Claude chủ động hỏi.
- **Claude** — quản lý + thực thi: nhận việc, thiết kế, chia task, giao, review / QC, merge, đánh giá, rút bài học. Code và QC KHÔNG còn vào hàng chờ duyệt của chủ dự án; chúng hiện ở nhật ký "Claude đã tự quyết" để chủ dự án xem lại khi muốn.
- **AI làm việc** (Codex, OpenCode, Antigravity, sau này thêm) — chỉ hiện thực hóa task.

## 2. Học từ hệ thống khác (theo hiểu biết của Claude, chưa tra cứu lại)
| Hệ thống | Lấy ý gì |
|---|---|
| Linear | Hộp "Cần xử lý" (triage) ở đầu; view lưu sẵn; phím tắt; trạng thái ít mà rõ |
| Jira | **Workflow theo loại việc** (issue type → các bước + người chịu trách nhiệm) |
| GitHub Projects | Cùng dữ liệu, nhiều view: bảng, Kanban, nhóm theo người |
| Quản lý thay đổi ITIL / PagerDuty | **Phân hạng rủi ro**: chỉ thay đổi rủi ro cao mới cần người duyệt, còn lại tự động có nhật ký |
| LangSmith / Langfuse | **Xem từng bước của một lượt AI** (trace): bước nào tốn thời gian / token, lỗi ở đâu, chấm điểm |
| Devin / OpenHands "playbook", Cursor rules | Bài học thành **luật chuẩn** gắn vào loại việc, tự đưa vào prompt lần sau |
| LiteLLM / OpenRouter | **Danh bạ dịch vụ AI dạng dữ liệu** (kiểu adapter + cấu hình), thêm AI không cần sửa code |

## 3. Kiến trúc thông tin (menu mới)
Mỗi thực thể có MỘT trang chi tiết với địa chỉ cố định; mọi màn hình liên kết chéo bằng link + bộ lọc trên URL (vd. `/Tracking?person=O&state=running`).

| Nhóm | Mục menu | Địa chỉ | Thay cho trang cũ |
|---|---|---|---|
| Làm việc | **Văn phòng** (Tổng quan) | `/` | gộp `/`, `/Overview`, `/Work` |
| | **Cần anh duyệt** (huy hiệu số) | `/Approvals` | `/Approvals` (đổi phạm vi) |
| | **Theo dõi việc** | `/Tracking` | `/Work`, Kanban dự án |
| | Giao việc / Yêu cầu | `/Requests` | giữ |
| | Trò chuyện AI · Phòng nghiên cứu | `/Chat`, `/Research` | giữ |
| Đánh giá | **Báo cáo & thống kê** | `/Reports` | gộp `/Performance`, `/Team`, phần số liệu `/Process` |
| | **Mẹo & bài học** | `/Reports/Lessons` | mới |
| Cấu hình | **Quy trình & vai trò** | `/Workflow` | gộp `/Map`, `/Process` (phần quy trình) |
| | **Tài khoản AI** | `/AiServices` | gộp `/AI`, `/AiAccounts`, `/Providers`, `/Accounts` |
| | Cài đặt · Két bí mật | `/Settings`, `/Secrets` | giữ |

Trang chi tiết dùng chung (đích của mọi liên kết): Yêu cầu `/r/R-xxx`, Task `/p/{dự án}/t/{mã}`, Lượt chạy `/Run`, Người / AI `/People/{mã}`, Dự án `/p/{tên}`, Luật `/Workflow/Rules/{mã}`.

## 4. Sáu màn hình

### 4.1 Văn phòng (Tổng quan) — `/`
Câu hỏi trả lời trong 5 giây: *"Có gì cần tôi? Team đang làm gì? Hôm nay tiến được bao nhiêu?"*
- **Dải "Cần anh"** trên cùng (chỉ hiện khi có): số việc nhạy cảm chờ duyệt → link `/Approvals`.
- **4 ô số** (số dùng chữ số đều — tabular): Đang chạy · Chờ review · Xong hôm nay · Bị chặn. Bấm ô ⇒ `/Tracking` lọc sẵn.
- **Bàn làm việc** mỗi người / AI: đang làm task gì, phút đã chạy, hạn mức còn lại. Bấm ⇒ `/People/{mã}`.
- **Yêu cầu đang mở**: thanh tiến độ 10 bước, bước hiện tại. Bấm ⇒ `/r/R-xxx`.
- **Dòng sự kiện**: giao, xong, review, merge, Claude tự quyết (mỗi dòng có link).
- Tự làm mới 15 giây (htmx), có chữ "cập nhật lúc …".

### 4.2 Cần anh duyệt — `/Approvals`
- **Chỉ** các loại: `secret` (cần mật khẩu / khóa / chuỗi kết nối), `security` (thay đổi chạm xác thực, quyền, mã hóa, quét bí mật báo động), `sensitive-data` (đọc / xuất dữ liệu cá nhân, dữ liệu khách), `external` (công khai repo, gửi ra ngoài, thanh toán, xóa vĩnh viễn), `decision` (Claude hỏi quyết định sản phẩm).
- `merge`, `improvement` nhỏ, QC ⇒ Claude tự quyết, ghi vào tab **"Claude đã tự quyết"** (xem lại, có nút "Không đồng ý — mở lại").
- Mỗi thẻ: mức rủi ro (Cao / Trung bình), lý do, ảnh hưởng nếu đồng ý / từ chối, link task / yêu cầu, ai xin, lúc nào.
- **Bí mật không bao giờ hiện / nhập vào ô web thường**: thẻ `secret` có nút "Mở két cục bộ" (trang `/Secrets`, chỉ nghe 127.0.0.1) hoặc lệnh `team.ps1 secret-set -Name X` để tự gõ. Hub chỉ biết *tên* khóa và trạng thái "đã có / chưa có".
- Duyệt / từ chối bắt buộc nhập lý do khi từ chối; có xác nhận.
- Tự động sinh thẻ: cổng kiểm tra phát hiện file nhạy cảm (`auth`, `crypto`, `secret`, `.env`, migration CSDL) trong diff ⇒ tạo `security` trước khi merge.

### 4.3 Theo dõi việc — `/Tracking`
- 3 cách xem cùng một dữ liệu (tab, nhớ lựa chọn): **Theo người** (mặc định: mỗi người một làn, cột Đang làm / Chờ review / Cần sửa / Xong 7 ngày) · **Theo danh sách** (nhóm theo yêu cầu R-xxx hoặc dự án, thanh tiến độ) · **Kanban** (10 trạng thái gộp thành 5 cột).
- Bộ lọc trên URL: dự án, người, trạng thái, độ khó, khoảng ngày; ô tìm kiếm.
- Mỗi thẻ task: mã, tên, người, độ khó S/M/L, vòng, thời gian chạy, cờ (quá giờ, vượt ngân sách, lỗi lặp).

### 4.4 Báo cáo & thống kê — `/Reports`
- Bộ lọc chung: khoảng ngày (7 / 30 ngày / tùy chọn), dự án, người / AI, độ khó.
- Tab **Tổng quan**: số task xong, phút AI, token, đạt vòng 1, số vòng TB; xu hướng theo tuần (đường) — mỗi biểu đồ có bảng số đi kèm.
- Tab **Theo dự án** và **Theo người / AI**: bảng xếp hạng có sắp xếp (aria-sort), **điểm hiệu suất 0–100** = 40% đạt vòng 1 + 25% trong ngân sách + 20% đúng giờ + 15% không lỗi AG (công thức hiện cạnh điểm).
- Tab **Lỗi**: ma trận nguyên nhân (TK / AG / CK / HT…) × loại (bảo mật, giao diện, test…), bấm ô ⇒ danh sách lỗi cụ thể ⇒ task.
- **Xem lại từng bước một yêu cầu** (`/r/R-xxx/review`): dòng thời gian 10 bước; mỗi bước: ai làm (AI nào, model), phút, token, vòng, lỗi phát sinh, chờ bao lâu. Mỗi bước có 2 nút: **"Ghi mẹo"** và **"Đề xuất cải tiến"** ⇒ tạo bản nháp ở Mẹo & bài học, gắn nguồn (yêu cầu / task / bước).
- **Mẹo & bài học** (`/Reports/Lessons`): danh sách nháp / đã thành luật / đã bỏ; lọc theo loại việc, bước, AI; nút "Nâng thành luật chuẩn" (Claude quyết, không cần chủ dự án trừ khi chạm bảo mật).
- Xuất CSV cho mọi bảng.

### 4.5 Quy trình & vai trò — `/Workflow`
- **Loại việc** (cấu hình được): Tính năng có giao diện · Tính năng lõi / CLI · Sửa lỗi · Tài liệu / dữ liệu · Nghiên cứu (chỉ đọc) · Bảo mật. Mỗi loại = danh sách bước có thứ tự: Tiếp nhận → Ý tưởng → Nghiên cứu → Kế hoạch → Duyệt kế hoạch → Chuẩn bị → Code → Cổng KT → QC → Nghiệm thu → Phát hành (bỏ / thêm bước theo loại).
- Mỗi bước: **người phụ trách** (Claude / AI cụ thể / cổng tự động / chủ dự án), **AI dự phòng**, **skill gắn kèm** (nhiều), **luật chuẩn** áp dụng, giới hạn (phút, vòng), điều kiện cần chủ dự án duyệt (chỉ loại Bảo mật / có bí mật).
- **Luật chuẩn**: thư viện luật (mã L-xxx), nguồn (từ bài học nào), phạm vi (loại việc / bước / AI), trạng thái. Luật đang hiệu lực tự chèn vào prompt khi giao task đúng loại / bước.
- Sơ đồ luồng (từ trái sang phải) cho loại việc đang chọn; cấu hình sửa ngay trên trang, có "Lưu" + lịch sử thay đổi (git).

### 4.6 Tài khoản AI — `/AiServices`
- Danh sách dịch vụ AI dạng thẻ: tên, kiểu kết nối, model, vai trò được làm (code / QC / nghiên cứu / chat), số việc song song, trạng thái đăng nhập, hạn mức còn / giờ hồi, chi phí (miễn phí / trả phí), bật / tắt.
- **Thêm dịch vụ** (form): mã, tên, kiểu adapter (`cli-json`, `cli-text`, `openai-compatible-http`), lệnh / URL, mẫu tham số, model, vai trò, giới hạn, tên khóa trong két (KHÔNG nhập giá trị khóa ở đây), link quản lý. Nút "Kiểm tra kết nối".
- Xóa = tắt (giữ lịch sử số đo), có xác nhận.

## 5. Dữ liệu và lưu trữ
| Dữ liệu | Lưu ở | Vì sao |
|---|---|---|
| Dịch vụ AI + tài khoản (không có bí mật) | **SQLite cục bộ** `%USERPROFILE%\.aiteam\hub\office.db`, bảng `ai_service`, `ai_account`, `ai_quota` | Riêng từng máy (đăng nhập khác nhau), sửa qua Hub, thêm / bớt = thêm / xóa dòng. Có "Xuất / Nhập `.ini`" để sao lưu hoặc chép sang máy kia |
| Bí mật | Két DPAPI có sẵn `%USERPROFILE%\.aiteam\vault.json` | Không đổi; dịch vụ AI chỉ tham chiếu *tên* khóa |
| Loại việc, bước, người phụ trách, skill gắn kèm, luật chuẩn | **Trong repo** `config/workflow.json` + `rules/L-xxx.md` | Dùng chung 2 máy qua git, có lịch sử |
| Mẹo / bài học nháp | `lessons/LS-xxx.md` trong repo | Qua git, liên kết tới yêu cầu / task |
| Duyệt | `approvals/A-xxx.json` (có sẵn) + trường `risk`, `kind` mới | Tương thích dữ liệu cũ |
| Số đo | `metrics/runs/*.json` (có sẵn) + `log-run` (V9-17) | Không đổi |

Bảng chính (SQLite):
- `ai_service(id TEXT PK, name, adapter, command, args_template, base_url, models_json, roles_json, max_jobs INT, paid INT, secret_key_name, manage_url, enabled INT, created_at, updated_at)`
- `ai_account(id PK, service_id FK, alias, mode, login_state, checked_at)`
- `ai_quota(service_id, account_id, remaining_pct, rest_until, source, observed_at)`

**Bỏ viết cứng danh sách AI trong C#**: `ProviderKind` enum + factory switch + `AiCatalog.All` + nhãn trong `PerformanceService` ⇒ thay bằng `IAiServiceRegistry` đọc SQLite; 3 adapter chung thay cho lớp riêng từng AI (Codex / OpenCode / agy giữ lớp riêng như "adapter đặc thù" đến khi chuyển hết).

## 6. Quy tắc giao diện (từ ui-ux-pro-max, áp dụng mọi trang)
- Phong cách: **Minimalism & Swiss + Data-Dense Dashboard** (skill gợi ý cho "Productivity Tool" / "Analytics Dashboard"); sáng mặc định, tối là biến thể tông (không đảo màu).
- Màu theo vai trò (token): nền `#F8FAFC`, chữ `#1E293B`, phụ `#475569`, viền `#E2E8F0`, chính `#1E40AF`, nhấn (hành động chính) `#D97706`; trạng thái: chạy xanh dương, chờ review tím nhạt, cần sửa cam, xong xanh lá `#16A34A`, lỗi / bảo mật đỏ `#DC2626`. **Không** dùng màu là tín hiệu duy nhất (luôn kèm chữ / biểu tượng). Không gradient tím-hồng.
- Chữ: **Inter** (tự host trong `wwwroot/fonts`, không gọi Google khi chạy), số dùng `font-variant-numeric: tabular-nums`; thang 12 / 14 / 16 / 20 / 24 / 32.
- Khoảng cách thang 4 px; mật độ cao (density 8): ô số 16 px đệm, bảng 8 px dòng.
- Tương phản ≥ 4.5:1; vòng focus rõ; nút chỉ có icon phải có `aria-label`; vùng bấm ≥ 24 px; sidebar ≥ 1024 px, thu gọn dưới đó; không cuộn ngang ở 1000 px.
- Mỗi màn hình đúng 1 nút hành động chính; trạng thái rỗng / đang tải (skeleton > 1 s) / lỗi có nút thử lại.
- Biểu đồ: đường cho xu hướng, cột cho so sánh, không tròn khi > 5 nhóm; luôn có chú giải + bảng số thay thế.
- Icon SVG một bộ (giữ bộ đang dùng), không emoji.

## 7. Luồng phải chạy được
- Lần đầu (chưa có `office.db`): tự tạo, nhập dữ liệu từ `team.json` / `team.local.json` / `accounts.json` cũ một lần, ghi nhật ký nhập.
- Dữ liệu cũ: task không có loại ⇒ suy từ mã (H = giao diện, V = lõi, W = quy trình…) hoặc "Chưa phân loại"; approvals cũ `merge` ⇒ hiện ở "Claude đã tự quyết".
- Máy không có một AI (vd. không cài agy) ⇒ thẻ hiện "Chưa cài", không lỗi trang.
- Rỗng: không có việc cần duyệt ⇒ "Không có gì cần anh lúc này".
- Hai máy: cấu hình quy trình / luật đi qua git; tài khoản AI riêng từng máy.

## 8. Dữ liệu không tin cậy và tấn công cần chặn
- Mọi chữ từ task / log / AI hiển thị qua Razor encode; markdown render có lọc (không HTML thô).
- Form thêm dịch vụ AI: `command` chỉ cho file `.exe` / `.cmd` có thật, không cho ký tự điều khiển shell; tham số truyền dạng mảng, không ghép chuỗi lệnh.
- `base_url` chỉ `https://` hoặc `http://127.0.0.1`; không theo chuyển hướng sang host khác.
- Hub chỉ nghe `127.0.0.1`; POST có antiforgery; trang `/Secrets` không bao giờ trả giá trị khóa.
- Nhập `.ini`: giới hạn 256 KB, bỏ khóa lạ, không nhận trường bí mật.

## 9. Kế hoạch triển khai (sau khi duyệt mockup)
| Đợt | Task | Nội dung | Ai |
|---|---|---|---|
| 1 Nền | W2-01 | `config/workflow.json` + loại việc + đọc / ghi + test | Codex |
| | V10-01 | SQLite `office.db` + `IAiServiceRegistry` + nhập dữ liệu cũ | Codex |
| | H9-01 | Token giao diện mới + font + khung layout / menu mới | OpenCode |
| | H9-02 | Approvals: loại `secret/security/sensitive-data/external`, rủi ro, tab "Claude đã tự quyết", tự sinh thẻ bảo mật từ cổng KT | Codex |
| 2 Màn hình | H9-03 | Văn phòng (gộp 3 trang) | OpenCode |
| | H9-04 | Theo dõi việc (3 view) | OpenCode |
| | H9-05 | Báo cáo & thống kê + điểm hiệu suất + ma trận lỗi | Codex |
| | H9-06 | Xem lại từng bước + Mẹo & bài học + nâng thành luật | Codex |
| | H9-07 | Quy trình & vai trò (sửa loại việc / bước / skill / luật) | Codex |
| | H9-08 | Tài khoản AI (thẻ + form thêm + kiểm tra kết nối + xuất / nhập .ini) | OpenCode |
| 3 Nối | V10-02 | `team.ps1 assign` / prompt đọc loại việc + luật chuẩn + skill theo bước; bỏ danh sách AI viết cứng | Codex |
| | H9-09 | Liên kết chéo + chuyển hướng trang cũ + xóa trang thừa | OpenCode |

Mỗi task có test nghiệm thu dựng trang thật + ảnh 1000 px; chạy song song khi khác file. Ước tính: 2–3 ngày làm việc của team (phụ thuộc hạn mức Codex).

## 10. Cần chủ dự án quyết
1. Duyệt bố cục 6 màn hình (mockup).
2. Lưu tài khoản AI: **SQLite** (đề xuất, có xuất / nhập `.ini`) hay **chỉ `.ini`**.
3. Danh sách việc phải qua anh duyệt (mục 4.2) đã đủ / thừa / thiếu.
