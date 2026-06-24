namespace Mitc_Registration_Portal.Contracts.MailingService
{
    public interface IMailService
    {
        public Task<bool> SendAdmissionStatus(string email, string name, string title, string status, string? departmentName);
    }
}
