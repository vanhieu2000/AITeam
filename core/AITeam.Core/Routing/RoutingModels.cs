using AITeam.Core.Providers;

namespace AITeam.Core.Routing;

// ĐỊNH TUYẾN PROVIDER (R-002 P0-4/5) — Claude thiết kế, agent KHÔNG đổi file này. Thiết kế: docs/design/V4-MULTI-AI.md mục B2, C (P0-4, P0-5).

/// <summary>Provider đang "nghỉ" (hết hạn mức / chưa đăng nhập) tới giờ Until (giờ địa phương). Lưu theo MÁY, không lên git.</summary>
public sealed record ProviderRest(string ProviderId, DateTime Until, RunStatus Reason, string Detail, DateTime At);

/// <summary>Kết quả chọn provider. ProviderId null ⇒ không có provider khả dụng; Skipped liệt kê lý do bỏ qua từng provider (để báo người dùng).</summary>
public sealed record RouteDecision(string? ProviderId, IReadOnlyList<string> Skipped)
{
    public bool Ok => ProviderId != null;
}

/// <summary>Có gọi review AI (Codex) tự động cho task không. Codex hạn mức nhỏ ⇒ chỉ task khó / được yêu cầu.</summary>
public sealed record ReviewDecision(bool AutoReview, string Reason);

public interface IProviderRouter
{
    /// <summary>Chọn provider cho vai trò; <paramref name="excludeProviderId"/> = provider đã code (cấm tự duyệt) hoặc vừa lỗi.</summary>
    RouteDecision Pick(AgentRole role, string? excludeProviderId, DateTime now);
}
