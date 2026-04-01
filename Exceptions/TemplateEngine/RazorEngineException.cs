namespace Mitc_Registration_Portal.Exceptions.TemplateEngine
{
    public class RazorEngineException : Exception
    {
#pragma warning disable CS0114
        public string Message { get; set; }
        public RazorEngineException(string message)
    : base(message) { }


        public RazorEngineException(string message, Exception innerException)
            : base(message, innerException) { }


    }
}
