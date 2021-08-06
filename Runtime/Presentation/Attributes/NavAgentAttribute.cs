using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Event;
using Syadeu.Presentation.Map;
using System;
using System.Collections;
//using Syadeu.ThreadSafe;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class NavAgentAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "AgentType")] public int m_AgentType = 0;
        [JsonProperty(Order = 1, PropertyName = "BaseOffset")] public float m_BaseOffset = 0;

        [Space, Header("Steering")]
        [JsonProperty(Order = 2, PropertyName = "Speed")] public float m_Speed = 3.5f;
        [JsonProperty(Order = 3, PropertyName = "AngularSpeed")] public float m_AngularSpeed = 120;
        [JsonProperty(Order = 4, PropertyName = "Acceleration")] public float m_Acceleration = 8;
        [JsonProperty(Order = 5, PropertyName = "StoppingDistance")] public float m_StoppingDistance = 0;

        [JsonIgnore] public NavMeshAgent NavMeshAgent { get; internal set; }
        [JsonIgnore] public bool IsMoving { get; internal set; }
        [JsonIgnore] public Vector3 Direction => NavMeshAgent.desiredVelocity;
        [JsonIgnore] private CoreRoutine Routine { get; set; }
        [JsonIgnore] public Vector3 PreviousTarget { get; set; }

        public void MoveTo(Vector3 point)
        {
            if (!NavMeshAgent.isOnNavMesh)
            {
                NavMeshAgent.enabled = false;
                NavMeshAgent.enabled = true;
            }

            PresentationSystem<EventSystem>.System
                .PostEvent(OnMoveStateChangedEvent.GetEvent(Parent, OnMoveStateChangedEvent.MoveState.AboutToMove));

            NavMeshAgent.ResetPath();
            NavMeshAgent.SetDestination(point);
            PreviousTarget = point;
            IsMoving = true;

            if (Routine.IsValid() && Routine.IsRunning)
            {
                CoreSystem.RemoveUnityUpdate(Routine);
            }
            Routine = CoreSystem.StartUnityUpdate(this, Updater());
        }
        private IEnumerator Updater()
        {
            Entity<IEntity> parent = Parent;
            ProxyTransform tr = parent.transform;

            if (!tr.hasProxy) yield break;

            while (NavMeshAgent.pathPending)
            {
                yield return null;
            }

            PresentationSystem<EventSystem>.System
                .PostEvent(OnMoveStateChangedEvent.GetEvent(parent, OnMoveStateChangedEvent.MoveState.OnMoving));

            while (NavMeshAgent.desiredVelocity.magnitude > 0 &&
                    NavMeshAgent.remainingDistance > .2f)
            {
                if (!tr.hasProxy) yield break;

                tr.Synchronize(ProxyTransform.SynchronizeOption.TR);
                PresentationSystem<EventSystem>.System
                .PostEvent(OnMoveStateChangedEvent.GetEvent(parent, OnMoveStateChangedEvent.MoveState.OnMoving));
                
                yield return null;
            }

            tr.position = PreviousTarget;
            if (NavMeshAgent.isOnNavMesh) NavMeshAgent.ResetPath();

            PresentationSystem<EventSystem>.System
            .PostEvent(OnMoveStateChangedEvent.GetEvent(parent, OnMoveStateChangedEvent.MoveState.Stopped));
            //if (Parent.GetAttribute<GridSizeAttribute>() != null)
            //{
            //    Parent.GetAttribute<GridSizeAttribute>().UpdateGridCell();
            //}

            IsMoving = false;
            PresentationSystem<EventSystem>.System
                .PostEvent(OnMoveStateChangedEvent.GetEvent(parent, OnMoveStateChangedEvent.MoveState.Idle));
        }
    }

    internal sealed class NavAgentProcessor : AttributeProcessor<NavAgentAttribute>, 
        IAttributeOnProxyCreatedSync, IAttributeOnProxyRemovedSync
    {
        public void OnProxyCreatedSync(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            NavAgentAttribute att = (NavAgentAttribute)attribute;

            att.NavMeshAgent = monoObj.GetComponent<NavMeshAgent>();
            if (att.NavMeshAgent == null) att.NavMeshAgent = monoObj.AddComponent<NavMeshAgent>();

            UpdateNavMeshAgent(att, att.NavMeshAgent);

            att.NavMeshAgent.enabled = true;
        }
        public void OnProxyRemovedSync(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            NavAgentAttribute att = (NavAgentAttribute)attribute;

            if (att.IsMoving)
            {
                ProxyTransform tr = entity.transform;

                NavMeshAgent agent = monoObj.GetComponent<NavMeshAgent>();
                if (agent.isOnNavMesh) agent.ResetPath();
                agent.enabled = false;

                tr.position = att.PreviousTarget;
                att.IsMoving = false;

                PresentationSystem<EventSystem>.System
                .PostEvent(OnMoveStateChangedEvent.GetEvent(entity, OnMoveStateChangedEvent.MoveState.Teleported));
            }

            PresentationSystem<EventSystem>.System
                .PostEvent(OnMoveStateChangedEvent.GetEvent(entity, OnMoveStateChangedEvent.MoveState.Idle));
        }

        private static void UpdateNavMeshAgent(NavAgentAttribute att, NavMeshAgent agent)
        {
            agent.agentTypeID = att.m_AgentType;
            agent.baseOffset = att.m_BaseOffset;

            agent.speed = att.m_Speed;
            agent.angularSpeed = att.m_AngularSpeed;
            agent.acceleration = att.m_Acceleration;
            agent.stoppingDistance = att.m_StoppingDistance;
        }
    }
    //internal sealed class NavMeshProcessor : AttributeProcessor<NavMeshAttribute>, IAttributeOnPresentation
    //{
    //    protected override void OnCreatedSync(NavMeshAttribute attribute, IEntity entity)
    //    {
    //        attribute.NavMeshQuery = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 256);
    //    }
    //    protected override void OnDestorySync(NavMeshAttribute attribute, IEntity entity)
    //    {
    //        attribute.NavMeshQuery.Dispose();
    //    }
    //    public void OnPresentation(AttributeBase attribute, IEntity entity)
    //    {
    //        NavMeshAttribute att = (NavMeshAttribute)attribute;
    //        DataTransform tr = entity.transform;

    //        if (tr.position.Equals(att.TargetPosition)) return;

    //        CalculatePath(att, tr.position, att.TargetPosition, Vector3.one * 1.5f, att.m_AgentType);

    //        if (att.Path == null) return;
    //        for (int i = 1; i < att.Path.Length; i++)
    //        {
    //            float sqr = (att.Path[i] - att.Path[i - 1]).SqrMagnitute;



    //            sqr = (att.Path[i] - tr.position).SqrMagnitute;
    //        }
    //    }

    //    private static void CalculatePath(NavMeshAttribute att, Vector3 start, Vector3 end, Vector3 extents, int agentType, int areaMask = -1)
    //    {
    //        NavMeshLocation from = att.NavMeshQuery.MapLocation(start, extents, agentType, areaMask);
    //        NavMeshLocation to = att.NavMeshQuery.MapLocation(end, extents, agentType, areaMask);

    //        //if (att.PathQueryStatus == PathQueryStatus.)

    //        PathQueryStatus status = att.NavMeshQuery.BeginFindPath(from, to, areaMask);
    //        if (status != PathQueryStatus.InProgress && status != PathQueryStatus.Success) return;

    //        status = att.NavMeshQuery.UpdateFindPath(1024, out _);
    //        if (status == PathQueryStatus.InProgress | status == (PathQueryStatus.InProgress | PathQueryStatus.OutOfNodes))
    //        {
    //            return;
    //        }

    //        if (status != PathQueryStatus.Success) return;

    //        status = att.NavMeshQuery.EndFindPath(out int pathSize);
    //        if (status != PathQueryStatus.Success) return;

    //        int maxPathSize = 1024;

    //        NativeArray<NavMeshLocation> navMeshLocations = new NativeArray<NavMeshLocation>(maxPathSize, Allocator.Temp);
    //        NativeArray<PolygonId> polygons = new NativeArray<PolygonId>(pathSize, Allocator.Temp);
    //        att.NavMeshQuery.GetPathResult(polygons);
    //        var straightPathFlags = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
    //        var vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
    //        var _cornerCount = 0;
    //        status = FindStraightPath(
    //            att.NavMeshQuery,
    //            start,
    //            end,
    //            polygons,
    //            pathSize,
    //            ref navMeshLocations,
    //            ref straightPathFlags,
    //            ref vertexSide,
    //            ref _cornerCount,
    //            maxPathSize
    //        );

    //        if (status != PathQueryStatus.Success)
    //        {
    //            navMeshLocations.Dispose();
    //            polygons.Dispose();
    //            straightPathFlags.Dispose();
    //            vertexSide.Dispose();
    //            return;
    //        }

    //        if (!att.Path.Length.Equals(_cornerCount))
    //        {
    //            att.Path = new TVector3[_cornerCount];
    //        }
    //        for (int i = 0; i < att.Path.Length; i++)
    //        {
    //            att.Path[i] = new TVector3(navMeshLocations[i].position);
    //        }

    //        navMeshLocations.Dispose();
    //        polygons.Dispose();
    //        straightPathFlags.Dispose();
    //        vertexSide.Dispose();
    //    }

    //    public static float Perp2D(Vector3 u, Vector3 v) => u.z * v.x - u.x * v.z;
    //    public static void Swap(ref Vector3 a, ref Vector3 b)
    //    {
    //        var temp = a;
    //        a = b;
    //        b = temp;
    //    }
    //    [System.Flags]
    //    public enum StraightPathFlags
    //    {
    //        Start = 0x01, // The vertex is the start position.
    //        End = 0x02, // The vertex is the end position.
    //        OffMeshConnection = 0x04 // The vertex is start of an off-mesh link.
    //    }
    //    public static bool SegmentSegmentCPA(out float3 c0, out float3 c1, float3 p0, float3 p1, float3 q0, float3 q1)
    //    {
    //        var u = p1 - p0;
    //        var v = q1 - q0;
    //        var w0 = p0 - q0;

    //        float a = math.dot(u, u);
    //        float b = math.dot(u, v);
    //        float c = math.dot(v, v);
    //        float d = math.dot(u, w0);
    //        float e = math.dot(v, w0);

    //        float den = (a * c - b * b);
    //        float sc, tc;

    //        if (den == 0)
    //        {
    //            sc = 0;
    //            tc = d / b;

    //            // todo: handle b = 0 (=> a and/or c is 0)
    //        }
    //        else
    //        {
    //            sc = (b * e - c * d) / (a * c - b * b);
    //            tc = (a * e - b * d) / (a * c - b * b);
    //        }

    //        c0 = math.lerp(p0, p1, sc);
    //        c1 = math.lerp(q0, q1, tc);

    //        return den != 0;
    //    }
    //    public static int RetracePortals(NavMeshQuery query, int startIndex, int endIndex, NativeSlice<PolygonId> path, int n, Vector3 termPos, ref NativeArray<NavMeshLocation> straightPath, ref NativeArray<StraightPathFlags> straightPathFlags, int maxStraightPath)
    //    {
    //        for (var k = startIndex; k < endIndex - 1; ++k)
    //        {
    //            var type1 = query.GetPolygonType(path[k]);
    //            var type2 = query.GetPolygonType(path[k + 1]);
    //            if (type1 != type2)
    //            {
    //                Vector3 l, r;
    //                var status = query.GetPortalPoints(path[k], path[k + 1], out l, out r);
    //                float3 cpa1, cpa2;
    //                SegmentSegmentCPA(out cpa1, out cpa2, l, r, straightPath[n - 1].position, termPos);
    //                straightPath[n] = query.CreateLocation(cpa1, path[k + 1]);

    //                straightPathFlags[n] = (type2 == NavMeshPolyTypes.OffMeshConnection) ? StraightPathFlags.OffMeshConnection : 0;
    //                if (++n == maxStraightPath)
    //                {
    //                    return maxStraightPath;
    //                }
    //            }
    //        }
    //        straightPath[n] = query.CreateLocation(termPos, path[endIndex]);
    //        straightPathFlags[n] = query.GetPolygonType(path[endIndex]) == NavMeshPolyTypes.OffMeshConnection ? StraightPathFlags.OffMeshConnection : 0;
    //        return ++n;
    //    }
    //    public static PathQueryStatus FindStraightPath(NavMeshQuery query, Vector3 startPos, Vector3 endPos, NativeSlice<PolygonId> path, int pathSize, ref NativeArray<NavMeshLocation> straightPath, ref NativeArray<StraightPathFlags> straightPathFlags, ref NativeArray<float> vertexSide, ref int straightPathCount, int maxStraightPath)
    //    {
    //        if (!query.IsValid(path[0]))
    //        {
    //            straightPath[0] = new NavMeshLocation(); // empty terminator
    //            return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
    //        }

    //        straightPath[0] = query.CreateLocation(startPos, path[0]);

    //        straightPathFlags[0] = StraightPathFlags.Start;

    //        var apexIndex = 0;
    //        var n = 1;

    //        if (pathSize > 1)
    //        {
    //            var startPolyWorldToLocal = query.PolygonWorldToLocalMatrix(path[0]);

    //            var apex = startPolyWorldToLocal.MultiplyPoint(startPos);
    //            var left = new Vector3(0, 0, 0); // Vector3.zero accesses a static readonly which does not work in burst yet
    //            var right = new Vector3(0, 0, 0);
    //            var leftIndex = -1;
    //            var rightIndex = -1;

    //            for (var i = 1; i <= pathSize; ++i)
    //            {
    //                var polyWorldToLocal = query.PolygonWorldToLocalMatrix(path[apexIndex]);

    //                Vector3 vl, vr;
    //                if (i == pathSize)
    //                {
    //                    vl = vr = polyWorldToLocal.MultiplyPoint(endPos);
    //                }
    //                else
    //                {
    //                    var success = query.GetPortalPoints(path[i - 1], path[i], out vl, out vr);
    //                    if (!success)
    //                    {
    //                        return PathQueryStatus.Failure; // | PathQueryStatus.InvalidParam;
    //                    }

    //                    vl = polyWorldToLocal.MultiplyPoint(vl);
    //                    vr = polyWorldToLocal.MultiplyPoint(vr);
    //                }

    //                vl = vl - apex;
    //                vr = vr - apex;

    //                // Ensure left/right ordering
    //                if (Perp2D(vl, vr) < 0)
    //                    Swap(ref vl, ref vr);

    //                // Terminate funnel by turning
    //                if (Perp2D(left, vr) < 0)
    //                {
    //                    var polyLocalToWorld = query.PolygonLocalToWorldMatrix(path[apexIndex]);
    //                    var termPos = polyLocalToWorld.MultiplyPoint(apex + left);

    //                    n = RetracePortals(query, apexIndex, leftIndex, path, n, termPos, ref straightPath, ref straightPathFlags, maxStraightPath);
    //                    if (vertexSide.Length > 0)
    //                    {
    //                        vertexSide[n - 1] = -1;
    //                    }

    //                    //Debug.Log("LEFT");

    //                    if (n == maxStraightPath)
    //                    {
    //                        straightPathCount = n;
    //                        return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
    //                    }

    //                    apex = polyWorldToLocal.MultiplyPoint(termPos);
    //                    left.Set(0, 0, 0);
    //                    right.Set(0, 0, 0);
    //                    i = apexIndex = leftIndex;
    //                    continue;
    //                }
    //                if (Perp2D(right, vl) > 0)
    //                {
    //                    var polyLocalToWorld = query.PolygonLocalToWorldMatrix(path[apexIndex]);
    //                    var termPos = polyLocalToWorld.MultiplyPoint(apex + right);

    //                    n = RetracePortals(query, apexIndex, rightIndex, path, n, termPos, ref straightPath, ref straightPathFlags, maxStraightPath);
    //                    if (vertexSide.Length > 0)
    //                    {
    //                        vertexSide[n - 1] = 1;
    //                    }

    //                    //Debug.Log("RIGHT");

    //                    if (n == maxStraightPath)
    //                    {
    //                        straightPathCount = n;
    //                        return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
    //                    }

    //                    apex = polyWorldToLocal.MultiplyPoint(termPos);
    //                    left.Set(0, 0, 0);
    //                    right.Set(0, 0, 0);
    //                    i = apexIndex = rightIndex;
    //                    continue;
    //                }

    //                // Narrow funnel
    //                if (Perp2D(left, vl) >= 0)
    //                {
    //                    left = vl;
    //                    leftIndex = i;
    //                }
    //                if (Perp2D(right, vr) <= 0)
    //                {
    //                    right = vr;
    //                    rightIndex = i;
    //                }
    //            }
    //        }

    //        // Remove the the next to last if duplicate point - e.g. start and end positions are the same
    //        // (in which case we have get a single point)
    //        if (n > 0 && (straightPath[n - 1].position == endPos))
    //            n--;

    //        n = RetracePortals(query, apexIndex, pathSize - 1, path, n, endPos, ref straightPath, ref straightPathFlags, maxStraightPath);
    //        if (vertexSide.Length > 0)
    //        {
    //            vertexSide[n - 1] = 0;
    //        }

    //        if (n == maxStraightPath)
    //        {
    //            straightPathCount = n;
    //            return PathQueryStatus.Success; // | PathQueryStatus.BufferTooSmall;
    //        }

    //        // Fix flag for final path point
    //        straightPathFlags[n - 1] = StraightPathFlags.End;

    //        straightPathCount = n;
    //        return PathQueryStatus.Success;
    //    }
    //}

}
