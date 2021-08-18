using Newtonsoft.Json;
using Syadeu.Database;
using Unity.Mathematics;

namespace Syadeu.Presentation.Entities
{
    public sealed class ConvertedEntity : EntityBase
    {
        public override bool IsValid() => transform != null;
    }
}
