using AITeam.Core.Metrics;

namespace AITeam.Core.Tests;

public sealed class V913RunFlagsTests
{
    private static RunDiagnosticInput Input => new() { Project = "P", Task = "T-1", Run = 2, DurationSec = 61, MaxMinutes = 1, TotalTokens = 100, TokensKnown = true, HasOutput = true };
    [Fact]
    public void Slow_expensive_and_infrastructure_have_independent_thresholds()
    {
        Assert.Empty(RunDiagnostics.Evaluate(Input, null, 60).Flags);
        Assert.Contains("CH", RunDiagnostics.Evaluate(Input with { DurationSec = 121 }, null, null).Flags);
        Assert.Contains("TO", RunDiagnostics.Evaluate(Input with { TotalTokens = 121 }, null, 60).Flags);
        Assert.Contains("HT", RunDiagnostics.Evaluate(Input with { TokensKnown = false, HasOutput = false }, null, null).Flags);
        Assert.DoesNotContain("HT", RunDiagnostics.Evaluate(Input with { TokensKnown = true, TotalTokens = 0, HasOutput = false }, null, null).Flags);
    }
    [Theory]
    [InlineData("HTTP 429 Too Many Requests", true)]
    [InlineData("RESOURCE_EXHAUSTED quota", true)]
    [InlineData("output_tokens: 42953", false)]
    [InlineData("output_tokens: 429", false)]
    public void Quota_errors_are_not_confused_with_token_numbers(string log, bool expected)
        => Assert.Equal(expected, RunDiagnostics.Evaluate(Input with { DiagnosticError = log }, null, null).Flags.Contains("HM"));
    [Fact]
    public void Same_test_and_stack_in_consecutive_runs_pauses_but_missing_trace_does_not()
    {
        const string log = "Failed H706.Navigation [10 ms]\nStack Trace:\n   at Hub.Pages.Projects.ExecuteAsync() in Projects.cshtml:line 4\n";
        var previous = Input with { Run = 1, VerifyOk = false, FailureFingerprint = RunDiagnostics.Fingerprint(log) };
        var current = Input with { VerifyOk = false, FailureLog = log.Replace("10 ms", "20 ms") };
        var flags = RunDiagnostics.Evaluate(current, previous, null);
        Assert.True(flags.Pause); Assert.Contains("TN", flags.Flags);
        Assert.False(RunDiagnostics.Evaluate(current with { Run = 3 }, previous, null).Pause);
        Assert.False(RunDiagnostics.Evaluate(current with { FailureLog = "Tests failed" }, previous, null).Pause);
        Assert.False(RunDiagnostics.Evaluate(current with { FailureLog = log.Replace("line 4", "line 9") }, previous, null).Pause);
    }
    [Fact]
    public void Repeated_test_is_detected_even_when_other_failures_change()
    {
        const string stable = "Failed H706.Navigation [10 ms]\n   at Hub.Pages.Projects.ExecuteAsync() in Projects.cshtml:line 4\n";
        var previous = Input with { Run = 1, VerifyOk = false, FailureFingerprints = RunDiagnostics.Fingerprints(stable + "Failed Other.B\n   at B() in B.cs:line 1") };
        var current = Input with { VerifyOk = false, FailureLog = stable + "Failed Other.C\n   at C() in C.cs:line 9" };
        Assert.True(RunDiagnostics.Evaluate(current, previous, null).Pause);
    }
}
