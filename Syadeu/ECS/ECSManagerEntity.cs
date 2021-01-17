using System.Collections;

using UnityEngine;
using UnityEngine.AI;
using System.Net;
using System.Collections.Concurrent;
using UnityEngine.Experimental.AI;
using System.Linq;
using UnityEngine.Jobs;
using System.Collections.Generic;
using System;

#if UNITY_JOBS && UNITY_MATH && UNITY_BURST && UNITY_COLLECTION && UNITY_ENTITIES

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Syadeu.ECS
{
    public abstract class ECSManagerEntity<T> : SystemBase
        where T : SystemBase
    {
        private static T m_Instance;
        protected static T p_Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    World world = World.DefaultGameObjectInjectionWorld;
                    m_Instance = world.GetOrCreateSystem<T>();
                }
                return m_Instance;
            }
        }

        protected TValue GetComponentData<TValue>(Entity entity) where TValue : struct, IComponentData
            => EntityManager.GetComponentData<TValue>(entity);
        protected void AddComponentData<TValue>(Entity entity, TValue component) where TValue : struct, IComponentData
            => EntityManager.AddComponentData(entity, component);
    }

    public enum PathfinderStatus
    {
        Idle = 0,
        
        PathQueued = 1 << 0,
        PathFound = 1 << 1,

        Paused = 1 << 2,
        Failed = 1 << 3,
        ExceedDistance = 1 << 4
    }
    public struct ECSPathfinderComponent : IComponentData
    {
        public int id;
        public int agentTypeId;

        public int areaMask;
        public float maxDistance;

        public int pathKey;
        public PathfinderStatus status;
        public float3 to;
        public float totalDistance;

        public bool temp;
    }
    public struct ECSPathBuffer : IBufferElementData
    {
        public float3 position;

        public static implicit operator float3(ECSPathBuffer e)
        {
            return e.position;
        }
        public static implicit operator ECSPathBuffer(float3 e)
        {
            return new ECSPathBuffer { position = e };
        }
        public static implicit operator ECSPathBuffer(Vector3 e)
        {
            return new ECSPathBuffer { position = e };
        }
    }
    public struct PathfinderID : IEquatable<PathfinderID>
    {
        internal Entity Entity { get; }
        internal int TrIndex { get; }
        internal PathfinderID(Entity entity, int trIndex)
        {
            Entity = entity;
            TrIndex = trIndex;
        }

        public bool Equals(PathfinderID other)
            => Entity.Index == other.Entity.Index;
    }

    public struct UpdateTranslationJob : IJobParallelForTransform
    {
        public NativeArray<float3> trArr;

        public void Execute(int index, TransformAccess transform)
        {
            //Debug.Log($"from {tr.Value} to {transform.position}");
            trArr[index] = transform.position;
        }
    }
}

#endif