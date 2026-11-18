# Kế hoạch dự án AITeam

> **Nguồn sự thật duy nhất của dự án.** Claude viết ngay khi nhận yêu cầu và cập nhật khi có yêu cầu mới hoặc thay đổi.
> Mọi AI phải đọc file này trước khi làm và chỉ làm đúng mã `CN` được giao. AI khác Claude KHÔNG sửa file này.

## 1. Tổng quan
- **Mục tiêu:** Điều phối team AI làm dự án cho chủ dự án
- **Người dùng:** HieuDV
- **Công nghệ:** ASP.NET Core .NET 10 Razor Pages + HTMX, PowerShell
- **Lệnh build:** `dotnet build AITeam.sln`
- **Lệnh test:** `dotnet test AITeam.sln`

## 2. Yêu cầu
| Mã | Ngày nhận | Lời chủ dự án | Mã chức năng | Trạng thái |
|---|---|---|---|---|
| R-018 | 19/11/2026 | Tách giao diện và backend, kế hoạch chung ngay khi nhận yêu cầu, chuẩn test, áp dụng cho toàn bộ dự án | CN-01 đến CN-05 | Đang làm |

## 3. Mockup đã duyệt
Giao diện chỉ được làm theo mockup có trạng thái **Đã duyệt**.

| Màn hình | File mockup | Ngày duyệt | Trạng thái |
|---|---|---|---|
| AITeam Hub | `docs/HUB-DESIGN.md` | Trước 19/11/2026 | Đã duyệt |

## 4. Danh sách chức năng
### CN-01 · Phân vai khi giao việc
- **Thao tác:** chạy `team.ps1 assign`
- **Quy tắc nghiệp vụ:** UI và TEST giao Codex, BE giao Antigravity; task phải có Loại và mã chức năng trong kế hoạch.
- **Lỗi và cách hiển thị:** giao sai người hoặc thiếu mã thì báo lỗi tiếng Việt và không giao.

### CN-02 · Chặn sửa sai vùng
- **Thao tác:** cổng kiểm tra `team.ps1 verify`
- **Quy tắc nghiệp vụ:** task BE không được sửa file giao diện; agent không được sửa file kế hoạch.

### CN-03 · Test vùng
- **Thao tác:** cổng kiểm tra chạy bộ lọc test của task
- **Quy tắc nghiệp vụ:** toàn bộ test chỉ chạy một lần khi merge.

### CN-04 · Lời giao việc theo vai
- **Thao tác:** `assign` tạo lời giao việc đúng vai trò, review tối đa 2 lượt.

### CN-05 · Dự án mới và luồng làm việc
- **Thao tác:** `add-project` tạo file kế hoạch, luồng làm việc theo phân vai mới.

## 5. Phân công
| Mã | Giao diện (Codex, dự phòng Claude) | Test (Codex, dự phòng Claude) | Backend + nối giao diện (Antigravity) | File giao diện | File backend | Trạng thái |
|---|---|---|---|---|---|---|
| CN-01 đến CN-05 | Không có | Codex | Codex (việc công cụ, Loại KT) | Không có | `team.ps1`, `projects.json`, `team.json`, `config/workflow.json` | Đang làm, task Q6-01 |

## 6. Kế hoạch test
| Mã test | Chức năng | Loại (Đúng / Lỗi / Liên kết) | Ca kiểm tra | Kết quả mong đợi | File test | Trạng thái |
|---|---|---|---|---|---|---|
| T-01 | CN-01 | Lỗi | Giao task UI cho `agy` | Bị từ chối | `tools/test-verify.ps1` | Chưa làm |
| T-02 | CN-01 | Lỗi | Giao task BE cho `codex` | Bị từ chối | `tools/test-verify.ps1` | Chưa làm |
| T-03 | CN-01 | Lỗi | Task có mã chức năng không có trong kế hoạch | Bị từ chối | `tools/test-verify.ps1` | Chưa làm |
| T-04 | CN-02 | Lỗi | Task BE sửa `.Designer.cs` | Cổng kiểm tra không đạt | `tools/test-verify.ps1` | Chưa làm |
| T-05 | CN-03 | Đúng | Task có Test vùng | Chỉ chạy bộ lọc của task | `tools/test-verify.ps1` | Chưa làm |

## 7. Tiến độ
- **Đang làm:** Q6-01 công cụ quy trình v6.
- **Việc tiếp theo:** áp dụng quy trình cho HDVTool, ChinaAndroidToolbox, MyControls.
- **Hàng chờ:** Không có.

## 8. Lịch sử thay đổi
| Ngày | Yêu cầu | Thay đổi | Mã chức năng ảnh hưởng |
|---|---|---|---|
| 19/11/2026 | R-018 | Tạo kế hoạch chung theo quy trình v6 | CN-01 đến CN-05 |
