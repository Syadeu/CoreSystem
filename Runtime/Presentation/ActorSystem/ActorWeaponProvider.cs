using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
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
    [ActorProviderRequire(typeof(ActorInventoryProvider))]
    public sealed class ActorWeaponProvider : ActorProviderBase,
        INotifyComponent<ActorWeaponComponent>
    {
        [Header("Accept Weapon Types")]
        [JsonProperty(Order = 0, PropertyName = "ExcludeWeapon")]
        internal Reference<ActorWeaponData>[] m_ExcludeWeapon = Array.Empty<Reference<ActorWeaponData>>();
        [JsonProperty(Order = 1, PropertyName = "IncludeWeapon")]
        internal Reference<ActorWeaponData>[] m_IncludeWeapon = Array.Empty<Reference<ActorWeaponData>>();
        [JsonProperty(Order = 2, PropertyName = "ExcludeWeaponType")]
        internal Reference<ActorWeaponTypeData>[] m_ExcludeWeaponType = Array.Empty<Reference<ActorWeaponTypeData>>();
        [JsonProperty(Order = 3, PropertyName = "IncludeWeaponType")]
        internal Reference<ActorWeaponTypeData>[] m_IncludeWeaponType = Array.Empty<Reference<ActorWeaponTypeData>>();

        [Header("General")]
        [JsonProperty(Order = 4, PropertyName = "DefaultWeapon")]
        internal Reference<ActorWeaponData> m_DefaultWeapon = Reference<ActorWeaponData>.Empty;
        [Tooltip("최대로 착용할 수 있는 무기의 개수입니다. 0과 같거나 작을 수 없습니다.")]
        [JsonProperty(Order = 5, PropertyName = "MaxEquipableCount")]
        internal int m_MaxEquipableCount = 1;

        [Header("Weapon Position")]
        [JsonProperty(Order = 6, PropertyName = "UseBone")]
        internal bool m_UseBone = true;
        [JsonProperty(Order = 7, PropertyName = "AttachedBone")]
        internal HumanBodyBones m_AttachedBone = HumanBodyBones.RightHand;
        [JsonProperty(Order = 8, PropertyName = "WeaponPosOffset")]
        internal float3 m_WeaponPosOffset = float3.zero;
        [JsonProperty(Order = 9, PropertyName = "WeaponRotOffset")]
        internal float3 m_WeaponRotOffset = float3.zero;

        [Header("TriggerAction")]
        [JsonProperty(Order = 10, PropertyName = "OnWeaponSelected")]
        internal Reference<TriggerAction>[] m_OnWeaponSelected = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 11, PropertyName = "OnEquipWeapon")]
        internal Reference<TriggerAction>[] m_OnEquipWeapon = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 12, PropertyName = "OnUnequipWeapon")]
        internal Reference<TriggerAction>[] m_OnUnequipWeapon = Array.Empty<Reference<TriggerAction>>();

        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            Parent.AddComponent<ActorWeaponComponent>();
            ref ActorWeaponComponent component = ref Parent.GetComponent<ActorWeaponComponent>();
            component.m_Parent = entity;
            component.m_Provider = new Instance<ActorWeaponProvider>(Idx);

            component.m_WeaponPoser = CoroutineJob.Null;

            if (m_UseBone && !entity.HasAttribute<AnimatorAttribute>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) doesn\'t have any {nameof(AnimatorAttribute)} but UseBone.");
            }

            if (m_MaxEquipableCount <= 0)
            {
                m_MaxEquipableCount = 1;
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) in {nameof(ActorWeaponProvider)} Max Equipable Count must be over 0. Force to set 1");
            }
            component.m_EquipedWeapons = new InstanceArray<ActorWeaponData>(m_MaxEquipableCount, Unity.Collections.Allocator.Persistent);

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
                component.m_DefaultWeaponInstance = m_DefaultWeapon.CreateInstance();
                component.m_EquipedWeapons[0] = component.m_DefaultWeaponInstance;
                m_OnEquipWeapon.Execute(Parent);
                component.SelectWeapon(0);
            }

            //Parent.AddComponent(component);
        }
        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            if (ev is IActorWeaponEquipEvent weaponEquipEvent)
            {
                ActorWeaponEquipEventHandler(weaponEquipEvent);
            }
        }
        private void ActorWeaponEquipEventHandler(IActorWeaponEquipEvent ev)
        {
            ref ActorWeaponComponent component = ref Parent.GetComponent<ActorWeaponComponent>();

            if (!component.IsEquipable(ev.Weapon))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) trying to equip weapon({ev.Weapon.Object.Name}) that doesn\'t fit.");
                return;
            }

            if ((ev.EquipOptions & ActorWeaponEquipOptions.SwitchWithSelected) == ActorWeaponEquipOptions.SwitchWithSelected)
            {
                m_OnUnequipWeapon.Execute(Parent);

                ActorInventoryProvider inventory = GetProvider<ActorInventoryProvider>().Object;
                if (inventory == null)
                {
                    CoreSystem.Logger.Log(Channel.Entity,
                        $"Destroying weapon instance({component.SelectedWeapon.Object.Name}) because there\'s no inventory in this actor({Parent.Name}).");

                    if (component.SelectedWeapon.Equals(component.m_DefaultWeaponInstance))
                    {
                        component.m_DefaultWeaponInstance = Instance<ActorWeaponData>.Empty;
                    }
                    component.m_EquipedWeapons[component.m_SelectedWeaponIndex].Destroy();
                }
                else
                {
                    if (component.SelectedWeapon.Equals(component.m_DefaultWeaponInstance))
                    {
                        component.m_EquipedWeapons[component.m_SelectedWeaponIndex].Destroy();
                        component.m_DefaultWeaponInstance = Instance<ActorWeaponData>.Empty;
                    }
                    else inventory.Insert(component.SelectedWeapon.Cast<ActorWeaponData, IObject>());
                }

                component.m_EquipedWeapons[component.m_SelectedWeaponIndex] = ev.Weapon;

                m_OnEquipWeapon.Execute(Parent);

                if ((ev.EquipOptions & ActorWeaponEquipOptions.SelectWeapon) == ActorWeaponEquipOptions.SelectWeapon)
                {
                    component.SelectWeapon(component.m_SelectedWeaponIndex);
                }

                CoreSystem.Logger.Log(Channel.Entity,
                    $"Entity({Parent.Name}) has equiped weapon({component.SelectedWeapon.Object.Name}).");
            }
            else
            {
                int emptySpace;
                if (component.m_EquipedWeapons[0].Equals(component.m_DefaultWeaponInstance))
                {
                    component.m_EquipedWeapons[0].Destroy();
                    component.m_DefaultWeaponInstance = Instance<ActorWeaponData>.Empty;
                    emptySpace = 0;
                }
                else emptySpace = GetEmptyEquipSpace(in component);

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
                    component.m_EquipedWeapons[emptySpace] = ev.Weapon;
                    m_OnEquipWeapon.Execute(Parent);

                    if ((ev.EquipOptions & ActorWeaponEquipOptions.SelectWeapon) == ActorWeaponEquipOptions.SelectWeapon)
                    {
                        component.SelectWeapon(emptySpace);
                    }

                    CoreSystem.Logger.Log(Channel.Entity,
                        $"Entity({Parent.Name}) has equiped weapon({component.SelectedWeapon.Object.Name}).");
                }
            }
        }

        protected override void OnProxyCreated(RecycleableMonobehaviour monoObj)
        {
            ref ActorWeaponComponent component = ref Parent.GetComponent<ActorWeaponComponent>();

            if (component.SelectedWeapon.IsValid() && 
                component.SelectedWeapon.Object.PrefabInstance.IsValid())
            {
                WeaponPoser weaponPoser = new WeaponPoser(Parent.As<IEntityData, ActorEntity>(), component.SelectedWeapon, 
                    m_UseBone, m_AttachedBone, m_WeaponPosOffset, m_WeaponRotOffset);
                component.m_WeaponPoser = StartCoroutine(weaponPoser);
            }
        }
        protected override void OnProxyRemoved(RecycleableMonobehaviour monoObj)
        {
            ref ActorWeaponComponent component = ref Parent.GetComponent<ActorWeaponComponent>();

            if (!component.m_WeaponPoser.IsNull() &&
                component.m_WeaponPoser.IsValid())
            {
                component.m_WeaponPoser.Stop();
                component.m_WeaponPoser = CoroutineJob.Null;
            }
        }

        private int GetEmptyEquipSpace(in ActorWeaponComponent component)
        {
            for (int i = 0; i < component.m_EquipedWeapons.Length; i++)
            {
                if (component.m_EquipedWeapons[i].IsEmpty())
                {
                    return i;
                }
            }
            return -1;
        }

        private struct WeaponPoser : ICoroutineJob
        {
            private Entity<ActorEntity> m_Entity;
            private Instance<ActorWeaponData> m_Weapon;

            private bool m_UseBone;
            private HumanBodyBones m_TargetBone;
            private float3 m_Offset, m_RotOffset;

            UpdateLoop ICoroutineJob.Loop => UpdateLoop.Transform;

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

                while (m_Weapon.IsValid() && m_Entity.IsValid())
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
                    weaponTr.rotation = targetRot;

                    float3 targetPos = targetTr.position;
                    if (overrideData.OverrideOptions == ActorWeaponData.OverrideOptions.None)
                    {
                        targetPos += math.mul(targetRot, m_Offset);
                    }
                    else if (overrideData.OverrideOptions == ActorWeaponData.OverrideOptions.Override)
                    {
                        targetPos += math.mul(targetRot, overrideData.WeaponPosOffset);
                    }
                    weaponTr.position = targetPos;
                    
                    yield return null;
                }
            }
        }
    }
}
