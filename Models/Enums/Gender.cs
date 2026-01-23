using System.Text.Json.Serialization;

namespace Mitc_Registration_Portal.Models.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Gender : byte
    {
        Male =1,
        Female,
        Other
    }
}
