# Brief R-006 (thử quy trình v4) — giới hạn vòng review
- **Mục tiêu:** một task bị trả về sửa quá `review.maxRounds` vòng (team.json, hiện 3) thì DỪNG giao lại tự động, tạo mục "Cần anh duyệt" để chủ dự án quyết (giao tiếp / Claude tự sửa / bỏ task).
- **Đếm vòng:** số tiêu đề `### Vòng N` trong mục "## Review (Claude)" của file task + số file `<Task>.review-*.json` có kết luận CHANGES_REQUESTED trong thư mục logs của dự án.
- **Nơi chặn:** `team.ps1 assign … -Resume` (giao lại có góp ý) và lần tự giao lại sau review AI CẦN SỬA. Khi đủ vòng: không chạy agent, gọi `approval-request -Kind decision` (đã có), bảng việc ghi "⏸ Chờ anh quyết · quá N vòng review".
- **Lõi:** logic đếm là hàm thuần trong `core/AITeam.Core/Review/` + lệnh `aiteam review-rounds --task-file <f> --logs <d> --task <T>` in số vòng (mã thoát 0) để team.ps1 gọi; có test nghiệm thu.
- **Không làm:** Playwright / E2E (cần tải phần mềm — để sau), không đổi trang Hub.
- **Ràng buộc:** không phá lệnh assign hiện có (task chưa có vòng nào ⇒ chạy như cũ); file task / json hỏng ⇒ coi như 0 vòng từ nguồn đó, không ném lỗi; mã task kiểm mẫu `^[A-Za-z0-9][A-Za-z0-9-]{0,63}$`.
- **Tiêu chí xong:** test nghiệm thu lõi pass; `tools/test-verify.ps1` có 1 kịch bản: task đã 3 vòng ⇒ assign -Resume không chạy agent và tạo mục duyệt.
- **Tiền tố mã task:** V6. Ưu tiên 1–2 task (lõi + nối team.ps1).
