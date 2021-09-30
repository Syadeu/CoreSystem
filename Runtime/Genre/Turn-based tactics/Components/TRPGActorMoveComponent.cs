using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGActorMoveComponent : IEntityComponent
    {
        internal EntityData<IEntityData> m_Parent;

        [BurstDiscard]
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

        #region Get Moveable GridPositions

        public INativeList<GridPosition> GetMoveablePositions()
        {
            if (!SafetyChecks()) return default(FixedList32Bytes<GridPosition>);

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            int maxValue = turnPlayer.ActionPoint * turnPlayer.ActionPoint;

            if (maxValue < 8) return GetMoveablePositions8();
            else if (maxValue < 16) return GetMoveablePositions16();
            else if (maxValue < 32) return GetMoveablePositions32();

            throw new NotImplementedException();
        }
        public void GetMoveablePositions(ref NativeList<GridPosition> gridPositions)
        {
            if (!SafetyChecks()) return;

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();

            NativeList<int> range = new NativeList<int>(512, Allocator.Temp);
            gridsize.GetRange(ref range, turnPlayer.ActionPoint);

            gridPositions.Clear();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.HasPath(range[i], turnPlayer.ActionPoint)) continue;

                gridPositions.Add(gridsize.GetGridPosition(range[i]));
            }
            range.Dispose();
        }
        public FixedList32Bytes<GridPosition> GetMoveablePositions8()
        {
            if (!SafetyChecks()) return default(FixedList64Bytes<GridPosition>);

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            var range = gridsize.GetRange8(turnPlayer.ActionPoint);

            FixedList32Bytes<GridPosition> indices = new FixedList32Bytes<GridPosition>();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.HasPath(range[i], turnPlayer.ActionPoint)) continue;

                indices.Add(gridsize.GetGridPosition(range[i]));
            }

            return indices;
        }
        public FixedList64Bytes<GridPosition> GetMoveablePositions16()
        {
            if (!SafetyChecks()) return default(FixedList64Bytes<GridPosition>);

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            var range = gridsize.GetRange16(turnPlayer.ActionPoint);

            FixedList64Bytes<GridPosition> indices = new FixedList64Bytes<GridPosition>();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.HasPath(range[i], turnPlayer.ActionPoint)) continue;

                indices.Add(gridsize.GetGridPosition(range[i]));
            }

            return indices;
        }
        public FixedList128Bytes<GridPosition> GetMoveablePositions32()
        {
            if (!SafetyChecks()) return default(FixedList64Bytes<GridPosition>);

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            var range = gridsize.GetRange32(turnPlayer.ActionPoint);

            FixedList128Bytes<GridPosition> indices = new FixedList128Bytes<GridPosition>();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.HasPath(range[i], turnPlayer.ActionPoint)) continue;

                indices.Add(gridsize.GetGridPosition(range[i]));
            }

            return indices;
        }
        public FixedList4096Bytes<GridPosition> GetMoveablePositions340()
        {
            if (!SafetyChecks()) return default(FixedList4096Bytes<GridPosition>);

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            var range = gridsize.GetRange1024(turnPlayer.ActionPoint);

            FixedList4096Bytes<GridPosition> indices = new FixedList4096Bytes<GridPosition>();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.HasPath(range[i], turnPlayer.ActionPoint)) continue;

                indices.Add(gridsize.GetGridPosition(range[i]));
            }

            return indices;
        }

        #endregion

        public void GetMoveablePath64(List<GridPath32> indices)
        {
            if (!SafetyChecks()) return;

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            var range = gridsize.GetRange16(turnPlayer.ActionPoint);

            indices.Clear();
            //FixedList64Bytes<GridPath32> indices = new FixedList64Bytes<GridPath32>();
            GridPath32 path = GridPath32.Create();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.GetPath64(range[i], ref path, turnPlayer.ActionPoint)) continue;

                indices.Add(path);

                if (i + 1 < range.Length) path = GridPath32.Create();
            }

            //return indices;
        }

        public void CalculateMoveableOutline(NativeArray<GridPosition> moveables, 
            ref NativeList<UnityEngine.Vector3> outlines, float heightOffset = .25f)
        {
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            GridPosition
                firstRow = moveables[0],
                lastRow = moveables[moveables.Length - 1];

            float3 offset = new float3(0, heightOffset, 0);

            outlines.Clear();

            outlines.Add(gridsize.IndexToPosition(firstRow.index) + offset);
            int count = moveables.Length - 1;
            for (int i = 1; i < count; i++)
            {
                if (moveables[i - 1].location.y != moveables[i].location.y)
                {
                    outlines.Add(gridsize.IndexToPosition(moveables[i].index) + offset);
                }
            }
            outlines.Add(gridsize.IndexToPosition(lastRow.index) + offset);
            for (int i = count - 1; i >= 1; i--)
            {
                if (moveables[i + 1].location.y != moveables[i].location.y)
                {
                    outlines.Add(gridsize.IndexToPosition(moveables[i].index) + offset);
                }
            }
        }
        public void CalculateMoveableOutlineVertices(NativeArray<GridPosition> moveables,
            ref NativeList<float3> vertices)
        {
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            float cellsize = gridsize.CellSize * .5f;

            float3
                upleft = new float3(-cellsize, 0, cellsize),
                upright = new float3(cellsize, 0, cellsize),
                downleft = new float3(-cellsize, 0, -cellsize),
                downright = new float3(cellsize, 0, cellsize);

            GridPosition
                firstRow = moveables[0],
                lastRow = moveables[moveables.Length - 1];

            vertices.Clear();

            // first cell
            {
                vertices.Add(gridsize.IndexToPosition(firstRow.index) + downright);
                vertices.Add(gridsize.IndexToPosition(firstRow.index) + downleft);
            }

            int count = moveables.Length - 1;
            for (int i = 1; i < count; i++)
            {
                if (moveables[i - 1].location.y != moveables[i].location.y)
                {
                    vertices.Add(gridsize.IndexToPosition(moveables[i].index) + downright);
                    vertices.Add(gridsize.IndexToPosition(moveables[i].index) + downleft);
                }
            }
            
            // last cell
            {
                vertices.Add(gridsize.IndexToPosition(lastRow.index) + upleft);
                vertices.Add(gridsize.IndexToPosition(lastRow.index) + upright);
            }

            for (int i = count - 1; i >= 1; i--)
            {
                if (moveables[i + 1].location.y != moveables[i].location.y)
                {
                    vertices.Add(gridsize.IndexToPosition(moveables[i].index) + upleft);
                    vertices.Add(gridsize.IndexToPosition(moveables[i].index) + upright);
                }
            }
        }
    }
}