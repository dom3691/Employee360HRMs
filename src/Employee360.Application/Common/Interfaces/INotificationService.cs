namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Raises in-app notifications (PRD FR-ESS-001; notification center). Used by
/// workflow slices alongside email so users see events in both channels.
/// </summary>
public interface INotificationService
{
    /// <summary>Creates a notification for a user account.</summary>
    /// <param name="userId">Recipient user id.</param>
    /// <param name="title">Short title.</param>
    /// <param name="message">Body text.</param>
    /// <param name="link">Optional client deep link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyUserAsync(
        Guid userId, string title, string message, string? link = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a notification for the user account linked to an employee.
    /// No-op when the employee has no user account.
    /// </summary>
    /// <param name="employeeId">Recipient employee id.</param>
    /// <param name="title">Short title.</param>
    /// <param name="message">Body text.</param>
    /// <param name="link">Optional client deep link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyEmployeeAsync(
        Guid employeeId, string title, string message, string? link = null,
        CancellationToken cancellationToken = default);
}
