using System;

namespace Syadeu.Presentation
{
    internal interface IAttributeProcessor
    {
        Type TargetAttribute { get; }
        void OnCreated(AttributeBase attribute, DataGameObject dataObj);
        void OnPresentation(AttributeBase attribute, DataGameObject dataObj);
        void OnDead(AttributeBase attribute, DataGameObject dataObj);
        void OnDestory(AttributeBase attribute, DataGameObject dataObj);
    }
}
