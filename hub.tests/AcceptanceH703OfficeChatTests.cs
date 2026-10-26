using System.Runtime.CompilerServices;
using AITeam.Hub.Models;
using AITeam.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AITeam.Hub.Tests;

public sealed class AcceptanceH703OfficeChatTests
{
    private sealed class Requests : IRequestService
    {
        public RequestThread? Thread = new("R-009", "Phiếu cũ", "", "", "", [], [], false, "R-009-cu.md");
        public readonly List<string> Reads = [];
        public Exception? Error;
        public IReadOnlyList<RequestThread> GetAll() => throw new InvalidOperationException("Không được quét phiếu.");
        public RequestThread? Get(string id)
        {
            Reads.Add(id);
            if (Error is not null) throw Error;
            return Thread;
        }
    }

    private sealed class Inbox : IInboxService
    {
        public readonly List<(string Project, string Text)> Writes = [];
        public Exception? Error;
        public IReadOnlyList<InboxItem> GetItems() => throw new InvalidOperationException("Không được đọc inbox.");
        public void Add(string project, string text)
        {
            Writes.Add((project, text));
            if (Error is not null) throw Error;
        }
    }

    private sealed class Config : ITeamConfigService
    {
        public IReadOnlyList<ProjectInfo> GetProjects() => throw new InvalidOperationException();
        public ProjectInfo? FindProject(string name) => name == "AITeam"
            ? new("AITeam", "", "", "", "", "", "", new Dictionary<string, string>(), false) : null;
        public TeamSettings GetSettings() => throw new InvalidOperationException();
        public void SaveSettings(TeamSettings settings) => throw new InvalidOperationException();
        public void SaveProject(ProjectInfo project) => throw new InvalidOperationException();
    }

    private readonly Requests _requests = new();
    private readonly Inbox _inbox = new();
    private OfficeChatService Service => new(_requests, _inbox, new Config());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Gui_moi_chi_qua_inbox(string? id)
    {
        Service.Send("AITeam", "  Yêu cầu mới\nDòng hai  ", id);
        Assert.Equal(("AITeam", "Yêu cầu mới\nDòng hai"), Assert.Single(_inbox.Writes));
        Assert.Empty(_requests.Reads);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4000)]
    public void Bo_sung_phieu_cu_xac_minh_ma_truoc_khi_gui(int length)
    {
        var before = _requests.Thread;
        var text = new string('á', length);
        Service.Send("AITeam", text, "R-009");
        Assert.Equal("R-009", Assert.Single(_requests.Reads));
        Assert.Equal(("AITeam", "[Bổ sung R-009] " + text), Assert.Single(_inbox.Writes));
        Assert.Same(before, _requests.Thread);
    }

    [Fact]
    public void Gui_moi_4000_ky_tu_duoc_giu_nguyen()
    {
        var text = new string('x', 4000);
        Service.Send("AITeam", text);
        Assert.Equal(("AITeam", text), Assert.Single(_inbox.Writes));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t ")]
    public void Noi_dung_trong_khong_doc_phieu_khong_ghi(string? text)
    {
        Assert.Equal("Nội dung yêu cầu không được để trống.",
            Assert.Throws<ArgumentException>(() => Service.Send("AITeam", text, "R-009")).Message);
        Assert.Empty(_inbox.Writes);
        Assert.Empty(_requests.Reads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Noi_dung_4001_ke_ca_khoang_trang_bi_chan(bool padded)
    {
        var text = padded ? " " + new string('x', 3999) + " " : new string('x', 4001);
        Assert.Equal("Nội dung quá dài (tối đa 4000 ký tự).",
            Assert.Throws<ArgumentException>(() => Service.Send("AITeam", text, "R-009")).Message);
        Assert.Empty(_inbox.Writes);
        Assert.Empty(_requests.Reads);
    }

    [Theory]
    [InlineData("KhongCo")]
    [InlineData("")]
    [InlineData("../AITeam")]
    public void Du_an_khong_hop_le_bi_chan(string project)
    {
        Assert.Equal("Dự án không hợp lệ.",
            Assert.Throws<ArgumentException>(() => Service.Send(project, "Tin", "R-009")).Message);
        Assert.Empty(_inbox.Writes);
        Assert.Empty(_requests.Reads);
    }

    [Theory]
    [InlineData("../R-009")]
    [InlineData("R-009/../../x")]
    [InlineData("C:\\requests\\R-009.md")]
    [InlineData("/R-009")]
    [InlineData("R-009-cu.md")]
    [InlineData("R-009\n")]
    [InlineData(" R-009")]
    [InlineData("r-009")]
    [InlineData("R-0009")]
    [InlineData("R-٠٠٩")]
    [InlineData(" ")]
    public void Ma_gia_mao_traversal_khong_duoc_chuyen_sang_dich_vu_doc(string id)
    {
        Assert.Equal("Phiếu không hợp lệ.",
            Assert.Throws<ArgumentException>(() => Service.Send("AITeam", "Tin", id)).Message);
        Assert.Empty(_requests.Reads);
        Assert.Empty(_inbox.Writes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Phieu_mat_hoac_dich_vu_tra_sai_ma_khong_gui(bool wrongId)
    {
        _requests.Thread = wrongId ? _requests.Thread! with { Id = "R-008" } : null;
        Assert.Equal("Phiếu không hợp lệ.",
            Assert.Throws<ArgumentException>(() => Service.Send("AITeam", "Tin", "R-009")).Message);
        Assert.Equal("R-009", Assert.Single(_requests.Reads));
        Assert.Empty(_inbox.Writes);
    }

    [Fact]
    public void Loi_doc_phieu_khong_bien_thanh_yeu_cau_moi()
    {
        var error = new IOException("Không đọc được phiếu.");
        _requests.Error = error;
        Assert.Same(error, Assert.Throws<IOException>(() => Service.Send("AITeam", "Tin", "R-009")));
        Assert.Empty(_inbox.Writes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("R-009")]
    public void Loi_inbox_truyen_nguyen_khong_thu_lai(string? id)
    {
        var error = new IOException("Không ghi được inbox.");
        _inbox.Error = error;
        Assert.Same(error, Assert.Throws<IOException>(() => Service.Send("AITeam", "Tin", id)));
        Assert.Single(_inbox.Writes);
    }

    [Theory]
    [InlineData("/Agents", "")]
    [InlineData("/Inbox", "")]
    [InlineData("/agents", "?handler=Stop&pid=123")]
    [InlineData("/Inbox", "?handler=Send&text=x")]
    public async Task Get_cu_tra_302_khong_chay_handler(string path, string query)
    {
        using var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        app.UseMiddleware<OfficeChatRedirectMiddleware>();
        var calls = 0;
        app.Run(_ => { calls++; throw new InvalidOperationException("Handler cũ đã chạy."); });
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        await app.Build()(context);
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal("/", context.Response.Headers.Location.ToString());
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("POST", "/Inbox")]
    [InlineData("POST", "/Agents")]
    [InlineData("HEAD", "/Agents")]
    [InlineData("GET", "/Agents/child")]
    [InlineData("GET", "/")]
    public async Task Request_khac_di_tiep_dung_mot_lan(string method, string path)
    {
        var calls = 0;
        var middleware = new OfficeChatRedirectMiddleware(context =>
        {
            calls++;
            context.Response.StatusCode = 204;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        await middleware.InvokeAsync(context);
        Assert.Equal(1, calls);
        Assert.Equal(204, context.Response.StatusCode);
        Assert.Empty(context.Response.Headers);
    }

    [Fact]
    public void Program_dang_ky_DI_va_dat_chuyen_huong_truoc_Razor()
    {
        var source = File.ReadAllText(Path.Combine(Path.GetDirectoryName(ThisFile())!, "..", "hub", "Program.cs"));
        Assert.Contains("builder.Services.AddSingleton<IOfficeChatService, OfficeChatService>();", source);
        var redirect = source.IndexOf("app.UseMiddleware<OfficeChatRedirectMiddleware>();", StringComparison.Ordinal);
        var pages = source.IndexOf("app.MapRazorPages();", StringComparison.Ordinal);
        Assert.True(redirect >= 0 && pages > redirect, "Chuyển hướng phải đứng trước endpoint Razor.");
    }

    private static string ThisFile([CallerFilePath] string path = "") => path;
}
