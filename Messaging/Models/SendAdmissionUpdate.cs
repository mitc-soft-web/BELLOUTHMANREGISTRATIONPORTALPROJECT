namespace Mitc_Registration_Portal.Messaging.Models
{
    public class SendAdmissionUpdate : Base
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string ActionText { get; set; } = "Open School Portal";
        public string Status { get; set; }
        public string? DepartmentName { get; set; }
    }
}
