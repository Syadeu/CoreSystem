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
    [UpdateAfter(typeof(TransformSystemGroup))]
    public class ECSPathSystemGroup : ComponentSystemGroup { }
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
        public float maxDistance;

        public int pathKey;
    }
    public struct ECSPathQuery : IComponentData
    {
        //public int pathKey;
        public PathStatus status;

        public int areaMask;
        
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
    [BurstCompile]
    public struct UpdateTranslationJob : IJobParallelForTransform
    {
        public NativeArray<float3> positions;

        public void Execute(int index, TransformAccess transform)
        {
            positions[index] = transform.position;
        }
    }

    public struct ManagedObjectRef<T> where T : class
    {
        public readonly ulong Id;

        public ManagedObjectRef(ulong id)
        {
            Id = id;
        }
    }
    public class ManagedObjectWorld<T> where T : class
    {
        private ulong m_NextId;
        private readonly Dictionary<ulong, T> m_Objects;

        public ManagedObjectWorld(int initialCapacity = 1000)
        {
            m_NextId = 1;
            m_Objects = new Dictionary<ulong, T>(initialCapacity);
        }

        public ManagedObjectRef<T> Add(T obj)
        {
            ulong id = m_NextId;
            m_NextId++;
            m_Objects[id] = obj;
            return new ManagedObjectRef<T>(id);
        }

        public T Get(ManagedObjectRef<T> objRef)
        {
            return m_Objects[objRef.Id];
        }

        public void Remove(ManagedObjectRef<T> objRef)
        {
            m_Objects.Remove(objRef.Id);
        }
    }
}

#endif