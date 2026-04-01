using brevo_csharp.Api;
using brevo_csharp.Model;
using Mitc_Registration_Portal.Contracts.Messaging;
using Mitc_Registration_Portal.Exceptions.Messaging;
using Mitc_Registration_Portal.Extensions;

namespace Mitc_Registration_Portal.Messaging
{
    public class MailSender(IConfiguration configuration, IWebHostEnvironment env,
        ILogger<MailSender> logger) : IMailSender
    {
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        private readonly ILogger<IMailSender> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(_env));
        public async Task<bool> Send(string from, string fromName, string to, string toName, string subject, string message, IDictionary<string, Stream> attachments = null)
        {
            var smtpApiKey = _configuration["MITCAPIs:BrevoApiKey"];

            var apiInstance = new TransactionalEmailsApi();
            var smtpAttachments = new List<SendSmtpEmailAttachment>();
            var sendSmtpEmail = new SendSmtpEmail
            {
                HtmlContent = message,
                Subject = subject,
                Sender = new SendSmtpEmailSender(fromName, from),
                To = new List<SendSmtpEmailTo>() { new SendSmtpEmailTo(to, toName) }
            };

            foreach (var asset in EmailAssetRegistry.AssetMap)
            {
                if (message.Contains($"cid:{asset.Key}"))
                {
                    string absolutePath = Path.Combine(_env.WebRootPath, asset.Value);

                    if (File.Exists(absolutePath))
                    {
                        byte[] imageBytes = File.ReadAllBytes(absolutePath);
                        smtpAttachments.Add(new SendSmtpEmailAttachment(
                            content: imageBytes,
                            name: asset.Key
                        ));
                    }
                    else
                    {
                        _logger.LogWarning($"Email asset not found at: {absolutePath}");
                    }
                }
            }

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    smtpAttachments.Add(new SendSmtpEmailAttachment(content: ReadFully(attachment.Value), name: attachment.Key));
                }
            }

            if (smtpAttachments.Count > 0)
            {
                sendSmtpEmail.Attachment = smtpAttachments;
            }

            if (!string.IsNullOrEmpty(smtpApiKey))
            {
                brevo_csharp.Client.Configuration.Default.AddApiKey("api-key", smtpApiKey);
                try
                {
                    await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError("Exception when calling TransactionalEmailsApi.SendTransacEmail: " + e.Message);
                    throw new MailSenderException(e.Message, e);
                }
            }

            _logger.LogError("SMTP API Key is not configured.");
            throw new MailSenderException("SMTP API Key is not configured.");

        }

        private static byte[] ReadFully(Stream input)
        {
            using MemoryStream ms = new();
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }
    
}

