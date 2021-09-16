﻿using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("ActorProvider: Weapon Provider")]
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
        [Tooltip("최대로 착용할 수 있는 무기의 개수입니다. 0과 같거나 작을 수 없습니다.")]
        [JsonProperty(Order = 5, PropertyName = "MaxEquipableCount")]
        protected int m_MaxEquipableCount = 1;

        [Header("Weapon Position")]
        [JsonProperty(Order = 6, PropertyName = "UseBone")]
        protected bool m_UseBone = true;
        [JsonProperty(Order = 7, PropertyName = "AttachedBone")]
        protected HumanBodyBones m_AttachedBone = HumanBodyBones.RightHand;
        [JsonProperty(Order = 8, PropertyName = "WeaponPosOffset")]
        protected float3 m_WeaponPosOffset = float3.zero;
        [JsonProperty(Order = 9, PropertyName = "WeaponRotOffset")]
        protected float3 m_WeaponRotOffset = float3.zero;

        [Header("TriggerAction")]
        [JsonProperty(Order = 10, PropertyName = "OnWeaponSelected")]
        protected Reference<TriggerAction>[] m_OnWeaponSelected = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 11, PropertyName = "OnEquipWeapon")]
        protected Reference<TriggerAction>[] m_OnEquipWeapon = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 12, PropertyName = "OnUnequipWeapon")]
        protected Reference<TriggerAction>[] m_OnUnequipWeapon = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] private Type[] m_ReceiveEventOnly = null;

        [JsonIgnore] private CoroutineJob m_WeaponPoser = CoroutineJob.Null;

        [JsonIgnore] private Instance<ActorWeaponData> m_DefaultWeaponInstance = Instance<ActorWeaponData>.Empty;
        [JsonIgnore] private InstanceArray<ActorWeaponData> m_EquipedWeapons;
        [JsonIgnore] private int m_SelectedWeaponIndex = 0;

        [JsonIgnore] protected override Type[] ReceiveEventOnly => m_ReceiveEventOnly;
        [JsonIgnore] public InstanceArray<ActorWeaponData> EquipedWeapons => m_EquipedWeapons;
        [JsonIgnore] public Instance<ActorWeaponData> SelectedWeapon => m_EquipedWeapons[m_SelectedWeaponIndex];
        [JsonIgnore] public float WeaponDamage
        {
            get
            {
                if (EquipedWeapons[m_SelectedWeaponIndex].IsEmpty())
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

                return EquipedWeapons[m_SelectedWeaponIndex].Object.Damage;
            }
        }

        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            m_ReceiveEventOnly = new Type[]
            {
                TypeHelper.TypeOf<IActorWeaponEquipEvent>.Type
            };

            if (!entity.HasAttribute<AnimatorAttribute>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) doesn\'t have any {nameof(AnimatorAttribute)}.");
            }

            if (m_MaxEquipableCount <= 0)
            {
                m_MaxEquipableCount = 1;
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) in {nameof(ActorWeaponProvider)} Max Equipable Count must be over 0. Force to set 1");
            }
            m_EquipedWeapons = new InstanceArray<ActorWeaponData>(m_MaxEquipableCount, Unity.Collections.Allocator.Persistent);

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

            if (!m_DefaultWeapon.IsEmpty() && m_DefaultWeapon.IsValid())
            {
                m_DefaultWeaponInstance = m_DefaultWeapon.CreateInstance();
                m_EquipedWeapons[0] = m_DefaultWeaponInstance;
                m_OnEquipWeapon.Execute(Parent.CastAs<ActorEntity, IEntityData>());
                SelectWeapon(0);
            }
        }
        protected override void OnDispose()
        {
            m_EquipedWeapons.Dispose();

            if (!m_WeaponPoser.IsNull() && m_WeaponPoser.IsValid())
            {
                m_WeaponPoser.Stop();
                m_WeaponPoser = CoroutineJob.Null;
            }
        }
        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            if (ev is IActorWeaponEquipEvent weaponEquipEvent)
            {
                ActorWeaponEquipEventHandler(weaponEquipEvent);
            }
        }
        protected void ActorWeaponEquipEventHandler(IActorWeaponEquipEvent ev)
        {
            if (!IsEquipable(ev.Weapon))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) trying to equip weapon({ev.Weapon.Object.Name}) that doesn\'t fit.");
                return;
            }

            if ((ev.EquipOptions & ActorWeaponEquipOptions.SwitchWithSelected) == ActorWeaponEquipOptions.SwitchWithSelected)
            {
                m_OnUnequipWeapon.Execute(Parent.CastAs<ActorEntity, IEntityData>());

                ActorInventoryProvider inventory = GetProvider<ActorInventoryProvider>().Object;
                if (inventory == null)
                {
                    CoreSystem.Logger.Log(Channel.Entity,
                        $"Destroying weapon instance({SelectedWeapon.Object.Name}) because there\'s no inventory in this actor({Parent.Name}).");

                    if (SelectedWeapon.Equals(m_DefaultWeaponInstance))
                    {
                        m_DefaultWeaponInstance = Instance<ActorWeaponData>.Empty;
                    }
                    m_EquipedWeapons[m_SelectedWeaponIndex].Destroy();
                }
                else
                {
                    if (SelectedWeapon.Equals(m_DefaultWeaponInstance))
                    {
                        m_EquipedWeapons[m_SelectedWeaponIndex].Destroy();
                        m_DefaultWeaponInstance = Instance<ActorWeaponData>.Empty;
                    }
                    else inventory.Insert(SelectedWeapon.Cast<ActorWeaponData, IObject>());
                }

                m_EquipedWeapons[m_SelectedWeaponIndex] = ev.Weapon;

                m_OnEquipWeapon.Execute(Parent.CastAs<ActorEntity, IEntityData>());

                if ((ev.EquipOptions & ActorWeaponEquipOptions.SelectWeapon) == ActorWeaponEquipOptions.SelectWeapon)
                {
                    SelectWeapon(m_SelectedWeaponIndex);
                }

                CoreSystem.Logger.Log(Channel.Entity,
                    $"Entity({Parent.Name}) has equiped weapon({SelectedWeapon.Object.Name}).");
            }
            else
            {
                int emptySpace;
                if (m_EquipedWeapons[0].Equals(m_DefaultWeaponInstance))
                {
                    m_EquipedWeapons[0].Destroy();
                    m_DefaultWeaponInstance = Instance<ActorWeaponData>.Empty;
                    emptySpace = 0;
                }
                else emptySpace = GetEmptyEquipSpace();

                if (emptySpace < 0)
                {
                    if ((ev.EquipOptions & ActorWeaponEquipOptions.DestroyIfIsFull) == ActorWeaponEquipOptions.DestroyIfIsFull)
                    {
                        ev.Weapon.Destroy();
                    }
                    else if ((ev.EquipOptions & ActorWeaponEquipOptions.ToInventoryIfIsFull) == ActorWeaponEquipOptions.ToInventoryIfIsFull)
                    {
                        ActorInventoryProvider inventory = GetProvider<ActorInventoryProvider>().Object;
                        if (inventory == null)
                        {
                            CoreSystem.Logger.LogError(Channel.Entity,
                                $"Destroying equip request weapon instance({ev.Weapon.Object.Name}). There\'s no inventory in this actor({Parent.Name}) but you\'re trying to insert inventory.");
                            ev.Weapon.Destroy();
                        }
                        else
                        {
                            inventory.Insert(ev.Weapon.Cast<ActorWeaponData, IObject>());
                        }
                    }
                }
                else
                {
                    m_EquipedWeapons[emptySpace] = ev.Weapon;
                    m_OnEquipWeapon.Execute(Parent.CastAs<ActorEntity, IEntityData>());

                    if ((ev.EquipOptions & ActorWeaponEquipOptions.SelectWeapon) == ActorWeaponEquipOptions.SelectWeapon)
                    {
                        SelectWeapon(emptySpace);
                    }

                    CoreSystem.Logger.Log(Channel.Entity,
                        $"Entity({Parent.Name}) has equiped weapon({SelectedWeapon.Object.Name}).");
                }
            }

            
        }

        protected override void OnProxyCreated(RecycleableMonobehaviour monoObj)
        {
            if (SelectedWeapon.IsValid() && SelectedWeapon.Object.PrefabInstance.IsValid())
            {
                WeaponPoser weaponPoser = new WeaponPoser(Parent, SelectedWeapon, 
                    m_UseBone, m_AttachedBone, m_WeaponPosOffset, m_WeaponRotOffset);
                m_WeaponPoser = StartCoroutine(weaponPoser);
            }
        }
        protected override void OnProxyRemoved(RecycleableMonobehaviour monoObj)
        {
            if (!m_WeaponPoser.IsNull() && m_WeaponPoser.IsValid())
            {
                m_WeaponPoser.Stop();
                m_WeaponPoser = CoroutineJob.Null;
            }
        }

        protected int GetEmptyEquipSpace()
        {
            for (int i = 0; i < m_EquipedWeapons.Length; i++)
            {
                if (m_EquipedWeapons[i].IsEmpty())
                {
                    return i;
                }
            }
            return -1;
        }

        public void SelectWeapon(int index)
        {
            if (index < 0 || index >= m_MaxEquipableCount)
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"{nameof(SelectWeapon)} index out of range. Index {index}.");
                return;
            }

            m_SelectedWeaponIndex = index;
            m_OnWeaponSelected.Execute(Parent.CastAs<ActorEntity, IEntityData>());
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

        private struct WeaponPoser : ICoroutineJob
        {
            private Entity<ActorEntity> m_Entity;
            private Instance<ActorWeaponData> m_Weapon;

            private bool m_UseBone;
            private HumanBodyBones m_TargetBone;
            private float3 m_Offset, m_RotOffset;

            public WeaponPoser(Entity<ActorEntity> entity, Instance<ActorWeaponData> weapon,
                bool useBone, HumanBodyBones targetBone, float3 offset, float3 rotOffset)
            {
                m_Entity = entity;
                m_Weapon = weapon;

                m_UseBone = useBone;
                m_TargetBone = targetBone;
                m_Offset = offset;
                m_RotOffset = rotOffset;
            }

            public void Dispose()
            {
            }
            public IEnumerator Execute()
            {
                if (!m_Weapon.IsValid()) yield break;

                AnimatorAttribute animator = m_Entity.GetAttribute<AnimatorAttribute>();
                if (animator == null) yield break;

                ActorWeaponData.OverrideData overrideData = m_Weapon.Object.Overrides;
                ITransform weaponTr = m_Weapon.Object.PrefabInstance.transform;
                Transform targetTr = null;

                while (m_Weapon.IsValid())
                {
                    if (!m_Entity.hasProxy)
                    {
                        targetTr = null;

                        yield return null;
                        continue;
                    }

                    if (targetTr == null)
                    {
                        if (overrideData.OverrideOptions == ActorWeaponData.OverrideOptions.Override)
                        {
                            if (overrideData.UseBone)
                            {
                                targetTr = animator.AnimatorComponent.Animator.GetBoneTransform(overrideData.AttachedBone);
                            }
                            else
                            {
                                targetTr = animator.AnimatorComponent.transform;
                            }

                            if (targetTr == null)
                            {
                                CoreSystem.Logger.LogError(Channel.Entity,
                                    $"Could not found bone transform({TypeHelper.Enum<HumanBodyBones>.ToString(overrideData.AttachedBone)}) in entity({m_Entity.Name}). Force to not use bone.");

                                targetTr = animator.AnimatorComponent.transform;
                            }
                        }
                        else
                        {
                            if (m_UseBone)
                            {
                                targetTr = animator.AnimatorComponent.Animator.GetBoneTransform(m_TargetBone);
                            }
                            else
                            {
                                targetTr = animator.AnimatorComponent.transform;
                            }

                            if (targetTr == null)
                            {
                                CoreSystem.Logger.LogError(Channel.Entity,
                                    $"Could not found bone transform({TypeHelper.Enum<HumanBodyBones>.ToString(m_TargetBone)}) in entity({m_Entity.Name}). Force to not use bone.");

                                targetTr = animator.AnimatorComponent.transform;
                            }
                        }
                    }

                    quaternion targetRot = targetTr.rotation;
                    if (overrideData.OverrideOptions == ActorWeaponData.OverrideOptions.None)
                    {
                        targetRot *= Quaternion.Euler(m_RotOffset);
                    }
                    else if (overrideData.OverrideOptions == ActorWeaponData.OverrideOptions.Override)
                    {
                        targetRot *= Quaternion.Euler(overrideData.WeaponRotOffset);
                    }
                    //weaponTr.rotation = Quaternion.Euler(targetRot);
                    weaponTr.rotation = targetRot;

                    float3 targetPos = targetTr.position;
                    if (overrideData.OverrideOptions == ActorWeaponData.OverrideOptions.None)
                    {
                        targetPos += math.mul(weaponTr.rotation, m_Offset);
                    }
                    else if (overrideData.OverrideOptions == ActorWeaponData.OverrideOptions.Override)
                    {
                        targetPos += math.mul(weaponTr.rotation, overrideData.WeaponPosOffset);
                    }
                    weaponTr.position = targetPos;

                    yield return null;
                }
            }
        }
    }
}
