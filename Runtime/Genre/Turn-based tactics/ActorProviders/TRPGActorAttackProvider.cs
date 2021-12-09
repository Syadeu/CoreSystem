using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
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
    public sealed class TRPGActorAttackProvider : ActorAttackProvider
    {
        [JsonProperty(Order = 1, PropertyName = "SearchRange")] private int m_SearchRange = 3;

        [JsonIgnore] private NativeList<int> m_TempGetRange;

        protected override void OnCreated()
        {
            m_TempGetRange = new NativeList<int>(512, Allocator.Persistent);
        }
        protected override void OnInitialize(ref ActorAttackComponent component)
        {
            Parent.AddComponent<TRPGActorAttackComponent>();

            ref var com = ref Parent.GetComponent<TRPGActorAttackComponent>();

            com = (new TRPGActorAttackComponent()
            {
                m_SearchRange = m_SearchRange
            });
        }
        protected override void OnReserve(ref ActorAttackComponent component)
        {
            Parent.RemoveComponent<TRPGActorAttackComponent>();

            m_TempGetRange.Clear();
        }
        protected override void OnDestroy()
        {
            m_TempGetRange.Dispose();
        }

        public FixedList512Bytes<InstanceID> GetTargetsInRange()
        {
            int
                weaponRange = Mathf.RoundToInt(Parent.GetComponent<ActorWeaponComponent>().SelectedWeapon.GetObject().Range),
                searchRange = Parent.GetComponent<TRPGActorAttackComponent>().m_SearchRange;

            return GetTargetsWithin(math.max(weaponRange, searchRange));
        }
        public FixedList512Bytes<InstanceID> GetTargetsWithin(in int range, bool sort = true)
        {
            if (!Parent.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) doesn\'t have any {nameof(GridSizeComponent)}.");

                return new FixedList512Bytes<InstanceID>();
            }

            GridSizeComponent gridSize = Parent.GetComponent<GridSizeComponent>();
            gridSize.GetRange(ref m_TempGetRange, in range);

            ref TRPGActorAttackComponent att = ref Parent.GetComponent<TRPGActorAttackComponent>();
            
            FixedList512Bytes<InstanceID> list = new FixedList512Bytes<InstanceID>();
            for (int i = 0; i < m_TempGetRange.Length; i++)
            {
                if (PresentationSystem<DefaultPresentationGroup, GridSystem>.System.GetEntitiesAt(m_TempGetRange[i], out var iter))
                {
                    foreach (var item in iter)
                    {
                        if (item.Equals(Parent.Idx) || !item.IsActorEntity()) continue;
                        else if (!item.IsEnemy(Parent.Idx)) continue;
                        // TODO : 임시코드
                        else if (item.GetEntity<IEntity>().GetAttribute<ActorStatAttribute>().HP <= 0) continue;

                        list.Add(item);
                    }
                }
            }
            
            if (sort)
            {
                IOrderedEnumerable<InstanceID> sorted = list.ToArray().OrderBy(Order, new Comparer(gridSize.IndexToPosition(gridSize.positions[0].index)));
                
                att.InitializeTargets(sorted.ToFixedList512());
            }
            else
            {
                att.InitializeTargets(list);
            }

            return att.GetTargets();
        }
        private static ITransform Order(InstanceID id)
        {
            return id.GetEntity<IEntity>().transform;
        }
        private struct Comparer : IComparer<ITransform>
        {
            public float3 myPos;

            public Comparer(float3 center)
            {
                myPos = center;
            }
            public int Compare(ITransform x, ITransform y)
            {
                float3
                    tempX = x.position - myPos,
                    tempY = y.position - myPos;
                float
                    xMag = math.dot(tempX, tempX),
                    yMag = math.dot(tempY, tempY);

                if (xMag < yMag) return -1;
                else if (xMag == yMag) return 0;
                return 1;
            }
        }

        public void Attack()
        {
            ref TRPGActorAttackComponent attackComponent = ref Parent.GetComponent<TRPGActorAttackComponent>();

            ActorAttackEvent ev = new ActorAttackEvent(attackComponent.GetTarget().GetEntity<IEntity>());
            ev.ScheduleEvent(Parent.As<IEntityData, ActorEntity>());
        }
        public void Attack(Entity<ActorEntity> target)
        {
            ActorAttackEvent ev = new ActorAttackEvent(target);
            ev.ScheduleEvent(Parent.As<IEntityData, ActorEntity>());
        }
        public void Attack(int index)
        {
            ref TRPGActorAttackComponent att = ref Parent.GetComponent<TRPGActorAttackComponent>();

            Attack(att.GetTargets()[index].GetEntity<ActorEntity>());
        }
    }
}