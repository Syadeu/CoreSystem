using Syadeu.Collections;
using Syadeu.Presentation.Actor;
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

        public void GetMoveablePositions(ref NativeList<GridPosition> gridPositions)
        {
            if (!SafetyChecks()) return;

            var grid = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;
            var turnPlayer = m_Parent.GetComponent<TurnPlayerComponent>();
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();

            NativeList<int> range = new NativeList<int>(512, Allocator.Temp);
            gridsize.GetRange(ref range, turnPlayer.ActionPoint);
            
            gridPositions.Clear();
            for (int i = 0; i < range.Length; i++)
            {
                if (!gridsize.HasPath(range[i], out int pathCount) ||
                    pathCount > turnPlayer.ActionPoint) continue;

                //if (grid.HasEntityAt(range[i])) continue;

                gridPositions.Add(gridsize.GetGridPosition(range[i]));
            }
            range.Dispose();
        }

        #endregion

        public bool GetPath(in GridPosition position, ref GridPath64 path)
        {
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            return gridsize.GetPath64(in position.index, ref path, 32);
        }
        public float3 TileToPosition(in GridTile tile)
        {
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            return gridsize.IndexToPosition(tile.index);
        }

        public void MoveTo(in GridPath64 path, in ActorMoveEvent ev)
        {
            NavMeshSystem navMesh = PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System;
            navMesh.MoveTo(m_Parent.As<IEntityData, IEntity>(), path, ev);
        }
        public void MoveTo(in float3 point, in ActorMoveEvent ev)
        {
            NavMeshSystem navMesh = PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System;
            navMesh.MoveTo(m_Parent.As<IEntityData, IEntity>(), point, ev);
        }
        public void MoveTo<TPredicate>(in float3 point, in ActorMoveEvent<TPredicate> ev)
            where TPredicate : unmanaged, IExecutable<Entity<ActorEntity>>
        {
            NavMeshSystem navMesh = PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System;
            navMesh.MoveTo(m_Parent.As<IEntityData, IEntity>(), point, ev);
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
            ref NativeList<UnityEngine.Vector3> vertices, float heightOffset = .25f)
        {
            var gridsize = m_Parent.GetComponent<GridSizeComponent>();
            float cellsize = gridsize.CellSize * .5f;

            float3
                upleft = new float3(-cellsize, heightOffset, cellsize),
                upright = new float3(cellsize, heightOffset, cellsize),
                downleft = new float3(-cellsize, heightOffset, -cellsize),
                downright = new float3(cellsize, heightOffset, -cellsize);

            vertices.Clear();

            if (moveables.Length == 0)
            {
                vertices.Add(gridsize.IndexToPosition(gridsize.positions[0].index) + upright);
                vertices.Add(gridsize.IndexToPosition(gridsize.positions[0].index) + downright);
                vertices.Add(gridsize.IndexToPosition(gridsize.positions[0].index) + downleft);
                vertices.Add(gridsize.IndexToPosition(gridsize.positions[0].index) + upleft);

                return;
            }
            else if (moveables.Length == 1)
            {
                vertices.Add(gridsize.IndexToPosition(moveables[0].index) + upright);
                vertices.Add(gridsize.IndexToPosition(moveables[0].index) + downright);
                vertices.Add(gridsize.IndexToPosition(moveables[0].index) + downleft);
                vertices.Add(gridsize.IndexToPosition(moveables[0].index) + upleft);

                return;
            }

            List<float3x2> temp = new List<float3x2>();
            for (int i = 0; i < moveables.Length; i++)
            {
                if (gridsize.HasDirection(moveables[i], Direction.Right, out GridPosition target) &&
                    !moveables.Contains(target))
                {
                    temp.Add(new float3x2(
                        gridsize.IndexToPosition(moveables[i].index) + upright,
                        gridsize.IndexToPosition(moveables[i].index) + downright
                        ));
                }

                // Down
                if (gridsize.HasDirection(moveables[i], Direction.Up, out target) &&
                    !moveables.Contains(target))
                {
                    temp.Add(new float3x2(
                        gridsize.IndexToPosition(moveables[i].index) + downright,
                        gridsize.IndexToPosition(moveables[i].index) + downleft
                        ));
                }

                if (gridsize.HasDirection(moveables[i], Direction.Left, out target) &&
                    !moveables.Contains(target))
                {
                    temp.Add(new float3x2(
                        gridsize.IndexToPosition(moveables[i].index) + downleft,
                        gridsize.IndexToPosition(moveables[i].index) + upleft
                        ));
                }

                // Up
                if (gridsize.HasDirection(moveables[i], Direction.Down, out target) &&
                    !moveables.Contains(target))
                {
                    temp.Add(new float3x2(
                        gridsize.IndexToPosition(moveables[i].index) + upleft,
                        gridsize.IndexToPosition(moveables[i].index) + upright
                        ));
                }
            }

            float3x2 current = temp[temp.Count - 1];
            temp.RemoveAt(temp.Count - 1);

            for (int i = temp.Count - 1; i >= 0; i--)
            {
                vertices.Add(current.c0);
                vertices.Add(current.c1);

                current = Find(temp, current.c1);
            }

            float3x2 Find(List<float3x2> list, float3 next)
            {
                float3x2 target = 0;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].c0.Equals(next) || list[i].c1.Equals(next))
                    {
                        target = list[i];
                        list.RemoveAt(i);
                        break;
                    }
                }
                return target;
            }
        }
    }
}