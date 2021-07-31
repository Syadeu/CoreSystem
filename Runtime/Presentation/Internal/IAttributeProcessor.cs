using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Internal;

namespace Syadeu.Presentation.Internal
{
    internal interface IAttributeProcessor : IProcessor
    {
        void OnCreated(AttributeBase attribute, IEntityData entity);
        void OnCreatedSync(AttributeBase attribute, IEntityData entity);
        void OnDestroy(AttributeBase attribute, IEntityData entity);
        void OnDestroySync(AttributeBase attribute, IEntityData entity);
    }
}
