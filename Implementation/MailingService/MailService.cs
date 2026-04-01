using Microsoft.Extensions.Options;
using Mitc_Registration_Portal.Configuration;
using Mitc_Registration_Portal.Contracts.MailingService;
using Mitc_Registration_Portal.Contracts.Messaging;
using Mitc_Registration_Portal.Exceptions.Messaging;
using Mitc_Registration_Portal.Exceptions.TemplateEngine;
using Mitc_Registration_Portal.Interface.TemplateEngine;
using Mitc_Registration_Portal.Messaging.Models;

namespace Mitc_Registration_Portal.Implementation.MailingService
{
    public class MailService(IMailSender mailSender, IRazorEngine razorEngine,
          IOptions<EmailConfiguration> options, ILogger<MailService> logger) : IMailService
    {
        private readonly IMailSender _mailSender = mailSender ?? throw new ArgumentNullException(nameof(mailSender));
        private readonly IRazorEngine _razorEngine = razorEngine ?? throw new ArgumentNullException(nameof(razorEngine));
        private readonly EmailConfiguration _emailConfiguration = options.Value ?? throw new ArgumentException(nameof(options));
        private readonly ILogger<MailService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        public async Task<bool> SendAdmissionStatus(string email, string name, string title, string status)
        {
            try
            {
                var model = new SendAdmissionUpdate()
                {
                    Name = name,
                    Email = email,
                    Title = title,
                    Status = status

                };
                var mailBody = await _razorEngine.ParseAsync("SendAdmissionStatus", model);
                return await _mailSender.Send(_emailConfiguration.FromEmail, _emailConfiguration.FromName, email, name, title, mailBody);
            }
            catch (RazorEngineException ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
            catch (MailSenderException ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
    }
}
