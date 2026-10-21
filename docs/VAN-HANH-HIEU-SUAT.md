# Vận hành hiệu suất cao + cải tiến liên tục

Người viết: Claude (quản lý). Bắt đầu 21/10/2026. Chủ dự án giao: "tự theo dõi, đánh giá hiệu suất, cải tiến liên tục".

## 1. Đo gì, ở đâu

| Nguồn | Cách lấy | Nội dung |
|---|---|---|
| `metrics/runs/*.json` (repo AITeam, **mọi máy cộng dồn qua git**) | `team.ps1 assign` tự ghi sau mỗi lượt agent; log cũ: `team.ps1 metrics-import` (1 lần mỗi máy) | Thời gian, token, model, dòng code thêm / xóa, cổng kiểm tra đạt hay không, có tự giao lại không |
| Mục "Review (Claude)" trong file task | `team.ps1 metrics` đọc tự động | Số vòng review, Claude có tự sửa, lỗi bảo mật / giao diện / BOM |
| `docs/HIEU-SUAT-LOG.md` | Claude ghi tay mỗi kỳ đánh giá | Kết luận, thử nghiệm đang chạy, quyết định giữ / bỏ |

Xem nhanh: `.\team.ps1 metrics` (hoặc `-Project X`).

## 2. Chỉ số chính (KPI) và mục tiêu

| KPI | Mốc 21/10/2026 | Mục tiêu | Vì sao quan trọng |
|---|---|---|---|
| Thời gian agent / task (cộng mọi lượt) | 24 phút | **≤ 15 phút** | Chủ dự án chờ ít hơn |
| Token / lượt | ~630 nghìn | **≤ 400 nghìn** | Hạn mức Antigravity (đã hết 2 lần) |
| Lượt agent / task | 1,21 | ≤ 1,1 | Mỗi lượt thêm = 20 phút |
| Số vòng review TB | 1,44 | **≤ 1,15** | Vòng sửa lại là lãng phí lớn nhất |
| Đạt ngay vòng 1 | 68% | **≥ 85%** | |
| Cổng kiểm tra tự động đạt ngay | (bắt đầu đo) | ≥ 80% | Lỗi cơ học phải hết trước khi tới Claude |
| Mất BOM khi review | 28% | 0% | Cổng kiểm tra đã tự sửa BOM |
| Lỗi bảo mật Claude mới bắt được khi review | 16% | 0% (bắt bằng test nghiệm thu trước) | |
| Dòng code / lượt so với ngân sách | (bắt đầu đo) | ≤ 1,0 × ngân sách | Code dư = thời gian + lỗi |

## 3. Nhịp vận hành

**Mỗi task (Claude tự làm, không cần nhắc):**
1. Trước khi giao: khung code + test nghiệm thu + ngân sách + model (CLAUDE.md bước 4).
2. Sau khi agent xong: review như cũ. Ghi rõ trong "Review (Claude)": vòng, Claude tự sửa bao nhiêu dòng, nguyên nhân gốc mỗi lỗi:
   - **TK** — thiết kế / task của Claude thiếu;
   - **AG** — agent làm sai dù task đã rõ;
   - **CK** — cổng kiểm tra đáng lẽ phải bắt.
3. Lỗi loại **CK** ⇒ thêm kiểm tra vào `Invoke-Verify` ngay. Lỗi **TK** ⇒ sửa mẫu task / sổ tay ngay. Lỗi **AG** lặp ≥ 2 lần ⇒ thêm vào "Lỗi hay gặp" của AGENTS.md hoặc lời giao việc.

**Mỗi 5 task merge (hoặc mỗi tuần, cái nào tới trước):**
1. Chạy `.\team.ps1 metrics`.
2. So với KPI và kỳ trước; ghi 1 mục vào `docs/HIEU-SUAT-LOG.md`: số liệu, 3 lỗi tốn thời gian nhất, thử nghiệm giữ / bỏ, thử nghiệm kế tiếp.
3. Báo chủ dự án 3–5 dòng.

**Đầu mỗi phiên:** `doctor`, `inbox.md`, `metrics` (xem có KPI nào xấu đi).

## 4. Thử nghiệm cải tiến (làm từng cái, đo trên ≥ 5 task rồi mới kết luận)

| # | Thử nghiệm | Trạng thái | Kỳ vọng | Cách đo |
|---|---|---|---|---|
| E1 | Khung code + test nghiệm thu trước, ngân sách code, sửa ngoài phạm vi bị chặn | **Đang áp dụng từ 21/10** | vòng review ↓, dòng code ↓ | vòng TB, dòng/ngân sách |
| E2 | Hướng dẫn "làm nhanh" trong lời giao việc (build tăng dần, test có lọc, chỉ mở file cần) | **Đang áp dụng từ 21/10** | phút / lượt ↓ 20% | phút TB trước / sau |
| E3 | Task nhỏ ≤ 500 dòng, tách lõi / giao diện giao song song A + B | **Đang áp dụng từ 21/10** | thời gian chờ kết quả ↓ | phút / task, thời gian giao → merge |
| E4 | `gemini-3.8-flash-medium` cho việc đơn giản | Chờ có task phù hợp | phút ↓, token ↓, vòng không tăng | so theo model |
| E5 | 3–4 agent song song (vùng code riêng) | **Cần chủ dự án đồng ý** (tốn hạn mức nhanh hơn) | thời gian ra tính năng ↓ | phút từ giao → merge cả tính năng |
| E6 | Build sẵn worktree trước khi agent bắt đầu | Ý tưởng | phút ↓ 1–2 | phút TB |
| E7 | Claude tự làm việc ≤ 100 dòng / tự sửa ≤ 50 dòng thay vì trả agent | **Đang áp dụng từ 21/10** | vòng ↓ | vòng TB, dòng Claude sửa |

Quy tắc quyết định:
- Giữ nếu KPI mục tiêu cải thiện ≥ 15% mà các KPI khác không xấu đi quá 10%.
- Bỏ nếu không cải thiện sau 10 task.
- Mỗi lúc chỉ thêm tối đa 2 thay đổi mới để còn phân biệt được tác dụng.

## 5. Cảnh báo — xử lý ngay, không chờ kỳ đánh giá

- Một lượt > 40 phút hoặc > 1,2 triệu token ⇒ task quá to: lần sau chia nhỏ.
- Cổng kiểm tra trượt 2 lượt liền cùng một lỗi ⇒ lỗi do lời giao việc / công cụ: sửa `team.ps1` / AGENTS.md.
- Hết hạn mức ⇒ báo chủ dự án `switch-account`. Nếu > 1 lần / tuần ⇒ giảm token (E1, E4) trước khi tăng số agent.
- Lỗi bảo mật lọt tới review ⇒ thêm ca vào mẫu test nghiệm thu cho loại task đó.
