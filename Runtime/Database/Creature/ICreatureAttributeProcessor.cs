using Syadeu.Presentation;
using System;

namespace Syadeu.Database.CreatureData
{
    internal interface ICreatureAttributeProcessor
    {
        Type TargetAttribute { get; }
        void OnCreated(CreatureAttribute attribute, Creature creature, DataGameObject dataObj);
    }
}
