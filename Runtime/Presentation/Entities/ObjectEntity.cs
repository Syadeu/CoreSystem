using Newtonsoft.Json;
using Syadeu.Database;

namespace Syadeu.Presentation.Entities
{
    public sealed class ObjectEntity : EntityBase
    {
        protected override ObjectBase Copy()
        {
            ObjectEntity clone = (ObjectEntity)base.Copy();
            return clone;
        }
    }
}
