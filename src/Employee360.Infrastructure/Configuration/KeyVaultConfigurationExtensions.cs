using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Extensions.Configuration;

namespace Employee360.Infrastructure.Configuration;

/// <summary>Azure Key Vault configuration provider (Batch 17).</summary>
public static class KeyVaultConfigurationExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source when <paramref name="keyVaultUri"/> is set.
    /// Uses DefaultAzureCredential via reflection to avoid Azure SDK assembly version conflicts.
    /// </summary>
    public static IConfigurationBuilder AddEmployee360KeyVault(
        this IConfigurationBuilder configurationBuilder,
        string? keyVaultUri)
    {
        if (string.IsNullOrWhiteSpace(keyVaultUri))
        {
            return configurationBuilder;
        }

        var credential = CreateDefaultAzureCredential();
        if (credential is null)
        {
            return configurationBuilder;
        }

        return configurationBuilder.AddAzureKeyVault(new Uri(keyVaultUri), credential);
    }

    private static TokenCredential? CreateDefaultAzureCredential()
    {
        var credentialType = Type.GetType(
            "Azure.Identity.DefaultAzureCredential, Azure.Identity",
            throwOnError: false);

        if (credentialType is null || !typeof(TokenCredential).IsAssignableFrom(credentialType))
        {
            return null;
        }

        return (TokenCredential?)Activator.CreateInstance(credentialType);
    }
}
