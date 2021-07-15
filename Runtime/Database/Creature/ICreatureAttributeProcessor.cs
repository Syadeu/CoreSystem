using Syadeu.Mono;
using Syadeu.Presentation;
using System;

namespace Syadeu.Database.CreatureData
{
    internal interface ICreatureAttributeProcessor
    {
        Type TargetAttribute { get; }
        void OnCreated(CreatureAttribute attribute, Creature creature, DataGameObject dataObj, CreatureBrain monoObj);
        void OnPresentation(CreatureAttribute attribute, Creature creature, DataGameObject dataObj, CreatureBrain monoObj);
        void OnDestory(CreatureAttribute attribute, Creature creature, DataGameObject dataObj, CreatureBrain monoObj);
    }
}
