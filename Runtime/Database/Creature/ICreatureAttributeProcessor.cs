using Syadeu.Mono;
using Syadeu.Presentation;
using System;

namespace Syadeu.Database.CreatureData.Attributes
{
    internal interface ICreatureAttributeProcessor
    {
        GameObjectProxySystem ProxySystem { get; set; }
        CreatureSystem CreatureSystem { get; set; }

        Type TargetAttribute { get; }
        void OnCreated(CreatureAttribute attribute, DataGameObject dataObj);
        void OnPresentation(CreatureAttribute attribute, DataGameObject dataObj);
        void OnDead(CreatureAttribute attribute, DataGameObject dataObj);
        void OnDestory(CreatureAttribute attribute, DataGameObject dataObj);
    }
}
