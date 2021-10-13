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

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("ActorProvider: TRPG Attack Provider")]
    public sealed class TRPGActorAttackProvider : ActorAttackProvider,
        INotifyComponent<TRPGActorAttackComponent>
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
            m_GridSystem = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;
            m_TempGetRange = new NativeList<int>(512, Allocator.Persistent);
        }
        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            entity.AddComponent(new TRPGActorAttackComponent()
            {
                m_HasTarget = false,

                m_AttackRange = m_AttackRange,
                m_ConsumeAP = m_DefaultConsumeAP
            });
        }
        protected override void OnDestroy()
        {
            Targets = null;
            m_TempGetRange.Dispose();

            m_GridSystem = null;
        }

        public IReadOnlyList<Entity<IEntity>> GetTargetsInRange()
            => GetTargetsWithin(in Parent.GetComponent<TRPGActorAttackComponent>().m_AttackRange);
        public IReadOnlyList<Entity<IEntity>> GetTargetsWithin(in int range)
        {
            if (!Parent.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) doesn\'t have any {nameof(GridSizeComponent)}.");

                return Array.Empty<Entity<IEntity>>();
            }

            GridSizeComponent gridSize = Parent.GetComponent<GridSizeComponent>();
            gridSize.GetRange(ref m_TempGetRange, in range);

            ref TRPGActorAttackComponent att = ref Parent.GetComponent<TRPGActorAttackComponent>();
            if (att.m_HasTarget)
            {
                //att.m_CurrentTargets.Dispose();
            }

            List<Entity<IEntity>> entities = new List<Entity<IEntity>>();
            for (int i = 0; i < m_TempGetRange.Length; i++)
            {
                if (m_GridSystem.GetEntitiesAt(m_TempGetRange[i], out var iter))
                {
                    foreach (var target in iter)
                    {
                        entities.Add(target.GetEntity<IEntity>());
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
            
            if (entities.Count > 0)
            {
                //att.m_CurrentTargets = entities.GetEnumerator();
                att.m_TargetCount = entities.Count;
            }
            else
            {
                att.m_HasTarget = false;
                att.m_TargetCount = 0;
            }

            Targets = entities;
            return entities;
        }
    }
}