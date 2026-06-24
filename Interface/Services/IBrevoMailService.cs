using System.Threading.Tasks;

namespace Mitc_Registration_Portal.Interface.Services
{
    public interface IBrevoMailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent);
    }
}
