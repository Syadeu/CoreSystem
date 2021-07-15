using Newtonsoft.Json;

namespace Syadeu.Database
{
    public interface IAttribute
    {
        [JsonProperty(Order = -2)] string Name { get; }
        [JsonProperty(Order = -1)] Hash Hash { get; }
    }
}
