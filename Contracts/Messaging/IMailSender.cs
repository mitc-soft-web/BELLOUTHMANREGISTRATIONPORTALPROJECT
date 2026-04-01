namespace Mitc_Registration_Portal.Contracts.Messaging
{
    public interface IMailSender
    {
        Task<bool> Send(string from, string fromName, string to, string toName, string subject, string message, IDictionary<string, Stream> attachments = null!);
    }
}
