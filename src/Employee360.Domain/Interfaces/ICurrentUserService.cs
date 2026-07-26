namespace Employee360.Domain.Interfaces;

/// <summary>
/// Abstraction over the authenticated user of the current request.
/// Implemented in Infrastructure from the JWT claims principal; consumed by the
/// audit interceptor and manager-scoped authorization checks.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The authenticated user's id, or null when anonymous (e.g. background jobs).</summary>
    Guid? UserId { get; }

    /// <summary>The authenticated user's email, or null when anonymous.</summary>
    string? Email { get; }

    /// <summary>The employee record linked to the authenticated user, when one exists.</summary>
    Guid? EmployeeId { get; }

    /// <summary>True when the current request carries a valid authenticated principal.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Role names held by the current user (empty when anonymous).</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Returns true when the current user holds <paramref name="role"/>.</summary>
    /// <param name="role">Role name to test.</param>
    bool IsInRole(string role);
}
