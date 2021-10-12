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
    public sealed class EntitySystem : PresentationSystemEntity<EntitySystem>
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
        public event Action<EntityData<IEntityData>> OnEntityCreated;
        /// <summary>
        /// 엔티티가 파괴될때 실행되는 이벤트 delegate 입니다.
        /// </summary>
        /// <remarks>
        /// 모든 프로세서가 동작한 후, 맨 마지막에 실행됩니다.
        /// </remarks>
        public event Action<EntityData<IEntityData>> OnEntityDestroy;

        private Unity.Mathematics.Random m_Random;

        internal readonly Dictionary<InstanceID, ObjectBase> m_ObjectEntities = new Dictionary<InstanceID, ObjectBase>();
        internal NativeHashMap<Hash, InstanceID> m_EntityGameObjects;

        private readonly Dictionary<Type, List<IAttributeProcessor>> m_AttributeProcessors = new Dictionary<Type, List<IAttributeProcessor>>();
        private readonly Dictionary<Type, List<IEntityDataProcessor>> m_EntityProcessors = new Dictionary<Type, List<IEntityDataProcessor>>();

        private readonly List<InstanceID> m_DestroyedObjectsInThisFrame = new List<InstanceID>();
        private readonly Queue<Query> m_Queries = new Queue<Query>();

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

            m_EntityGameObjects = new NativeHashMap<Hash, InstanceID>(10240, Allocator.Persistent);

            PresentationManager.Instance.PreUpdate += Instance_PreUpdate;

            return base.OnInitialize();
        }
        private void Instance_PreUpdate()
        {
            //if (CoreSystem.BlockCreateInstance) return;

            for (int i = 0; i < m_DestroyedObjectsInThisFrame.Count; i++)
            {
                var targetObject = m_ObjectEntities[m_DestroyedObjectsInThisFrame[i]];

                if (targetObject is IEntityData entityData)
                {
                    if (targetObject is IEntity entity)
                    {
                        if (entity.transform is ProxyTransform tr)
                        {
                            Hash index = tr.m_Hash;
                            tr.Destroy();
                            //m_EntityGameObjects.Remove(index);
                        }
                        else if (entity.transform is UnityTransform unityTr)
                        {
                            UnityEngine.Object.Destroy(unityTr.provider.gameObject);
                            ((IDisposable)unityTr).Dispose();
                        }
                    }
                    else
                    {
                        ProcessEntityOnDestroy(this, entityData);

                        RemoveAllComponents(m_DestroyedObjectsInThisFrame[i]);

                        ((IDisposable)targetObject).Dispose();
                        m_ObjectEntities.Remove(m_DestroyedObjectsInThisFrame[i]);
                    }
#if DEBUG_MODE
                    CoreSystem.WaitInvoke(1, () =>
                    {
                        if (Debug_HasComponent(targetObject, out int count, out string names))
                        {
                            CoreSystem.Logger.LogError(Channel.Entity,
                                $"Entity({targetObject.Name}) has " +
                                $"number of {count} components that didn\'t disposed. {names}");
                        }
                        else
                        {
                            "good".ToLog();
                        }
                    });
#endif
                }
                else
                {
                    if (m_ObjectEntities[m_DestroyedObjectsInThisFrame[i]] is DataObjectBase dataObject)
                    {
                        dataObject.InternalOnDestroy();
                    }

                    if (m_ObjectEntities[m_DestroyedObjectsInThisFrame[i]] is Components.INotifyComponent notifyComponent)
                    {
                        var notifies = GetComponentInterface(m_ObjectEntities[m_DestroyedObjectsInThisFrame[i]].GetType());
                        foreach (var item in notifies)
                        {
                            Type componentType = item.GetGenericArguments()[0];
                            m_ComponentSystem.RemoveComponent(notifyComponent.Parent, componentType);
#if DEBUG_MODE
                            Debug_RemoveComponent(notifyComponent.Parent, componentType);
#endif
                        }
                    }

                    ((IDisposable)m_ObjectEntities[m_DestroyedObjectsInThisFrame[i]]).Dispose();
                    m_ObjectEntities.Remove(m_DestroyedObjectsInThisFrame[i]);
                }

            }

            m_DestroyedObjectsInThisFrame.Clear();
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

            #region Processor Registeration
            Type[] processors = TypeHelper.GetTypes(ProcessorPredicate);
            for (int i = 0; i < processors.Length; i++)
            {
                ConstructorInfo ctor = processors[i].GetConstructor(BindingFlags.Public | BindingFlags.Instance,
                    null, CallingConventions.HasThis, Array.Empty<Type>(), null);

                IProcessor processor;
                if (TypeHelper.TypeOf<IAttributeProcessor>.Type.IsAssignableFrom(processors[i]))
                {
                    if (ctor == null) processor = (IAttributeProcessor)Activator.CreateInstance(processors[i]);
                    else
                    {
                        processor = (IAttributeProcessor)ctor.Invoke(null);
                    }

                    if (!TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(processor.Target))
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                            $"Attribute processor {processors[i].Name} has an invalid target");
                    }

                    if (!m_AttributeProcessors.TryGetValue(processor.Target, out var values))
                    {
                        values = new List<IAttributeProcessor>();
                        m_AttributeProcessors.Add(processor.Target, values);
                    }
                    values.Add((IAttributeProcessor)processor);
                }
                else
                {
                    if (ctor == null) processor = (IEntityDataProcessor)Activator.CreateInstance(processors[i]);
                    else
                    {
                        processor = (IEntityDataProcessor)ctor.Invoke(null);
                    }

                    if (!TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(processor.Target))
                    {
                        throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                            $"Entity processor {processors[i].Name} has an invalid target");
                    }

                    if (!m_EntityProcessors.TryGetValue(processor.Target, out var values))
                    {
                        values = new List<IEntityDataProcessor>();
                        m_EntityProcessors.Add(processor.Target, values);
                    }
                    //$"{entityProcessor.GetType().Name} added".ToLog();
                    values.Add((IEntityDataProcessor)processor);
                }

                ProcessorBase baseProcessor = (ProcessorBase)processor;
                baseProcessor.m_EntitySystem = this;

                processor.OnInitializeAsync();
            }
            #endregion

            return base.OnInitializeAsync();

            static bool ProcessorPredicate(Type other) => !other.IsAbstract && !other.IsInterface && TypeHelper.TypeOf<IProcessor>.Type.IsAssignableFrom(other);
        }
        public override void OnDispose()
        {
            Instance_PreUpdate();

            var entityList = m_ObjectEntities.Values.ToArray();
            for (int i = 0; i < entityList.Length; i++)
            {
                if (m_DestroyedObjectsInThisFrame.Contains(entityList[i].Idx)) continue;

                if (entityList[i] is IEntityData entity)
                {
                    //ProcessEntityOnDestroy(this, entity);
                    ProcessEntityOnDestroy(this, entity);

                    RemoveAllComponents(entity.Idx);
                }
                else
                {
                    InternalDestroyEntity(entityList[i].Idx);
                }
            }

            foreach (var item in m_EntityProcessors)
            {
                for (int a = 0; a < item.Value.Count; a++)
                {
                    item.Value[a].Dispose();
                }
            }
            foreach (var item in m_AttributeProcessors)
            {
                for (int a = 0; a < item.Value.Count; a++)
                {
                    item.Value[a].Dispose();
                }
            }

            PresentationManager.Instance.PreUpdate -= Instance_PreUpdate;
            m_SceneSystem.OnSceneChangeCalled -= M_SceneSystem_OnSceneChangeCalled;

            OnEntityCreated = null;
            OnEntityDestroy = null;

            m_ObjectEntities.Clear();
            m_AttributeProcessors.Clear();
            m_EntityProcessors.Clear();

            m_ProxySystem.OnDataObjectDestroy -= M_ProxySystem_OnDataObjectDestroyAsync;
            m_ProxySystem.OnDataObjectVisible -= OnDataObjectVisible;
            m_ProxySystem.OnDataObjectInvisible -= OnDataObjectInvisible;

            m_ProxySystem.OnDataObjectProxyCreated -= M_ProxySystem_OnDataObjectProxyCreated;
            m_ProxySystem.OnDataObjectProxyRemoved -= M_ProxySystem_OnDataObjectProxyRemoved;

            m_EntityGameObjects.Dispose();

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

            m_ProxySystem.OnDataObjectDestroy += M_ProxySystem_OnDataObjectDestroyAsync;
            m_ProxySystem.OnDataObjectVisible += OnDataObjectVisible;
            m_ProxySystem.OnDataObjectInvisible += OnDataObjectInvisible;

            m_ProxySystem.OnDataObjectProxyCreated += M_ProxySystem_OnDataObjectProxyCreated;
            m_ProxySystem.OnDataObjectProxyRemoved += M_ProxySystem_OnDataObjectProxyRemoved;
        }
        private void M_ProxySystem_OnDataObjectDestroyAsync(ProxyTransform obj)
        {
            if (!m_EntityGameObjects.TryGetValue(obj.m_Hash, out InstanceID entityHash) ||
                !m_ObjectEntities.ContainsKey(entityHash) || 
                !(m_ObjectEntities[entityHash] is IEntityData entitydata))
            {
                "not intented handle".ToLogError();
                return;
            }

            ProcessEntityOnDestroy(this, entitydata);

            m_EntityGameObjects.Remove(obj.m_Hash);

            ((IDisposable)m_ObjectEntities[entityHash]).Dispose();
            m_ObjectEntities.Remove(entityHash);
        }
        private void OnDataObjectVisible(ProxyTransform tr)
        {
#if DEBUG_MODE
            if (!m_EntityGameObjects.TryGetValue(tr.m_Hash, out InstanceID eCheckHash) ||
                !m_ObjectEntities.ContainsKey(eCheckHash))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Internal EntitySystem error. ProxyTransform doesn\'t have entity.");
                return;
            }
#endif
            InstanceID entityHash = m_EntityGameObjects[tr.m_Hash];

            m_EventSystem.PostEvent<OnEntityVisibleEvent>(OnEntityVisibleEvent.GetEvent(
                Entity<IEntity>.GetEntityWithoutCheck(entityHash), tr));
        }
        private void OnDataObjectInvisible(ProxyTransform tr)
        {
#if DEBUG_MODE
            if (!m_EntityGameObjects.TryGetValue(tr.m_Hash, out InstanceID eCheckHash) ||
                !m_ObjectEntities.ContainsKey(eCheckHash))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Internal EntitySystem error. ProxyTransform doesn\'t have entity.");
                return;
            }
#endif
            InstanceID entityHash = m_EntityGameObjects[tr.m_Hash];

            m_EventSystem.PostEvent<OnEntityVisibleEvent>(OnEntityVisibleEvent.GetEvent(
                Entity<IEntity>.GetEntityWithoutCheck(entityHash), tr));
        }
        private void M_ProxySystem_OnDataObjectProxyCreated(ProxyTransform tr, RecycleableMonobehaviour monoObj)
        {
#if DEBUG_MODE
            if (!m_EntityGameObjects.TryGetValue(tr.m_Hash, out InstanceID eCheckHash) ||
                !m_ObjectEntities.ContainsKey(eCheckHash))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Internal EntitySystem error. ProxyTransform doesn\'t have entity.");
                return;
            }
#endif
            InstanceID entityHash = m_EntityGameObjects[tr.m_Hash];
            IEntity entity = (IEntity)m_ObjectEntities[entityHash];

            monoObj.m_Entity = Entity<IEntity>.GetEntity(entity.Idx);
            ProcessEntityOnProxyCreated(this, entity, monoObj);
        }
        private void M_ProxySystem_OnDataObjectProxyRemoved(ProxyTransform tr, RecycleableMonobehaviour monoObj)
        {
            if (!m_EntityGameObjects.ContainsKey(tr.m_Hash)) return;

#if DEBUG_MODE
            if (!m_EntityGameObjects.TryGetValue(tr.m_Hash, out InstanceID eCheckHash) ||
                !m_ObjectEntities.ContainsKey(eCheckHash))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Internal EntitySystem error. ProxyTransform doesn\'t have entity.");
                return;
            }
#endif
            InstanceID entityHash = m_EntityGameObjects[tr.m_Hash];
            IEntity entity = (IEntity)m_ObjectEntities[entityHash];

            ProcessEntityOnProxyRemoved(this, entity, monoObj);
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
            m_SceneSystem.OnSceneChangeCalled += M_SceneSystem_OnSceneChangeCalled;
        }
        private void M_SceneSystem_OnSceneChangeCalled()
        {
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            foreach (var item in m_EntityProcessors)
            {
                for (int i = 0; i < item.Value.Count; i++)
                {
                    item.Value[i].OnInitialize();
                }
            }
            foreach (var item in m_AttributeProcessors)
            {
                for (int i = 0; i < item.Value.Count; i++)
                {
                    item.Value[i].OnInitialize();
                }
            }

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
            if (m_Queries.Count > 0)
            {
                int queryCount = m_Queries.Count;
                for (int i = 0; i < queryCount; i++)
                {
                    Query query = m_Queries.Dequeue();
                    var iter = query.GetEnumerator();
                    while (iter.MoveNext()) { }
                    query.Terminate();
                }
            }
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
        /// <inheritdoc cref="CreateEntity(in string, in float3, in quaternion, in float3, in bool)"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(in string name, in float3 position)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);
            if (!InternalEntityValidation(name, position, out EntityBase temp))
            {
                return Entity<IEntity>.Empty;
            }

            ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, quaternion.identity, 1);
            return InternalCreateEntity(in temp, in obj);
        }
        /// <summary>
        /// <inheritdoc cref="CreateEntity(in Hash, in float3, in quaternion, in float3, in bool)"/>
        /// </summary>
        /// <param name="hash"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(in Hash hash, in float3 position)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);
            if (!InternalEntityValidation(in hash, in position, out EntityBase temp))
            {
                return Entity<IEntity>.Empty;
            }

            ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, quaternion.identity, 1);
            return InternalCreateEntity(in temp, in obj);
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
            if (!InternalEntityValidation(in name, in position, out EntityBase temp))
            {
                return Entity<IEntity>.Empty;
            }

            ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, in rotation, in localSize);
            return InternalCreateEntity(in temp, in obj);
        }
        /// <summary>
        /// 엔티티를 생성합니다. <paramref name="hash"/>에는 <seealso cref="Reference"/>값으로 대체 가능합니다.
        /// </summary>
        /// <param name="hash"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="localSize"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(in Hash hash, in float3 position, in quaternion rotation, in float3 localSize)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);
            if (!InternalEntityValidation(in hash, in position, out EntityBase temp))
            {
                return Entity<IEntity>.Empty;
            }

            ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, in rotation, in localSize);
            return InternalCreateEntity(in temp, in obj);
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

        private ProxyTransform InternalCreateProxy(in EntityBase from,
            in PrefabReference<GameObject> prefab, in float3 pos, in quaternion rot, in float3 scale)
        {
#if DEBUG_MODE
            if (!prefab.IsNone() && !prefab.IsValid())
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"{from.Name} has an invalid prefab. This is not allowed.");
            }
#endif
            return m_ProxySystem.CreateNewPrefab(in prefab, in pos, in rot, in scale, from.m_EnableCull, from.Center, from.Size, from.StaticBatching);
        }
        private Entity<IEntity> InternalCreateEntity(in EntityBase entityBase, in ProxyTransform obj)
        {
            EntityBase entity = (EntityBase)entityBase.Clone();

            entity.transform = obj;
            entity.m_IsCreated = true;
            entity.m_HashCode = m_Random.NextInt(0, int.MaxValue);

            m_ObjectEntities.Add(entity.Idx, entity);

            m_EntityGameObjects.Add(obj.m_Hash, entity.Idx);

            ProcessEntityOnCreated(this, entity);
            return Entity<IEntity>.GetEntity(entity.Idx);
        }

        #endregion

        #region Create EntityDataBase

        /// <summary>
        /// 데이터 엔티티를 생성합니다. <paramref name="hash"/>는 <seealso cref="Reference"/> 값으로 대체 가능합니다.
        /// </summary>
        /// <param name="hash"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <returns></returns>
        public EntityData<IEntityData> CreateObject(Hash hash)
        {
            if (!InternalEntityDataValidation(hash, out EntityDataBase original))
            {
                return EntityData<IEntityData>.Empty;
            }
            return InternalCreateObject(original);
        }
        /// <summary>
        /// 데이터 엔티티를 생성합니다. <paramref name="name"/>은 <seealso cref="IEntityData.Name"/>입니다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public EntityData<IEntityData> CreateObject(string name)
        {
            if (!InternalEntityDataValidation(name, out EntityDataBase original))
            {
                return EntityData<IEntityData>.Empty;
            }
            return InternalCreateObject(original);
        }

        #region EntityData Validation
        private bool InternalEntityDataValidation(string name, out EntityDataBase entityData)
        {
            entityData = null;
            ObjectBase original = EntityDataList.Instance.GetObject(name);
            return InternalEntityDataValidation(name, original, out entityData);
        }
        private bool InternalEntityDataValidation(Hash hash, out EntityDataBase entityData)
        {
            entityData = null;
            ObjectBase original = EntityDataList.Instance.GetObject(hash);
            return InternalEntityDataValidation(hash.ToString(), original, out entityData);
        }
        private bool InternalEntityDataValidation(string name, ObjectBase original, out EntityDataBase entityData)
        {
#if DEBUG_MODE
            entityData = null;
            if (original == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_ObjectNotFoundError, name));
                return false;
            }
            if (original is EntityBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "You're creating entity with CreateObject method. This is not allowed.");
                return false;
            }
            else if (original is AttributeBase)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "This object is attribute and cannot be created. Request ignored.");
                return false;
            }
#endif
            entityData = (EntityDataBase)original;
            return true;
        }
        #endregion

        private EntityData<IEntityData> InternalCreateObject(EntityDataBase obj)
        {
            EntityDataBase objClone = (EntityDataBase)obj.Clone();
            objClone.m_IsCreated = true;
            objClone.m_HashCode = m_Random.NextInt(0, int.MaxValue);

            IEntityData clone = (IEntityData)objClone;

            m_ObjectEntities.Add(clone.Idx, objClone);

            ProcessEntityOnCreated(this, clone);
            return EntityData<IEntityData>.GetEntity(clone.Idx);
        }

        #endregion

        #region Create Instance

        internal Instance<T> CreateInstance<T>(Reference<T> obj) where T : class, IObject
            => CreateInstance<T>(obj.GetObject());
        internal Instance CreateInstance(Reference obj)
            => CreateInstance(obj.GetObject());
        internal Instance<T> CreateInstance<T>(IObject obj) where T : class, IObject
        {
#if DEBUG_MODE
            Type objType = obj.GetType();
            if (TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(objType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You should you {nameof(CreateEntity)} on create entity({obj.Name}). This will be slightly cared.");
                Entity<IEntity> entity = CreateEntity(obj.Hash, float3.zero);
                return new Instance<T>(entity.Idx);
            }
            else if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(objType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You should you {nameof(CreateObject)} on create entity({obj.Name}). This will be slightly cared.");
                EntityData<IEntityData> entity = CreateObject(obj.Hash);
                return new Instance<T>(entity.Idx);
            }
#endif
            ObjectBase clone = InternalCreateInstance(obj);

            return new Instance<T>(clone.Idx);
        }
        internal Instance CreateInstance(IObject obj)
        {
#if DEBUG_MODE
            Type objType = obj.GetType();
            if (TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(objType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You should you {nameof(CreateEntity)} on create entity({obj.Name}). This will be slightly cared but not in build.");
                Entity<IEntity> entity = CreateEntity(obj.Hash, float3.zero);
                return new Instance(entity.Idx);
            }
            else if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(objType))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"You should you {nameof(CreateObject)} on create entity({obj.Name}). This will be slightly cared but not in build.");
                EntityData<IEntityData> entity = CreateObject(obj.Hash);
                return new Instance(entity.Idx);
            }
#endif
            ObjectBase clone = InternalCreateInstance(obj);

            return new Instance(clone.Idx);
        }
        private ObjectBase InternalCreateInstance(IObject obj)
        {
            ObjectBase clone = (ObjectBase)obj.Clone();

            clone.m_HashCode = m_Random.NextInt(0, int.MaxValue);

            m_ObjectEntities.Add(clone.Idx, clone);
            if (clone is DataObjectBase dataObject)
            {
                dataObject.InternalOnCreated();
            }

            return clone;
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
        public void DestroyEntity(Entity<IEntity> entity) => InternalDestroyEntity(entity.Idx);
        /// <inheritdoc cref="DestroyEntity(Entity{IEntity})"/>
        public void DestroyEntity(EntityData<IEntityData> entity) => InternalDestroyEntity(entity.Idx);
        public void DestroyObject<T>(Instance<T> instance) where T : class, IObject => InternalDestroyEntity(instance.Idx);
        public void DestroyObject(Instance instance) => InternalDestroyEntity(instance.Idx);
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
            if (m_ObjectEntities[hash] is IEntityData entityData)
            {
                // M_ProxySystem_OnDataObjectDestroyAsync 에서 전부 핸들

                //ProcessEntityOnDestroy(this, entityData);

                //if (!CoreSystem.BlockCreateInstance && m_ObjectEntities[hash] is IEntity entity)
                //{
                //    if (entity.transform is ProxyTransform tr)
                //    {
                //        Hash index = tr.m_Hash;
                //        tr.Destroy();
                //        m_EntityGameObjects.Remove(index);
                //    }
                //    else if (entity.transform is UnityTransform unityTr)
                //    {
                //        UnityEngine.Object.Destroy(unityTr.provider.gameObject);
                //        ((IDisposable)unityTr).Dispose();
                //    }
                //}
            }
            else
            {
                //if (m_ObjectEntities[hash] is DataObjectBase dataObject)
                //{
                //    dataObject.InternalOnDestroy();
                //}

                //RemoveAllComponents(in hash);
            }

            m_DestroyedObjectsInThisFrame.Add(hash);
        }

        internal bool IsDestroyed(in InstanceID id) => IsDestroyed(id.Hash);
        internal bool IsDestroyed(in Hash idx)
        {
            return !m_ObjectEntities.ContainsKey(idx);
        }
        internal bool IsMarkedAsDestroyed(in InstanceID id) => IsMarkedAsDestroyed(id.Hash);
        internal bool IsMarkedAsDestroyed(in Hash idx)
        {
            return m_DestroyedObjectsInThisFrame.Contains(idx);
        }

        private static IEnumerable<Type> GetComponentInterface(Type t)
        {
            return t.GetInterfaces()
                .Where(i => i.IsGenericType)
                .Where(i => i.GetGenericTypeDefinition() == typeof(Components.INotifyComponent<>));
        }
        //private static IEnumerable<Type> GetComponentGenerics(Type t)
        //{
        //    return t.GetInterfaces()
        //        .Where(i => i.IsGenericType)
        //        .Where(i => i.GetGenericTypeDefinition() == typeof(Components.INotifyComponent<>))
        //        .Select(i => i.GetGenericArguments().First());
        //}

        #endregion

#line default

        private void RemoveAllComponents(in InstanceID hash)
        {
            var interfaceTypes = GetComponentInterface(m_ObjectEntities[hash].GetType());
            foreach (var interfaceType in interfaceTypes)
            {
                m_ComponentSystem.RemoveComponent(m_ObjectEntities[hash], interfaceType);
#if DEBUG_MODE
                Debug_RemoveComponent(m_ObjectEntities[hash], interfaceType.GetGenericArguments()[0]);
#endif
            }

//#if DEBUG_MODE
//            if (Debug_HasComponent(m_ObjectEntities[hash], out int count, out string names))
//            {
//                CoreSystem.Logger.LogError(Channel.Entity,
//                    $"Entity({m_ObjectEntities[hash].Name}) has " +
//                    $"number of {count} components that didn\'t disposed. {names}");
//            }
//#endif
        }

#if DEBUG_MODE
        private readonly Dictionary<InstanceID, List<Type>> m_AddedComponents = new Dictionary<InstanceID, List<Type>>();

        private bool Debug_HasComponent(ObjectBase entity, out int count, out string names)
        {
            if (m_AddedComponents.TryGetValue(entity.Idx, out var list))
            {
                count = list.Count;
                names = list[0].Name;
                for (int i = 1; i < list.Count; i++)
                {
                    names += $", {list[i].Name}";
                }

                return true;
            }

            count = 0;
            names = string.Empty;
            return false;
        }
        internal void Debug_AddComponent<TComponent>(EntityData<IEntityData> entity)
        {
            if (!m_AddedComponents.TryGetValue(entity.Idx, out var list))
            {
                list = new List<Type>();
                m_AddedComponents.Add(entity.Idx, list);
            }

            if (!list.Contains(TypeHelper.TypeOf<TComponent>.Type))
            {
                list.Add(TypeHelper.TypeOf<TComponent>.Type);
            }
        }
        internal void Debug_RemoveComponent<TComponent>(ObjectBase entity)
            => Debug_RemoveComponent(entity, TypeHelper.TypeOf<TComponent>.Type);
        internal void Debug_RemoveComponent(ObjectBase entity, Type component)
        {
            if (entity is Actor.ActorProviderBase actorProvider)
            {
                Debug_RemoveComponent(actorProvider.Parent, component);
                return;
            }

            if (!m_AddedComponents.TryGetValue(entity.Idx, out var list))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have component at all but trying to remove {component.Name}.");
                return;
            }

            if (!list.Contains(component))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have {component.Name}.");
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(component))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
            if (list.Count == 0) m_AddedComponents.Remove(entity.Idx);
        }
        internal void Debug_RemoveComponent<TComponent>(EntityData<IEntityData> entity)
            => Debug_RemoveComponent(entity, TypeHelper.TypeOf<TComponent>.Type);
        internal void Debug_RemoveComponent(EntityData<IEntityData> entity, Type component)
        {
            if (!m_AddedComponents.TryGetValue(entity.Idx, out var list))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.RawName}) doesn\'t have component at all.");
                return;
            }

            if (!list.Contains(component))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.RawName}) doesn\'t have {component.Name}.");
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(component))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
            if (list.Count == 0) m_AddedComponents.Remove(entity.Idx);
        }
#endif

        public int CreateHashCode() => m_Random.NextInt(int.MinValue, int.MaxValue);

        #region Processor
        private static void ProcessEntityOnCreated(EntitySystem system, IEntityData entity)
        {
            CoreSystem.Logger.Log(Channel.Entity,
                $"Create entity({entity.Name})");

            EntityData<IEntityData> entityData = EntityData<IEntityData>.GetEntityWithoutCheck(entity.Idx);

            #region Attributes
            for (int i = 0; i < entity.Attributes.Length; i++)
            {
                if (entity.Attributes[i] == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    return;
                }

                Type t = entity.Attributes[i].GetType();

                if (!TypeHelper.TypeOf<AttributeBase>.Type.Equals(t.BaseType))
                {
                    if (system.m_AttributeProcessors.TryGetValue(t.BaseType, out List<IAttributeProcessor> groupProcessors))
                    {
                        for (int j = 0; j < groupProcessors.Count; j++)
                        {
                            IAttributeProcessor processor = groupProcessors[j];

                            try
                            {
                                processor.OnCreated(entity.Attributes[i], entityData);
                            }
                            catch (Exception ex)
                            {
                                CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(IAttributeProcessor.OnCreated));
                            }
                        }
                        CoreSystem.Logger.Log(Channel.Entity, $"Processed OnCreated at entity({entity.Name}), {t.Name}");
                    }
                }

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        IAttributeProcessor processor = processors[j];

                        try
                        {
                            processor.OnCreated(entity.Attributes[i], entityData);
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(IAttributeProcessor.OnCreated));
                        }
                    }
                    CoreSystem.Logger.Log(Channel.Entity, $"Processed OnCreated at entity({entity.Name}), {t.Name}");
                }
            }
            #endregion

            #region Entity
            if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            {
                for (int i = 0; i < entityProcessor.Count; i++)
                {
                    IEntityDataProcessor processor = entityProcessor[i];

                    try
                    {
                        processor.OnCreated(entity);
                    }
                    catch (Exception ex)
                    {
                        CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(IEntityDataProcessor.OnCreated));
                    }
                }
            }
            #endregion

            system.OnEntityCreated?.Invoke(entityData);
        }

        private static void ProcessEntityOnPresentation(EntitySystem system, IEntityData entity)
        {
            EntityData<IEntityData> entityData = EntityData<IEntityData>.GetEntity(entity.Idx);

            //#region Entity
            //if (system.m_EntityProcessors.TryGetValue(t, out List<IEntityProcessor> entityProcessor))
            //{
            //    for (int i = 0; i < entityProcessor.Count; i++)
            //    {
            //        IEntityProcessor processor = entityProcessor[i];

            //    }
            //}
            //#endregion

            //#region Attributes
            //Array.ForEach(entity.Attributes, (other) =>
            //{
            //    if (other == null)
            //    {
            //        CoreSystem.Logger.LogWarning(Channel.Presentation,
            //            string.Format(c_AttributeEmptyWarning, entity.Name));
            //        return;
            //    }

            //    Type t = other.GetType();

            //    if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
            //    {
            //        for (int j = 0; j < processors.Count; j++)
            //        {
            //            if (!(processors[j] is IAttributeOnPresentation onPresentation)) continue;
            //            onPresentation.OnPresentation(other, entityData);
            //        }
            //    }
            //});
            //#endregion
        }
        private static void ProcessEntityOnDestroy(EntitySystem system, IEntityData entity)
        {
            CoreSystem.Logger.Log(Channel.Entity,
                $"Destroying entity({entity.Name})");

            EntityData<IEntityData> entityData = EntityData<IEntityData>.GetEntityWithoutCheck(entity.Idx);

            #region Attributes
            for (int i = 0; i < entity.Attributes.Length; i++)
            {
                AttributeBase other = entity.Attributes[i];
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    return;
                }

                Type t = other.GetType();

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        IAttributeProcessor processor = processors[j];

                        try
                        {
                            processor.OnDestroy(other, entityData);
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(IAttributeProcessor.OnDestroy));
                        }
                    }
                }

                if (other is Components.INotifyComponent notifyComponent)
                {
                    var interfaceTypes = GetComponentInterface(other.GetType());
                    foreach (var interfaceType in interfaceTypes)
                    {
                        Type componentType = interfaceType.GetGenericArguments()[0];
                        system.m_ComponentSystem.RemoveComponent(entityData, componentType);
#if DEBUG_MODE
                        system.Debug_RemoveComponent(entityData, componentType);
#endif
                    }
                }

                other.Dispose();
            }
            #endregion

            #region Entity
            if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            {
                for (int i = 0; i < entityProcessor.Count; i++)
                {
                    IEntityDataProcessor processor = entityProcessor[i];

                    try
                    {
                        processor.OnDestroy(entity);
                    }
                    catch (Exception ex)
                    {
                        CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(IEntityDataProcessor.OnDestroy));
                    }
                }
            }
            #endregion

            system.OnEntityDestroy?.Invoke(entityData);
        }

        private static void ProcessEntityOnProxyCreated(EntitySystem system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            CoreSystem.Logger.Log(Channel.Entity,
                $"Processing OnProxyCreated at {entity.Name}");

            if (system.IsDestroyed(entity.Idx) || system.IsMarkedAsDestroyed(entity.Idx))
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    $"Fast deletion at {entity.Name}. From {nameof(ProcessEntityOnProxyCreated)}");
                return;
            }

            Entity<IEntity> entityData = Entity<IEntity>.GetEntityWithoutCheck(entity.Idx);

            #region Entity
            if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            {
                for (int i = 0; i < entityProcessor.Count; i++)
                {
                    if (entityProcessor[i] is IEntityOnProxyCreated onProxyCreated)
                    {
                        try
                        {
                            onProxyCreated.OnProxyCreated((EntityBase)entity, entityData, monoObj);
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(IEntityOnProxyCreated.OnProxyCreated));
                        }
                    }
                }
            }
            #endregion

            #region Attributes
            for (int i = 0; i < entity.Attributes.Length; i++)
            {
                var other = entity.Attributes[i];
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    continue;
                }

                Type t = other.GetType();

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        if (processors[j] is IAttributeOnProxyCreated onProxyCreated)
                        {
                            try
                            {
                                onProxyCreated.OnProxyCreated(other, entityData, monoObj);
                            }
                            catch (Exception ex)
                            {
                                CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(IAttributeOnProxyCreated.OnProxyCreated));
                            }
                        }
                    }
                }
            }
            #endregion
        }
        private static void ProcessEntityOnProxyRemoved(EntitySystem system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            CoreSystem.Logger.Log(Channel.Entity,
                $"Processing OnProxyRemoved at {entity.Name}");

            if (system.IsDestroyed(entity.Idx) || system.IsMarkedAsDestroyed(entity.Idx))
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    $"Fast deletion at {entity.Name}. From {nameof(ProcessEntityOnProxyRemoved)}");
                return;
            }

            Entity<IEntity> entityData = Entity<IEntity>.GetEntityWithoutCheck(entity.Idx);

            #region Entity
            if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            {
                for (int i = 0; i < entityProcessor.Count; i++)
                {
                    if (entityProcessor[i] is IEntityOnProxyRemoved onProxyRemoved)
                    {
                        try
                        {
                            onProxyRemoved.OnProxyRemoved((EntityBase)entity, entityData, monoObj);
                        }
                        catch (Exception ex)
                        {
                            CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(IEntityOnProxyRemoved.OnProxyRemoved));
                        }
                    }
                }
            }
            #endregion

            #region Attributes
            for (int i = 0; i < entity.Attributes.Length; i++)
            {
                var other = entity.Attributes[i];
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                }

                Type t = other.GetType();

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        if (processors[j] is IAttributeOnProxyRemoved onProxyRemoved)
                        {
                            try
                            {
                                onProxyRemoved.OnProxyRemoved(other, entityData, monoObj);
                            }
                            catch (Exception ex)
                            {
                                CoreSystem.Logger.LogError(Channel.Entity, ex, nameof(IAttributeOnProxyRemoved.OnProxyRemoved));
                            }
                        }
                    }
                }
            }
            #endregion
        }

        #endregion

        #region Experiments

        /// <summary>
        /// 이미 생성된 유니티 게임 오브젝트를 엔티티 시스템로 편입시켜 엔티티로 변환하여 반환합니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="Entity{T}.transform"/> 은 <seealso cref="IUnityTransform"/>을 담습니다.
        /// </remarks>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Entity<ConvertedEntity> Convert(GameObject obj)
        {
            CoreSystem.Logger.ThreadBlock(nameof(Convert), ThreadInfo.Unity);

            ConvertedEntity temp = new ConvertedEntity
            {
                Name = obj.name,
                Hash = Hash.Empty
            };
            ConvertedEntity entity = (ConvertedEntity)temp.Clone();

            entity.transform = new UnityTransform
            {
                entity = entity,
                provider = obj.transform
            };

            ConvertedEntityComponent component = obj.AddComponent<ConvertedEntityComponent>();
            component.m_Entity = entity;

            entity.m_IsCreated = true;

            m_ObjectEntities.Add(entity.Idx, entity);

            ProcessEntityOnCreated(this, entity);
            return Entity<ConvertedEntity>.GetEntity(entity.Idx);
        }

        public sealed class Query : System.Collections.IEnumerable
        {
            static readonly Stack<Query> m_Pool = new Stack<Query>();

            readonly EntitySystem m_EntitySystem;
            readonly List<Reference> m_Has = new List<Reference>();
            readonly List<Reference> m_HasNot = new List<Reference>();

            EntityData<IEntityData> m_Entity;
            System.Collections.IEnumerator m_Enumerator;
            
            internal static Query Dequeue(EntitySystem entitySystem, EntityData<IEntityData> entity)
            {
                if (m_Pool.Count == 0) return new Query(entitySystem, entity);
                else return m_Pool.Pop();
            }
            private Query(EntitySystem entitySystem, EntityData<IEntityData> entity)
            {
                m_EntitySystem = entitySystem;
                m_Entity = entity;
            }
            internal void Terminate()
            {
                m_Has.Clear();
                m_HasNot.Clear();
                m_Enumerator = null;
                m_Entity = EntityData<IEntityData>.Empty;
                m_Pool.Push(this);
            }

            public Query Has(Reference attributeHash)
            {
                m_Has.Add(attributeHash);
                return this;
            }
            public Query HasNot(Reference attributeHash)
            {
                m_HasNot.Add(attributeHash);
                return this;
            }

            public void Schedule(Action action)
            {
                m_Enumerator = GetEnumerator(action);
                m_EntitySystem.m_Queries.Enqueue(this);
            }
            public void Schedule<T>(Action<T> action, T t)
            {
                m_Enumerator = GetEnumerator(action, t);
                m_EntitySystem.m_Queries.Enqueue(this);
            }
            public void Schedule<T0, T1>(Action<T0, T1> action, T0 t0, T1 t1)
            {
                m_Enumerator = GetEnumerator(action, t0, t1);
                m_EntitySystem.m_Queries.Enqueue(this);
            }

            #region Enumerator
            private System.Collections.IEnumerator GetEnumerator(Action action)
            {
                if (!IsExcutable()) yield break;

                try
                {
                    action.Invoke();
                }
                catch (Exception)
                {
                    yield break;
                }
            }
            private System.Collections.IEnumerator GetEnumerator<T>(Action<T> action, T t)
            {
                if (!IsExcutable()) yield break;

                try
                {
                    action.Invoke(t);
                }
                catch (Exception)
                {
                    yield break;
                }
            }
            private System.Collections.IEnumerator GetEnumerator<T0, T1>(Action<T0, T1> action, T0 t0, T1 t1)
            {
                if (!IsExcutable()) yield break;

                try
                {
                    action.Invoke(t0, t1);
                }
                catch (Exception)
                {
                    yield break;
                }
            }

            public System.Collections.IEnumerator GetEnumerator() => m_Enumerator;

            private bool IsExcutable()
            {
                for (int i = 0; i < m_Has.Count; i++)
                {
                    if (!m_Entity.HasAttribute(m_Has[i])) return false;
                }
                for (int i = 0; i < m_HasNot.Count; i++)
                {
                    if (m_Entity.HasAttribute(m_Has[i])) return false;
                }
                return true;
            }
            #endregion
        }
        /// <summary>
        /// [Experiment] 테스트 중인 기능입니다.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Query GetQuery(EntityData<IEntityData> entity) => Query.Dequeue(this, entity);

        #endregion
    }
}
