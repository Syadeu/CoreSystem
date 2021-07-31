using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Internal;

namespace Syadeu.Presentation.Internal
{
    internal interface IAttributeProcessor : IProcessor
    {
        void OnCreated(AttributeBase attribute, IObject entity);
        void OnCreatedSync(AttributeBase attribute, IObject entity);
        void OnDestroy(AttributeBase attribute, IObject entity);
        void OnDestroySync(AttributeBase attribute, IObject entity);
    }
}
