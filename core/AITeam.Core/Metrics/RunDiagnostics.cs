using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AITeam.Core.Metrics;

public sealed record RunDiagnosticInput
{
    public string Project { get; init; } = "";
    public string Task { get; init; } = "";
    public int Run { get; init; }
    public double DurationSec { get; init; }
    public int MaxMinutes { get; init; } = 30;
    public string Difficulty { get; init; } = "";
    public long? TotalTokens { get; init; }
    public bool? TokensKnown { get; init; }
    public bool HasOutput { get; init; }
    public string DiagnosticError { get; init; } = "";
    public bool? VerifyOk { get; init; }
    public string FailureLog { get; init; } = "";
    public string? FailureFingerprint { get; init; }
    public IReadOnlyList<string> FailureFingerprints { get; init; } = [];
    public bool KnownTokens => TokensKnown != false && TotalTokens >= 0 && (TokensKnown == true || TotalTokens > 0);
}
public sealed record RunFlags(IReadOnlyList<string> Flags, bool Pause, string? FailureFingerprint, IReadOnlyList<string> FailureFingerprints);

public static class RunDiagnostics
{
    public static string? Fingerprint(string? log) => Fingerprints(log).FirstOrDefault();
    public static IReadOnlyList<string> Fingerprints(string? log)
    {
        if (string.IsNullOrWhiteSpace(log)) return [];
        var lines = log.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).ToArray();
        var fingerprints = new List<string>(); string? name = null; var stack = new List<string>();
        void Flush()
        {
            if (name != null && stack.Count > 0)
                fingerprints.Add(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name + "\n" + string.Join('\n', stack)))));
            stack.Clear(); name = null;
        }
        foreach (var line in lines)
        {
            if (line.StartsWith("Failed ", StringComparison.Ordinal))
            { Flush(); name = Regex.Replace(line, @"\s+\[[\d.]+\s*(?:ms|s|m)\]$", ""); }
            else if (line.StartsWith("at ", StringComparison.Ordinal) && name != null) stack.Add(line);
            else if (line.StartsWith("Results File:", StringComparison.Ordinal) || line.StartsWith("Failed!", StringComparison.Ordinal)) Flush();
        }
        Flush(); return fingerprints.Distinct().ToList();
    }
    public static RunFlags Evaluate(RunDiagnosticInput current, RunDiagnosticInput? previous, double? averageTokens)
    {
        var flags = new List<string>();
        if (current.DurationSec > Math.Max(1, current.MaxMinutes) * 120d) flags.Add("CH");
        if (current.KnownTokens && averageTokens is > 0 && current.TotalTokens > averageTokens * 2) flags.Add("TO");
        if (!current.KnownTokens && !current.HasOutput) flags.Add("HT");
        if (Regex.IsMatch(current.DiagnosticError, @"(?i)\bquota\b|RESOURCE_EXHAUSTED|\b(?:HTTP|status|error)\W*429\b|\b429\s+Too\s+Many")) flags.Add("HM");
        var fingerprints = Fingerprints(current.FailureLog);
        if (fingerprints.Count == 0) fingerprints = current.FailureFingerprints.Count > 0 ? current.FailureFingerprints : current.FailureFingerprint == null ? [] : [current.FailureFingerprint];
        var old = previous?.FailureFingerprints.Count > 0 ? previous.FailureFingerprints : previous?.FailureFingerprint == null ? [] : new[] { previous.FailureFingerprint };
        var pause = current.VerifyOk == false && previous?.VerifyOk == false && current.Project == previous.Project && current.Task == previous.Task
            && current.Run == previous.Run + 1 && fingerprints.Intersect(old).Any();
        if (pause) flags.Add("TN");
        return new(flags, pause, fingerprints.FirstOrDefault(), fingerprints);
    }
}
