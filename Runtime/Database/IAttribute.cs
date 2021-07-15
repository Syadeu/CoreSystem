using Newtonsoft.Json;

namespace Syadeu.Database
{
    public interface IAttribute
    {
        [JsonProperty(Order = -11)] string Name { get; }
        [JsonProperty(Order = -10)] Hash Hash { get; }
    }
}
