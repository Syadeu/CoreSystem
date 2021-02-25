using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Jobs;

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Concurrent;

namespace Syadeu
{
    internal sealed class TransformJobManager : StaticDataManager<TransformJobManager>
    {
        internal Dictionary<TransformIdx, Transform> m_Transforms = new Dictionary<TransformIdx, Transform>();
        private TransformAccessArray m_TransformAccessArray;

        private ConcurrentQueue<MoveJobPayload> moveJobPayloads = new ConcurrentQueue<MoveJobPayload>();

        private struct MoveJobPayload
        {
            public TransformIdx m_Tr;
            public Vector3 m_Target;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();

            StartBackgroundUpdate(Updater());
        }

        internal TransformIdx Register(in Transform tr)
        {
            TransformIdx idx = TransformIdx.NewIdx();
            m_Transforms.Add(idx, tr);

            return idx;
        }

        private IEnumerator Updater()
        {
            while (true)
            {


                yield return null;
            }
        }
    }

    internal struct TransformIdx : IEquatable<TransformIdx>
    {
        public static TransformIdx NewIdx()
        {
            return new TransformIdx
            {
                m_Idx = TransformJobManager.Instance.m_Transforms.Count,
                m_Guid = Guid.NewGuid()
            };
        }

        public bool Equals(TransformIdx other) => m_Guid.Equals(other.m_Guid);

        public int m_Idx;
        public Guid m_Guid;

        public bool IsValid() => m_Guid != Guid.Empty;
    }

    public ref struct TransformJob
    {
        internal TransformIdx m_Idx;
        private bool m_Initialized;

        private bool m_IsMoveJob;
        private float3 m_TargetPos;

        public static TransformJob Create(in Transform tr)
        {
            TransformJob job = new TransformJob
            {
                m_Idx = TransformJobManager.Instance.Register(tr),
                m_Initialized = true
            };

            return job;
        }
        private static void Initialize(ref TransformJob job)
        {
            var idx = job.m_Idx;
            if (!idx.IsValid())
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Jobs,
                    "정상적으로 생성되지 않은 TransformJob의 메소드 호출");
            }

            job = default;

            job.m_Idx = idx;
        }

        public bool IsValid() => m_Initialized;

        public TransformJob MoveTo(Vector3 target)
        {
            m_IsMoveJob = true;
            m_TargetPos = target;

            return this;
        }


        public void Schedule()
        {

        }
    }

    //public class TransformJobBuilder
    //{
    //    private TransformJobType m_JobType;
    //    private TransformAccessArray m_TransformAccessArray;

    //    private float3[] m_VectorArray;

    //    public TransformJobBuilder(TransformJobType jobType, params Transform[] transforms)
    //    {
    //        m_JobType = jobType;
    //        m_TransformAccessArray = new TransformAccessArray(transforms);

    //        switch (jobType)
    //        {
    //            case TransformJobType.Move:
    //            case TransformJobType.Cache:
    //                m_VectorArray = new float3[transforms.Length];
    //                break;
    //        }
    //    }

    //    public TransformJobBuilder MoveTo(int i, Vector3 point)
    //    {
    //        if (m_JobType != TransformJobType.Move) throw new Exception();

    //        m_VectorArray[i] = point;
    //        return this;
    //    }
    //    public TransformJobBuilder MoveToAll(Vector3 point)
    //    {
    //        if (m_JobType != TransformJobType.Move) throw new Exception();

    //        for (int i = 0; i < m_VectorArray.Length; i++)
    //        {
    //            m_VectorArray[i] = point;
    //        }
    //        return this;
    //    }

    //    public JobHandle BuildMove()
    //    {
    //        TransformMoveJob job = new TransformMoveJob
    //        {
    //            positions = new NativeArray<float3>(m_VectorArray, Allocator.TempJob)
    //        };

    //        return job.Schedule(m_TransformAccessArray);
    //    }
    //    public JobHandle BuildCache(NativeArray<TransformCache> caches)
    //    {
    //        TransformCacheJob job = new TransformCacheJob
    //        {
    //            caches = caches
    //        };

    //        return job.Schedule(m_TransformAccessArray);
    //    }
    //}

    //public enum TransformJobType
    //{
    //    Move,
    //    Cache,
    //}

    //public struct TransformCache
    //{
    //    public Vector3 position;
    //    public Vector3 localPosition;

    //    public Vector3 localScale;

    //    public Quaternion rotation;
    //    public Quaternion localRotation;

    //    public Matrix4x4 worldToLocalMatrix;
    //    public Matrix4x4 localToWorldMatrix;

    //    internal TransformCache(TransformAccess tr)
    //    {
    //        position = tr.position;
    //        localPosition = tr.localPosition;

    //        localScale = tr.localScale;

    //        rotation = tr.rotation;
    //        localRotation = tr.localRotation;

    //        worldToLocalMatrix = tr.worldToLocalMatrix;
    //        localToWorldMatrix = tr.localToWorldMatrix;
    //    }
    //}

    //internal struct TransformMoveJob : IJobParallelForTransform
    //{
    //    [DeallocateOnJobCompletion]
    //    public NativeArray<float3> positions;

    //    public void Execute(int i, TransformAccess transform)
    //    {
    //        transform.position = positions[i];
    //    }
    //}
    //internal struct TransformCacheJob : IJobParallelForTransform
    //{
    //    public NativeArray<TransformCache> caches;

    //    public void Execute(int i, TransformAccess transform)
    //    {
    //        caches[i] = new TransformCache(transform);
    //    }
    //}
}
