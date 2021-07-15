using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation;
using System;

namespace Syadeu.Database.CreatureData
{
    public abstract class CreatureAttributeProcessor : ICreatureAttributeProcessor
    {
        Type ICreatureAttributeProcessor.TargetAttribute => TargetAttribute;
        void ICreatureAttributeProcessor.OnCreated(CreatureAttribute attribute, Creature creature, DataGameObject dataObj, CreatureBrain monoObj) => OnCreated(attribute, creature, dataObj, monoObj);

        protected abstract Type TargetAttribute { get; }
        protected abstract void OnCreated(CreatureAttribute attribute, Creature creature, DataGameObject dataObj, CreatureBrain monoObj);
    }
    public abstract class CreatureAttributeProcessor<T> : ICreatureAttributeProcessor where T : CreatureAttribute
    {
        Type ICreatureAttributeProcessor.TargetAttribute => TargetAttribute;
        void ICreatureAttributeProcessor.OnCreated(CreatureAttribute attribute, Creature creature, DataGameObject dataObj, CreatureBrain monoObj) => OnCreated((T)attribute, creature, dataObj, monoObj);

        private Type TargetAttribute => TypeHelper.TypeOf<T>.Type;
        protected abstract void OnCreated(T attribute, Creature creature, DataGameObject dataObj, CreatureBrain monoObj);
    }
}
