using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation;
using System;

namespace Syadeu.Database.CreatureData.Attributes
{
    public abstract class CreatureAttributeProcessor : ICreatureAttributeProcessor
    {
        Type ICreatureAttributeProcessor.TargetAttribute => TargetAttribute;
        void ICreatureAttributeProcessor.OnCreated(CreatureAttribute attribute, DataGameObject dataObj) => OnCreated(attribute, dataObj);
        void ICreatureAttributeProcessor.OnPresentation(CreatureAttribute attribute, DataGameObject dataObj) => OnPresentation(attribute, dataObj);
        void ICreatureAttributeProcessor.OnDead(CreatureAttribute attribute, DataGameObject dataObj) => OnDead(attribute, dataObj);
        void ICreatureAttributeProcessor.OnDestory(CreatureAttribute attribute, DataGameObject dataObj) => OnDestory(attribute, dataObj);

        protected abstract Type TargetAttribute { get; }
        protected abstract void OnCreated(CreatureAttribute attribute, DataGameObject dataObj);
        protected virtual void OnPresentation(CreatureAttribute attribute, DataGameObject dataObj) { }
        protected virtual void OnDead(CreatureAttribute attribute, DataGameObject dataObj) { }
        protected abstract void OnDestory(CreatureAttribute attribute, DataGameObject dataObj);
    }
    public abstract class CreatureAttributeProcessor<T> : ICreatureAttributeProcessor where T : CreatureAttribute
    {
        Type ICreatureAttributeProcessor.TargetAttribute => TargetAttribute;
        void ICreatureAttributeProcessor.OnCreated(CreatureAttribute attribute, DataGameObject dataObj) => OnCreated((T)attribute, dataObj);
        void ICreatureAttributeProcessor.OnPresentation(CreatureAttribute attribute, DataGameObject dataObj) => OnPresentation((T)attribute, dataObj);
        void ICreatureAttributeProcessor.OnDead(CreatureAttribute attribute, DataGameObject dataObj) => OnDead((T)attribute, dataObj);
        void ICreatureAttributeProcessor.OnDestory(CreatureAttribute attribute, DataGameObject dataObj) => OnDestory((T)attribute, dataObj);

        private Type TargetAttribute => TypeHelper.TypeOf<T>.Type;
        protected abstract void OnCreated(T attribute, DataGameObject dataObj);
        protected virtual void OnPresentation(T attribute, DataGameObject dataObj) { }
        protected virtual void OnDead(T attribute, DataGameObject dataObj) { }
        protected abstract void OnDestory(T attribute, DataGameObject dataObj);
    }
}
