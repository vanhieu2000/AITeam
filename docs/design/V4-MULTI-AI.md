# AITeam v4 — Audit, kiến trúc đa AI, lộ trình

Người viết: Claude (kiến trúc sư / quản lý kỹ thuật). Ngày 23/10/2026.
Đầu vào: yêu cầu "Nâng cấp AITeam thành hệ thống multi-AI tối ưu cho cá nhân".
Dữ liệu audit: mã AITeam tại commit `9817ef0`; mã Agent Office (`tung1998/agent-office`, commit `8a2e19f`, ~80 nghìn dòng Go) đọc trực tiếp; kiểm tra thực tế trên máy DESKTOP-PP14EFS.

---

## A. AUDIT AITEAM HIỆN TẠI

### A1. Đã chạy thật, đã kiểm chứng (giữ nguyên)
| Thành phần | Bằng chứng |
|---|---|
| Giao việc agy chạy ngầm trên **git worktree riêng / nhánh riêng** mỗi task | ~40 task 20–22/10 trên 3 repo |
| **Cổng kiểm tra tự động** (`verify`): commit / push, BOM (tự sửa), quét bí mật, build 0 warning, test (chặn treo 3 phút), phạm vi (chặn), test nghiệm thu không bị sửa, ngân sách | Bắt lỗi thật ở R-001 (chưa commit, test đỏ, ngoài phạm vi) |
| Tự giao lại 1 lần khi cổng kiểm tra trượt | Chạy được; có lỗi đã sửa: dừng agent không dừng lệnh bao ngoài ⇒ thêm `stop` |
| Test nghiệm thu viết trước + khóa không cho agent sửa | Kiểm trong `verify` (so commit) |
| Khóa chống chạy trùng giữa 2 máy (ghi "đang chạy · máy X" lên bảng việc qua git) | Dùng thực tế nhà / công ty |
| Kho bí mật DPAPI + hook chặn bí mật trước commit | Có test (Hub) + hook trên 3 repo |
| Hub ASP.NET Core (127.0.0.1, antiforgery): Tổng quan, Dự án, Task 3 tab, Agent, Hộp thư, Tài khoản, Bí mật, Cài đặt | 159 ca test, clone sạch |
| Số đo mỗi lượt (`metrics/runs`, theo lượt khi `-Resume`), `request-report`, `staff-report` | Dùng cho đánh giá R-001 |
| `review` / `merge` / `try` / `stop` | Mới (21/10), mới chạy thử `review`; `merge` CHƯA chạy thật lần nào |

### A2. Chỉ có trong tài liệu / chưa kiểm chứng
- `team.ps1 merge` (viết 21/10, chưa dùng lần nào).
- Giới hạn thời gian `--print-timeout` theo task: chưa chạy thật với giá trị khác 0 — chưa biết agy dừng có kịp commit không.
- `-Model` theo task: chưa task nào dùng.
- Khung code viết trước, bố cục cụ thể cho task giao diện: mới là quy định (QUY-TRINH v3), chưa áp dụng lần nào.

### A3. Trùng lặp / thừa
- Quy trình viết ở 4 chỗ: `CLAUDE.md` (167 dòng), `QUY-TRINH.md`, `docs/VAN-HANH-HIEU-SUAT.md`, `BAN-GIAO.md` ⇒ dễ lệch nhau. Đề xuất: QUY-TRINH = luật; BAN-GIAO = trạng thái; CLAUDE.md rút xuống "tra cứu + bài học"; VAN-HANH gộp vào HIEU-SUAT-LOG.
- `metrics` (đọc review từ file task) và `staff-report` / `request-report` (đọc `metrics/runs`): 3 báo cáo chồng nhau.
- `metrics-import`: không còn cần (chủ dự án bỏ dữ liệu cũ).

### A4. Phụ thuộc Claude quá nhiều
Claude (phiên desktop đang chạy) làm TẤT CẢ: phân tích, thiết kế, viết task, theo dõi, review nội dung, tự sửa, merge, chụp ảnh, báo cáo, cải tiến công cụ.
- Không có reviewer thứ hai ⇒ review = 1 người, tốn hạn mức Claude, chậm.
- Cả hệ thống chỉ chạy khi có phiên Claude mở (không có tiến trình điều phối nào tự chạy).

### A5. Điểm nghẽn tốn token / thời gian (số liệu R-001)
| Nghẽn | Số liệu |
|---|---|
| Treo (test mở hộp thoại, agy chờ lệnh nền 30 phút) | ~120 phút agent / 372 |
| Agent thử mò khi task thiếu chi tiết | T-007: 73 phút, 2,3 triệu token cho 51 dòng |
| Vòng sửa lại | vòng TB 2,33; đạt vòng 1: 1/3 |
| Claude review thủ công, gõ chuỗi lệnh dài | ~75 phút Claude cho 3 task |

### A6. Rủi ro
| Rủi ro | Mức | Chi tiết |
|---|---|---|
| Agent có toàn quyền shell (`--dangerously-skip-permissions`) | Cao | Chỉ có luật trong prompt / AGENTS.md. Không có rào kỹ thuật chặn push `main`, xóa file ngoài worktree, đọc kho bí mật |
| **Không bảo vệ được nhánh `main` trên GitHub** | Cao | Repo riêng tư gói miễn phí: GitHub trả 403 "Upgrade to GitHub Pro" |
| `team.ps1` 1.422 dòng / 65 hàm, **0 test**; sửa bằng thay chuỗi | Cao | Đã có lỗi hồi quy thật (mất dòng xử lý hết hạn mức khi sửa 21/10, phát hiện kịp) |
| Nuốt lỗi | TB | 5 `catch {}` rỗng, 27 `SilentlyContinue`, 53 `2>$null` |
| Trạng thái rải rác: bảng việc Markdown, `logs/` từng máy, `metrics/runs`, file task | TB | Không có trạng thái job rõ ràng ⇒ khó phục hồi sau crash, Hub phải đoán từ Markdown |
| Hai tài khoản Antigravity không chạy song song | Đã biết | Windows lưu đăng nhập `agy` trong Credential Manager (`gemini:antigravity`), 1 tài khoản / user Windows |
| `cmd /c "$command"` (lệnh build / test từ `projects.json`) | Thấp | Nguồn là cấu hình của chủ dự án, không phải dữ liệu agent |

### A7. Phải giữ nguyên
Worktree / nhánh mỗi task · cổng kiểm tra · test nghiệm thu khóa · Markdown + git làm nguồn sự thật (đổi máy / đổi tài khoản vẫn chạy) · kho bí mật DPAPI · Hub nhẹ (Razor + HTMX) · phiếu yêu cầu + đánh giá hiệu suất · `CHAY-THU.cmd`.

### A8. Đáng học từ Agent Office (đọc mã thật)
| Ý tưởng | Agent Office làm thế nào | AITeam có gì | Đáng làm? |
|---|---|---|---|
| **Gọi nhiều CLI chính thức** | `claude -p --output-format json`; `codex exec --json --sandbox read-only`; `agy`; `gemini`. Mỗi tài khoản 1 thư mục cấu hình (`CLAUDE_CONFIG_DIR`, `CODEX_HOME`) | Chỉ `agy` | **CÓ — P0** (Codex review chỉ đọc) |
| **Kết luận review dạng máy đọc được** | Dòng `VERDICT: AGREE/DISAGREE`, lấy dòng đầu khớp, nếu không thì dòng cuối; không khớp chuỗi con | Claude tự viết "ĐẠT / CẦN SỬA" | **CÓ — P0** (JSON có lược đồ + `VERDICT:`) |
| **Quyền theo cấp + lệnh nguy hiểm luôn hỏi** | Cấp quyền lồng nhau; lệnh chạy không qua shell (cấm `;`, `&&`, `$`); `git push`/xóa luôn hỏi; thư mục nhạy cảm bị chặn | Không có (agy toàn quyền) | **Một phần — P1:** không kiểm soát được bên trong agy, nhưng chặn ở git (hook chặn push `main` từ worktree) + kiểm tra sau khi chạy |
| Ghi mọi lần gọi + ngân sách ngày + cảnh báo gần hạn mức + kết nối dự phòng khi lỗi | Có | Có số đo, chưa có cảnh báo / dự phòng | **CÓ — P0 phần dự phòng; P1 cảnh báo** |
| Worktree + duyệt diff trước khi nhận | Có | Có (nhánh + merge) | Đã có |
| Bộ nhớ agent 2 tầng (ghi chú lõi + theo chủ đề, nén bằng model nhanh) | Có | `staff/` lưu ý riêng, `AGENTS.md` lỗi hay gặp, `BAN-GIAO.md` | **Đủ dùng — chỉ thêm nguồn + ngày cho mỗi mục (P1)** |
| Workflow có cổng duyệt / bỏ phiếu / hội đồng | Có (nặng) | Quy trình cố định | **Không** — quá nặng cho 1 người |
| MCP gateway | Có | Không | **Không lúc này** — chưa có nhu cầu |
| Giám sát tự khởi động lại, tự cập nhật | Có | Không | **Không** — Hub chạy theo yêu cầu là đủ |
| Kênh Discord / Telegram | Có | Không | P2 (nếu muốn giao việc từ điện thoại) |

---

## B. KIẾN TRÚC ĐỀ XUẤT

### B1. Nguyên tắc
1. **Không viết lại.** `team.ps1` vẫn là cửa vào; phần mới viết bằng **.NET (C#) có test** trong thư viện `AITeam.Core` + CLI nhỏ `aiteam.exe`. `team.ps1` gọi sang dần (kiểu "thay từng phần"). Hub dùng chung thư viện ⇒ một nguồn logic, có test.
2. **Git + Markdown vẫn là nguồn sự thật dùng chung giữa các máy** (task, review, kết quả, số đo). Trạng thái tiến trình (PID, nhịp sống) lưu cục bộ từng máy.
3. **Máy kiểm được thì máy kiểm** (build, test, phạm vi, BOM, bí mật, diff đã review). AI chỉ làm phần cần phán đoán.
4. **Lỗi là dừng an toàn**: review timeout / provider lỗi / không đọc được kết quả ⇒ `BLOCKED`, không bao giờ coi là `PASS`.

### B2. Vai trò ↔ provider (cấu hình, không cứng)
| Role | Mặc định | Dự phòng | Ghi chú |
|---|---|---|---|
| Planner / Architect / Manager | **Claude (phiên desktop)** | — | Chỉ việc cần phán đoán: thiết kế, chốt task, quyết định khó, xung đột |
| Developer | **Antigravity (agy)** | Codex (`codex exec`, sandbox ghi trong worktree) | agy hết hạn mức ⇒ chuyển Codex cho task nhỏ |
| Reviewer | **Codex, chỉ đọc** (`codex exec --sandbox read-only`) | Claude CLI (nếu cài) ⇒ Claude desktop | **Cấm tự duyệt**: reviewer ≠ provider đã code |
| QA máy | AITeam Core (`verify`) | — | Build / test / phạm vi / bí mật |
| Báo cáo | AITeam Core (`request-report`, `staff-report`) | — | |

Không đề xuất Claude CLI chạy ngầm làm Developer / Reviewer mặc định: dùng **chung hạn mức với phiên Claude đang quản lý** ⇒ ăn vào phần quan trọng nhất. Chỉ là dự phòng.

### B3. Chạy đồng thời — giới hạn thực tế
| Provider | Song song được | Căn cứ |
|---|---|---|
| agy, 1 tài khoản | **2 job đã chạy song song ổn** (20–22/10). Nhiều hơn: chưa đo | Thực tế |
| agy, tài khoản thứ 2 | **Không song song** trên cùng user Windows (Credential Manager). Chỉ dùng **luân phiên** khi tài khoản 1 hết hạn mức (`switch-account`). Song song thật cần user Windows thứ 2 (P2, cần chủ dự án quyết) | Kiểm tra 23/10 |
| Codex | 1 job (review) — tăng sau khi đo | Chưa cài |
| Claude desktop | 1 (chính phiên quản lý) | — |

⇒ Cấu hình mặc định: **tối đa 2 Developer agy + 1 Reviewer Codex cùng lúc.**
### B3b. Nghiên cứu: tài khoản Antigravity thứ 2 chạy song song bằng user Windows thứ 2 (23/10)
**Căn cứ đã kiểm trên máy:**
- `agy` lưu đăng nhập trong Windows Credential Manager (`gemini:antigravity`), không có file token. Credential Manager tách theo user Windows ⇒ **mỗi user Windows = 1 tài khoản Antigravity** — cách tách chính thống, không đụng token.
- Tiền lệ: app Codex desktop đã tự tạo 2 user `CodexSandboxOffline` / `CodexSandboxOnline` để chạy agent cách ly.
- Máy: Windows 10 Pro; user `ADMIN` thuộc nhóm Administrators (cần UAC khi tạo user); dịch vụ Secondary Logon đang chạy (cần cho `Start-Process -Credential`).

**Cách làm đề xuất (user `aiteam-agy2`, tài khoản thường, không phải admin):**
1. Chủ dự án tạo user (UAC) + mật khẩu ngẫu nhiên; mật khẩu lưu bằng `team.ps1 secret-set -Name AITEAM_AGY2_PASSWORD` (DPAPI, chỉ user ADMIN đọc được). Không ghi vào git / log.
2. Cấp quyền tối thiểu:
   - Modify trên thư mục worktree của agent: `D:\AITeam-work`, `D:\HDVTool-agents`;
   - Modify trên `.git` của từng repo (worktree ghi object vào repo chính);
   - KHÔNG cấp quyền vào `%USERPROFILE%\.aiteam` (kho bí mật) và các thư mục khác của ADMIN.
3. Một lần, dưới user mới (`runas /profile /user:aiteam-agy2`):
   - `agy` → đăng nhập **tài khoản Antigravity số 2**;
   - `gh auth login` (cùng GitHub `vanhieu2000`, để push nhánh agent);
   - `git config --global safe.directory` cho các repo.
4. `team.json`: developer B thêm `"runAs": "aiteam-agy2"` ⇒ `assign` chạy agy bằng `Start-Process -Credential` (+ `-LoadUserProfile`), log ghi vào thư mục user đó được quyền ghi.
5. Developer A giữ user ADMIN + tài khoản số 1 ⇒ **2 tài khoản chạy song song thật**, mỗi tài khoản có hạn mức riêng.

**Phải thử nghiệm trước khi tin (chưa kiểm chứng):**
- Từ user ADMIN không nâng quyền có dừng được tiến trình của user kia không (cho `team.ps1 stop`). Nếu không: dựa vào `--print-timeout` để agy tự dừng, `stop` cần UAC.
- `git` / `dotnet build` dưới user mới: NuGet cache riêng (build lần đầu chậm), safe.directory.
- agy dưới `Start-Process -Credential` không có cửa sổ: có đọc được Credential Manager không (cần nạp profile).

**Rủi ro:** cấu hình ACL sai (quá rộng / thiếu); mật khẩu user nằm trong kho DPAPI; thêm 1 user phải bảo trì.

**Đánh giá:** khả thi bằng cách chính thống. Chi phí làm khoảng 1 task M (`runAs` trong `assign` / `stop` + script cấp quyền + thử nghiệm). Lợi ích: gấp đôi hạn mức Antigravity dùng song song.

### B4. Luồng một task (mới)
```
READY ─assign→ RUNNING (agy, worktree) ─xong→ VERIFYING (máy)
   ├ trượt (lỗi cơ học, ≤1 lần) → RUNNING (tự sửa)
   └ đạt → REVIEWING (Codex chỉ đọc, ghi hash diff)
        ├ PASS → APPROVAL (Claude / chủ dự án duyệt; diff đổi sau review ⇒ review lại) → merge → MERGED → DONE
        ├ CHANGES_REQUESTED (≤1 lần tự động) → RUNNING với danh sách lỗi của reviewer
        └ BLOCKED (timeout / provider lỗi / không đọc được) → chờ Claude
Lỗi được phân loại: hạ tầng · provider (hạn mức / đăng nhập) · build · code · thiết kế · nghiệp vụ. Có trạng thái PAUSED / CANCELLED / INTERRUPTED (tiến trình chết).
```
Bảng việc Markdown vẫn hiện trạng thái cho người đọc; mỗi lần chuyển trạng thái ghi 1 dòng vào `logs/<task>.events.jsonl` (cục bộ) + tóm tắt lên bảng việc (git).

### B5. Hợp đồng review (Codex)
- **Đầu vào (gói review, máy tự gom):** file task (tiêu chí), đoạn thiết kế được trỏ tới, `git diff base...head` (kèm hash), kết quả cổng kiểm tra, báo cáo agent.
- **Đầu ra bắt buộc**: JSON theo lược đồ —
  `verdict` (`PASS` | `CHANGES_REQUESTED` | `BLOCKED`), `findings[]` (`severity` blocker / required / optional, `file`, `line`, `evidence`, `fix`), `unverified[]`, `provider`, `model`, `durationSec`, `tokens`.
  Không đọc được JSON ⇒ `BLOCKED`.
- Lưu: `logs/<task>-review-<n>.json` + tóm tắt vào mục "Review" của file task.

### B6. Rào kỹ thuật (không chỉ dựa vào prompt)
- Hook `pre-push` trong mọi worktree: chặn đẩy lên `main` / `master` (agent vẫn có thể bỏ hook bằng `--no-verify` ⇒ thêm kiểm tra sau chạy: nếu `origin/main` đổi mà không phải do `team.ps1 merge` ⇒ báo động).
- Sau mỗi lượt: kiểm tra không có file thay đổi ngoài worktree (so `git status` của repo chính, kho bí mật không bị đọc / đổi thời gian).
- Merge chỉ qua `team.ps1 merge`, yêu cầu: verify ĐẠT + review PASS trên **đúng hash diff** + có dòng duyệt.

---

## C. LỘ TRÌNH

Ký hiệu độ phức tạp: S (≤ 200 dòng), M (≤ 500), L (> 500).

### P0 — làm ngay (theo thứ tự)
| # | Task | Mục tiêu | File / module | Phụ thuộc | Độ phức tạp | Rủi ro | Nghiệm thu / test | Rollback |
|---|---|---|---|---|---|---|---|---|
| P0-1 | **AITeam.Core + aiteam.exe (khung)** | Thư viện .NET + CLI + project test; đọc `team.json`, `projects.json`, file task (tái dùng logic Hub) | `core/`, `core.tests/`, `AITeam.sln` | — | M | Trùng logic với Hub | Build 0 warning; test đọc cấu hình + parse task | Xóa thư mục, `team.ps1` không đổi |
| P0-2 | **Provider adapter** | `IAgentProvider` (agy, codex, claude-cli), `IProviderHealth` (cài chưa / đăng nhập chưa / hạn mức gần nhất), cấu hình `providers` trong `team.json` | `core/Providers/` | P0-1 | M | Định dạng output CLI đổi | Test bằng tiến trình giả (stdout mẫu thật); `aiteam doctor` liệt kê provider | Cấu hình cũ vẫn chạy (agy mặc định) |
| P0-3 | **Codex Reviewer chỉ đọc** | `aiteam review` gom gói review → `codex exec --json --sandbox read-only` → JSON verdict; timeout / lỗi ⇒ BLOCKED; ghi hash diff | `core/Review/` | P0-2 + **cài Codex CLI** | M | Codex trên Windows chưa kiểm chứng | Test: JSON hợp lệ / hỏng / timeout / thiếu verdict ⇒ BLOCKED; không bao giờ PASS khi lỗi | Không gọi review (cờ cấu hình) |
| P0-4 | **Cross-provider + cấm tự duyệt** | Chọn reviewer ≠ developer; bộ chọn theo role / độ sẵn sàng / lỗi gần đây | `core/Routing/` | P0-2, P0-3 | S | — | Test: dev=agy ⇒ codex; dev=codex ⇒ claude-cli / desktop; không reviewer khả dụng ⇒ BLOCKED | Cấu hình tĩnh |
| P0-5 | **Routing / fallback** | Phân loại lỗi provider (hạn mức / đăng nhập / mạng); đánh dấu "nghỉ" tới giờ X; developer dự phòng | `core/Routing/`, `team.ps1 assign` | P0-2 | S | Phân loại sai | Test bằng log mẫu thật (credits, 429, chưa đăng nhập) | Tắt fallback |
| P0-6 | **Job state + phục hồi** | Trạng thái B4, `events.jsonl`, khóa chống chạy trùng theo task, phát hiện tiến trình chết ⇒ INTERRUPTED | `core/Jobs/`, `team.ps1 status` | P0-1 | M | Trạng thái lệch bảng việc | Test chuyển trạng thái hợp lệ / cấm; mô phỏng PID chết | Bảng việc vẫn là nguồn chính |
| P0-7 | **Gắn vào luồng giao việc** | Sau verify ĐẠT ⇒ review tự động ⇒ CHANGES_REQUESTED tự sửa ≤1 lần ⇒ APPROVAL | `team.ps1 assign` | P0-3..6 | S | Vòng lặp | Test tích hợp với provider giả | Cờ `-NoReview` |
| P0-8 | **Hub: trạng thái + verdict** | Trang task hiện trạng thái job, kết luận review, lỗi của reviewer | `hub/` | P0-6, P0-3 | S | — | Test PageModel | Ẩn khối mới |

### P1 — sau khi P0 chạy ổn 1 tuần
- Hook chặn push `main` trong worktree + kiểm tra sau chạy (B6) — S.
- Duyệt lại khi diff đổi sau review (hash) + nút Duyệt / Từ chối trên Hub (ghi file duyệt, `merge` đọc) — M.
- Phát hiện xung đột trước khi giao song song (2 task cùng file) — S.
- Cảnh báo gần hạn mức / lỗi liên tiếp theo provider; trang Provider trên Hub — M.
- Test cho phần còn lại của `team.ps1` (Pester) hoặc chuyển dần các lệnh lõi sang `aiteam.exe` — L, chia nhỏ.
- Bộ nhớ dự án: `MEMORY.md` mỗi repo (mục có nguồn + ngày), gộp tài liệu quy trình (A3) — S.

### P2 — khi có nhu cầu thật
- Tài khoản Antigravity thứ 2 chạy song song bằng user Windows thứ 2 (cần chủ dự án quyết; có chi phí vận hành).
- Giao việc từ điện thoại (Telegram / Discord).
- Workflow nhiều bước tùy biến, MCP gateway, ước tính chi phí tiền (gói thuê bao không có giá theo token ⇒ chỉ hiện token).

### D. Triển khai
- P0-1 → P0-8 theo thứ tự.
- Phần khung (P0-1, P0-2) Claude tự làm để chốt kiến trúc.
- Từ P0-3 dùng chính quy trình v3 giao Minh / Lan (khung code + test nghiệm thu viết trước).
- Mỗi task merge xong mới làm task sau; hệ thống cũ luôn chạy được.
