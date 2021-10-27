using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("ActorProvider: TRPG Attack Provider")]
    public sealed class TRPGActorAttackProvider : ActorAttackProvider,
        INotifyComponent<TRPGActorAttackComponent>
    {
        [JsonProperty(Order = 1, PropertyName = "SearchRange")] private int m_SearchRange = 3;
        [JsonProperty(Order = 2, PropertyName = "DefaultConsumeAP")] private int m_DefaultConsumeAP = 1;

        [JsonIgnore] private NativeList<int> m_TempGetRange;

        protected override void OnCreated()
        {
            m_TempGetRange = new NativeList<int>(512, Allocator.Persistent);
        }
        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            base.OnCreated(entity);

            entity.AddComponent<TRPGActorAttackComponent>();
            ref var com = ref entity.GetComponent<TRPGActorAttackComponent>();

            com = (new TRPGActorAttackComponent()
            {
                m_SearchRange = m_SearchRange,
                m_ConsumeAP = m_DefaultConsumeAP,

                m_Targets = new FixedList512Bytes<EntityID>()
            });
        }
        protected override void OnReserve()
        {
            base.OnReserve();

            m_TempGetRange.Clear();
        }
        protected override void OnDestroy()
        {
            m_TempGetRange.Dispose();
        }

        public FixedList512Bytes<EntityID> GetTargetsInRange()
        {
            int
                weaponRange = Mathf.RoundToInt(Parent.GetComponent<ActorWeaponComponent>().SelectedWeapon.GetObject().Range),
                searchRange = Parent.GetComponent<TRPGActorAttackComponent>().m_SearchRange;

            return GetTargetsWithin(math.max(weaponRange, searchRange));
        }
        public FixedList512Bytes<EntityID> GetTargetsWithin(in int range)
        {
            if (!Parent.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) doesn\'t have any {nameof(GridSizeComponent)}.");

                return new FixedList512Bytes<EntityID>();
            }

            GridSizeComponent gridSize = Parent.GetComponent<GridSizeComponent>();
            gridSize.GetRange(ref m_TempGetRange, in range);

            ref TRPGActorAttackComponent att = ref Parent.GetComponent<TRPGActorAttackComponent>();
            att.m_Targets.Clear();

            for (int i = 0; i < m_TempGetRange.Length; i++)
            {
                if (PresentationSystem<DefaultPresentationGroup, GridSystem>.System.GetEntitiesAt(m_TempGetRange[i], out var iter))
                {
                    foreach (var target in iter)
                    {
                        if (Parent.Idx.Equals(target)) continue;
                        else if (!target.GetEntityData<IEntityData>().HasComponent<TurnPlayerComponent>())
                        {
                            continue;
                        }

                        att.m_Targets.Add(target);
                    }
                }
            }

            return att.m_Targets;
        }

        public void Attack(Entity<ActorEntity> target)
        {
            ActorAttackEvent ev = new ActorAttackEvent(target);
            ev.ScheduleEvent(Parent.As<IEntityData, ActorEntity>());
        }
        public void Attack(int index)
        {
            ref TRPGActorAttackComponent att = ref Parent.GetComponent<TRPGActorAttackComponent>();

            Attack(att.m_Targets[index].GetEntity<ActorEntity>());
        }
    }
}