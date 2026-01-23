using System.Text.Json.Serialization;

namespace Mitc_Registration_Portal.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Status : byte
    {
        Admitted = 1,
        Pending,
        Rejected
    }
}
