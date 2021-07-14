using Newtonsoft.Json;
using Syadeu.Database.CreatureData;
using Syadeu.Presentation;
using System;

namespace Syadeu.Database.Converters
{
    internal sealed class CreatureAttributeJsonConverter : JsonConverter<CreatureAttributeEntity>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override CreatureAttributeEntity ReadJson(JsonReader reader, Type objectType, CreatureAttributeEntity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, CreatureAttributeEntity value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
