# R-016 · Bo control WinForms MyControls (thiet ke + giao viec + nghiem thu)

- **Nhận lúc:** 13:37 09/11/2026 · máy HIEUDV
- **Trạng thái:** 🟡 Đang làm
- **Lời chủ dự án:**
  > thư mục này là dự án phát triển control custom lại cho winform. mục đích thay thế hiển thị hoặc chức năng của control gốc rất sơ sài hoặc khi muôn lấy truyền nhân data vào control lại phải code lại. tôi muốn xây dựng 1 bộ control cần thiếu để design tốt hơn. cài đặt trực tiếp qua cài đặt design của visual chứ không cần viết lại code. Lên ý tưởng thiết kế cho tôi: về giao diện, chức năng , event ,.. mục đích thiếu kê giao diẹn mềm mại , có bo góc , hiện đại,.. . có các nhóm như, loai menu, dọc hay ngang, dang tree hay list ,... , loại control cha như panel, group,.. thêm lạo thẻ card như web..., các con trol nhập liệu như textbox, label , combobox,.. grid,..  đại khái là tôi thấy devxpress control khá ổn nhưng dll rất nặng. tôi muốn custom theo phong cách cá nhân. Tham khảo thêm cả control bạn đã tại bên dự án CIM_MES. bạn lên ý tưởng và check giao diện chức năng  còn giao cho AI khác làm . bạn lên ý tưởng và quản lí phát yêu cầu và keiemr cuối nghiệm thu
  >
  > (bổ sung) tên các control đặt theo quy chuẩn gồm: my + tên gốc , ví dụ : mytextbox,mylabel,.. các control đóng vào lib gen thành dll để cài dll vào projecct khác có thể dùng control ngay
  >
  > (duyệt thiết kế) Duyệt, làm đi

## 1. Phân tích (Claude)
- Mục tiêu hiểu được: thư viện control WinForms bo góc hiện đại `MyControls.dll` (tên My + tên gốc), chỉnh hoàn toàn trong Designer, có theme + data binding/validation không cần code, cài DLL vào project khác dùng ngay.
- Dự án / phần bị ảnh hưởng: dự án mới MyControls (`D:\99. Test\IOT_Control`, trước là IotScadaControls + IotScadaDemo).
- Quyết định đã tự chọn (không hỏi): .NET Framework 4.7.2, không thư viện ngoài, icon = font Segoe Fluent/MDL2, font mặc định Segoe UI 10, csproj SDK-style để `dotnet build/test` chạy được, 6 giai đoạn ~22 task.
- Điều cần chủ dự án quyết (nếu có): đã duyệt thiết kế + mockup + quy tắc tên (13:3x 09/11).
- Thiết kế: `docs/DESIGN.md`, mockup `docs/design/mockup.html` (repo MyControls)

## 2. Chia việc
| Task | Dự án | Ai | Ngân sách | Model | Vì sao giao / tự làm |
|---|---|---|---|---|---|
| GĐ0 | MyControls | Claude | — | — | Khung dự án, đổi tên, cấu trúc |
| U-01 Theme + lớp nền | MyControls | A | ~450 dòng | mặc định | Lõi Core |
| U-02 DataBinder + Validator | MyControls | B | ~450 dòng | mặc định | Lõi Data, Review AI: có |

Mỗi file task phải có dòng `- **Yêu cầu:** R-016` để `team.ps1 request-report` gom số liệu.

## 3. Nhật ký (ghi ngay khi xảy ra, giờ:phút)
| Giờ | Sự kiện | Ghi chú |
|---|---|---|
| ~11:30 | Nhận yêu cầu | Lên ý tưởng, viết DESIGN.md + mockup |
| ~12:00 | Chủ dự án bổ sung quy tắc tên My* + đóng gói DLL | Cập nhật thiết kế mục 4, 11 |
| 13:37 | Chủ dự án duyệt thiết kế | Bắt đầu GĐ 0 (Claude) |
| ~13:45 | add-project MyControls (kho private vanhieu2000/MyControls) | |
| ~14:05 | Xong GĐ0 (Claude): đổi tên My*, csproj SDK-style net472, sln, test xUnit, BUILD-DLL, AGENTS.md luật riêng | build 0 warning, 1/1 test |
| 13:49 | Xong task + khung + test nghiệm thu U-01 (25 ca), U-02 (16 ca) | Giao A: U-01, B: U-02 (Antigravity) |
| 13:5x | Giao lần 1–2 thất bại trước khi chạy: Set-BoardStatus đòi `<dự án>/requests` (Claude sửa 1 dòng team.ps1, 8a46d33); TaskLinter chặn mục Phạm vi | [CK] + [TK] |
| 14:00 | Giao U-01 → Minh (agy), U-02 → Codex | |
| 14:05 | U-02 Codex xong 277 s; cổng KT lỗi "Không ghi được nhật ký phiếu" (phiếu phải nằm trong repo dự án) ⇒ thêm `requests/R-016…` vào MyControls, chạy lại verify ĐẠT 21/21 | [CK] |
| 14:08 | Review U-02 vòng 1 ĐẠT, merge | 476/450 dòng; 2 Optional |
| 14:17 | U-01 lượt 2 xong (lượt 1 không push ⇒ cổng KT tự giao lại) ; cổng KT ĐẠT 37/37 | [AG] không push |
| 14:20 | Review U-01 vòng 1 ĐẠT, merge | 4 Optional; ngân sách ước thấp [TK] |
| 14:25 | Clone sạch: build 0 warning, 57/57 test; BUILD-DLL ⇒ MyControls.dll 87 KB; project net48 trống tham chiếu DLL build + chạy OK | GĐ1 (U-01, U-02) xong |
| 14:3x | Chủ dự án: "Làm tiếp GĐ2" | Chia 3 đợt × 2 task song song |
| 14:49 | Giao U-03 (agy). U-07 bị KHÓA PHẠM VI (U-03 khai cả `MyControls/Inputs/`) ~25′ | [TK] phạm vi rộng; [CK] khóa giữ phạm vi lúc giao, không đọc lại |
| 15:17–15:20 | U-03 ĐẠT (lượt 2) merge; giao U-07 (Codex) | |
| 15:21 | Giao U-04 (agy, Review AI), U-05 (agy) | Claude tự gỡ thuộc tính cũ của demo trong commit khung để 2 task không cùng giữ file demo |
| 16:22 | U-07 ĐẠT vòng 1 merge (ghép thử với main 99/99) | (gián đoạn ~40′: hạn mức Claude) |
| 16:25 | U-05 ĐẠT vòng 1 merge | |
| 16:2x | U-04: review AI 2 vòng (7 lỗi) → Claude CẦN SỬA 3 lỗi → Codex sửa; test đỏ do test nghiệm thu dùng CultureInfo("vi-VN") ăn thiết lập vùng của máy [TK] → Claude sửa môi trường test | |
| 16:37 | U-04 ĐẠT vòng 2 merge; bỏ miễn trừ quy ước MyButton/MyTextBox/MyToggleSwitch | 145/145 |
| 16:4x | Giao U-06 (agy, Review AI), U-08 (Codex) | |
| 16:49 | U-08 ĐẠT vòng 1 merge | Codex 322 s |
| 16:57 | U-03B (5 Optional gom lại, Codex 190 s) ĐẠT merge | |
| 17:0x | Claude chụp ảnh nghiệm thu bằng app tạm ⇒ phát hiện MyLabel cao 0 px [TK] ⇒ U-05B | |
| 17:22 | U-06 ĐẠT vòng 2 (review AI 6 lỗi, Codex sửa 2) merge; U-05B ĐẠT merge | |
| 17:25 | Nghiệm thu GĐ2: clone sạch build 0 warning, **189/189**, MyControls.dll 152 KB, app ngoài repo tham chiếu dist DLL chụp 3 theme | Ảnh `docs/screenshots/GD2/`. Chưa thử kéo thả trong VS Designer (cần chủ dự án) |

| 19:5x 09/11 | Chủ dự án: "tự động làm xong thì báo cáo không cần xác nhận giai đoạn nữa" ⇒ chạy liền GĐ3→GĐ6 | |
| 20:0x | Kiểm DLL bằng TypeDescriptor: 10 control GĐ2 bị ẨN khỏi Toolbox VS (ToolboxItem(false) của lớp nền kế thừa xuống) [TK] ⇒ U-03C (Codex) merge 20:14 | test quy ước đọc attribute inherit:false |
| 20:1x–20:40 | Viết task + khung + test nghiệm thu GĐ3 (U-09/10/11), GĐ4 (U-12..15), GĐ5/6 (U-16, U-17B, U-18, U-18B, U-19, U-20, U-21A/B) | Khung bị chặn 3 lần ở bước TN: CS0067 sự kiện chưa dùng, xUnit2029 / xUnit1031, NRE trong test [TK] |
| 20:23–21:15 | Merge U-09, U-13, U-15, U-19, U-18B, U-21A (Codex vòng 1); U-11 (review AI 2 vòng + Codex sửa) | Lệnh merge lỗi bước sau với mã có chữ đuôi U-21A [CK] |
| 21:2x | Clone sạch: testhost SẬP ngẫu nhiên ~20 % ⇒ chia đôi merge: chỉ U-11; test agent sửa LicenseManager / DpiHelper (reflection) / designer không Dispose ⇒ Claude sửa test (sự cố chặn dây chuyền) 15/15 không sập | [AG] test sửa trạng thái toàn cục; [CK] cổng KT chạy test 1 lần không bắt flaky |
| 22:1x–22:30 | Merge U-21B, U-10, U-12, U-17B, U-14, U-16, U-18 (đa số: Antigravity + review AI Codex 2 vòng + Codex sửa lỗi cuối) | |
| 22:42 | Codex hết hạn mức (tới 01:09) ⇒ U-17 (perf 10.000 dòng 32 s) chuyển Antigravity tối ưu | |
| 22:49 | Merge U-20 | |
| 23:32 | Merge U-17 (tối ưu bằng SetRowState nội bộ — rủi ro ghi trong review) | |
| 23:3x–00:30 10/11 | U-22 Gallery: ảnh chụp trống (test nghiệm thu chỉ kiểm kích thước [TK]) ⇒ thêm kiểm nội dung; xem 18 ảnh ⇒ sửa bố cục demo + tách U-23 (ký tự & thành gạch dưới ở 18 file, OeeCard đè chữ) | lỗi & có từ GĐ2 [TK] |
| 00:31 | Merge U-23; nghiệm thu cuối: clone sạch 0 warning, **437/437**, 12/12 lần không sập, DLL 310 KB, project net48 ngoài repo chỉ tham chiếu dist DLL build + chạy OK | Ảnh `docs/screenshots/GD6/` |

Sự kiện cần ghi:
- xong phân tích / thiết kế;
- giao task;
- agent xong (phút, cổng kiểm tra);
- review vòng N (ĐẠT / CẦN SỬA, lỗi + nguyên nhân gốc **TK** / **AG** / **CK**);
- Claude tự sửa (bao nhiêu dòng, mấy phút, vì sao);
- merge;
- báo chủ dự án.

| 20:45 09/11/2026 | Cờ tự động MyControls/U-15 lượt 1 | TO |
| 21:05 09/11/2026 | Cờ tự động MyControls/U-11 lượt 4 | TO |
| 21:33 09/11/2026 | Cờ tự động MyControls/U-21B-scada-chart-theo-theme lượt 1 | TO |
| 22:41 09/11/2026 | Cờ tự động MyControls/U-17 lượt 1 | TO |
| 22:42 09/11/2026 | Cờ tự động MyControls/U-17 lượt 3 | HT |
## 4. Đánh giá sau khi xong (Claude)
Dán kết quả `.\team.ps1 request-report -Request R-016`, rồi điền:

| Chỉ số | Giá trị |
|---|---|
| Tổng thời gian (nhận → báo xong) | ~11 giờ (09/11 13:25 → 10/11 00:40, gồm ~40′ gián đoạn hạn mức Claude) |
| Thời gian Claude (phân tích + thiết kế + review + tự sửa) | ~6 giờ (thiết kế 30 task + khung + ~250 test nghiệm thu, review, chẩn đoán sập) |
| Thời gian agent (cộng mọi lượt) | 867 phút (song song tới 6 agent; Codex ~6′/task, Antigravity ~40′/task) |
| Thời gian chờ không ai làm (vd. chờ hạn mức, chờ chủ dự án) | ~0 (Codex hết hạn mức 22:42 ⇒ chuyển Antigravity ngay) |
| Số lượt agent / số vòng review | 76 lượt / vòng TB 1,37 (20/30 đạt vòng 1) |
| Dòng code / ngân sách | thường vượt 1,5–2× ở control lớn (ngân sách ước thấp) |
| Claude tự sửa: số lần / tổng dòng | 1 lần code test (TabControlTests ~60 dòng, sự cố chặn dây chuyền) + ~10 lần sửa khung / test nghiệm thu của chính Claude |
| Lỗi theo nguyên nhân: TK / AG / CK | 11 / 5 / 5 |

```
.\team.ps1 request-report -Request R-016 ⇒ 30 task · 76 lượt agent · 867,1 phút agent · 59,7 triệu token · vòng TB 1,37 · đạt vòng 1: 20/30
```

**Đánh giá nhân viên** (chi tiết ghi thêm vào `staff/<mã>-<tên>.md`):

| Nhân viên | Task | Lượt · phút | Vòng | Dòng / ngân sách | Làm tốt | Lỗi AG | Hướng cải thiện |
|---|---|---|---|---|---|---|---|
| Codex (C) | U-03B/03C/05B/09/13/15/17/17B/18B/19/21A + sửa sau review | 1–2 lượt · ~6′ | 1 | sát ngân sách | nhanh, đúng đặc tả, đạt vòng 1 | không push được nhánh trong sandbox (U-17B) | — |
| Minh (A) | U-10/12/16/20/22 | 2–5 lượt · ~40–60′ | 2–3 | 1,5–4× | làm đủ chức năng lớn | commit file TestResults (U-10); ngồi chờ lệnh treo 30′ (U-20, U-22) | không chạy lệnh nền chờ vô hạn |
| Lan (B) | U-11/14/18/21B/23 | 1–5 lượt | 1–2 | 1,5–2× | U-23 sửa 18 file gọn | test TabControl sửa trạng thái toàn cục .NET ⇒ sập testhost | không đụng static .NET trong test |

**Team đang bất ổn ở đâu** (mỗi mục ghi bằng chứng từ nhật ký):
- Kỹ thuật: (1) testhost sập ngẫu nhiên chỉ lộ khi gộp nhiều task — cổng KT chạy test 1 lần trên nhánh nên không bắt [CK]; (2) `team.ps1 merge` lỗi bước sau với mã có chữ đuôi (U-21A, U-17B); TaskLinter không nhận dòng miễn test nghiệm thu khi file CRLF; (3) Codex hết hạn mức giữa chừng.
- Quy trình: test nghiệm thu "đo được" nhưng không thử dữ liệu thật — lỗi `&`, ảnh chụp trống, control ẩn khỏi Toolbox đều chỉ lộ khi Claude chụp ảnh / kiểm DLL.
- Quản lý (TK): khung code 3 lần bị bước TN chặn (CS0067, analyzer xUnit, NRE) vì Claude chỉ kiểm lỗi CS; ngân sách control lớn ước thấp 1,5–4×.
- **Tốn thời gian nhất ở đâu:** chẩn đoán testhost sập (~45′, chia đôi 8 merge) và vòng review AI 2 vòng × Antigravity 40′.
- **Cái gì làm tốt (giữ lại):** Codex cho task ≤ 400 dòng (đạt vòng 1, ~6′); review AI (Codex) bắt ~30 lỗi thật ở task của Antigravity; chụp ảnh Gallery 3 theme làm bước nghiệm thu cuối; chia đôi merge để tìm gốc lỗi.
- **Cải tiến quy trình áp dụng cho yêu cầu sau** (tối đa 2, cụ thể, sửa file nào):
  1. Trước khi push nhánh khung: chạy ĐÚNG lệnh của bước TN (`dotnet build -warnaserror` toàn solution, đọc mọi warning kể cả analyzer xUnit; `dotnet test --filter` test nghiệm thu phải đỏ bằng Assert / NotImplemented, không NRE). Ghi vào `CLAUDE.md` mục Bài học.
  2. Cuối mỗi giai đoạn (≥ 3 merge): chạy toàn bộ test 10 lần trên clone sạch để bắt sập ngẫu nhiên; thêm luật "test không sửa trạng thái tĩnh của .NET" vào `templates/AGENTS.md`.
- Đã ghi vào `docs/HIEU-SUAT-LOG.md`: có
