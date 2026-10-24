# H3-06 · Hub: trang Provider (AI nào sẵn sàng, đang nghỉ tới giờ nào, kiểm tra, hết nghỉ)

- **Agent:** B
- **Nhánh:** `agent-b/H3-06-hub-providers`
- **Ưu tiên:** cao
- **Ngân sách:** ~260 dòng
- **Yêu cầu:** R-004
- **Độ khó:** M
- **Thời gian tối đa:** 30 phút

## Tóm tắt
- **Chức năng mới — trang "Provider":** bảng các AI của team (Antigravity, Codex…): vai trò, model, sẵn sàng hay đang nghỉ (hết hạn mức / chưa đăng nhập) tới giờ nào.
- Nút **Kiểm tra** (đã cài chưa, đã đăng nhập chưa) và **Hết nghỉ** (khi anh biết hạn mức đã có lại, ví dụ vừa đổi tài khoản).
- Thứ tự dự phòng (Developer: agy → codex; Reviewer: codex) và lịch sử nghỉ gần đây.

## Đọc (CHỈ những file này)
- `hub/Pages/Providers.cshtml.cs` (khung — điền `TODO(agent)`)
- `hub/Pages/Providers.cshtml`, `hub/Pages/Shared/_ProviderHealthPartial.cshtml` (viết theo Bố cục)
- `core/AITeam.Core/Providers/ProviderConfig.cs`, `core/AITeam.Core/Providers/ProviderModels.cs`, `core/AITeam.Core/Routing/RoutingModels.cs`, `core/AITeam.Core/Routing/Routing.cs` (chỉ đọc API: `ProviderSettings.Load`, `Create`, `ProviderStateStore.All/RestOf/Clear`)
- `hub/Pages/Index.cshtml.cs` (chỉ xem cách dựng `PartialViewResult` trong `OnGetRunningAgents`)
- `hub/Pages/Inbox.cshtml` (class CSS có sẵn)
- `hub.tests/AcceptanceH306Tests.cs` (test nghiệm thu — đặc tả chính xác)

## Bối cảnh
Hub đã tham chiếu `AITeam.Core` (R-004) ⇒ dùng chung cấu hình provider và trạng thái nghỉ `.state/providers.json` với `aiteam.exe`, KHÔNG viết lại.

## Ràng buộc
- Id từ form / URL chỉ nhận provider có trong cấu hình. Không `Html.Raw`. Kiểm tra provider tối đa 20 giây, lỗi ⇒ hiện "LỖI", không làm sập trang.

## Bố cục giao diện (`Providers.cshtml`, `@page "/Providers"`, tiêu đề "Provider")
1. Hộp `Message` (xanh) / `ErrorMessage` (đỏ) như `Inbox.cshtml` dòng 7–21; `Model.Error` ⇒ hộp đỏ.
2. Chữ phụ `muted`: "Provider = AI chạy bằng dòng lệnh chính thức của từng hãng. Hết hạn mức ⇒ tự nghỉ tới giờ ghi trong câu báo lỗi rồi dùng lại. Bấm 'Hết nghỉ' khi anh biết hạn mức đã có lại (vd. vừa đổi tài khoản)."
3. Panel "Provider" (`overflow-x:auto`): bảng cột Id (`<b>`) · Loại · Vai trò · Model · Tối đa job · Trạng thái · Kiểm tra · (nút).
   - Trạng thái: `!Enabled` ⇒ `span.pill.n` "Đang tắt"; `RestUntil` có ⇒ `span.pill.err` "Nghỉ tới HH:mm dd/MM" (thuộc tính `title` = "{RestReason}: {RestDetail}") ; còn lại ⇒ `span.pill.ok` "Sẵn sàng".
   - Kiểm tra: `<button class="btn sm" hx-get="?handler=Health&id=@r.Id" hx-target="#health-@r.Id" hx-swap="innerHTML">Kiểm tra</button>` và `<span id="health-@r.Id"></span>`.
   - Cột cuối: chỉ khi đang nghỉ — `<form method="post" asp-page-handler="Clear">` + `@Html.AntiForgeryToken()` + `<input type="hidden" name="id" value="@r.Id">` + `<button class="btn sm">Hết nghỉ</button>`.
4. Panel "Thứ tự dự phòng": mỗi dòng `<b>@Role</b>: <code>@Chain</code>`.
5. Panel "Lịch sử nghỉ gần đây": bảng Provider · Lý do · Từ ("HH:mm dd/MM" của At) · Tới (Until) · Chi tiết (1 dòng, `text-overflow:ellipsis`, `max-width:420px`, `title` = đầy đủ). Rỗng ⇒ `div.empty` "Chưa có lần nghỉ nào.".
6. `_ProviderHealthPartial.cshtml` (`@model AITeam.Hub.Pages.ProviderHealthView`): `span.pill` theo Tag (OK ⇒ `ok`, CHƯA ĐĂNG NHẬP / CHƯA CÀI ⇒ `fix`, LỖI ⇒ `err`, KHÔNG RÕ ⇒ `n`) + chữ `muted` Detail.
7. Bề rộng 1000px không tràn ngang. Không thêm mục menu (Claude thêm khi merge).

## Mục tiêu
Điền `ProvidersModel` theo chú thích khung; viết 2 file Razor theo Bố cục.

## Phạm vi (được sửa)
- `hub/Pages/Providers.cshtml.cs`
- `hub/Pages/Providers.cshtml`
- `hub/Pages/Shared/_ProviderHealthPartial.cshtml`
- `hub.tests/` (chỉ thêm test, không sửa test nghiệm thu)

## Không làm
- Không đổi `core/`, `Program.cs`, `_Layout.cshtml`, `hub.css`, dịch vụ khác. KHÔNG gọi agy / Codex thật trong test.

## Test nghiệm thu (Claude viết trước, agent KHÔNG được sửa)
- `hub.tests/AcceptanceH306Tests.cs`

## Tiêu chí hoàn thành
- [x] `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết (gồm 11 ca nghiệm thu H3-06).
- [x] Trang đúng mục Bố cục (Claude chạy Hub kiểm tra khi review).

## Báo cáo của agent
| Tiêu chí hoàn thành | Bằng chứng (lệnh + kết quả thật) |
|---|---|
| `dotnet build AITeam.sln` 0 warning; `dotnet test AITeam.sln --blame-hang-timeout 3m` pass hết | `dotnet build AITeam.sln` → 0 Warning(s), 0 Error(s); `dotnet test AITeam.sln --blame-hang-timeout 3m` → Passed: 111 (`AITeam.Core.Tests`) + 289 (`AITeam.Hub.Tests`), gồm 11/11 ca `AcceptanceH306Tests` và 1 ca `ProvidersPageTests`. |
| Trang đúng mục Bố cục | Đã viết `Providers.cshtml` và `_ProviderHealthPartial.cshtml` đúng mục Bố cục, kiểm qua `AcceptanceH306Tests.Razor_markup_follows_layout` (Passed). |

- **Đã làm:** Hoàn thiện `ProvidersModel` (`OnGet`, `OnPostClear`, `OnGetHealthAsync`, `TagOf`), giao diện `Providers.cshtml`, partial `_ProviderHealthPartial.cshtml`, và thêm test `ProvidersPageTests.cs`.
- **File đã đổi:** `hub/Pages/Providers.cshtml.cs`, `hub/Pages/Providers.cshtml`, `hub/Pages/Shared/_ProviderHealthPartial.cshtml`, `hub.tests/ProvidersPageTests.cs`, `tasks/H3-06-hub-providers.md`.
- **Điều chưa chắc:** Không có (chưa chạy Hub thật theo quy định prompt, chờ Claude kiểm tra khi review).

## Review (Claude)
### Vòng 1 — ĐẠT (24/10/2026, Claude)
- Cổng kiểm tra ĐẠT lần đầu; 10/10 ca nghiệm thu + trang /Providers dựng thật trong test; 111 + 289 test; build 0 warning; ~12 phút, 1 lượt; 265/260 dòng.
- Xem Hub thật (cổng 5058): bảng provider, thứ tự dự phòng, lịch sử nghỉ; bấm **Kiểm tra** với agy và Codex THẬT ⇒ cả hai OK ("Đã đăng nhập", "Logged in using ChatGPT").
- [AG] (BOM) mất BOM 3 file khung — lần 2 của Lan (V4-08, H3-06) ⇒ thêm Lưu ý riêng. Cổng KT tự trả.
- [CK] (công cụ) review bỏ qua trả mã 4 rò thành mã thoát của `assign` ⇒ lượt chạy báo "failed" dù đạt. Claude đã sửa (`$global:LASTEXITCODE = 0`).

Nhân viên: Lan (B) — 1 lượt, ~12 phút, 265/260 dòng.

## Kết quả
- **Chức năng mới — trang Provider:** xem AI nào sẵn sàng / đang nghỉ tới giờ nào (hết hạn mức, chưa đăng nhập), nút Kiểm tra (đã cài, đã đăng nhập chưa) và Hết nghỉ, thứ tự dự phòng, lịch sử nghỉ.
- **Kiểm chứng:** 10 ca nghiệm thu; bấm Kiểm tra thật với agy + Codex.
- **Cần cải tiến:** chưa có cảnh báo "lỗi liên tiếp" theo provider (P1 sau).

## Đánh giá quy trình
| Giai đoạn | Ai | Điểm | Thời gian | Vấn đề | Nguyên nhân | Đề xuất |
|---|---|---|---|---|---|---|
| 1. Tiếp nhận & quản lý | Claude | 5 | (chung R-004) | — | — | — |
| 2. Ý tưởng | Claude | 5 | (chung R-004) | Dùng lại lõi C# trong Hub thay vì viết lại | — | — |
| 3. Plan / thiết kế | Claude | 5 | ~20' | Khung nói rõ cách dựng PartialViewResult (bài học PageContext) | — | — |
| 4. Giao việc | Claude | 5 | ~10' | Trang mới có sẵn trong test dựng trang (IMP-002) | — | — |
| 5. Thực hiện code | Lan (B) | 4 | ~12' (1 lượt) | Đúng đặc tả; mất BOM lần 2 | AG | Lưu ý riêng |
| 6. Review / QC | Cổng KT + Claude | 4 | ~10' | Mã thoát 4 rò ra ngoài | CK | Đã sửa |
| 7. Đánh giá & bàn giao | Claude | 5 | ~5' | — | — | — |