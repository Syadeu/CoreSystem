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

        //[Header("Weapon Position")]
        //[JsonProperty(Order = 6, PropertyName = "UseBone")]
        //internal bool m_UseBone = true;
        //[JsonProperty(Order = 7, PropertyName = "AttachedBone")]
        //internal HumanBodyBones m_AttachedBone = HumanBodyBones.RightHand;
        //[JsonProperty(Order = 8, PropertyName = "WeaponPosOffset")]
        //internal float3 m_WeaponPosOffset = float3.zero;
        //[JsonProperty(Order = 9, PropertyName = "WeaponRotOffset")]
        //internal float3 m_WeaponRotOffset = float3.zero;

        [Header("TriggerAction")]
        [JsonProperty(Order = 10, PropertyName = "OnWeaponSelected")]
        internal Reference<TriggerAction>[] m_OnWeaponSelected = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 11, PropertyName = "OnEquipWeapon")]
        internal Reference<TriggerAction>[] m_OnEquipWeapon = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 12, PropertyName = "OnUnequipWeapon")]
        internal Reference<TriggerAction>[] m_OnUnequipWeapon = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] CoroutineJob m_WeaponPoser;

        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            Parent.AddComponent<ActorWeaponComponent>();
            ref ActorWeaponComponent component = ref Parent.GetComponent<ActorWeaponComponent>();
            component.m_Parent = entity;

            component.m_DefaultWeapon = m_DefaultWeapon;
            component.m_MaxEquipableCount = m_MaxEquipableCount;

            component.m_ExcludeWeapon = m_ExcludeWeapon.ToFixedList16();
            component.m_IncludeWeapon = m_IncludeWeapon.ToFixedList16();
            component.m_ExcludeWeaponType = m_ExcludeWeaponType.ToFixedList16();
            component.m_IncludeWeaponType = m_IncludeWeaponType.ToFixedList16();

            component.m_OnWeaponSelected = m_OnWeaponSelected.ToFixedList16();
            component.m_OnEquipWeapon = m_OnEquipWeapon.ToFixedList16();
            component.m_OnUnequipWeapon = m_OnUnequipWeapon.ToFixedList16();

            if (m_MaxEquipableCount <= 0)
            {
                m_MaxEquipableCount = 1;
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) in {nameof(ActorWeaponProvider)} Max Equipable Count must be over 0. Force to set 1");
            }
            
            component.m_EquipedWeapons = new FixedInstanceList16<ActorWeaponData>();

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
                //m_OnEquipWeapon.Execute(Parent);
                //component.SelectWeapon(0);

                ActorWeaponEquipEvent ev = new ActorWeaponEquipEvent(
                    ActorWeaponEquipOptions.SelectWeapon, m_DefaultWeapon);
                ScheduleEvent(ev);
                //component.m_DefaultWeaponInstance = m_DefaultWeapon.CreateInstance();
                //component.m_EquipedWeapons[0] = component.m_DefaultWeaponInstance;
            }

            WeaponPoser weaponPoser = new WeaponPoser(Parent.As<IEntityData, ActorEntity>());
            m_WeaponPoser = StartCoroutine(weaponPoser);
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
                    $"Entity({Parent.Name}) trying to equip weapon({ev.Weapon.GetObject().Name}) that doesn\'t fit.");
                return;
            }

            if ((ev.EquipOptions & ActorWeaponEquipOptions.SwitchWithSelected) == ActorWeaponEquipOptions.SwitchWithSelected)
            {
                component.m_OnUnequipWeapon.Execute(Parent);

                ActorInventoryProvider inventory = GetProvider<ActorInventoryProvider>().GetObject();
                if (inventory == null)
                {
                    CoreSystem.Logger.Log(Channel.Entity,
                        $"Destroying weapon instance({component.SelectedWeapon.GetObject().Name}) because there\'s no inventory in this actor({Parent.Name}).");

                    //if (component.SelectedWeapon.Equals(component.m_DefaultWeaponInstance))
                    //{
                    //    component.m_DefaultWeaponInstance = Instance<ActorWeaponData>.Empty;
                    //}
                    component.m_EquipedWeapons[component.m_SelectedWeaponIndex].Destroy();
                }
                else
                {
                    //if (component.Selected == 0)
                    //{
                    //    component.m_EquipedWeapons[component.m_SelectedWeaponIndex].Destroy();
                    //    component.m_DefaultWeaponInstance = Instance<ActorWeaponData>.Empty;
                    //}
                    //else inventory.Insert(component.SelectedWeapon.Cast<ActorWeaponData, IObject>());

                    inventory.Insert(component.SelectedWeapon.Cast<ActorWeaponData, IObject>());
                }

                component.m_EquipedWeapons[component.m_SelectedWeaponIndex] = ev.Weapon;

                component.m_OnEquipWeapon.Execute(Parent);

                if ((ev.EquipOptions & ActorWeaponEquipOptions.SelectWeapon) == ActorWeaponEquipOptions.SelectWeapon)
                {
                    component.SelectWeapon(component.m_SelectedWeaponIndex);
                }

                CoreSystem.Logger.Log(Channel.Entity,
                    $"Entity({Parent.Name}) has equiped weapon({component.SelectedWeapon.GetObject().Name}).");
            }
            else
            {
                if (component.Equiped >= component.m_MaxEquipableCount)
                {
                    if ((ev.EquipOptions & ActorWeaponEquipOptions.DestroyIfIsFull) == ActorWeaponEquipOptions.DestroyIfIsFull)
                    {
                        ev.Weapon.Destroy();
                    }
                    else if ((ev.EquipOptions & ActorWeaponEquipOptions.ToInventoryIfIsFull) == ActorWeaponEquipOptions.ToInventoryIfIsFull)
                    {
                        ActorInventoryProvider inventory = GetProvider<ActorInventoryProvider>().GetObject();
                        if (inventory == null)
                        {
                            CoreSystem.Logger.LogError(Channel.Entity,
                                $"Destroying equip request weapon instance({ev.Weapon.GetObject().Name}). There\'s no inventory in this actor({Parent.Name}) but you\'re trying to insert inventory.");
                            ev.Weapon.Destroy();
                        }
                        else
                        {
                            inventory.Insert(ev.Weapon.Cast<ActorWeaponData, IObject>());
                        }
                    }
                    else
                    {
                        "unhandled".ToLogError();
                    }
                }
                else
                {
                    int index = component.Equiped;
                    component.m_EquipedWeapons.Add(ev.Weapon);
                    m_OnEquipWeapon.Execute(Parent);

                    if ((ev.EquipOptions & ActorWeaponEquipOptions.SelectWeapon) == ActorWeaponEquipOptions.SelectWeapon)
                    {
                        component.SelectWeapon(index);
                    }

                    CoreSystem.Logger.Log(Channel.Entity,
                        $"Entity({Parent.Name}) has equiped weapon({component.SelectedWeapon.GetObject().Name}).");


                    $"Entity({Parent.Name}) has equiped weapon({component.SelectedWeapon.GetObject().Name}).".ToLog();
                }
            }
        }

        protected override void OnDestroy()
        {
            m_WeaponPoser.Stop();
        }

        private struct WeaponPoser : ICoroutineJob
        {
            private Entity<ActorEntity> m_Entity;

            UpdateLoop ICoroutineJob.Loop => UpdateLoop.Transform;

            public WeaponPoser(Entity<ActorEntity> entity)
            {
                m_Entity = entity;
            }

            public void Dispose()
            {
            }
            private static void SetPosition(Entity<ActorEntity> entity, in AnimatorAttribute animator, in Instance<ActorWeaponData> weapon, bool weaponDrawn)
            {
                ActorWeaponData data = weapon.GetObject();
                if (data.PrefabInstance.IsEmpty() || !data.PrefabInstance.IsValid())
                {
                    return;
                }

                ActorWeaponData.OverrideData overrideData = data.Overrides;
                ITransform weaponTr = data.PrefabInstance.transform;

                ActorWeaponData.OverrideOptions options = weaponDrawn ? overrideData.DrawOverrideOptions : overrideData.HolsterOverrideOptions;
                bool useBone = weaponDrawn ? overrideData.DrawUseBone : overrideData.HolsterUseBone;
                HumanBodyBones attachedBone = weaponDrawn ? overrideData.DrawAttachedBone : overrideData.HolsterAttachedBone;
                float3 posOffset = weaponDrawn ? overrideData.DrawWeaponPosOffset : overrideData.HolsterWeaponPosOffset;
                float3 rotOffset = weaponDrawn ? overrideData.DrawWeaponRotOffset : overrideData.HolsterWeaponRotOffset;

                float3 targetPosition;
                quaternion targetRotation;
                if (!useBone || !entity.hasProxy)
                {
                    var tr = entity.transform;
                    targetPosition = tr.position;
                    targetRotation = tr.rotation;
                }
                else
                {
                    var tr = animator.AnimatorComponent.Animator.GetBoneTransform(attachedBone);
                    targetPosition = tr.position;
                    targetRotation = tr.rotation;
                }

                //
                if (options == ActorWeaponData.OverrideOptions.Addictive)
                {
                    //targetRot *= Quaternion.Euler(m_RotOffset);
                    "not implements".ToLogError();
                }
                else if (options == ActorWeaponData.OverrideOptions.Override)
                {
                    targetRotation *= Quaternion.Euler(rotOffset);
                    targetPosition += math.mul(targetRotation, posOffset);
                }

                weaponTr.rotation = targetRotation;
                weaponTr.position = targetPosition;
                //
            }
            private static void SetWeaponPositions(Entity<ActorEntity> entity, in AnimatorAttribute animator)
            {
                ref ActorWeaponComponent weaponComponent = ref entity.GetComponent<ActorWeaponComponent>();

                for (int i = weaponComponent.m_EquipedWeapons.Length - 1; i >= 0; i--)
                {
                    bool weaponDrawn = weaponComponent.Selected == i && !weaponComponent.Holster;
                    SetPosition(entity, in animator, weaponComponent.m_EquipedWeapons[i], weaponDrawn);
                }
            }
            public IEnumerator Execute()
            {
                AnimatorAttribute animator = m_Entity.GetAttribute<AnimatorAttribute>();

                while (m_Entity.IsValid())
                {
                    SetWeaponPositions(m_Entity, in animator);

                    yield return null;
                }
            }
        }
    }
}
