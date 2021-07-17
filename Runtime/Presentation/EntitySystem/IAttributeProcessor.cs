using System;

namespace Syadeu.Presentation
{
    internal interface IAttributeProcessor
    {
        Type TargetAttribute { get; }
        void OnCreated(AttributeBase attribute, DataGameObject dataObj);
        void OnDestory(AttributeBase attribute, DataGameObject dataObj);
    }
    public interface IAttributeOnPresentation
    {
        void OnPresentation(AttributeBase attribute, DataGameObject dataObj);
    }
    public interface IAttributeOnProxy : IAttributeOnProxyCreated, IAttributeOnProxyRemoved { }
    public interface IAttributeOnProxyCreated
    {
        void OnProxyCreated(AttributeBase attribute, DataGameObject dataObj);
    }
    public interface IAttributeOnProxyRemoved
    {
        void OnProxyRemoved(AttributeBase attribute, DataGameObject dataObj);
    }
}
