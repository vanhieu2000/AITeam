# /// <summary>
# /// Ghi bài học vào skills/Skill/SKILL.md dưới Root tuyệt đối của repo AITeam.
# /// Skill chỉ gồm chữ ASCII, số, gạch ngang/gạch dưới; cấm tên DOS (cả phần mở rộng,
# /// khác hoa thường), dấu chấm, dấu phân cách, đường dẫn tuyệt đối và reparse point
# /// tại Root/skills/thư mục skill/file. Sai: "Skill không hợp lệ.".
# /// Thiếu file/mục: "Không tìm thấy skill hoặc mục Lỗi hay gặp.".
# /// Text một dòng không trắng, tối đa 1000 ký tự trước chuẩn hóa; Source tùy chọn
# /// tối đa 100 ký tự; cấm CR/LF cả hai. Sai: "Nội dung ghi chú không hợp lệ.".
# /// Text trim/gộp whitespace thành dấu cách; Source trim, trắng coi như thiếu.
# /// Bullet: "- Text" hoặc "- Text (Nguồn: Source)". So trùng Text ordinal có phân
# /// biệt hoa thường trong mục ## Lỗi hay gặp (cho phép hậu tố tiêu đề), bỏ nguồn
# /// bullet cũ. Trùng khác Source không ghi/commit, trả "Bài học đã tồn tại.".
# /// Chèn cuối mục trước tiêu đề cấp 1/2 kế tiếp; mục cuối chèn cuối file. Giữ BOM,
# /// CRLF/LF, byte ngoài vị trí chèn và trạng thái có/không newline cuối file.
# /// Kiểm git worktree/index đúng file trước mọi ghi, kể cả trùng; file bẩn:
# /// "File skill đang có thay đổi chưa commit.". Chỉ stage/commit đúng file với
# /// pathspec, giữ nguyên file khác đã stage/chưa stage. Không push/reset/clean/stash.
# /// Git timeout hữu hạn; kiểm exit code từng lệnh. Git lỗi ném thông điệp
# /// "Không thể ghi hoặc commit bài học."; không báo thành công/xóa dữ liệu người dùng.
# /// Thành công trả "Đã ghi và commit bài học.". Exception không có tiền tố;
# /// team.ps1 bắt mọi lỗi, in stderr "LỖI: …", exit 1 (khung: "LỖI: chưa làm").
# /// </summary>
function Invoke-SkillNote {
    param(
        [string] $Root,
        [AllowNull()][AllowEmptyString()][string] $Skill,
        [AllowNull()][AllowEmptyString()][string] $Text,
        [AllowNull()][AllowEmptyString()][string] $Source = ""
    )
    throw [System.NotImplementedException]::new(); # TODO(agent)
}
