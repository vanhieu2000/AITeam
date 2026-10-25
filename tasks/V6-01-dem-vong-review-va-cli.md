# V6-01 · Đếm vòng review và CLI

- **Agent:** A
- **Nhánh:** `agent-a/V6-01-dem-vong-review-va-cli`
- **Ưu tiên:** trung bình
- **Ngân sách:** ~320 dòng
- **Yêu cầu:** R-006
- **Thời gian tối đa:** 15 phút
- **Review AI:** có
- **Độ khó:** M <!-- S (≤150 dòng, rõ ràng) · M (≤400, vài file) · L (>400 / nhiều module / rủi ro cao) -->
<!-- Tùy chọn: - **Model:** `gemini-3.8-flash-medium` cho việc nhỏ / dữ liệu; bỏ trống = mặc định. Xem: .\team.ps1 models -->

## Tóm tắt
- Có chức năng mới: lệnh CLI cho biết một task đã trải qua bao nhiêu vòng review.
- Cộng các vòng Claude ghi trong task với các lượt AI yêu cầu sửa trong log.
- Đếm số lần review thực tế, kể cả số vòng bị trùng hoặc nhảy số; không lấy số vòng lớn nhất.
- Nguồn bị thiếu hoặc hỏng không làm mất số vòng đọc được từ nguồn khác.

## Đọc (CHỈ những file này)
- `AGENTS.md` (đặc biệt mục Lỗi hay gặp), `README.md`.
- `docs/design/V4-MULTI-AI.md` mục B1; `docs/HUB-DESIGN.md` mục 3 về nguồn Markdown/log (task này không đổi Hub).
- `tasks/V6-01-dem-vong-review-va-cli.md`.
- `core/AITeam.Core/Review/ReviewRoundCounter.cs`, `core/AITeam.Cli/Program.cs`.
- `core.tests/AcceptanceV601ReviewRoundsTests.cs`, `core.tests/ReviewCliTests.cs`, `core.tests/AITeam.Core.Tests.csproj`.
- `core/AITeam.Core/Review/ReviewRunner.cs` (OutcomeJson), `core/AITeam.Core/Process/ProcessRunner.cs`.
- `core.tests/Fixtures/codex-review-last.json` (mẫu JSON review thật).

## Bối cảnh
Kế hoạch V6-01 đã duyệt, thuộc R-006. Logic dùng chung đặt trong Core, CLI chỉ nạp nguồn và in kết quả theo kiến trúc v4 mục B1. Khung đã nối nhánh lệnh `review-rounds`; hai thân hàm còn TODO(agent). ProjectReference CLI đã có trong project test.

## Mục tiêu
Hiện thực `ReviewRoundCounter.Count(string? taskContent, IEnumerable<string?>? reviewLogs)` và hàm cục bộ `ReviewRounds()` của CLI; giữ chữ ký khung, được thêm helper riêng trong hai file sản phẩm.

## Phạm vi (được sửa)
- `core/AITeam.Core/Review/ReviewRoundCounter.cs`
- `core/AITeam.Cli/Program.cs`
- `core.tests/AcceptanceV601ReviewRoundsTests.cs` — Codex chuẩn bị, Antigravity KHÔNG sửa.
- `tasks/V6-01-dem-vong-review-va-cli.md` — chỉ điền Báo cáo của agent.

## Không làm
- Không đổi Hub, hợp đồng dữ liệu, cấu hình, cách review/provider hoạt động hoặc các lệnh CLI cũ.
- Không sửa `team.ps1`, `CLAUDE.md`, `team.json`, `projects.json`, `skill/`, `tools/`, project hay fixture.
- Không gọi agy/codex, đọc/ghi dữ liệu người dùng thật, sửa/xóa/Skip test nghiệm thu.
- Không triển khai logic bên ngoài hai file sản phẩm. Task này không cần DI vì không thêm dịch vụ Hub.

## Ràng buộc
- Ngân sách ~320 dòng cho khung + test + phần triển khai; tối đa 15 phút. Không tính văn bản đặc tả.
- Hàm Core thuần, không I/O/cấu hình/provider; null/rỗng ở mỗi nguồn đóng góp 0.
- Chỉ mục `## Review (Claude)` đầu tiên ngoài khối mã, tới tiêu đề cấp 2 tiếp theo ngoài mã.
- Đếm từng tiêu đề cấp 3 `### Vòng N`: N là dãy chữ số ASCII, phải kết thúc hoặc có khoảng trắng rồi hậu tố tùy ý. Không lấy max, không loại ĐẠT/CẦN SỬA, số trùng vẫn đếm.
- Không đếm văn bản thường, trích dẫn, danh sách, cấp tiêu đề khác, dòng thụt >= 4 dấu cách/tab hoặc nội dung khối mã có rào backtick/dấu ngã >= 3. Rào đóng cùng loại, dài >= rào mở; rào chưa đóng kéo dài đến hết nội dung.
- Chấp nhận BOM đầu nội dung, LF và CRLF. Mỗi JSON hợp lệ có gốc object và thuộc tính `verdict` dạng chuỗi đúng `CHANGES_REQUESTED` đóng góp 1; phân biệt hoa thường. PASS/BLOCKED/SKIPPED, thiếu/sai kiểu, gốc khác object, JSON hỏng đóng góp 0 riêng từng log.
- CLI: `aiteam review-rounds --task-file <f> --logs <d> --task <T>`. Nạp file UTF-8 có/không BOM. Chỉ file ngay trong logs có tiền tố chính xác `<T>.review-` và hậu tố `.json` (Ordinal); không quét đệ quy, không theo reparse point. Không đếm `<T>-n.json`, `<T>.prep.json`, task có tiền tố gần giống hoặc `.json.bak`.
- Kiểm tra tham số trước I/O: thiếu/null/trắng hoặc tùy chọn không có giá trị → stderr chính xác `LỖI: thiếu tham số bắt buộc (--task-file, --logs, --task).` + xuống dòng; stdout rỗng, exit 2. Kiểm tra thiếu trước mã sai.
- Mã task phải khớp TOÀN CHUỖI `[A-Za-z0-9][A-Za-z0-9_-]{0,63}` (không nhận ký tự xuống dòng cuối): dài 1/64 hợp lệ; 65, dấu chấm, dấu phân cách đường dẫn, wildcard, khoảng trắng, ký tự khác bị từ chối. Mã sai → stderr chính xác `LỖI: mã task không hợp lệ.` + xuống dòng; stdout rỗng, exit 2.
- Không dùng mã task làm mẫu wildcard không kiểm soát, không ghép tham số thành dòng lệnh. Đường dẫn nguồn do người dùng chỉ định có thể tuyệt đối, có dấu/khoảng trắng; không ghi file.
- Thiếu/không đọc được task → task đóng góp 0; thiếu/không liệt kê được logs → logs đóng góp 0; từng log không đọc được/hỏng → riêng log đó 0, giữ log tốt. Không in cảnh báo cho lỗi nguồn; không nuốt mọi exception ngoài lỗi đọc nguồn dự kiến.
- Thành công luôn exit 0, stderr rỗng, stdout duy nhất số nguyên thập phân + xuống dòng; không đọc cấu hình team/provider.
- File C# UTF-8 CÓ BOM. Test tiến trình dùng DLL tại AppContext.BaseDirectory, tham số tách riêng, timeout hữu hạn, chỉ gọi CLI của dự án.

## Dữ liệu mẫu thật
- `core.tests/Fixtures/codex-review-last.json`: kết quả review thật đã có trong repo; test nạp bản được chép kèm output.
- Tên log `<Task>.review-n.json` đã đối chiếu phần review hiện có của team.ps1 khi chuẩn bị. Không có thư mục `tests/fixtures/`; dùng fixture hiện hữu, không tạo dữ liệu giả rồi gọi là mẫu thật.
- Markdown biên và JSON hỏng trong test là dữ liệu dựng có chủ đích. Không cần chạy provider thật.

## Test nghiệm thu
`core.tests/AcceptanceV601ReviewRoundsTests.cs`

## Tiêu chí hoàn thành
- [ ] Core đếm đúng mục Markdown, số trùng/nhảy số, mọi kết luận; bỏ mã/văn bản thường và xử lý BOM/LF/CRLF/null/dữ liệu cũ.
- [ ] JSON đúng gốc/kiểu/giá trị; lỗi một log không ảnh hưởng log tốt; cộng được hai nguồn, dùng cả fixture thật.
- [ ] CLI chỉ chọn log review đúng task ở cấp trực tiếp; nguồn thiếu/không đọc được tính 0 riêng.
- [ ] Mã task 1/64 hợp lệ; mã sai và tham số thiếu trả chính xác stdout/stderr/exit theo đặc tả.
- [ ] CLI chạy với đường dẫn có dấu/khoảng trắng; không chạy provider và không đọc cấu hình.
- [ ] `dotnet build AITeam.sln`: 0 lỗi, 0 warning sau triển khai.
- [ ] `dotnet test AITeam.sln`: toàn bộ pass, test nghiệm thu giữ nguyên. Khi giao, test đỏ vì TODO(agent) là chủ đích.
- [ ] Báo cáo có bảng từng tiêu chí → lệnh + kết quả thật, file đã sửa và lý do, quyết định tự đưa ra, điều chưa xác minh.

## Bàn giao khung (Codex)
- Đã chuẩn bị đặc tả, khung Core + nhánh CLI và 62 ca xUnit; hai hàm sản phẩm chỉ ném NotImplementedException. Không chạy lệnh git theo yêu cầu của bước 4.
- File thay đổi: task này (đặc tả); ReviewRoundCounter.cs (API thuần); Program.cs (định tuyến + khung CLI); AcceptanceV601ReviewRoundsTests.cs (test khóa cho agent).

| Tiêu chí chuẩn bị | Bằng chứng thực tế |
|---|---|
| Khung biên dịch | `dotnet build AITeam.sln -m:1 -p:UseSharedCompilation=false -p:NuGetAudit=false --verbosity minimal`: thành công, 0 warning, 0 error. |
| Test nghiệm thu được phát hiện | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --list-tests --filter FullyQualifiedName~AcceptanceV601ReviewRoundsTests`: 62 ca. |
| Test Core đang đỏ | `dotnet test core.tests/AITeam.Core.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~Empty_or_legacy_task_has_zero_rounds\|FullyQualifiedName~Counts_occurrences_not_max_or_verdict\|FullyQualifiedName~Only_headings_inside_first_review_section_count\|FullyQualifiedName~Code_blocks_cannot_open_or_close_review_sections\|FullyQualifiedName~Json_requires_exact_root_string_verdict\|FullyQualifiedName~Adds_both_sources_and_preserves_good_logs_around_bad_log" --logger "console;verbosity=quiet"`: 34 failed, 0 passed, 0 skipped, do hàm khung chưa triển khai (bỏ dấu thoát Markdown trước ký tự nối khi chạy lệnh). |
| Mã hóa | Kiểm tra ba byte đầu ba file C#: EF BB BF, đều có BOM. |

Quyết định cụ thể hóa: dùng tên log `.review-` theo code hiện hữu; mã task cho phép ASCII chữ/số, gạch dưới/gạch ngang, bắt đầu chữ/số; chỉ mục Review đầu tiên. Không cần DI, không đổi project vì đã có ProjectReference.

Chưa xác minh: chức năng thực tế còn chờ Antigravity triển khai; chưa chạy toàn bộ solution test. Lượt chạy cả 62 ca đã dừng sau khi test tiến trình CLI bị timeout 20 giây với khung ném lỗi; không coi đó là kết quả chạy đủ 62 ca. Chưa kiểm thử riêng reparse point. Lệnh build mặc định trong sandbox trả exit 1 với 0 error/0 warning, không có chẩn đoán; build một node như trên thành công. Restore khi bật NuGet audit báo NU1900 vì không truy cập được nuget.org; tắt audit chỉ cho lượt kiểm tra offline, không sửa cấu hình dự án. Tham số UseSharedCompilation=false tránh lỗi quyền truy cập named pipe compiler server đã thấy trong log chẩn đoán.

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng thực tế |
|---|---|
| Core Markdown/BOM/null/số vòng | Lệnh test toàn solution bên dưới: các ca Core đếm vòng pass. |
| JSON, lỗi riêng từng log, fixture thật | Các ca `Json_requires_exact_root_string_verdict` và `Adds_both_sources_and_preserves_good_logs_around_bad_log` pass. |
| CLI lọc log/nguồn thiếu | Các ca nguồn thiếu/khóa pass; ca lọc log bị chặn tại test dòng 117: tạo thư mục `con` trên Windows ném `DirectoryNotFoundException` trước khi gọi CLI. |
| Mã task, thiếu tham số, đường dẫn có dấu | Các ca biên mã task và lỗi tham số pass; ca đường dẫn có dấu/lọc log chưa xác minh trọn vẹn vì lỗi `con` nêu trên. |
| Build 0 lỗi/0 warning | `dotnet build AITeam.sln` exit 1, 0 warning/0 error, không chẩn đoán; chạy `dotnet build AITeam.sln -m:1 -p:UseSharedCompilation=false -p:NuGetAudit=false --verbosity minimal` exit 0, 0 warning/0 error. |
| Test toàn bộ, giữ nguyên nghiệm thu | `dotnet test AITeam.sln --no-restore --blame-hang-timeout 2m -m:1 -p:UseSharedCompilation=false -p:NuGetAudit=false`: 668 pass, 1 fail, 0 skip (Hub 386/386; Core 282/283); không sửa test nghiệm thu. |
| File và phạm vi | Lượt này chỉ cập nhật báo cáo; giữ triển khai trong `ReviewRoundCounter.cs`, `Program.cs`; cả hai có BOM EF BB BF. |
| File ngoài phạm vi cần Claude quyết | Giữ nguyên `core.tests/ReviewCliTests.cs`: dùng `Path.Combine(AppContext.BaseDirectory, "aiteam.dll")` là bắt buộc theo AGENTS.md để test tiến trình dùng đúng DLL được chép bởi ProjectReference; không sửa file này trong lượt này, chưa đối chiếu bản gốc vì cấm chạy git. |
| Quyết định và điều chưa xác minh | Dùng build một node, tắt compiler server/audit chỉ qua tham số để kiểm tra sandbox; chưa xác minh build mặc định/cổng kiểm tra đạt; không thay môi trường hoặc code để né lỗi test `con`. |
| Bàn giao | Không có công cụ MCP AITeam trong phiên; không chạy git theo chỉ dẫn Codex, hệ thống phụ trách commit/push; cần Claude xử lý test nghiệm thu lỗi tên thiết bị Windows. |
## Review (Claude)
<!-- Claude điền mỗi vòng: "### Vòng N — ĐẠT / CẦN SỬA", đã kiểm tra gì (kèm kết quả cổng kiểm tra), rồi:
**Blocker** / **Required**: file/vị trí · vấn đề · bằng chứng / cách tái hiện · hành vi mong muốn · test xác nhận.
**Optional**: ghi thành task riêng ở hàng chờ, không bắt sửa trong task này. -->
### Vòng 1 — ĐẠT (nghiệm thu, 25/10/2026, Claude) — task đầu tiên chạy trọn quy trình v4
- Bước 4 Codex chuẩn bị (343s, ~967k token vào). Bước 5 Minh 2 lượt (lượt 1 treo vì hộp lỗi dotnet.exe của CLI khung), rồi Antigravity HẾT HẠN MỨC ⇒ Codex dự phòng sửa (111s).
- Bước 6 Codex QC vòng 1 CẦN SỬA 3 lỗi thật: đổi TEMP/TMP toàn tiến trình bằng P/Invoke (Minh né test), chọn nhầm mục Review (StartsWith), không kiểm reparse point thư mục cha. Đã sửa hết.
- Claude sửa: test nghiệm thu (do Codex viết) tạo thư mục tên thiết bị DOS `con` ⇒ Windows cấm ⇒ đổi `thu-muc-con`. Đây là gốc khiến Minh thêm mẹo TEMP.
- Codex QC vòng 2: 1 điểm hẹp (tab sau `##`, `#` đóng tiêu đề) ⇒ Claude nghiệm thu xếp **Optional** (task do mẫu sinh, không gặp thực tế; sửa tiếp tốn ~400k token Codex).
- Build 0 warning; 386 + 283 test (2 lần chạy); cổng KT đạt (lần trượt trước là test MCP chập chờn + khóa file build).
- [TK] (test) test nghiệm thu bước chuẩn bị dùng tên thư mục `con` — mẫu codex-prep cần nhắc "tên thiết bị DOS".
- [AG] (chất lượng) đổi môi trường tiến trình để test qua (Codex QC bắt).
- [CK] (công cụ) cổng KT trượt vì test chập chờn + khóa file khi 2 bản build chạy song song.

## Kết quả
- **Không có chức năng mới cho người dùng** — lõi đếm vòng review + lệnh `aiteam review-rounds` (V6-02 sẽ dùng để dừng giao lại sau 3 vòng).
- Đếm `### Vòng N` trong "## Review (Claude)" + log Codex CHANGES_REQUESTED; chặn mã task lạ, đường dẫn tắt (junction), file hỏng ⇒ 0.
- Kiểm chứng: test nghiệm thu + 2 vòng Codex QC.
- Cần cải tiến (hàng chờ): nhận diện tiêu đề `##<tab>`.

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | ~5' | Brief 8 dòng | — | Giữ |
| 2. Ý tưởng / kế hoạch | Codex | 5 | 92s | Kế hoạch tốt, có dẫn chứng file:dòng | — | Giữ |
| 3. Duyệt | Claude | 5 | ~3' | — | — | — |
| 4. Chuẩn bị task | Codex | 3 | 343s | ~967k token; test tên `con` | TK | Giới hạn đọc file (đã sửa mẫu) + nhắc tên DOS |
| 5. Thực hiện code | Minh → Codex | 3 | ~22' | Treo 1 lượt; mẹo TEMP; hết hạn mức agy | AG/HT | Luật CLI bắt mọi ngoại lệ (đã thêm) |
| 6. QC | Codex | 5 | 47s + 35s | Bắt 3 lỗi thật | — | Giữ |
| 7. Nghiệm thu | Claude | 4 | ~15' | Phải sửa test + chạy lại cổng | CK | Tách việc test chập chờn |
