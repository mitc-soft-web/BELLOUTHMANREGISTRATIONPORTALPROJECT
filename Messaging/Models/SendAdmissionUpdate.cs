namespace Mitc_Registration_Portal.Messaging.Models
{
    public class SendAdmissionUpdate : Base
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string ActionText { get; set; } = "Open School Portal";
        public string ActionUrl { get; set; } = "https://mitc.iaaae.org";
        public string Status { get; set; }
        public string? DepartmentName { get; set; }
        public string LogoUrl { get; set; } = "https://mitc.iaaae.org/wp-content/uploads/2024/12/logo3.jpg";
    }
}
