using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CommunityShareStack.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body);
    }

    public class ConsoleEmailSender : IEmailSender
    {
        private readonly ILogger<ConsoleEmailSender> _logger;

        public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string body)
        {
            _logger.LogInformation("Email to {To}: {Subject} - {Body}", to, subject, body);
            return Task.CompletedTask;
        }
    }
}
