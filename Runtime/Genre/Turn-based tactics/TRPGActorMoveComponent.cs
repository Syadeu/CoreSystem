using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using Unity.Collections;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorMoveComponent : IEntityComponent
    {
        internal EntityData<IEntityData> m_Parent;

        private bool SafetyChecks()
        {
            if (!m_Parent.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({m_Parent.RawName}) doesn\'t have any {nameof(TurnPlayerComponent)}.");
                return false;
            }
            else if (!m_Parent.HasComponent<GridSizeComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({m_Parent.RawName}) doesn\'t have any {nameof(GridSizeComponent)}.");
                return false;
            }
            return true;
        }
        public FixedList64Bytes<int> GetMoveablePositions64()
        {
            if (!SafetyChecks()) return default(FixedList64Bytes<int>);

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            var range = gridsize.GetRange64(turnPlayer.ActionPoint);

            FixedList64Bytes<int> indices = new FixedList64Bytes<int>();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.HasPath(range[i], turnPlayer.ActionPoint)) continue;

                indices.Add(range[i]);
            }

            return indices;
        }
    }
}