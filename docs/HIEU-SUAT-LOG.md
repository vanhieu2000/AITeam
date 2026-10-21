# Nhật ký đánh giá hiệu suất

Mỗi kỳ (5 task merge hoặc 1 tuần): số liệu `team.ps1 metrics`, nhận xét, quyết định. Kế hoạch: `docs/VAN-HANH-HIEU-SUAT.md`.

## Kỳ 0 — Mốc ban đầu (21/10/2026)

**Số liệu:**
- 25 task đã đạt, 3 đang làm.
- 17 lượt chạy có số đo; chỉ có số đo của máy nhà, máy công ty chưa `metrics-import`.

| Chỉ số | Giá trị |
|---|---|
| Thời gian TB / lượt | 20,1 phút (trung vị 20) |
| Thời gian agent / task | 24 phút |
| Token TB / lượt | ~630 nghìn |
| Lượt / task | 1,21 |
| Số vòng review TB | 1,44 |
| Đạt vòng 1 | 68% |
| Claude tự sửa | 76% task |
| Lỗi bảo mật / giao diện / mất BOM trong review | 16% / 8% / 28% |

**Nhận xét:**
- Thời gian tỉ lệ với lượng code agent phải viết. Ví dụ C-008: ~1.450 dòng, 23 phút, 100k token đầu ra.
- Lãng phí lớn nhất là vòng sửa lại: C-010, T-003, T-006 mỗi task 3 vòng, mỗi vòng thêm 20–30 phút.
- Code dư (test nhân bản, file giao diện ~1.000 dòng) vì chưa có ngân sách.
- Agent còn tự thiết kế phần chi tiết vì task chỉ mô tả hành vi, chưa có khung code.

**Quyết định:** áp dụng E1, E2, E3, E7 từ 21/10 (xem kế hoạch). Kỳ 1 so sánh sau 5 task giao theo cách mới.

**Việc của chủ dự án:**
- Trên máy công ty chạy `git pull` trong `D:\AITeam` rồi `.\team.ps1 metrics-import` để cộng số đo máy đó.
- Quyết định có thử E5 (3–4 agent song song) không.
