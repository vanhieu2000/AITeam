namespace AITeam.Hub.Services;

// F1-02 (R-015): KHUNG do Claude viết — giữ nguyên chữ ký, agent điền thân. Thiết kế: docs/design/R-015-chuc-nang.md mục F1-02.

public enum RequestState { Open, Done, Cancelled }

public sealed record RequestTaskStatus(string Project, string Code, string Title, OfficeTaskStatus Status);

public sealed record RequestProgress(string Id, RequestState State, int Total, int Merged, int Running, int Review, int NeedsFix,
    bool AllMerged, IReadOnlyList<RequestTaskStatus> Tasks);

public sealed record RequestStateResult(bool Ok, string? Error);

public interface IRequestProgressService
{
    /// <summary>Theo IRequestService.GetAll(), cùng thứ tự.</summary>
    IReadOnlyList<RequestProgress> GetAll();

    /// <summary>id sai dạng ^R-\d{3}$ hoặc không có ⇒ null.</summary>
    RequestProgress? Get(string id);

    /// <summary>Ghi dòng "- **Trạng thái:**" của phiếu + 1 dòng nhật ký. Sai ⇒ Ok=false, không ghi.</summary>
    RequestStateResult SetState(string id, RequestState state, string? note);
}

public sealed class RequestProgressService : IRequestProgressService
{
    private readonly HubPaths _paths;
    private readonly ITeamConfigService _config;
    private readonly IRequestService _requests;
    private readonly TimeProvider _clock;

    public RequestProgressService(HubPaths paths, ITeamConfigService config, IRequestService requests, TimeProvider clock)
    {
        _paths = paths;
        _config = config;
        _requests = requests;
        _clock = clock;
    }

    public IReadOnlyList<RequestProgress> GetAll() => throw new NotImplementedException(); // TODO(agent)

    public RequestProgress? Get(string id) => throw new NotImplementedException(); // TODO(agent)

    public RequestStateResult SetState(string id, RequestState state, string? note) => throw new NotImplementedException(); // TODO(agent)
}
