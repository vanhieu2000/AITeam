# R-005 · Nâng Hub thành văn phòng mini: giám sát + điều khiển, MCP, Accounts, Chat Bridge (AITeam V2 hybrid)

- **Nhận lúc:** 01:12 25/10/2026 · máy DESKTOP-PP14EFS
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > nghiên cứu và cải tiến web ai team thành như 1 văn phòng mini . tô icaanf giám sát và tích hợp điều khiển trên đó
  > (kèm bản dán "PROMPT TRIỂN KHAI AITEAM V2 — HYBRID WEB / DESKTOP / MCP" 10 mục: audit As-Is/Gap/To-Be, MCP Gateway, AI Accounts, Chat Bridge + manual handoff, QA, Research Pool, không bypass đăng nhập / điều khoản, trả audit + kiến trúc + danh sách file + rủi ro connector + kế hoạch PR TRƯỚC khi code)

## 1. Phân tích (Claude)
- Mục tiêu hiểu được:
- Dự án / phần bị ảnh hưởng:
- Quyết định đã tự chọn (không hỏi):
- Điều cần chủ dự án quyết (nếu có):
- Thiết kế: `docs/design/V5-VAN-PHONG-MINI.md` + mockup `docs/design/v5-van-phong-mini-mockup.html` (chờ duyệt)

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| MessageStore (tin nhắn theo task) | AITeam | Claude | ~110 | | nền chung, làm thẳng |
| V5-01 MCP Gateway `aiteam mcp` | AITeam | Minh (A) | ~220 | mặc định | Review AI: có |
| V5-04 trang Văn phòng | AITeam | Lan (B) | ~330 | mặc định | song song V5-01 |
| V5-02 assign đặt mcp_config + lời nhắn báo tiến độ | AITeam | Claude | ~50 | | sau V5-01 |
| V5-05 điều khiển trên web (CommandService) | AITeam | Minh (A) | | | sau V5-04 |
| V5-06 AI Accounts · V5-07 Phòng nghiên cứu · V5-10/11 BrowserConnector | AITeam | | | | sau |

Mỗi file task phải có dòng `- **Yêu cầu:** R-005` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| 01:05 | Nhận yêu cầu | |
| 01:08 | Kiểm tra 7 repo tham khảo (giấy phép, cách làm): grok-mcp dùng cookie + API nội bộ ⇒ loại; WebChatMCP.js tự động web cả khách ⇒ rủi ro điều khoản; codex-chatgpt-web có chế độ thủ công 'Zero Risk' ⇒ học mẫu | |
| 01:12 | Đọc docs MCP Antigravity: `<workspace>/.agents/mcp_config.json`, stdio | |
| 01:15 | **PoC**: máy chủ MCP C# (ModelContextProtocol 2.3.0) + agy không tương tác ⇒ gọi công cụ thật, 40s | khả thi |
| 01:17 | Xong audit + kiến trúc + rủi ro + kế hoạch + mockup; chờ chủ dự án duyệt 3 quyết định | |
| 01:24 | Chủ dự án quyết: Hub điều khiển có giới hạn; chat nghiên cứu TỰ ĐỘNG (ChatGPT qua Codex CLI, Gemini qua Gemini CLI — đã cài 0.65.0, chờ đăng nhập; DeepSeek/Grok qua trình duyệt tự động, bật từng cái); Văn phòng là trang chủ | |
| 01:20 | Claude: MessageStore + 9 test (138 + 289 pass) | |
| 01:27 | Thử API client MCP C# với PoC (CallToolAsync OK) ⇒ test nghiệm thu dùng client thật | |
| 01:35 | Giao V5-01 (Minh): khung + 15 ca nghiệm thu (1 ca chạy thật aiteam mcp) | |
| 01:32 | Giao V5-04 (Lan): khung OfficeBuilder + 11 ca nghiệm thu + trang dựng thật | song song |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-005`, rồi điền:

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian (nhận → báo xong) | |
| Thời gian Claude (phân tích + thiết kế + review + tự sửa) | |
| Thời gian agent (cộng mọi lượt) | |
| Thời gian chờ không ai làm (vd. chờ hạn mức, chờ chủ dự án) | |
| Số lượt agent / số vòng review | |
| Dòng code / ngân sách | |
| Claude tự sửa: số lần / tổng dòng | |
| Lỗi theo nguyên nhân: TK / AG / CK | |

**Đánh giá nhân viên** (chi tiết ghi thêm vào `staff/<mã>-<tên>.md`):

| Nhân viên | Task | Lượt · phút | Vòng | Dòng / ngân sách | Làm tốt | Lỗi AG | Hướng cải thiện |
|---|---|---|---|---|---|---|---|

**Team đang bất ổn ở đâu** (mỗi mục ghi bằng chứng từ nhật ký):
- Kỹ thuật (công cụ, build, cổng kiểm tra, môi trường, model):
- Quy trình (thiết kế, chia task, giao việc, review, phối hợp 2 máy):
- Quản lý (Claude: lỗi TK, quyết định sai, chậm review):
- **Tốn thời gian nhất ở đâu:**
- **Cái gì làm tốt (giữ lại):**
- **Cải tiến quy trình áp dụng cho yêu cầu sau** (tối đa 2, cụ thể, sửa file nào):
  1.
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có / chưa
