namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Merges configurable email templates with runtime tokens (PRD FR-ADM-003).
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Replaces {TokenName} placeholders in the given text with matching values.
    /// Unknown tokens are left unchanged.
    /// </summary>
    string MergeTokens(string template, IReadOnlyDictionary<string, string> tokens);

    /// <summary>
    /// Loads an active template by code and merges subject + body with tokens.
    /// Falls back to built-in defaults when no active DB row exists.
    /// </summary>
    Task<(string Subject, string BodyHtml)> RenderAsync(
        string code,
        IReadOnlyDictionary<string, string> tokens,
        CancellationToken cancellationToken = default);
}
