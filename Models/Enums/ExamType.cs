using System.Text.Json.Serialization;

namespace Mitc_Registration_Portal.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExamType : byte
    {
        Waec = 1,
        Neco,
        GCE,
        Other
    }
}
