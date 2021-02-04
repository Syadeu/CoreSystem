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

namespace Syadeu
{
    public class TransformJobBuilder
    {
        private TransformJobType m_JobType;
        private TransformAccessArray m_TransformAccessArray;

        private float3[] m_VectorArray;

        public TransformJobBuilder(TransformJobType jobType, params Transform[] transforms)
        {
            m_JobType = jobType;
            m_TransformAccessArray = new TransformAccessArray(transforms);

            switch (jobType)
            {
                case TransformJobType.Move:
                case TransformJobType.Cache:
                    m_VectorArray = new float3[transforms.Length];
                    break;
            }
        }

        public TransformJobBuilder MoveTo(int i, Vector3 point)
        {
            if (m_JobType != TransformJobType.Move) throw new Exception();

            m_VectorArray[i] = point;
            return this;
        }
        public TransformJobBuilder MoveToAll(Vector3 point)
        {
            if (m_JobType != TransformJobType.Move) throw new Exception();

            for (int i = 0; i < m_VectorArray.Length; i++)
            {
                m_VectorArray[i] = point;
            }
            return this;
        }

        public JobHandle BuildMove()
        {
            TransformMoveJob job = new TransformMoveJob
            {
                positions = new NativeArray<float3>(m_VectorArray, Allocator.TempJob)
            };

            return job.Schedule(m_TransformAccessArray);
        }
        public JobHandle BuildCache(NativeArray<TransformCache> caches)
        {
            TransformCacheJob job = new TransformCacheJob
            {
                caches = caches
            };

            return job.Schedule(m_TransformAccessArray);
        }
    }

    public enum TransformJobType
    {
        Move,
        Cache,
    }

    public struct TransformCache
    {
        public Vector3 position;
        public Vector3 localPosition;

        public Vector3 localScale;

        public Quaternion rotation;
        public Quaternion localRotation;

        public Matrix4x4 worldToLocalMatrix;
        public Matrix4x4 localToWorldMatrix;

        internal TransformCache(TransformAccess tr)
        {
            position = tr.position;
            localPosition = tr.localPosition;

            localScale = tr.localScale;

            rotation = tr.rotation;
            localRotation = tr.localRotation;

            worldToLocalMatrix = tr.worldToLocalMatrix;
            localToWorldMatrix = tr.localToWorldMatrix;
        }
    }

    internal struct TransformMoveJob : IJobParallelForTransform
    {
        [DeallocateOnJobCompletion]
        public NativeArray<float3> positions;

        public void Execute(int i, TransformAccess transform)
        {
            transform.position = positions[i];
        }
    }
    internal struct TransformCacheJob : IJobParallelForTransform
    {
        public NativeArray<TransformCache> caches;

        public void Execute(int i, TransformAccess transform)
        {
            caches[i] = new TransformCache(transform);
        }
    }
}
