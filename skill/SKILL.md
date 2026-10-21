---
name: aiteam
description: Điều hành team AI của HieuDV — Claude là quản lý (ý tưởng, thiết kế, kế hoạch, review), các agent Google Antigravity CLI (agy) là lập trình viên. Dùng khi người dùng nói "Team:", "đội", "giao việc", "Antigravity", "agy", "agent", "dự án mới", "lấy repo về", "review task", "tiến độ", hoặc làm việc trong một dự án có trong projects.json của AITeam. Cũng dùng khi cần ý kiến thứ hai / review độc lập bằng model khác.
---

# AITeam — quản lý team AI

Bộ vận hành nằm ở `{{AITEAM}}` (repo private `vanhieu2000/AITeam`).

**Trước khi làm bất cứ gì với team: `git pull` rồi đọc `{{AITEAM}}\QUY-TRINH.md` (1 trang, quy trình đang áp dụng) và `{{AITEAM}}\BAN-GIAO.md` (việc đang dở).** `{{AITEAM}}\CLAUDE.md` chỉ để tra cứu chi tiết khi cần.

Tóm tắt nhanh:
- **Quy trình:** thiết kế trước (Claude; giao diện có mockup chủ dự án duyệt, dữ liệu có mẫu thật) → task + **test nghiệm thu viết trước** → `team.ps1 assign` (chạy nền, tự giữ chỗ theo máy, tự chạy cổng kiểm tra `verify`) → review Blocker / Required / Optional → merge hoặc `-Resume -Feedback`.
- **Lệnh:** `{{AITEAM}}\team.ps1 doctor | status | new-request | assign | review | merge | stop | try | request-report | staff-report | verify | metrics | ask | add-project | new-task | cleanup | secret-*`.
- **Vai trò cố định:** Claude chốt task đầy đủ một lần (khung code, test nghiệm thu, bố cục, file cần đọc, ngân sách, thời gian) rồi review; agent CHỈ viết code.
- **Cách chạy:** PowerShell với `dangerouslyDisableSandbox: true`. Việc dài thì `run_in_background: true`.
- **Bí mật:** chỉ nằm trong kho cục bộ (`team.ps1 secret-set`) hoặc `.env.local` đã bị gitignore. KHÔNG đưa vào prompt cho agent, không đưa lên git. Mọi repo có pre-commit hook `tools\secret-scan.ps1`.
