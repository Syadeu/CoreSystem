using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.TurnTable
{
    [BurstCompatible]
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
        public FixedList64Bytes<GridPosition> GetMoveablePositions64()
        {
            if (!SafetyChecks()) return default(FixedList64Bytes<GridPosition>);

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            var range = gridsize.GetRange64(turnPlayer.ActionPoint);

            FixedList64Bytes<GridPosition> indices = new FixedList64Bytes<GridPosition>();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.HasPath(range[i], turnPlayer.ActionPoint)) continue;

                indices.Add(gridsize.GetGridPosition(range[i]));
            }

            return indices;
        }
        public FixedList64Bytes<GridPath64> GetMoveablePath64()
        {
            if (!SafetyChecks()) return default(FixedList64Bytes<GridPath64>);

            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            var range = gridsize.GetRange64(turnPlayer.ActionPoint);

            FixedList64Bytes<GridPath64> indices = new FixedList64Bytes<GridPath64>();
            GridPath64 path = GridPath64.Create();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.GetPath64(range[i], ref path, turnPlayer.ActionPoint)) continue;

                indices.Add(path);

                if (i + 1 < range.Length) path = GridPath64.Create();
            }

            return indices;
        }
        public void CalculateMoveableOutline(FixedList64Bytes<GridPosition> moveables, 
            ref FixedList64Bytes<GridPosition> outlines, ref FixedList128Bytes<float3> vertices)
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

            int 
                row = lastRow.location.y - firstRow.location.y,
                column = lastRow.location.x - firstRow.location.x;

            outlines.Clear();
            vertices.Clear();

            for (int yy = 0; yy < row; yy++)
            {
                for (int xx = 0; xx < column; xx++)
                {
                    if (yy != 0 || yy != row - 1)
                    {
                        if (xx != 0 || xx != column - 1) continue;
                    }

                    GridPosition currentpos = gridsize.GetGridPosition(firstRow.location + new int2(xx, yy));

                    if (moveables.Contains(currentpos))
                    {
                        outlines.Add(currentpos);

                        if (yy == 0)
                        {
                            if (xx == 0) vertices.Add(gridsize.IndexToPosition(currentpos.index) + downleft);
                            else if (xx == column - 1) vertices.Add(gridsize.IndexToPosition(currentpos.index) + downright);

                            vertices.Add(gridsize.IndexToPosition(currentpos.index) + upleft);
                            vertices.Add(gridsize.IndexToPosition(currentpos.index) + upright);
                        }
                        else if (yy == row - 1)
                        {
                            if (xx == 0) vertices.Add(gridsize.IndexToPosition(currentpos.index) + upleft);
                            else if (xx == column - 1) vertices.Add(gridsize.IndexToPosition(currentpos.index) + upright);

                            vertices.Add(gridsize.IndexToPosition(currentpos.index) + downleft);
                            vertices.Add(gridsize.IndexToPosition(currentpos.index) + downright);
                        }
                        else
                        {
                            if (xx == 0)
                            {
                                vertices.Add(gridsize.IndexToPosition(currentpos.index) + upleft);
                                vertices.Add(gridsize.IndexToPosition(currentpos.index) + downleft);
                            }
                            else
                            {
                                vertices.Add(gridsize.IndexToPosition(currentpos.index) + upright);
                                vertices.Add(gridsize.IndexToPosition(currentpos.index) + downright);
                            }
                        }
                        continue;
                    }

                    // 첫번째 줄일때
                    if (yy == 0)
                    {
                        // 줄을 아래로 밀어서 존재하는 타일을 찾는다.
                        bool found = false;
                        for (int i = 0; i < row; i++)
                        {
                            currentpos = gridsize.GetGridPosition(firstRow.location + new int2(xx, yy + i));
                            if (!moveables.Contains(currentpos)) continue;

                            found = true;
                            break;
                        }

                        if (!found) throw new System.Exception("1");

                        outlines.Add(currentpos);

                        vertices.Add(gridsize.IndexToPosition(currentpos.index) + upleft);
                        vertices.Add(gridsize.IndexToPosition(currentpos.index) + upright);
                    }
                    // 마지막 줄일때
                    else if (yy == row - 1)
                    {
                        // 줄을 위로 밀어서 존재하는 타일을 찾는다.
                        bool found = false;
                        for (int i = 0; i < row; i++)
                        {
                            currentpos = gridsize.GetGridPosition(firstRow.location + new int2(xx, yy - i));
                            if (!moveables.Contains(currentpos)) continue;

                            found = true;
                            break;
                        }

                        if (!found) throw new System.Exception("2");

                        outlines.Add(currentpos);

                        vertices.Add(gridsize.IndexToPosition(currentpos.index) + downleft);
                        vertices.Add(gridsize.IndexToPosition(currentpos.index) + downright);
                    }
                    // 그 사이 줄일때
                    else
                    {
                        // 첫번째 행이면
                        if (xx == 0)
                        {
                            // 행을 오른쪽으로 밀어서 존재하는 타일을 찾는다.
                            bool found = false;
                            for (int i = 0; i < column; i++)
                            {
                                currentpos = gridsize.GetGridPosition(firstRow.location + new int2(xx + i, yy));
                                if (!moveables.Contains(currentpos)) continue;

                                found = true;
                                break;
                            }

                            if (!found) throw new System.Exception("3");

                            outlines.Add(currentpos);

                            vertices.Add(gridsize.IndexToPosition(currentpos.index) + upleft);
                            vertices.Add(gridsize.IndexToPosition(currentpos.index) + downleft);
                        }
                        // 마지막 행이면
                        else
                        {
                            // 행을 왼쪽으로 밀어서 존재하는 타일을 찾는다.
                            bool found = false;
                            for (int i = 0; i < column; i++)
                            {
                                currentpos = gridsize.GetGridPosition(firstRow.location + new int2(xx - i, yy));
                                if (!moveables.Contains(currentpos)) continue;

                                found = true;
                                break;
                            }

                            if (!found) throw new System.Exception("4");

                            outlines.Add(currentpos);

                            vertices.Add(gridsize.IndexToPosition(currentpos.index) + upright);
                            vertices.Add(gridsize.IndexToPosition(currentpos.index) + downright);
                        }
                    }
                }
            }
            //
        }
    }
}