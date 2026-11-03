using AITeam.Core.Process;
using AITeam.Core.Providers;
namespace AITeam.Core.Routing;
public sealed record DeveloperRoutingSnapshot(DeveloperTask Task, Dictionary<string,int> Loads, string[] AgyModels, bool AgyInstalled, string RequiredAgyModel = "argon-4", string? ProjectPath = null, string? TaskId = null);
/// <summary>Ghép trạng thái thật và kiểm CLI cục bộ vào bảng quyết định, không gọi model AI.</summary>
public static class DeveloperRouting
{
    public static async Task<DeveloperSelection> SelectAsync(string root, DeveloperRoutingSnapshot snapshot, IProcessRunner runner, DateTimeOffset now, CancellationToken ct)
    {
        var loads = DeveloperRouteMemory.Loads(root, snapshot);
        var settings=ProviderSettings.Load(root); var state=new ProviderStateStore(root);var candidates=new List<DeveloperCandidate>();
        var openCode=OpenCodeProvider.Configuration(root,!snapshot.Task.PrivateRepository,settings.Providers.FirstOrDefault(p=>p.Id.Equals("opencode",StringComparison.OrdinalIgnoreCase)));
        foreach(var id in new[]{"codex","opencode","agy"})
        {
            var config=id=="opencode"?openCode:settings.Providers.FirstOrDefault(p=>p.Id.Equals(id,StringComparison.OrdinalIgnoreCase));
            if(config==null)continue;
            var rest=state.RestOf(id,now.LocalDateTime);
            string? model=config.Model;var available=config.Enabled&&config.Roles.Contains(AgentRole.Developer)&&rest==null;
            if(id=="agy")
            {
                model=snapshot.AgyModels.FirstOrDefault(m=>!string.IsNullOrWhiteSpace(snapshot.RequiredAgyModel)&&m.Contains(snapshot.RequiredAgyModel,StringComparison.OrdinalIgnoreCase));
                available&=snapshot.AgyInstalled&&model!=null;
            }
            else if(available)
            {
                var health=await settings.Create(config,runner).CheckAsync(ct);
                // OpenCode có model free không cần đăng nhập; không gọi version là đã đăng nhập.
                available=health.Installed&&health.LoggedIn!=false;
            }
            candidates.Add(new(id,available,id!="opencode"||openCode.Paid,model,loads.GetValueOrDefault(id),
                rest==null?null:new DateTimeOffset(rest.Until),rest==null?"CLI chưa sẵn sàng hoặc thiếu model/vai trò":$"Đang nghỉ ({rest.Reason})"));
        }
        var result=DeveloperSelector.Select(snapshot.Task,candidates,now,snapshot.RequiredAgyModel);
        if(result.ProviderId!=null) DeveloperRouteMemory.Record(root,snapshot,result.ProviderId);
        return result;
    }
}
