using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using Unity.Collections;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorMoveComponent : IEntityComponent
    {
        internal EntityData<IEntityData> m_Parent;

        public FixedList32Bytes<int> GetMoveablePositions64()
        {
            var gridSystem = PresentationSystem<GridSystem>.System;
            if (!m_Parent.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({m_Parent.Name}) doesn\'t have any {nameof(TurnPlayerComponent)}.");
                return default(FixedList32Bytes<int>);
            }
            else if (!m_Parent.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({m_Parent.Name}) doesn\'t have any {nameof(GridSizeComponent)}.");
                return default(FixedList32Bytes<int>);
            }

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            var range = gridsize.GetRange64(turnPlayer.ActionPoint);

            FixedList32Bytes<int> indices = new FixedList32Bytes<int>();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.HasPath(range[i], turnPlayer.ActionPoint)) continue;

                indices.Add(range[i]);
            }

            return indices;
        }
    }
}