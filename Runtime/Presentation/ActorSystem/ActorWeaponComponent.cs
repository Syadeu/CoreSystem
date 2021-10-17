using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Linq;

namespace Syadeu.Presentation.Actor
{
    public struct ActorWeaponComponent : IEntityComponent, IDisposable
    {
        internal Entity<ActorEntity> m_Parent;
        internal Instance<ActorWeaponProvider> m_Provider;
        internal CoroutineJob m_WeaponPoser;

        internal Reference<ActorWeaponData> m_DefaultWeapon;
        internal Instance<ActorWeaponData> m_DefaultWeaponInstance;
        internal FixedInstanceList16<ActorWeaponData> m_EquipedWeapons;
        internal int m_SelectedWeaponIndex;

        public ActorWeaponProvider Provider => m_Provider.GetObject();
        public FixedInstanceList16<ActorWeaponData> EquipedWeapons => m_EquipedWeapons;
        public Instance<ActorWeaponData> SelectedWeapon => m_EquipedWeapons[m_SelectedWeaponIndex];
        public float WeaponDamage
        {
            get
            {
                if (EquipedWeapons[m_SelectedWeaponIndex].IsEmpty())
                {
                    if (m_DefaultWeapon.IsEmpty()) return 0;
                    else if (!m_DefaultWeapon.IsValid())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"Entity({m_Parent.Name}) has an invalid default weapon.");
                        return 0;
                    }

                    return m_DefaultWeapon.GetObject().Damage;
                }

                return EquipedWeapons[m_SelectedWeaponIndex].GetObject().Damage;
            }
        }

        public void SelectWeapon(int index)
        {
            ActorWeaponProvider provider = Provider;
            if (index < 0 || index >= provider.m_MaxEquipableCount)
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"{nameof(SelectWeapon)} index out of range. Index {index}.");
                return;
            }

            m_SelectedWeaponIndex = index;
            Provider.m_OnWeaponSelected.Execute(m_Parent.As<ActorEntity, IEntityData>());
        }
        public bool IsEquipable(Instance<ActorWeaponData> weapon)
        {
            var original = weapon.AsOriginal();
            var weaponObj = weapon.GetObject();

            ActorWeaponProvider provider = Provider;

            if (provider.m_ExcludeWeaponType.Contains(weaponObj.WeaponType))
            {
                if (!provider.m_IncludeWeapon.Contains(original)) return false;
            }
            else if (provider.m_IncludeWeaponType.Contains(weaponObj.WeaponType))
            {
                if (provider.m_ExcludeWeapon.Contains(original)) return false;
            }

            return true;
        }

        void IDisposable.Dispose()
        {
            if (!m_WeaponPoser.IsNull() && m_WeaponPoser.IsValid())
            {
                m_WeaponPoser.Stop();
                m_WeaponPoser = CoroutineJob.Null;
            }

            for (int i = 0; i < m_EquipedWeapons.Length; i++)
            {
                if (m_EquipedWeapons[i].IsEmpty()) continue;

                m_EquipedWeapons[i].Destroy();
            }
            //m_EquipedWeapons.Dispose();

            if (m_DefaultWeaponInstance.IsValid())
            {
                m_DefaultWeaponInstance.Destroy();
            }
        }
    }
}
