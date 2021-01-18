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
        public static T Instance
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

    public enum PathStatus
    {
        Idle = 0,
        
        PathQueued = 1 << 0,

        PathFound = 1 << 1,
        Failed = 1 << 2,
        //Paused = 1 << 2,

        //ExceedDistance = 1 << 4
    }
    public struct ECSPathFinder : IComponentData
    {
        public int id;
        public int agentTypeId;

        public int areaMask;
        public float maxDistance;

        public PathStatus status;
        public int pathKey;
        public float3 to;
        public float totalDistance;
    }
    public struct ECSPathBuffer : IBufferElementData
    {
        public float3 position;

        public static implicit operator float3(ECSPathBuffer e)
            => e.position;
        public static implicit operator ECSPathBuffer(float3 e)
            => new ECSPathBuffer { position = e };
        public static implicit operator ECSPathBuffer(Vector3 e)
            => new ECSPathBuffer { position = e };
    }
    //public struct PathfinderID : IEquatable<PathfinderID>
    //{
    //    internal Entity Entity { get; }
    //    internal int TrIndex { get; }
    //    internal PathfinderID(Entity entity, int trIndex)
    //    {
    //        Entity = entity;
    //        TrIndex = trIndex;
    //    }

    //    public bool Equals(PathfinderID other)
    //        => Entity.Index == other.Entity.Index;
    //}

    public struct UpdateTranslationJob : IJobParallelForTransform
    {
        public NativeArray<float3> positions;

        public void Execute(int index, TransformAccess transform)
        {
            positions[index] = transform.position;
        }
    }
}

#endif