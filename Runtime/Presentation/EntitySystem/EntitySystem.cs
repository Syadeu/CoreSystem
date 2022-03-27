// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using MoonSharp.Interpreter;
using Syadeu.Collections;
using Syadeu.Collections.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Proxy;

namespace Syadeu.Presentation
{
    public sealed class EntitySystem : PresentationSystemEntity<EntitySystem>,
        INotifySystemModule<EntityRecycleModule>,
        INotifySystemModule<EntityProcessorModule>,
        INotifySystemModule<EntityIDModule>,
        INotifySystemModule<EntityHierarchyModule>,
        INotifySystemModule<EntityTransformModule>
#if DEBUG_MODE
        , INotifySystemModule<EntityDebugModule>
#endif
    {
        private const string c_ObjectNotFoundError = "Object({0}) not found.";
        private const string c_EntityNotFoundError = "Entity({0}) not found. Cannot spawn at {1}";
        private const string c_IsNotEntityError = "This object({0}) is not a entity. Use CreateObject instead";
        private const string c_EntityHasInvalidPrefabError = "This entity({0}) has an invalid prefab. This is not allowed";
        private const string c_AttributeWarning = "Attribute({0}) on entity({1}) has invaild value. {2}. Request Ignored.";
        private const string c_AttributeEmptyWarning = "Entity({0}) has empty attribute. This is not allowed. Request Ignored.";

        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        /// <summary>
        /// 엔티티가 생성될때 실행되는 이벤트 delegate 입니다.
        /// </summary>
        /// <remarks>
        /// 모든 프로세서가 동작한 후, 맨 마지막에 실행됩니다.
        /// </remarks>
        public event Action<IObject> OnEntityCreated
        {
            add
            {
                GetModule<EntityProcessorModule>().OnEntityCreated += value;
            }
            remove
            {
                GetModule<EntityProcessorModule>().OnEntityCreated -= value;
            }
        }
        /// <summary>
        /// 엔티티가 파괴될때 실행되는 이벤트 delegate 입니다.
        /// </summary>
        /// <remarks>
        /// 모든 프로세서가 동작한 후, 맨 마지막에 실행됩니다.
        /// </remarks>
        public event Action<IObject> OnEntityDestroy
        {
            add
            {
                GetModule<EntityProcessorModule>().OnEntityDestroy += value;
            }
            remove
            {
                GetModule<EntityProcessorModule>().OnEntityDestroy -= value;
            }
        }

        private Unity.Mathematics.Random m_Random;

        internal readonly Dictionary<InstanceID, ObjectBase> m_ObjectEntities = new Dictionary<InstanceID, ObjectBase>();
        [Obsolete("", true)]
        internal NativeHashMap<ProxyTransformID, InstanceID> m_EntityGameObjects;

        private readonly Stack<InstanceID> m_DestroyedObjectsInThisFrame = new Stack<InstanceID>();

        private static Unity.Profiling.ProfilerMarker
            m_CreateEntityMarker = new Unity.Profiling.ProfilerMarker($"{nameof(EntitySystem)}.{nameof(CreateEntity)}")
            /*m_CreateEntityDataMarker = new Unity.Profiling.ProfilerMarker($"{nameof(EntitySystem)}.{nameof(CreateObject)}")*/;

        private ActionWrapper m_DestroyedObjectsInThisFrameAction;
        private EntityProcessorModule m_EntityProcessorModule;

        internal DataContainerSystem m_DataContainerSystem;
        internal GameObjectProxySystem m_ProxySystem;
        internal Events.EventSystem m_EventSystem;
        internal CoroutineSystem m_CoroutineSystem;
        internal Actor.ActorSystem m_ActorSystem;
        internal Components.EntityComponentSystem m_ComponentSystem;
        private SceneSystem m_SceneSystem;

        #region Presentation Methods
        protected override PresentationResult OnInitialize()
        {
            m_Random = new Unity.Mathematics.Random();
            m_Random.InitState();

            //m_EntityGameObjects = new NativeHashMap<ProxyTransformID, InstanceID>(10240, Allocator.Persistent);

            m_DestroyedObjectsInThisFrameAction = ActionWrapper.GetWrapper();
            m_DestroyedObjectsInThisFrameAction.SetProfiler("DestroyedObjectsInThisFrame");
            m_DestroyedObjectsInThisFrameAction.SetAction(Instance_PreUpdate);

            m_EntityProcessorModule = GetModule<EntityProcessorModule>();

            PresentationManager.Instance.PreUpdate += m_DestroyedObjectsInThisFrameAction.Invoke;

            return base.OnInitialize();
        }

        private Queue<InstanceID> m_WaitForRemove = new Queue<InstanceID>();
        private void Instance_PreUpdate()
        {
            if (CoreSystem.BlockCreateInstance) return;

            while (m_DestroyedObjectsInThisFrame.Count > 0)
            {
                InstanceID id = m_DestroyedObjectsInThisFrame.Pop();
                ObjectBase targetObject = m_ObjectEntities[id];

                m_EntityProcessorModule.ProcessOnReserve(targetObject);

                m_WaitForRemove.Enqueue(id);
            }

            int count = m_WaitForRemove.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                InstanceID id = m_WaitForRemove.Dequeue();
                m_ObjectEntities.Remove(id);
            }
        }

        protected override PresentationResult OnInitializeAsync()
        {
            RequestSystem<DefaultPresentationGroup, DataContainerSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GameObjectProxySystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Events.EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Actor.ActorSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Components.EntityComponentSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);

            return base.OnInitializeAsync();
        }
        protected override void OnShutDown()
        {
            m_DestroyedObjectsInThisFrame.Clear();

            var entityList = m_ObjectEntities.Values.ToArray();
            for (int i = 0; i < entityList.Length; i++)
            {
                m_EntityProcessorModule.ProcessDisposal(entityList[i]);
            }

            GetModule<EntityRecycleModule>().ExecuteDisposeAll();
        }
        protected override void OnDispose()
        {
            PresentationManager.Instance.PreUpdate -= m_DestroyedObjectsInThisFrameAction.Invoke;
            m_SceneSystem.OnSceneChanged -= M_SceneSystem_OnSceneLoadCall;

            m_DestroyedObjectsInThisFrameAction.Reserve();
            m_DestroyedObjectsInThisFrameAction = null;

            m_EntityProcessorModule = null;

            m_ObjectEntities.Clear();
            
            m_ProxySystem.OnDataObjectVisible -= OnDataObjectVisible;
            m_ProxySystem.OnDataObjectInvisible -= OnDataObjectInvisible;

            m_ProxySystem.OnDataObjectProxyCreated -= M_ProxySystem_OnDataObjectProxyCreated;
            m_ProxySystem.OnDataObjectProxyRemoved -= M_ProxySystem_OnDataObjectProxyRemoved;

            //m_EntityGameObjects.Dispose();

            m_DataContainerSystem = null;
            m_ProxySystem = null;
            m_EventSystem = null;
            m_CoroutineSystem = null;
            m_ComponentSystem = null;
            m_SceneSystem = null;
        }

        #region Binds

        private void Bind(DataContainerSystem other)
        {
            m_DataContainerSystem = other;
        }
        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;

            m_ProxySystem.OnDataObjectVisible += OnDataObjectVisible;
            m_ProxySystem.OnDataObjectInvisible += OnDataObjectInvisible;

            m_ProxySystem.OnDataObjectProxyCreated += M_ProxySystem_OnDataObjectProxyCreated;
            m_ProxySystem.OnDataObjectProxyRemoved += M_ProxySystem_OnDataObjectProxyRemoved;
        }
        private void OnDataObjectVisible(ProxyTransform tr)
        {
            //#if DEBUG_MODE
            //            if (!m_EntityGameObjects.TryGetValue(tr.m_Hash, out InstanceID eCheckHash) ||
            //                !m_ObjectEntities.ContainsKey(eCheckHash))
            //            {
            //                CoreSystem.Logger.LogError(Channel.Entity,
            //                    $"Internal EntitySystem error. ProxyTransform doesn\'t have entity.");
            //                return;
            //            }
            //#endif
            //            InstanceID entityHash = m_EntityGameObjects[tr.m_Hash];
            EntityTransformModule transformModule = GetModule<EntityTransformModule>();
            if (!transformModule.HasEntity(in tr))
            {
                return;
            }

            InstanceID entityHash = transformModule.GetEntity(in tr);

            //ObjectBase entity = GetEntityByTransform(tr);
            //if (entity == null) return;

            m_EventSystem.PostEvent<OnEntityVisibleEvent>(OnEntityVisibleEvent.GetEvent(
                Entity<IEntity>.GetEntityWithoutCheck(entityHash), tr));
        }
        private void OnDataObjectInvisible(ProxyTransform tr)
        {
            EntityTransformModule transformModule = GetModule<EntityTransformModule>();
            if (!transformModule.HasEntity(in tr))
            {
                return;
            }
            //if (!m_EntityGameObjects.TryGetValue(tr.m_Hash, out InstanceID eCheckHash) ||
            //    !m_ObjectEntities.ContainsKey(eCheckHash))
            //{
            //    //CoreSystem.Logger.LogError(Channel.Entity,
            //    //    $"Internal EntitySystem error. ProxyTransform({tr.prefab.GetObjectSetting().Name}) doesn\'t have entity.");
            //    return;
            //}

            //InstanceID entityHash = m_EntityGameObjects[tr.m_Hash];
            InstanceID entityHash = transformModule.GetEntity(in tr);

            m_EventSystem.PostEvent<OnEntityVisibleEvent>(OnEntityVisibleEvent.GetEvent(
                Entity<IEntity>.GetEntityWithoutCheck(entityHash), tr));
        }
        private void M_ProxySystem_OnDataObjectProxyCreated(ProxyTransform tr, RecycleableMonobehaviour monoObj)
        {
            EntityTransformModule transformModule = GetModule<EntityTransformModule>();
//#if DEBUG_MODE
            if (/*!m_EntityGameObjects.TryGetValue(tr.m_Hash, out InstanceID eCheckHash) ||*/
                /*!m_ObjectEntities.ContainsKey(eCheckHash)*/
                !transformModule.HasEntity(in tr))
            {
                //CoreSystem.Logger.LogError(Channel.Entity,
                //    $"Internal EntitySystem error. ProxyTransform doesn\'t have entity.");
                return;
            }
//#endif
            //InstanceID entityHash = m_EntityGameObjects[tr.m_Hash];
            InstanceID entityHash = transformModule.GetEntity(in tr);
            IEntity entity = (IEntity)m_ObjectEntities[entityHash];

            monoObj.m_Entity = Entity<IEntity>.GetEntityWithoutCheck(in entityHash);
            EntityProcessorModule.ProcessEntityOnProxyCreated(GetModule<EntityProcessorModule>(), entity, monoObj);
        }
        private void M_ProxySystem_OnDataObjectProxyRemoved(ProxyTransform tr, RecycleableMonobehaviour monoObj)
        {
            //if (!m_EntityGameObjects.ContainsKey(tr.m_Hash))
            //{
            //    "?? error".ToLogError();
            //    return;
            //}
            EntityTransformModule transformModule = GetModule<EntityTransformModule>();
            if (!transformModule.HasEntity(in tr))
            {
                //CoreSystem.Logger.LogError(Channel.Entity,
                //    $"Internal EntitySystem error. ProxyTransform({tr.prefab}) doesn\'t have entity.");
                return;
            }

            InstanceID entityHash = transformModule.GetEntity(in tr);
            //if (!m_ObjectEntities.TryGetValue(m_EntityGameObjects[tr.m_Hash], out ObjectBase objectBase))
            if (!m_ObjectEntities.TryGetValue(entityHash, out ObjectBase objectBase))
            {
                // Intended.

                //CoreSystem.Logger.LogError(Channel.Entity,
                //    $"Internal EntitySystem error. ProxyTransform doesn\'t have entity.");
                return;
            }

            IEntity entity = (IEntity)objectBase;

            EntityProcessorModule.ProcessEntityOnProxyRemoved(GetModule<EntityProcessorModule>(), entity, monoObj);
            monoObj.m_Entity = Entity<IEntity>.Empty;
        }
        
        private void Bind(Events.EventSystem other)
        {
            m_EventSystem = other;
        }

        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
        }

        private void Bind(Actor.ActorSystem other)
        {
            m_ActorSystem = other;
        }
        private void Bind(Components.EntityComponentSystem other)
        {
            m_ComponentSystem = other;
        }

        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
            m_SceneSystem.OnSceneLoadCall += M_SceneSystem_OnSceneLoadCall;
        }
        private void M_SceneSystem_OnSceneLoadCall()
        {
            //using (var iter = m_EntityGameObjects.GetEnumerator())
            //{
            //    while (iter.MoveNext())
            //    {
            //        InternalDestroyEntity(iter.Current.Value);
            //    }
            //}

            //m_DestroyedObjectsInThisFrameAction.Invoke();
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            //ConsoleWindow.CreateCommand((cmd) =>
            //{
            //    while (m_ObjectEntities.Any())
            //    {
            //        var temp = m_ObjectEntities.First().Value;
            //        if (!temp.TryAsReference(out var refer)) continue;

            //        refer.Destroy();
            //    }
            //}, "destroy", "all");

            return base.OnStartPresentation();
        }

        protected override PresentationResult OnPresentation()
        {
            return base.OnPresentation();
        }
        protected override PresentationResult OnPresentationAsync()
        {
            // TODO : 이거 매우 심각한 GC 문제를 일으킴.
            //var temp = m_ObjectEntities.Values.ToArray();
            //for (int i = 0; i < temp.Length; i++)
            //{
            //    ProcessEntityOnPresentation(this, temp[i]);
            //}

            return base.OnPresentationAsync();
        }

        #endregion

#line hidden

        #region Create EntityBase

        /// <summary>
        /// <inheritdoc cref="CreateEntity(in string, in float3, in quaternion, in float3)"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(in string name, in float3 position)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);
            using (m_CreateEntityMarker.Auto())
            {
                if (!InternalEntityValidation(name, position, out EntityBase temp))
                {
                    return Entity<IEntity>.Empty;
                }

                //ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, quaternion.identity, 1);
                //Entity<IEntity> entity = InternalCreateEntity(in temp, in obj);
                Entity<IEntity> entity = InternalCreateEntity(in temp, in position, quaternion.identity, 1);

                return entity;
            }
        }
        /// <summary>
        /// <inheritdoc cref="CreateEntity(in Reference, in float3, in quaternion, in float3)"/>
        /// </summary>
        /// <param name="reference"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(in Reference reference, in float3 position)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);

            using (m_CreateEntityMarker.Auto())
            {
                if (!InternalEntityValidation(reference.Hash, in position, out EntityBase temp))
                {
                    return Entity<IEntity>.Empty;
                }

                //ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, quaternion.identity, 1);
                //Entity<IEntity> entity = InternalCreateEntity(in temp, in obj);
                Entity<IEntity> entity = InternalCreateEntity(in temp, in position, quaternion.identity, 1);

                return entity;
            }
        }
        /// <summary>
        /// 엔티티를 생성합니다. <paramref name="name"/>은 <seealso cref="IEntityData.Name"/> 입니다.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="localSize"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(in string name, in float3 position, in quaternion rotation, in float3 localSize)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);

            using (m_CreateEntityMarker.Auto())
            {
                if (!InternalEntityValidation(in name, in position, out EntityBase temp))
                {
                    return Entity<IEntity>.Empty;
                }

                //ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, in rotation, in localSize);
                //Entity<IEntity> entity = InternalCreateEntity(in temp, in obj);
                Entity<IEntity> entity = InternalCreateEntity(in temp, in position, in rotation, in localSize);

                return entity;
            }
        }
        /// <summary>
        /// 엔티티를 생성합니다. <paramref name="reference"/>에는 <seealso cref="Hash"/>값으로 대체 가능합니다.
        /// </summary>
        /// <param name="reference"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="localSize"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(in Reference reference, in float3 position, in quaternion rotation, in float3 localSize)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);

            using (m_CreateEntityMarker.Auto())
            {
                if (!InternalEntityValidation(reference.Hash, in position, out EntityBase temp))
                {
                    return Entity<IEntity>.Empty;
                }

                Entity<IEntity> entity = InternalCreateEntity(in temp, in position, in rotation, in localSize);

                return entity;
            }
        }
        public Entity<IObject> CreateEntity(Reference obj)
        {
            ObjectBase baseObj = obj.GetObject();
            if (baseObj is EntityBase entityBase)
            {
                return InternalCreateEntity(in entityBase, 0, quaternion.identity, 1).ToEntity<IObject>();
            }

            return InternalCreateEntity(baseObj);
        }
        public Entity<T> CreateEntity<T>(Reference<T> obj) where T : class, IObject
        {
            T baseObj = obj.GetObject();
            if (baseObj is EntityBase entityBase)
            {
                return InternalCreateEntity(in entityBase, 0, quaternion.identity, 1).ToEntity<T>();
            }

            return InternalCreateEntity(baseObj).ToEntity<T>();
        }
        public Entity<IObject> CreateEntity(IFixedReference obj)
        {
            ObjectBase baseObj = obj.GetObject();
            if (baseObj is EntityBase entityBase)
            {
                return InternalCreateEntity(in entityBase, 0, quaternion.identity, 1).ToEntity<IObject>();
            }

            return InternalCreateEntity(baseObj);
        }
        public Entity<T> CreateEntity<T>(IFixedReference<T> obj)
            where T : class, IObject
        {
            T baseObj = obj.GetObject();
#if DEBUG_MODE
            if (baseObj == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Cannot create an invalid entity.");

                return Entity<T>.Empty;
            }
#endif
            if (baseObj is EntityBase entityBase)
            {
                return InternalCreateEntity(in entityBase, 0, quaternion.identity, 1).ToEntity<T>();
            }

            return InternalCreateEntity(baseObj).ToEntity<T>();
        }

        #region Entity Validation
        private bool InternalEntityValidation(in string name, in float3 targetPos, out EntityBase entity)
        {
            ObjectBase original = EntityDataList.Instance.GetObject(name);
            return InternalEntityValidation(in name, in original, in targetPos, out entity);
        }
        private bool InternalEntityValidation(in Hash hash, in float3 targetPos, out EntityBase entity)
        {
            ObjectBase original = EntityDataList.Instance.GetObject(hash);
            return InternalEntityValidation(hash.ToString(), in original, in targetPos, out entity);
        }
        private bool InternalEntityValidation(in string key, in ObjectBase original, in float3 targetPos, out EntityBase entity)
        {
#if DEBUG_MODE
            entity = null;
            if (original == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityNotFoundError, key, targetPos));
                return false;
            }
            if (!(original is EntityBase))
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_IsNotEntityError, key));
                return false;
            }
#endif
            entity = (EntityBase)original;
#if DEBUG_MODE
            if (!entity.Prefab.IsNone() && !entity.Prefab.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_EntityHasInvalidPrefabError, original.Name));
                return false;
            }
#endif
            return true;
        }
        #endregion

        private Entity<IObject> InternalCreateEntity(in IObject entityBase)
        {
            ObjectBase objClone = GetModule<EntityRecycleModule>().GetOrCreateInstance<ObjectBase>(entityBase);

            m_ObjectEntities.Add(objClone.Idx, objClone);

            GetModule<EntityProcessorModule>().ProceessOnCreated(objClone);
            return Entity<IObject>.GetEntity(objClone.Idx);
        }
        private Entity<IEntity> InternalCreateEntity(in EntityBase entityBase, in float3 position, in quaternion rotation, in float3 localSize)
        {
            EntityBase entity = GetModule<EntityRecycleModule>().GetOrCreateInstance<EntityBase>(entityBase);
            
            //entity.transform = obj;

            m_ObjectEntities.Add(entity.Idx, entity);
            //m_EntityGameObjects.Add(obj.m_Hash, entity.Idx);
            GetModule<EntityTransformModule>()
                .CreateTransform(
                entity.Idx, entityBase.Prefab, 
                in position, in rotation, in localSize, 
                entityBase.EnableCull, entityBase.Center, entityBase.Size, entityBase.StaticBatching);

            GetModule<EntityProcessorModule>().ProceessOnCreated(entity);
            return Entity<IEntity>.GetEntity(entity.Idx);
        }

        #endregion

        #region Destroy

        /// <summary>
        /// 해당 엔티티를 즉시 파괴합니다.
        /// </summary>
        /// <remarks>
        /// 씬이 전환되는 경우, 해당 씬에서 생성된 <see cref="EntityBase"/>는 자동으로 파괴되므로 호출하지 마세요. 단, <see cref="EntityDataBase"/>(<seealso cref="ITransform"/>이 없는 엔티티)는 씬이 전환되어도 자동으로 파괴되지 않습니다.
        /// </remarks>
        /// <param name="hash"><seealso cref="IEntityData.Idx"/> 값</param>
        public void DestroyEntity(InstanceID instance) => InternalDestroyEntity(instance);
        /// <inheritdoc cref="DestroyEntity(InstanceID)"/>
        public void DestroyEntity(IObject instance) => InternalDestroyEntity(instance.Idx);
        internal void InternalDestroyEntity(in InstanceID hash)
        {
            if (!m_ObjectEntities.ContainsKey(hash))
            {
                if (!CoreSystem.BlockCreateInstance)
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Already destroyed entity {hash}");
                }
                
                return;
            }
#if DEBUG_MODE
            else if (m_DestroyedObjectsInThisFrame.Contains(hash))
            {
                if (!CoreSystem.BlockCreateInstance)
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"This entity({m_ObjectEntities[hash].Name}) marked as destroyed");
                }
                
                return;
            }
#endif
            if (CoreSystem.BlockCreateInstance) return;

            m_DestroyedObjectsInThisFrame.Push(hash);
        }

        internal bool IsDestroyed(in InstanceID idx)
        {
            return !m_ObjectEntities.ContainsKey(idx);
        }
        internal bool IsMarkedAsDestroyed(in InstanceID idx)
        {
            return m_DestroyedObjectsInThisFrame.Contains(idx);
        }

        //private static IEnumerable<Type> GetComponentInterface(Type t)
        //{
        //    return t.GetInterfaces()
        //        .Where(i => i.IsGenericType)
        //        .Where(i => i.GetGenericTypeDefinition() == typeof(Components.INotifyComponent<>));
        //}
        //private static IEnumerable<Type> GetComponentGenerics(Type t)
        //{
        //    return t.GetInterfaces()
        //        .Where(i => i.IsGenericType)
        //        .Where(i => i.GetGenericTypeDefinition() == typeof(Components.INotifyComponent<>))
        //        .Select(i => i.GetGenericArguments().First());
        //}

        #endregion

#line default

        internal EntityShortID Convert(InstanceID id)
        {
            return GetModule<EntityIDModule>().Convert(id);
        }
        internal InstanceID Convert(EntityShortID id)
        {
            return GetModule<EntityIDModule>().Convert(id);
        }

        public ObjectBase GetEntityByID(InstanceID id)
        {
            if (m_ObjectEntities.TryGetValue(id, out var value))
            {
                return value;
            }

            CoreSystem.Logger.LogError(Channel.Entity,
                $"ID({id.Hash}) entity not found.");
            return null;
        }
        public ObjectBase GetEntityByTransform(ProxyTransform tr)
        {
            //if (m_EntityGameObjects.TryGetValue(tr.m_Hash, out var value))
            //{
            //    return GetEntityByID(value);
            //}

            EntityTransformModule transformModule = GetModule<EntityTransformModule>();
            if (transformModule.HasEntity(in tr))
            {
                InstanceID entityHash = transformModule.GetEntity(in tr);
                return GetEntityByID(entityHash);
            }

            return null;
        }
        public int CreateHashCode() => m_Random.NextInt(int.MinValue, int.MaxValue);

        public EntityProcessor<T> GetProcessor<T>() where T : class, IObject
        {
            return GetModule<EntityProcessorModule>().GetProcessor<T>();
        }
    }
}
