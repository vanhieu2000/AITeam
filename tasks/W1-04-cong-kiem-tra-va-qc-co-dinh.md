# W1-04 · Cổng kiểm tra và checklist QC cố định

- **Agent:** C
- **Nhánh:** `agent-c/W1-04-cong-kiem-tra-qc`
- **Yêu cầu:** R-012 (tiếp nối R-011)
- **Ngân sách:** ~100 dòng triển khai
- **Độ khó:** M

## Tóm tắt
- Kiểm tra cảnh báo test mới không dùng helper; bổ sung hướng dẫn test và checklist QC theo plan R-011.

## Đọc
`plans/R-011/plan.md`, `plans/R-012/YEU-CAU-CODEX.md`, `team.ps1`, `tasks/W1-03-sinh-mockup-va-khoa-prep-truoc-duyet.md`.

## Phạm vi
- Sau khi đủ phụ thuộc: `team.ps1`, `docs/TEST-CHUAN.md`, `skills/09-qc/SKILL.md`; hiện chỉ file task này và bảng việc.

## Không làm
- Không triển khai trước W1-03 vào main; không gộp nhánh W1-03 hoặc sửa assert để vượt phụ thuộc.

## Tiêu chí
- [ ] W1-03 có trên origin/main.
- [ ] Cảnh báo ProcessStartInfo ở test mới, miễn helper đúng phạm vi.
- [ ] Tài liệu ≤60 dòng và checklist QC JSON đúng hợp đồng.

## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Phụ thuộc | origin/main mới nhất vẫn là 759ddaf; W1-03 ở nhánh riêng 2423462, chưa có trên main. |
| Chỉ định | A3 trong YEU-CAU-CODEX.md ghi rõ chỉ bắt đầu W1-04 khi W1-03 vào main. |
| Trạng thái | BỊ CHẶN bởi phụ thuộc chưa merge; không thay sản phẩm/test và không tự merge. |
- Chưa chạy build/test cho mã chưa triển khai; chỉ báo cáo/tạo task. Phút: chưa đo; token: chưa đo.
