# V9-14 — Định tuyến theo sáu luật

- **Yêu cầu:** R-012
- Nhánh: `agent-c/V9-14-dinh-tuyen-theo-luat`; yêu cầu R-012.
- Đọc: docs/MO-TA-HE-THONG.md, plans/R-012/YEU-CAU-CODEX.md.
- Phạm vi: theo mã trong yêu cầu R-012; không sửa assert nghiệm thu cũ, không merge main.
- Ngân sách/phút/token: chưa đo.
## Tiêu chí hoàn thành
- [ ] Đủ luồng và bằng chứng nghiệm thu theo R-012; xem giới hạn bên dưới.
## Báo cáo của agent
| Tiêu chí | Bằng chứng |
|---|---|
| Bảng quyết định | DeveloperSelector mới, 9 test V914 cho Dev chỉ định, riêng tư, nghỉ, S/M/L, PowerShell/>300 dòng, tải, model Argon/DeepSeek, khóa và giờ hồi. |
| Build | dotnet build AITeam.sln -warnaserror -nodeReuse:false: 0 warning, 0 error. |
| Test | Toàn bộ: Core 406/699 đạt, 293 lỗi nền; Hub 776/790 đạt, 14 lỗi nền Windows; /tmp/r012-c3-tests. Không sửa assert cũ. |
| Chưa đủ | Hàm thuần đã có; chưa nối assign/nhật ký: main 759ddaf chưa có provider OpenCode C1, quan sát hạn mức B2 và cờ B4. Không tự nhập code các nhánh trước vào nhánh độc lập. |
| Quyết định | Luật độ khó ưu tiên trước cân tải; mặc định riêng tư không được coi là public; nghỉ không tự đổi Dev đã chỉ định. Cần review hợp đồng và tích hợp sau các nhánh nền. |
| Đo đạc | Phút: chưa đo; token: chưa đo. Chưa nghiệm thu điều phối tự động. |

## Báo cáo của agent — phần 2, đợt 2
| Tiêu chí | Bằng chứng |
|---|---|
| Nối assign | Khi không -Dev, Select-TaskDeveloper gọi CLI route-developer → DeveloperRouting → DeveloperSelector. Dev của task, riêng tư, rest, model Argon/DeepSeek, phạm vi khóa đều đi qua bảng quyết định; -Dev cụ thể giữ hành vi chỉ định. |
| Dữ liệu/cân tải | Probe cục bộ codex login status/OpenCode version, không gọi model. Lưu provider được chọn riêng Agent trong .state/developer-routes.json; đếm task 🟡 trên bảng của dự án, không nhầm Codex Agent A thành agy. JSON có khóa/ghi nguyên tử, hỏng không ghi đè. |
| Nhật ký | Set-BoardStatus -Journal ghi provider/lý do ở lần claim; không có người thì Chờ người làm, ghi lý do/giờ thử lại và không khởi chạy AI. |
| Test | 4 test tích hợp mới + 9 test bảng quyết định cũ đạt; có private/rest, Dev ghim, không gọi model khi probe và cân tải theo provider thật. |
| Build/toàn bộ | Build -warnaserror -nodeReuse:false 0 warning/0 error; Core 505/833 đạt, 328 lỗi nền; Hub 803/817 đạt, 14 lỗi nền; PS AST 0 lỗi; chưa chạy Windows. Log /tmp/r012-d2-v914-verified-tests. |
| Giới hạn | Cân tải theo bảng của dự án hiện tại, chưa cộng đồng thời mọi dự án; hook/journal/health CLI thật chưa nghiệm thu Windows. Trạng thái login OpenCode vẫn chưa rõ, không coi kiểm phiên bản là đăng nhập. |
| Đo đạc | Phút: chưa đo; token: chưa đo. |

## Báo cáo của agent — OpenCode
- Sửa `team.ps1`: `-Resume` không gọi `Select-TaskDeveloper`; thêm `Get-RecordedDeveloper`/`ConvertTo-DeveloperProvider` đọc `.state/developer-routes.json` (khóa SHA-256 như DeveloperRouteMemory), rồi mã Agent trong file task / bảng việc (C ⇒ codex, O ⇒ opencode, A/B ⇒ agy). Không xác định được ⇒ giữ đường assign cũ, không gọi CLI định tuyến.
- `Invoke-Assign` thêm cờ `$devFromResume` để agy đọc lại từ file vẫn được dự phòng Codex khi hết hạn mức (giữ test nghiệm thu S1-04).
- `core.tests/AcceptanceS104SkillPromptsTests.cs`: thêm 2 tên hàm vào danh sách nạp AST (`Get-RecordedDeveloper`, `ConvertTo-DeveloperProvider`) — chỉ phần dựng harness, không đổi assert.
- Test mới `V914RoutingIntegrationTests.Resume_keeps_recorded_developer_and_does_not_call_router`.
- Build: `dotnet build AITeam.sln -warnaserror -nodeReuse:false` → 0 warning, 0 error.
- Test: `dotnet test AITeam.sln --no-build --blame-hang-timeout 3m` → Core 834/834, Hub 817/817 (tổng 1651/1651).
- Chưa xác minh: chạy assign thật trên Windows khi `.state/developer-routes.json` có sẵn provider codex/opencode.
