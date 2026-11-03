using AITeam.Core.AiServices.Adapters;

namespace AITeam.Hub.Services;

public sealed class VaultSecretLookup(VaultService vault) : ISecretLookup
{
    private readonly VaultService _vault = vault;

    public string? Get(string name) => _vault.ReadValue(name);
}
