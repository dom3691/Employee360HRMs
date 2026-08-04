using Employee360.Application.Common.Interfaces;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.Services.Email;

/// <summary>
/// <see cref="IEmailService"/> implementation that enqueues outbound mail on the
/// Hangfire "email" queue when enabled, otherwise sends synchronously (FR-LV-011,
/// PRD Integration: SMTP email with background dispatch).
/// </summary>
public sealed class QueuedEmailService : IEmailService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueuedEmailService> _logger;

    public QueuedEmailService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<QueuedEmailService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (_configuration.GetValue("Hangfire:Enabled", defaultValue: false))
        {
            BackgroundJob.Enqueue<EmailSendJob>(
                job => job.ExecuteAsync(to, subject, htmlBody));

            _logger.LogInformation("Email queued for {Recipient}: {Subject}", to, subject);
            return Task.CompletedTask;
        }

        return SendDirectAsync(to, subject, htmlBody, cancellationToken);
    }

    private async Task SendDirectAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        await sender.SendAsync(to, subject, htmlBody, cancellationToken);
        _logger.LogInformation("Email sent directly to {Recipient}: {Subject}", to, subject);
    }
}
