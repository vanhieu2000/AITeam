using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace AITeam.Core.Routing;
/// <summary>Lưu provider được chọn riêng với Agent; đọc bảng để chỉ đếm task thực sự đang chạy.</summary>
internal static class DeveloperRouteMemory
{
    private static string ProjectKey(string path)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant())));
    private static string TaskKey(string task)=>Regex.Match(task,@"^[A-Za-z][A-Za-z0-9]*-\d+").Value.ToUpperInvariant();
    private static string FilePath(string root)=>Path.Combine(root,".state","developer-routes.json");
    private static Dictionary<string,string> Read(string root)
    {
        var file=FilePath(root);
        foreach(var p in new[]{root,Path.GetDirectoryName(file)!,file})
            if((File.Exists(p)||Directory.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Không đọc định tuyến qua symlink/reparse point.");
        if(!File.Exists(file))return new(StringComparer.OrdinalIgnoreCase);
        if(new FileInfo(file).Length>1024*1024)throw new InvalidDataException("Lịch sử định tuyến quá lớn.");
        return new(JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(file))??throw new InvalidDataException("Lịch sử định tuyến hỏng."),StringComparer.OrdinalIgnoreCase);
    }
    internal static Dictionary<string,int> Loads(string root,DeveloperRoutingSnapshot snapshot)
    {
        if(snapshot.ProjectPath==null)return snapshot.Loads;
        var board=Path.Combine(snapshot.ProjectPath,"tasks","README.md");
        if(!File.Exists(board))return snapshot.Loads;
        foreach(var path in new[]{snapshot.ProjectPath,Path.GetDirectoryName(board)!,board})
            if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Không đọc bảng việc qua symlink/reparse point.");
        if(new FileInfo(board).Length>2*1024*1024)throw new InvalidDataException("Bảng việc quá lớn.");
        using var gate=FailureStreakStore.Lock(root);var memory=Read(root);var key=ProjectKey(snapshot.ProjectPath);
        var loads=new Dictionary<string,int>{{"codex",0},{"opencode",0},{"agy",0}};
        foreach(var line in File.ReadLines(board))
        {
            if(!line.StartsWith('|')||!line.Contains("🟡",StringComparison.Ordinal))continue;
            var cells=line.Split('|');if(cells.Length<4)continue;
            var task=Regex.Match(cells[1],@"[A-Za-z][A-Za-z0-9]*-\d+").Value.ToUpperInvariant();
            var provider=memory.GetValueOrDefault(key+":"+task)??(cells[2].Trim().ToUpperInvariant() switch{"C"=>"codex","O"=>"opencode","A" or "B"=>"agy",_=>""});
            if(loads.ContainsKey(provider))loads[provider]++;
        }
        return loads;
    }
    internal static void Record(string root,DeveloperRoutingSnapshot snapshot,string provider)
    {
        if(snapshot.ProjectPath==null||snapshot.TaskId==null)return;
        using var gate=FailureStreakStore.Lock(root);var memory=Read(root);
        var task=TaskKey(snapshot.TaskId);if(task.Length==0)throw new InvalidDataException("Mã task định tuyến không hợp lệ.");
        memory[ProjectKey(snapshot.ProjectPath)+":"+task]=provider;
        var file=FilePath(root);Directory.CreateDirectory(Path.GetDirectoryName(file)!);var temporary=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temporary,JsonSerializer.Serialize(memory),new UTF8Encoding(false));File.Move(temporary,file,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
