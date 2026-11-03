using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AITeam.Core.Research;
namespace AITeam.Hub.Services;

public sealed record HubMetric(DateTimeOffset At,string Project,string Task,int Run,string Provider,string? Model,
    double? Seconds,long? Tokens,int? Added,int? Removed,bool? VerifyOk,string Status,string Difficulty,
    IReadOnlyList<string> Flags,IReadOnlyList<string> Causes,IReadOnlyList<string> Files)
{ public string? ConversationId { get; init; } }
public sealed record HubMetricSnapshot(IReadOnlyList<HubMetric> Runs,IReadOnlyList<string> Warnings);
/// <summary>Đọc metrics nullable cho UI mới; không biến lượt chưa đo thành 0, không ghi dữ liệu.</summary>
public static class HubMetricReader
{
    public static string TaskCode(string value)=>Regex.Match(value,@"^[A-Za-z][A-Za-z0-9]*-\d+").Value.ToUpperInvariant();
    public static string? ReadText(string root,string relative,int limit=2*1024*1024)
    {
        relative=relative.Replace('\\','/');
        if(Path.IsPathRooted(relative)||relative.Contains(':')||relative.Split('/').Contains(".."))throw new InvalidDataException("Đường dẫn nguồn không hợp lệ.");
        var file=Path.GetFullPath(Path.Combine(root,relative));
        CheckPath(file);
        if(!File.Exists(file))return null;
        if(new FileInfo(file).Length>limit)throw new InvalidDataException("Nguồn vượt giới hạn đọc.");
        return File.ReadAllText(file,new UTF8Encoding(false,true));
    }
    private static void CheckPath(string file)
    {
        for(string? p=file;p!=null;p=Directory.GetParent(p)?.FullName)
            if((File.Exists(p)||Directory.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)
                throw new InvalidDataException("Không đọc nguồn qua symlink/reparse point.");
    }
    public static HubMetricSnapshot Read(HubPaths paths)
    {
        var runs=new List<HubMetric>();var warnings=new List<string>();var directory=Path.Combine(paths.AITeamRoot,"metrics","runs");
        try
        {
            CheckPath(directory);if(!Directory.Exists(directory))return new(runs,warnings);
            var files=Directory.EnumerateFiles(directory,"*.json").Take(10001).ToArray();
            if(files.Length>10000)throw new InvalidDataException("Quá nhiều bản ghi lượt chạy.");
            foreach(var file in files)
            {
                try
                {
                    using var document=JsonDocument.Parse(ReadText(paths.AITeamRoot,"metrics/runs/"+Path.GetFileName(file))!);
                    var e=document.RootElement;if(e.ValueKind!=JsonValueKind.Object)throw new JsonException();
                    var project=Text(e,"project");var task=TaskCode(Text(e,"task")??"");var n=Number(e,"run");
                    if(string.IsNullOrWhiteSpace(project)||project.Length>128||task.Length==0||n is null or <=0 or >int.MaxValue||!DateTimeOffset.TryParse(Text(e,"at"),CultureInfo.InvariantCulture,DateTimeStyles.None,out var at))throw new JsonException();
                    var rawModel=Text(e,"model");var provider=Text(e,"provider")?.ToLowerInvariant();
                    if(string.IsNullOrWhiteSpace(provider))provider=rawModel?.Contains("codex",StringComparison.OrdinalIgnoreCase)==true||Text(e,"agent")=="C"?"codex":rawModel?.Contains("deepseek",StringComparison.OrdinalIgnoreCase)==true?"opencode":Text(e,"agent") is "A" or "B"?"agy":"không rõ";
                    var model=rawModel is "codex" or "opencode" or "agy"?null:rawModel;
                    var tokens=Number(e,"totalTokens");if(Boolean(e,"tokensKnown")==false||tokens<0||(tokens==0&&Boolean(e,"tokensKnown")!=true))tokens=null;
                    runs.Add(new(at,project,task,(int)n.Value,provider,model==null?null:SecretRedactor.Redact(model).Text,PositiveDouble(e,"durationSec"),tokens,
                        Integer(e,"linesAdded"),Integer(e,"linesRemoved"),Boolean(e,"verifyOk"),Text(e,"status")??"Chưa ghi",Text(e,"difficulty")??"Chưa ghi",Strings(e,"flags"),FindingCauses(e),Strings(e,"filesChanged")) { ConversationId=Text(e,"conversationId") });
                }
                catch(Exception ex) when(ex is IOException or InvalidDataException or JsonException or DecoderFallbackException or UnauthorizedAccessException)
                {warnings.Add("Không đọc được một bản ghi lượt chạy; số liệu có thể chưa đầy đủ.");}
            }
        }
        catch(Exception ex)when(ex is IOException or InvalidDataException or UnauthorizedAccessException){warnings.Add("Không đọc được nguồn metrics/runs.");}
        var adjusted=new List<HubMetric>();var previous=new Dictionary<string,HubMetric>();
        foreach(var run in runs.OrderBy(r=>r.At).ThenBy(r=>r.Run))
        {
            var key=run.ConversationId==null?null:run.Project+"/"+run.Task+"/"+run.ConversationId;
            var current=run;
            if(key!=null&&previous.TryGetValue(key,out var prior))
                current=run with{Seconds=run.Seconds.HasValue&&prior.Seconds.HasValue?Math.Max(0,run.Seconds.Value-prior.Seconds.Value):null,
                    Tokens=run.Tokens.HasValue&&prior.Tokens.HasValue?Math.Max(0,run.Tokens.Value-prior.Tokens.Value):null};
            adjusted.Add(current);if(key!=null)previous[key]=run;
        }
        return new(adjusted,warnings.Distinct().ToList());
    }
    private static JsonElement? Value(JsonElement e,string name)=>e.TryGetProperty(name,out var v)?v:null;
    private static string? Text(JsonElement e,string name)=>Value(e,name) is {ValueKind:JsonValueKind.String} v?v.GetString():null;
    private static long? Number(JsonElement e,string name)
    {
        var v=Value(e,name);return v is {ValueKind:JsonValueKind.Number} n&&n.TryGetInt64(out var a)?a:v is {ValueKind:JsonValueKind.String} s&&long.TryParse(s.GetString(),NumberStyles.Integer,CultureInfo.InvariantCulture,out var b)?b:null;
    }
    private static int? Integer(JsonElement e,string name)=>Number(e,name) is >=0 and <=int.MaxValue? (int?)Number(e,name):null;
    private static double? PositiveDouble(JsonElement e,string name)
    {var v=Value(e,name);return v is {ValueKind:JsonValueKind.Number} n&&n.TryGetDouble(out var a)&&double.IsFinite(a)&&a>=0?a:null;}
    private static bool? Boolean(JsonElement e,string name)=>Value(e,name)?.ValueKind switch{JsonValueKind.True=>true,JsonValueKind.False=>false,_=>null};
    private static IReadOnlyList<string> Strings(JsonElement e,string name)=>Value(e,name) is {ValueKind:JsonValueKind.Array} a?a.EnumerateArray().Where(v=>v.ValueKind==JsonValueKind.String).Select(v=>v.GetString()!).Where(v=>v.Length<=256).ToArray():[];
    private static IReadOnlyList<string> FindingCauses(JsonElement e)=>Value(e,"findings") is {ValueKind:JsonValueKind.Array} a?a.EnumerateArray().Where(v=>v.ValueKind==JsonValueKind.Object).Select(v=>Text(v,"Cause")??Text(v,"cause")??"khác").ToArray():[];
}
