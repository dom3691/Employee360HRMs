using Employee360.Application.Common.Interfaces;
using Employee360.Infrastructure.Services.Email;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>
/// Tests for Hangfire-queued email dispatch (direct fallback when Hangfire disabled).
/// </summary>
public class QueuedEmailServiceTests
{
    [Fact]
    public async Task SendAsync_WhenHangfireDisabled_InvokesEmailSenderDirectly()
    {
        var sender = new Mock<IEmailSender>();
        sender
            .Setup(s => s.SendAsync(
                "user@company.ng",
                "Subject",
                "<p>Body</p>",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var services = new ServiceCollection();
        services.AddSingleton(sender.Object);
        var provider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "false",
            })
            .Build();

        var service = new QueuedEmailService(
            provider,
            configuration,
            NullLogger<QueuedEmailService>.Instance);

        await service.SendAsync("user@company.ng", "Subject", "<p>Body</p>");

        sender.Verify(
            s => s.SendAsync(
                "user@company.ng",
                "Subject",
                "<p>Body</p>",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EmailSendJob_InvokesUnderlyingSender()
    {
        var sender = new Mock<IEmailSender>();
        sender
            .Setup(s => s.SendAsync(
                "hr@company.ng",
                "Payroll",
                "<p>Ready</p>",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var job = new EmailSendJob(sender.Object, NullLogger<EmailSendJob>.Instance);

        await job.ExecuteAsync("hr@company.ng", "Payroll", "<p>Ready</p>");

        sender.Verify(
            s => s.SendAsync(
                "hr@company.ng",
                "Payroll",
                "<p>Ready</p>",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
