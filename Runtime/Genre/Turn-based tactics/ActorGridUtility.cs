using Syadeu.Presentation.Map;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public static class ActorGridUtility
    {
        public static bool IsMoveable(in GridSizeAttribute gridSize, in int to, in int currentAP, out int requireAP)
        {
            // Early out
            if (PresentationSystem<GridSystem>.System.GetEntitiesAt(to).Count > 0)
            {
                requireAP = 9999;
                return false;
            }

            GridSizeComponent component = gridSize.Parent.GetComponent<GridSizeComponent>();

            int2
                current = component.positions[0].location,
                targetReletive = PresentationSystem<GridSystem>.System.IndexToLocation(to) - current,
                adaptive = new int2(targetReletive.x < 0 ? -1 : 1, targetReletive.y < 0 ? -1 : 1),
                targetReletiveAbs = math.abs(targetReletive);

            //for (int yy = 1; yy < targetReletiveAbs.y + 1; yy++)
            //{
            //    for (int xx = 1; xx < targetReletiveAbs.x + 1; xx++)
            //    {
            //        int2 target = current + (new int2(xx, yy) * adaptive);

            //    }
            //}

            //int tempAP = 0;
            //while (tempAP <= currentAP)
            //{

            //}

            targetReletiveAbs.x = targetReletiveAbs.x == 0 ? 1 : targetReletiveAbs.x;
            targetReletiveAbs.y = targetReletiveAbs.y == 0 ? 1 : targetReletiveAbs.y;

            //float sqr = math.mul(targetReletive, targetReletive);

            //float ap = sqr / gridSize.CellSize;
            //requireAP = math.max(1, Mathf.FloorToInt(ap));

            requireAP = targetReletiveAbs.x + targetReletiveAbs.y - 1;

            // TODO : 여기 중간에 구멍뚤려도 갈수있다 판정하니 추가할 것
            if (requireAP <= currentAP) return true;
            return false;
        }
        private static int2 FindNext(in int2 current, in int2 target)
        {
            int2
                closest = current - new int2(-1, 0),
                closestRel = closest - target,
                closestSqr = math.mul(closestRel, closestRel);

            int2 
                xMax = current + new int2(1, 0),
                xMaxRel = xMax - target,
                xMaxSqr = math.mul(xMaxRel, xMaxRel);

            if ((xMaxSqr.x + xMaxSqr.y) < (closestSqr.x + closestSqr.y))
            {
                closest = xMax;
                closestRel = xMaxRel;
                closestSqr = xMaxSqr;
            }

            int2
                yMin = current + new int2(0, -1),
                yMinRel = yMin - target,
                yMinSqr = math.mul(yMinRel, yMinRel);

            if ((yMinSqr.x + yMinSqr.y) < (closestSqr.x + closestSqr.y))
            {
                closest = yMin;
                closestRel = yMinRel;
                closestSqr = yMinSqr;
            }

            int2
                yMax = current + new int2(0, 1),
                yMaxRel = yMax - target,
                yMaxSqr = math.mul(yMaxRel, yMaxRel);

            if ((yMaxSqr.x + yMaxSqr.y) < (closestSqr.x + closestSqr.y))
            {
                closest = yMax;
                closestRel = yMaxRel;
                closestSqr = yMaxSqr;
            }

            return closest;
        }
    }
}
