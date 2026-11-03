using AITeam.Core.Tasks;
namespace AITeam.Core.Tests;
public sealed class V907HarnessPolicyTests
{
    [Fact] public void Setup_changes_require_explicit_declaration()
    {
        const string diff = "--- a/tests/T.cs\n+++ b/tests/T.cs\n@@ -1 +1 @@\n- var host = OldHost();\n+ var host = CorrectHost();";
        Assert.False(AcceptanceHarnessPolicy.Allows(diff, false));
        Assert.True(AcceptanceHarnessPolicy.Allows(diff, true));
        Assert.False(AcceptanceHarnessPolicy.Allows("", true));
    }
    [Theory]
    [InlineData("-Assert.Equal(1, actual);\n+Assert.Equal(2, actual);")]
    [InlineData("-Assert.\n-  Equal(1, actual);")]
    [InlineData("+[Fact(Skip = \"bỏ\")]")]
    [InlineData("-[Theory]")]
    [InlineData("+Need ($false) 'lỗi'")]
    public void Test_conditions_and_assertions_cannot_be_changed_even_with_permission(string diff) => Assert.False(AcceptanceHarnessPolicy.Allows(diff, true));
}
