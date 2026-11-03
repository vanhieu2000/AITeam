# V9-12 · Bản ghi lỗi có cấu trúc

- **Agent:** C
- **Nhánh:** `agent-c/V9-12-loi-co-cau-truc`
- **Yêu cầu:** R-012
- **Ngân sách:** ~300 dòng
- **Độ khó:** M

## Tóm tắt
- Chuẩn hóa chín mã nguyên nhân dùng chung cho metrics, staff-report và Hub.

## Đọc
`docs/MO-TA-HE-THONG.md` mục 7.2; yêu cầu B3; `hub/Services/InsightParsers.cs`; `hub.tests/AcceptanceH301Tests.cs`.

## Phạm vi
- Hiện chỉ báo cáo này và bảng việc; chưa đổi parser dùng chung khi hợp đồng cũ chưa được chốt.

## Không làm
- Không sửa assert, không tự đưa NV thành mã thứ mười, không thay đổi H8-14/V9-04 hoặc merge main.

## Tiêu chí
- [ ] Chín mã TK/TN/AG/CK/HT/HM/CH/TO/DP; mã lạ là “khác”.
- [ ] Dùng chung ba nơi và 5 file task thật.

## Báo cáo của agent
| Điều kiện | Bằng chứng |
|---|---|
| Yêu cầu mới | B3 và mục 7.2 quy định chín mã; mã lạ ⇒ “khác”; dữ liệu cũ AG/TK/CK vẫn được nhận. NV không thuộc danh sách. |
| Test cũ | AcceptanceH301Tests.cs:203–207 yêu cầu ParseReviewIssues trả Cause="NV" với dòng - [NV] Lan dừng giữa chừng. |
| Mâu thuẫn | Cùng dữ liệu NV không thể vừa chuẩn hóa Cause="khác" vừa giữ Cause="NV" tại hợp đồng parser dùng cho Hub. |
- DỪNG trước khi đổi parser theo chỉ định chủ dự án; không sửa assert hoặc sinh mã thứ mười để qua test.
- Chưa chạy build/test mới vì chỉ thay tài liệu; chưa viết parser thay thế hoặc gắn vào metrics/staff-report. Phút: chưa đo; token: chưa đo.
- Cần quản lý chốt ánh xạ NV cũ hoặc cập nhật hợp đồng nghiệm thu trước khi tiếp tục; không merge main.
