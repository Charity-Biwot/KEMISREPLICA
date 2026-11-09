namespace KemisOnboarding.Api.Services;

public interface IInstitutionEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

public class LoggingEmailSender : IInstitutionEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL to {To} | {Subject} | {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}