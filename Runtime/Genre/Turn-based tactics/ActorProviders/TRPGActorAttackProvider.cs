﻿using Newtonsoft.Json;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGActorAttackProvider : ActorAttackProvider
    {
        [JsonProperty(Order = 0, PropertyName = "AttackRange")] private int m_AttackRange;
        [JsonProperty(Order = 1, PropertyName = "DefaultConsumeAP")] private int m_DefaultConsumeAP = 1;

        [JsonIgnore] private NativeList<int> m_TempGetRange;
        [JsonIgnore] public int AttackRange => m_AttackRange;
        /// <summary>
        /// 마지막으로 찾은 타겟의 리스트를 반환합니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="GetTargetsInRange"/>를 호출하면 여기에 담깁니다.
        /// </remarks>
        [JsonIgnore] public IReadOnlyList<Entity<IEntity>> Targets { get; internal set; }

        [JsonIgnore] private GridSystem m_GridSystem;

        protected override void OnCreated()
        {
            m_GridSystem = PresentationSystem<GridSystem>.System;
            m_TempGetRange = new NativeList<int>(512, Allocator.Persistent);
        }
        protected override void OnDestroy()
        {
            m_TempGetRange.Dispose();
        }

        public IReadOnlyList<Entity<IEntity>> GetTargetsInRange()
            => GetTargetsWithin(in m_AttackRange);
        public IReadOnlyList<Entity<IEntity>> GetTargetsWithin(in int range)
        {
            //GridSizeAttribute gridSize = Parent.GetAttribute<GridSizeAttribute>();
            if (!Parent.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) doesn\'t have any {nameof(GridSizeComponent)}.");

                return Array.Empty<Entity<IEntity>>();
            }

            List<Entity<IEntity>> entities = new List<Entity<IEntity>>();
            for (int i = 0; i < m_TempGetRange.Length; i++)
            {
                //if (component.positions.Contains(indices[i])) continue;
                //$"{m_TempGetRange[i]}".ToLog();
                IReadOnlyList<Entity<IEntity>> targets = m_GridSystem.GetEntitiesAt(m_TempGetRange[i]);
                for (int j = 0; j < targets.Count; j++)
                {
                    if (targets[j].Idx.Equals(Parent.Idx))
                    {
                        continue;
                    }

                    entities.Add(targets[j]);
                }

                //entities.AddRange(m_GridSystem.GetEntitiesAt(indices[i]));
            }

            Targets = entities;
            return entities;
        }
    }
}