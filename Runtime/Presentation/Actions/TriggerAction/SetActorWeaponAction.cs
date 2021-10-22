#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Set Actor Weapon")]
    public sealed class SetActorWeaponAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Holster")]
        private bool m_Holster;
        [JsonProperty(Order = 1, PropertyName = "Aiming")]
        private bool m_Aiming;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            ref ActorWeaponComponent weapon = ref entity.GetComponent<ActorWeaponComponent>();
            weapon.Holster = m_Holster;
            weapon.Aiming = m_Aiming;
        }
    }
}
