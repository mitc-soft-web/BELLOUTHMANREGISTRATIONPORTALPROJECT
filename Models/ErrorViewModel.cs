namespace Mitc_Registration_Portal.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public int StatusCode { get; set; } = 500;

        public string Title { get; set; } = "Something Went Wrong";

        public string Message { get; set; } = "An unexpected error occurred while processing your request.";

        public string? Detail { get; set; }
    }
}
