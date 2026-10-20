# AITeam — team AI nhỏ cho mọi dự án của HieuDV

```
        Bạn (chủ dự án)
             │  yêu cầu bằng lời: "Team: dự án X, làm Y"
             ▼
   Claude — QUẢN LÝ (Claude Code / Claude desktop)
   ý tưởng → kế hoạch → chia task → giao việc → review + test → merge
             │  team.ps1 assign
     ┌───────┴────────┐
     ▼                ▼
  Agent A           Agent B          ← Antigravity CLI (agy) chạy ngầm,
  nền tảng          tính năng           mỗi task một thư mục + nhánh git riêng
     └───────┬────────┘
             ▼
   GitHub private (vanhieu2000/…) — nguồn sự thật duy nhất, máy nào cũng pull về được
```

## 1. Cấu hình nằm ở đâu

| File | Nội dung | Lên git? |
|---|---|---|
| `D:\AITeam\team.json` | Thành viên team, đường dẫn agy, tài khoản GitHub, số vòng review tối đa | Có |
| `D:\AITeam\projects.json` | Danh sách dự án: repo, thư mục, lệnh build/test, vùng code của A/B | Có |
| `D:\AITeam\team.local.json` | Ghi đè theo máy (ổ đĩa khác, thư mục khác…) | **Không** |
| `D:\AITeam\CLAUDE.md` | Sổ tay quy trình cho Claude (quản lý) | Có |
| `D:\AITeam\templates\` | Mẫu `AGENTS.md`, task, bảng việc, `.gitignore` cho dự án mới | Có |
| `%USERPROFILE%\.claude\CLAUDE.md` | 3 dòng nhắc Claude đọc sổ tay trên (do `team.ps1 setup` tạo) | (theo máy) |
| `<dự án>\AGENTS.md`, `<dự án>\tasks\` | Luật cho agent và bảng việc của từng dự án | Có (repo dự án) |

Đăng nhập **không** nằm trong file nào ở trên. GitHub và Antigravity lưu đăng nhập trong Windows Credential Manager.
Vì vậy kho cấu hình này đưa lên git an toàn.

Ví dụ `team.local.json` cho máy công ty (dự án để ở ổ E):
```json
{ "projectsRoot": "E:\\Projects", "workRoot": "E:\\AITeam-work", "paths": { "HDVTool": "E:\\Projects\\HDVTool" } }
```

## 2. Giao việc cho team

Mở Claude (Claude Code hoặc tab Code của Claude desktop), mở thư mục `D:\` hoặc thư mục dự án, rồi nói bằng lời thường. Ví dụ:

- `Team: dự án HDVTool, thêm chức năng xuất báo cáo Excel cho Nhắc việc`
- `Team: tạo dự án mới "QuanLyKho" (web .NET + SQLite trên máy), lên ý tưởng và làm bản đầu`
- `Team: lấy dự án github.com/xxx/yyy về, đánh giá code và sửa lỗi build`
- `Team: báo cáo tiến độ` hoặc `team status`

Claude tự làm hết: lập kế hoạch, viết task, giao cho A/B, chờ, review, viết test kiểm chứng, chụp giao diện, trả về sửa hoặc merge, rồi báo lại cho bạn.

Claude chỉ hỏi bạn khi:
- cần bạn chọn hướng sản phẩm;
- cần đăng nhập tài khoản;
- cần bấm UAC;
- Antigravity hết hạn mức.

## 3. Vận hành hằng ngày

| Việc | Lệnh (chạy trong `D:\AITeam`) |
|---|---|
| Kiểm tra mọi thứ sẵn sàng | `.\team.ps1 doctor` |
| Xem tiến độ, agent đang chạy | `.\team.ps1 status` |
| Đổi tài khoản Antigravity (hết hạn mức) | `.\team.ps1 switch-account` |
| Thêm dự án | `.\team.ps1 add-project -Project Ten -New` (mới) / `-Repo owner/ten` (trên GitHub) / `-Path D:\Ten` (trên máy) |

Bình thường bạn **không cần** gõ các lệnh này. Claude tự chạy.

## 4. Đổi tài khoản

| Đổi gì | Làm gì |
|---|---|
| Tài khoản **Claude** | Đăng nhập tài khoản mới trong app Claude (hoặc `/login` trong Claude Code). Không cần làm gì thêm: quy trình nằm trong file. |
| Tài khoản **Antigravity** | `.\team.ps1 switch-account` → gõ `/logout` → chạy lại → đăng nhập tài khoản khác → `/quit`. Kiểm tra lại bằng `.\team.ps1 doctor`. |
| Tài khoản **GitHub** | `gh auth login --web`, rồi sửa `github.owner` trong `team.json` nếu đổi chủ sở hữu kho. |

Lưu ý: hai tài khoản Antigravity không chạy song song được trên cùng một user Windows. Các agent dùng chung tài khoản đang đăng nhập; hết hạn mức thì đổi sang tài khoản còn lại.

## 5. Máy mới (nhà / công ty)

1. Cài công cụ:
   ```powershell
   winget install Git.Git GitHub.cli
   ```
   Cài thêm SDK mà dự án cần, ví dụ `Microsoft.DotNet.SDK.10`.
2. Đăng nhập GitHub và lấy bộ team về:
   ```powershell
   gh auth login --web
   gh repo clone vanhieu2000/AITeam D:\AITeam
   ```
3. Cài Antigravity CLI:
   ```powershell
   irm https://antigravity.google/cli/install.ps1 -OutFile agy-install.ps1
   .\agy-install.ps1 --dir C:\Tools\agy\bin
   C:\Tools\agy\bin\agy.exe
   ```
   Lệnh cuối mở agy để đăng nhập. Đăng nhập xong thì `/quit`.
4. Gắn quy trình cho Claude và kiểm tra:
   ```powershell
   cd D:\AITeam
   .\team.ps1 setup
   ```
   Nếu máy dùng ổ / thư mục khác, tạo `team.local.json` trước.
5. Lấy dự án về:
   ```powershell
   .\team.ps1 add-project -Project HDVTool -Repo vanhieu2000/HDVTool
   ```
   Lệnh này clone nếu máy chưa có.

## 6. An toàn

- Agent chạy với quyền tự duyệt lệnh (`--dangerously-skip-permissions`), vì chạy ngầm không có ai bấm cho phép.
- Giới hạn bằng luật trong `AGENTS.md`:
  - chỉ làm trong thư mục task;
  - không đụng hệ thống hay dữ liệu thật;
  - không push `main`.
- Claude kiểm tra lại khi review; chỉ Claude merge vào `main`.
- Mọi kho đều **private**. Bí mật (khóa, mật khẩu, `.env`) bị chặn bởi `.gitignore` và được kiểm tra trước mỗi lần push.
- Mọi thay đổi đều có lịch sử git → quay lại bản cũ được bất cứ lúc nào.
