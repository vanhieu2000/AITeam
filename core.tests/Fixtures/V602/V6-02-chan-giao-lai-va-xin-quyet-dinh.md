# V6-02 · Chặn giao lại và xin quyết định

- **Agent:** A
- **Nhánh:** `agent-a/V6-02-chan-giao-lai-va-xin-quyet-dinh`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~330 dòng
- **Yêu cầu:** R-006
- **Thời gian tối đa:** 25 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
<!-- Cho CHỦ DỰ ÁN đọc trên Hub: 3–5 gạch đầu dòng, lời thường. Có thêm chức năng gì cho người dùng không (ghi rõ "Không" nếu chỉ là kỹ thuật), sửa cái gì, vì sao cần. -->

## Đọc (CHỈ những file này)
<!-- Liệt kê đúng các file agent cần mở (khung code, test nghiệm thu, file cần sửa, 1 file mẫu tương tự). Agent không lục tìm thêm. -->

## Bối cảnh
<!-- Vì sao cần làm, hiện trạng. Trỏ tới thiết kế: "Làm theo docs/design/<file>.md mục X". Liệt kê code có sẵn để tái dùng. -->

## Mục tiêu
<!-- Kết quả mong muốn nhìn từ phía người dùng + các lớp / hàm / màn hình cần có. -->

## Phạm vi (được sửa những gì)
<!-- Ghi đường dẫn trong `…` để cổng kiểm tra đối chiếu, ví dụ `src/Foo/`, `tests/`. -->
-

## Không làm
-

## Ràng buộc (bảo mật / dữ liệu / tương thích)
<!-- Claude điền TRƯỚC khi giao, đừng để tới lúc review mới nêu. Ví dụ:
- Dữ liệu không tin cậy: <file người dùng mở / mục trong zip / form / output thiết bị> → chặn `..`, đường dẫn tuyệt đối, giới hạn kích thước.
- Luồng phải chạy được: <bản cài + portable / lần đầu + dữ liệu cũ / null + rỗng>.
- Không đổi định dạng / hợp đồng: <...>. Giữ tương thích với dữ liệu cũ: <...>.
KIỂM TRA "HỢP ĐỒNG NGOÀI & KHUNG" (IMP-001 — lỗi TK lặp 3/3 task chu kỳ 1), Claude tự đánh dấu trước khi giao:
- [ ] File đưa cho tiến trình / CLI ngoài: mã hóa (UTF-8 có / không BOM), xuống dòng, qua stdin hay tham số.
- [ ] Gọi tiến trình ngoài (git, agy, codex, adb…): giới hạn thời gian, đọc output không chặn, có cần cache / bao lâu.
- [ ] Mọi file khung trong commit "Test nghiệm thu" đều nằm trong Phạm vi, hoặc ghi rõ "KHÔNG đổi" ở Không làm.
- [ ] Task dùng hàm / lớp từ task khác: hàm đó đã có trên main (chưa có ⇒ đưa vào khung của task này).
- [ ] Trang / dịch vụ mới: đã đăng ký DI trong khung + thêm trang vào test dựng trang thật (R-003: /Requests trắng trang).
- [ ] Đọc dữ liệu thật (metrics, phiếu, git, file người dùng): ≥ 1 ca nghiệm thu dựng từ bản sao dữ liệu THẬT, đủ dài / nhiều ngày (R-003: khung giờ, số đo cộng dồn).
- [ ] (IMP-005) Đọc file theo đường dẫn ⇒ chặn symlink / reparse point; dịch vụ singleton / gọi đồng thời (MCP, Hub) ⇒ khóa + 1 ca test đồng thời (V5-01: Codex bắt cả hai).
- [ ] Test có chuỗi GIỐNG bí mật (sk-…, ghp_…, password=…) ⇒ ghép lúc chạy (`string.Concat("sk-", …)`), không ghi thẳng — hook quét bí mật chặn commit (V5-07a).
- [ ] (IMP-005) Hiển thị ghép chữ / giờ ⇒ dựng thử đặc tả bằng dữ liệu đang có (phiếu, log) trước khi giao (V5-04: bàn Claude hiện 2 giờ). -->
-

## Dữ liệu mẫu thật
<!-- Với dữ liệu đọc từ thiết bị / file / API: đường dẫn mẫu THẬT trong `tests/fixtures/...` và nguồn.
Chưa có mẫu thật → ghi "Chưa xác minh trên máy thật, theo <nguồn>". Xóa mục nếu không liên quan. -->
-

## Giao diện
<!-- Xóa mục nếu task không có giao diện. Mockup / mô tả đã được chủ dự án duyệt: docs/design/... mục ...
- Kích thước tối thiểu phải đúng: <ví dụ 1100×700>, DPI 100 / 125 / 150 %.
- Chuỗi dài nhất phải thử: <...>.
- Ảnh phải nộp vào `docs/screenshots/V6-02/`: <trạng thái 1>, <trạng thái 2>, <trạng thái lỗi / rỗng>. -->
-

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
<!-- Claude viết và commit lên main TRƯỚC khi giao: các ca biên, lỗi, bảo mật, dữ liệu cũ, định dạng thật.
Ghi đường dẫn trong `…`, ví dụ `tests/Foo.Tests/AcceptanceT003Tests.cs`. Lúc giao các test này đang đỏ (hoặc chưa biên dịch được vì chưa có lớp). -->
-

## Bố cục giao diện (nếu có)
<!-- Claude ghi sẵn: cây control (TableLayoutPanel/FlowLayoutPanel), cột %, MinimumSize, chỗ xuống dòng, + lỗi hiện có từ tools/UiSnapshot. Agent làm đúng, không thử mò. Agent KHÔNG chụp ảnh — Claude chụp khi review. -->

## Tiêu chí hoàn thành
- [ ] Build `dotnet build AITeam.sln` 0 warning
- [ ] Test `dotnet test AITeam.sln` pass hết, gồm toàn bộ test nghiệm thu (không sửa) + test mới của agent cho phần đã làm
- [ ]

## Báo cáo của agent
<!-- Agent điền:
| Tiêu chí | Bằng chứng (lệnh + kết quả thật / ảnh) |
|---|---|
File đã đổi và lý do · Quyết định tự đưa ra · Điều chưa xác minh / rủi ro. -->

## Review (Claude)
<!-- Claude điền mỗi vòng: "### Vòng N — ĐẠT / CẦN SỬA", đã kiểm tra gì (kèm kết quả cổng kiểm tra), rồi:
**Blocker** / **Required**: file/vị trí · vấn đề · bằng chứng / cách tái hiện · hành vi mong muốn · test xác nhận.
**Optional**: ghi thành task riêng ở hàng chờ, không bắt sửa trong task này. -->

## Kết quả
<!-- Claude điền khi review ĐẠT, cho chủ dự án: 3–5 gạch đầu dòng — đã làm được gì (người dùng thấy gì), kiểm chứng ra sao, còn gì cần cải tiến thêm (đưa vào hàng chờ). -->

## Đánh giá quy trình
<!-- Claude điền ngay sau khi merge. Điểm 1–5 (5 = không vấn đề). Nguyên nhân: TK (task / thiết kế Claude) · AG (agent) · CK (công cụ / cổng kiểm tra) · NV (nghiệp vụ / chủ dự án) · HT (hạ tầng / provider). -->
<!-- Bài học kinh nghiệm: Với lỗi mới phát hiện (nguyên nhân TK / AG / CK), chọn skill đúng bước để cập nhật.
     Đối chiếu mục Lỗi hay gặp của skill, lỗi trùng không ghi lại.
     Chỉ ghi lỗi mới đã đối chiếu skill bằng lệnh (thay mã task thực tế, không chạy trực tiếp lệnh từ template):
     team.ps1 skill-note -Skill code-test -Text "Mô tả bài học mới" -Source S1-07 -->
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | | | | | |
| 2. Ý tưởng | Claude | | | | | |
| 3. Plan / thiết kế | Claude | | | | | |
| 4. Giao việc | Claude | | | | | |
| 5. Thực hiện code | | | | | | |
| 6. Review / QC | Cổng KT + reviewer | | | | | |
| 7. Đánh giá & bàn giao | Claude | | | | | |
