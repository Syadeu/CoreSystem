using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public struct ActorWeaponEquipEvent : IActorWeaponEquipEvent
    {
        private ActorWeaponEquipOptions m_EquipOptions;
        private Instance<ActorWeaponData> m_Weapon;

        public ActorWeaponEquipOptions EquipOptions => m_EquipOptions;
        public Instance<ActorWeaponData> Weapon => m_Weapon;

        public bool BurstCompile => false;

        public ActorWeaponEquipEvent(ActorWeaponEquipOptions options, Instance<ActorWeaponData> weapon)
        {
            m_EquipOptions = options;
            m_Weapon = weapon;
        }
        public ActorWeaponEquipEvent(ActorWeaponEquipOptions options, Reference<ActorWeaponData> weapon)
        {
            m_EquipOptions = options;
            m_Weapon = weapon.CreateInstance();
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
        }
    }
}
