using Newtonsoft.Json;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGActorAttackProvider : ActorAttackProvider
    {
        [JsonProperty(Order = 0, PropertyName = "AttackRange")] private int m_AttackRange;

        [JsonIgnore] public int AttackRange => m_AttackRange;
        /// <summary>
        /// 마지막으로 찾은 타겟의 리스트를 반환합니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="GetTargetsInRange"/>를 호출하면 여기에 담깁니다.
        /// </remarks>
        [JsonIgnore] public IReadOnlyList<Entity<IEntity>> Targets { get; internal set; }

        public IReadOnlyList<Entity<IEntity>> GetTargetsInRange()
            => GetTargetsWithin(in m_AttackRange);
        public IReadOnlyList<Entity<IEntity>> GetTargetsWithin(in int range)
        {
            GridSizeAttribute gridSize = Parent.GetAttribute<GridSizeAttribute>();
            if (gridSize == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Parent.Name}) doesn\'t have any {nameof(GridSizeAttribute)}.");

                return Array.Empty<Entity<IEntity>>();
            }

            int[] indices = gridSize.GetRange(range);
            List<Entity<IEntity>> entities = new List<Entity<IEntity>>();
            for (int i = 0; i < indices.Length; i++)
            {
                if (gridSize.CurrentGridIndices.Contains(indices[i])) continue;

                entities.AddRange(gridSize.GetEntitiesAt(indices[i]));
            }

            Targets = entities;
            return entities;
        }
    }
}