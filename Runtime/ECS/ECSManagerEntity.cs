using System.Collections;
using UnityEngine.AI;
using System.Net;
using System.Collections.Concurrent;
using UnityEngine.Experimental.AI;
using System.Linq;
using UnityEngine.Jobs;
using System.Collections.Generic;
using System;

using Unity.Burst;
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

    //public struct ManagedObjectRef<T> where T : class
    //{
    //    public readonly ulong Id;

    //    public ManagedObjectRef(ulong id)
    //    {
    //        Id = id;
    //    }
    //}
    //public class ManagedObjectWorld<T> where T : class
    //{
    //    private ulong m_NextId;
    //    private readonly Dictionary<ulong, T> m_Objects;

    //    public ManagedObjectWorld(int initialCapacity = 1000)
    //    {
    //        m_NextId = 1;
    //        m_Objects = new Dictionary<ulong, T>(initialCapacity);
    //    }

    //    public ManagedObjectRef<T> Add(T obj)
    //    {
    //        ulong id = m_NextId;
    //        m_NextId++;
    //        m_Objects[id] = obj;
    //        return new ManagedObjectRef<T>(id);
    //    }

    //    public T Get(ManagedObjectRef<T> objRef)
    //    {
    //        return m_Objects[objRef.Id];
    //    }

    //    public void Remove(ManagedObjectRef<T> objRef)
    //    {
    //        m_Objects.Remove(objRef.Id);
    //    }
    //}
}