using AITeam.Hub.Models;
using AITeam.Hub.Pages;
using AITeam.Hub.Services;

namespace AITeam.Hub.Tests;

public sealed class CommandServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    public void Dispose() => _env.Dispose();

    private sealed class StubOffice : IOfficeService
    {
        public OfficeView Get() => new(Array.Empty<Desk>(), 0, 0, 0, 0, DateTime.Now);
    }

    private sealed class ThrowingCommands : ICommandService
    {
        public string? Validate(CommandRequest req) => null;
        public CommandJob Start(CommandRequest req) => throw new IOException("Lỗi đĩa");
        public IReadOnlyList<CommandJob> Jobs() => Array.Empty<CommandJob>();
        public IReadOnlyList<AuditEntry> Audit(int max = 50) => Array.Empty<AuditEntry>();
        public IReadOnlyList<TaskCard> Assignable() => Array.Empty<TaskCard>();
        public IReadOnlyList<TaskCard> Resumable() => Array.Empty<TaskCard>();
    }

    [Fact]
    public void Unexpected_exception_in_post_command_sets_prefix_error()
    {
        var page = new OfficeModel(new StubOffice(), new ThrowingCommands());
        page.OnPostCommand("Assign", "AITeam/V5-06", null);
        Assert.Equal("Không chạy được lệnh: Lỗi đĩa", page.CommandError);
    }
}
