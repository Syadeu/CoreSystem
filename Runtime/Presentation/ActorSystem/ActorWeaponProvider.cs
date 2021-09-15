using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using System.Linq;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public class ActorWeaponProvider : ActorProviderBase
    {
        [Header("Accept Weapon Types")]
        [JsonProperty(Order = 0, PropertyName = "ExcludeWeapon")]
        protected Reference<ActorWeaponData>[] m_ExcludeWeapon = Array.Empty<Reference<ActorWeaponData>>();
        [JsonProperty(Order = 1, PropertyName = "IncludeWeapon")]
        protected Reference<ActorWeaponData>[] m_IncludeWeapon = Array.Empty<Reference<ActorWeaponData>>();
        [JsonProperty(Order = 2, PropertyName = "ExcludeWeaponType")]
        protected Reference<ActorWeaponTypeData>[] m_ExcludeWeaponType = Array.Empty<Reference<ActorWeaponTypeData>>();
        [JsonProperty(Order = 3, PropertyName = "IncludeWeaponType")]
        protected Reference<ActorWeaponTypeData>[] m_IncludeWeaponType = Array.Empty<Reference<ActorWeaponTypeData>>();

        [Header("General")]
        [JsonProperty(Order = 4, PropertyName = "DefaultWeapon")]
        protected Reference<ActorWeaponData> m_DefaultWeapon = Reference<ActorWeaponData>.Empty;

        [JsonIgnore] private Type[] m_ReceiveEventOnly = null;
        [JsonIgnore] private Instance<ActorWeaponData> m_EquipedWeapon;

        [JsonIgnore] protected override Type[] ReceiveEventOnly => m_ReceiveEventOnly;
        [JsonIgnore] public Instance<ActorWeaponData> EquipedWeapon => m_EquipedWeapon;
        [JsonIgnore] public float WeaponDamage
        {
            get
            {
                if (EquipedWeapon.IsEmpty())
                {
                    if (m_DefaultWeapon.IsEmpty()) return 0;
                    else if (!m_DefaultWeapon.IsValid())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"Entity({Parent.Name}) has an invalid default weapon.");
                        return 0;
                    }

                    return m_DefaultWeapon.GetObject().Damage;
                }

                return EquipedWeapon.Object.Damage;
            }
        }

        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            m_ReceiveEventOnly = new Type[]
            {
                TypeHelper.TypeOf<IActorWeaponEquipEvent>.Type
            };

            for (int i = 0; i < m_ExcludeWeapon.Length; i++)
            {
                if (m_IncludeWeapon.Contains(m_ExcludeWeapon[i]))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"1");
                }
            }
            for (int i = 0; i < m_ExcludeWeaponType.Length; i++)
            {
                if (m_IncludeWeaponType.Contains(m_ExcludeWeaponType[i]))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"2");
                }
            }
        }
        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            if (ev is IActorWeaponEquipEvent weaponEquipEvent)
            {
                ActorWeaponEquipEventHandler(weaponEquipEvent);
            }
        }
        protected virtual void ActorWeaponEquipEventHandler(IActorWeaponEquipEvent ev)
        {
            if (!IsEquipable(ev.Weapon))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) trying to equip weapon({ev.Weapon.Object.Name}) that doesn\'t fit.");
                return;
            }

            m_EquipedWeapon = ev.Weapon;
            CoreSystem.Logger.Log(Channel.Entity,
                $"Entity({Parent.Name}) has equiped weapon({m_EquipedWeapon.Object.Name}).");
        }

        public bool IsEquipable(Instance<ActorWeaponData> weapon)
        {
            var original = weapon.AsOriginal();
            var weaponObj = weapon.Object;

            if (m_ExcludeWeaponType.Contains(weaponObj.WeaponType))
            {
                if (!m_IncludeWeapon.Contains(original)) return false;
            }
            else if (m_IncludeWeaponType.Contains(weaponObj.WeaponType))
            {
                if (m_ExcludeWeapon.Contains(original)) return false;
            }

            return true;
        }
    }
}
