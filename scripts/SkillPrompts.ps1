# /// <summary>
# /// Nối chỉ dẫn skill vào Prompt, giữ nguyên toàn bộ nội dung cũ và không đọc nội dung skill.
# /// Root là gốc AITeam tuyệt đối, không lấy từ thư mục dự án hoặc thư mục hiện hành.
# /// Stage: plan → 03-research, 04-ke-hoach; prep → 06-chuan-bi-task, code-test;
# /// code → 07-code, code-test. Sau đó thêm skill stack theo thứ tự winforms, web, app.
# /// Stack không phân biệt hoa thường: WinForms; ASP.NET hoặc Blazor; MAUI, console hoặc CLI.
# /// Stack null/rỗng/lạ không thêm skill stack; hỗn hợp thêm đủ, không lặp code-app.
# /// Chỉ phát đường dẫn tuyệt đối tới file skills/<id>/SKILL.md tồn tại, không reparse point
# /// ở skills, thư mục id hoặc file. Thiếu file bỏ qua; không có file hợp lệ trả nguyên Prompt.
# /// Mỗi file một dòng: Đọc skill: `đường dẫn tuyệt đối`. Không lặp đường dẫn kể cả gọi lại.
# /// Khi có skill, thêm đúng một câu: Được phép đọc các skill được liệt kê dù danh sách file cũ bị giới hạn.
# /// Không thay đổi quyền sandbox, không thực thi Stack, không nhúng Stack vào prompt.
# /// Stage sai ném ArgumentException("Bước skill không hợp lệ."); Root không tuyệt đối
# /// ném ArgumentException("Gốc AITeam phải là đường dẫn tuyệt đối.").
# /// </summary>
function Add-SkillPrompt {
    param(
        [AllowEmptyString()][string] $Prompt,
        [string] $Root,
        [string] $Stage,
        [AllowNull()][AllowEmptyString()][string] $Stack
    )
    throw [System.NotImplementedException]::new() # TODO(agent)
}
