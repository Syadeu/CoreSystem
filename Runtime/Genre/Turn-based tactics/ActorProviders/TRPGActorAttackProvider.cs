﻿using Newtonsoft.Json;
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
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("ActorProvider: TRPG Attack Provider")]
    public sealed class TRPGActorAttackProvider : ActorAttackProvider,
        INotifyComponent<TRPGActorAttackComponent>
    {
        [JsonProperty(Order = 0, PropertyName = "AttackRange")] private int m_AttackRange = 1;
        [Tooltip("GridDetector 가 있으면 이 값은 무시됩니다.")]
        [JsonProperty(Order = 1, PropertyName = "SearchRange")] private int m_SearchRange = 3;
        [JsonProperty(Order = 2, PropertyName = "DefaultConsumeAP")] private int m_DefaultConsumeAP = 1;

        [JsonIgnore] private NativeList<int> m_TempGetRange;
        //[JsonIgnore] public int AttackRange => m_AttackRange;
        ///// <summary>
        ///// 마지막으로 찾은 타겟의 리스트를 반환합니다.
        ///// </summary>
        ///// <remarks>
        ///// <seealso cref="GetTargetsInRange"/>를 호출하면 여기에 담깁니다.
        ///// </remarks>
        //[JsonIgnore] public IReadOnlyList<Entity<IEntity>> Targets { get; internal set; }

        [JsonIgnore] private GridSystem m_GridSystem;

        protected override void OnCreated()
        {
            m_GridSystem = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;
            m_TempGetRange = new NativeList<int>(512, Allocator.Persistent);
        }
        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            entity.AddComponent<TRPGActorAttackComponent>();
            ref var com = ref entity.GetComponent<TRPGActorAttackComponent>();

            com = (new TRPGActorAttackComponent()
            {
                //m_HasTarget = false,

                m_AttackRange = m_AttackRange,
                m_SearchRange = m_SearchRange,
                m_ConsumeAP = m_DefaultConsumeAP,

                m_Targets = new FixedList512Bytes<EntityID>()
            });
        }
        protected override void OnDestroy()
        {
            //Targets = null;
            m_TempGetRange.Dispose();

            m_GridSystem = null;
        }

        public FixedList512Bytes<EntityID> GetTargetsInRange()
        {
            if (Parent.HasComponent<GridDetectorComponent>())
            {
                var temp = Parent.GetComponentReadOnly<GridDetectorComponent>();
                FixedList512Bytes<EntityID> list = new FixedList512Bytes<EntityID>();
                for (int i = 0; i < temp.Detected.Length; i++)
                {
                    list.Add(temp.Detected[i].GetEntityID());
                }

                ref TRPGActorAttackComponent att = ref Parent.GetComponent<TRPGActorAttackComponent>();
                att.m_Targets = list;

                return list;
            }
            else
            {
                return GetTargetsWithin(in Parent.GetComponent<TRPGActorAttackComponent>().m_SearchRange);
            }
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
            //if (att.m_HasTarget)
            //{
            //    //att.m_CurrentTargets.Dispose();
            //}
            att.m_Targets.Clear();

            //List<Entity<IEntity>> entities = new List<Entity<IEntity>>();
            for (int i = 0; i < m_TempGetRange.Length; i++)
            {
                if (m_GridSystem.GetEntitiesAt(m_TempGetRange[i], out var iter))
                {
                    foreach (var target in iter)
                    {
                        if (Parent.Idx.Equals(target)) continue;

                        att.m_Targets.Add(target);
                    }
                }

                //IReadOnlyList<Entity<IEntity>> targets = m_GridSystem.GetEntitiesAt(m_TempGetRange[i]);
                //for (int j = 0; j < targets.Count; j++)
                //{
                //    if (targets[j].Idx.Equals(Parent.Idx))
                //    {
                //        continue;
                //    }

                //    entities.Add(targets[j]);
                //}
            }

            //if (entities.Count > 0)
            //{
            //    //att.m_CurrentTargets = entities.GetEnumerator();
            //    att.m_TargetCount = entities.Count;
            //}
            //else
            //{
            //    att.m_HasTarget = false;
            //    att.m_TargetCount = 0;
            //}

            //Targets = entities;
            return att.m_Targets;
        }
    }
}