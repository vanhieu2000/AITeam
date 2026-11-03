# V9-11 · Hạn mức còn lại và giờ hồi

- **Agent:** C
- **Nhánh:** `agent-c/V9-11-han-muc-va-gio-hoi`
- **Yêu cầu:** R-012
- **Ngân sách:** ~250 dòng triển khai/test
- **Độ khó:** M

## Tóm tắt
- Mở rộng state với usedPercent/windowMinutes/resetsAt/source; ghi rõ ước tính hoặc chưa biết, không đoán số dư.

## Đọc
`docs/MO-TA-HE-THONG.md`, yêu cầu B2, `core/AITeam.Core/Routing/`, `core.tests/AcceptanceV407RoutingTests.cs`.

## Phạm vi
`core/AITeam.Core/Routing/Routing.cs`, `RoutingModels.cs`; tạo `QuotaObservation.cs`, `core.tests/V911QuotaTests.cs`; task/bảng việc.

## Không làm
- Không sửa assert/fixture cũ, không khai báo số dư/gói giả vào team.json, không merge main.

## Tiêu chí
- [x] Ba nguồn, nullable, ghi state và nghỉ đến reset khi hết hạn mức.
- [x] Ước tính USD/24 giờ chỉ dự báo hết trước reset; không có chi tiêu ⇒ unknown.
- [x] Đọc cấu hình nhiều cửa sổ: quotas.provider = [{limitUsd,windowMinutes},...].
- [ ] Tích hợp dữ liệu chi tiêu/provider-rate thật; chưa có mẫu rate-limit Codex thật hoặc cost OpenCode.
- [ ] Toàn bộ nghiệm thu không xung đột.

## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Mô hình/quota | 6 test V911 mới đạt: 3 nguồn, forecast, state/reset, cấu hình nhiều cửa sổ, không ghi đè state hỏng. |
| Build | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning, 0 error. |
| Toàn bộ | Core 402/696 đạt, 294 lỗi; Hub 776/790 đạt, 14 lỗi; 0 skip. |
| Xung đột | AcceptanceV407RoutingTests.Broken_state_file_is_treated_as_empty_and_repaired yêu cầu sửa file hỏng bằng dữ liệu rỗng; AGENTS.md cấm coi hỏng là rỗng rồi ghi đè. Save hiện chặn bằng JsonException và giữ byte cũ. |
- DỪNG theo chỉ định khi nghiệm thu cũ xung đột, không sửa assert và không bỏ bảo vệ dữ liệu để qua test.
- Các lỗi khác là nền Windows/4 trích dẫn tài liệu. Mốc reset có thật từ QuotaResetParser được ghi source=provider; không có số đo thì unknown.
- Chưa nối dữ liệu cost thực vào ước tính hoặc đo rate-limit mới trên cloud; không tự nhận tính năng end-to-end hoàn tất. Phút: chưa đo; token: chưa đo.

## Báo cáo của agent — đợt 2
| Tiêu chí | Bằng chứng |
|---|---|
| Cập nhật | Pull commit Claude b449238, gộp origin/main 9bc9a8e vào đúng nhánh cũ; chỉ giải quyết xung đột bảng việc, giữ nguyên test. |
| State hỏng | All đổi tên nguyên byte sang providers.json.hong-giờ-guid, khóa liên tiến trình, không lặp backup; Record/RecordQuota/Clear dùng chung lock. Chặn reparse point. |
| Nghiệm thu mới | Broken_state_file_is_backed_up_then_rebuilt của Claude đạt: gọi Record 2 lần, chỉ 1 backup nguyên byte và 1 state mới. |
| Build/test | Build -warnaserror -nodeReuse:false: 0 warning/0 error; Core 506/835 đạt, 329 lỗi; Hub 803/817 đạt, 14 lỗi nền Linux; chưa chạy Windows. Log /tmp/r012-d2-v911-tests. |
| DỪNG xung đột còn lại | V911QuotaTests.cs:73–74 (test đợt 1 chưa được cập nhật) vẫn Assert.Throws JsonException khi RecordQuota gặp state hỏng và đòi file giữ tại chỗ. Test mới yêu cầu backup rồi chạy tiếp. Không sửa assert, không phân biệt input để né hợp đồng. |
| Giới hạn | Quota có nguồn và dữ liệu nullable; chưa có quota/cost thật để kiểm e2e. Cần Claude cập nhật test V911 cũ trước nghiệm thu; H8-01 quota và V9-15 vẫn chờ V9-11 vào main. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |
