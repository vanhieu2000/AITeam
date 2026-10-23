using AITeam.Hub.Pages;

namespace AITeam.Hub.Tests;

public sealed class H304TeamProcessTests
{
    [Fact]
    public void Helpers_handle_edge_values()
    {
        Assert.Equal(0.0, ProcessModel.BarWidth(-1.0));
        Assert.Equal("0%", TeamModel.Pct(0.0));
        Assert.Equal("100%", TeamModel.Pct(1.0));
        Assert.Equal("khác 3", TeamModel.AgErrorsText(new Dictionary<string, int> { ["   "] = 3 }));
    }
}
