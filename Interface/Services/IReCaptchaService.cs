namespace Mitc_Registration_Portal.Interface.Services
{
    public interface IReCaptchaService
    {
        Task<bool> IsCapchaValid(string? token);
    }
}
