using MoonSharp.Interpreter;
using Syadeu.Database;
using Syadeu.Database.Lua;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class EntitySystem : PresentationSystemEntity<EntitySystem>
    {
        private const string c_ObjectNotFoundError = "Object({0}) not found.";
        private const string c_EntityNotFoundError = "Entity({0}) not found. Cannot spawn at {1}";
        private const string c_IsNotEntityError = "This object({0}) is not a entity. Use CreateObject instead";
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

        internal readonly Dictionary<Hash, IEntityData> m_ObjectEntities = new Dictionary<Hash, IEntityData>();
        internal readonly Dictionary<Hash, Hash> m_EntityGameObjects = new Dictionary<Hash, Hash>();

        private readonly Dictionary<Type, List<IAttributeProcessor>> m_AttributeProcessors = new Dictionary<Type, List<IAttributeProcessor>>();
        private readonly Dictionary<Type, List<IEntityDataProcessor>> m_EntityProcessors = new Dictionary<Type, List<IEntityDataProcessor>>();

        private readonly Queue<Query> m_Queries = new Queue<Query>();

        internal DataContainerSystem m_DataContainerSystem;
        internal GameObjectProxySystem m_ProxySystem;
        internal Events.EventSystem m_EventSystem;

        #region Presentation Methods
        protected override PresentationResult OnInitializeAsync()
        {
            RequestSystem<DataContainerSystem>(Bind);
            RequestSystem<GameObjectProxySystem>(Bind);
            RequestSystem<Events.EventSystem>(Bind);

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
            var entityList = m_ObjectEntities.Values.ToArray();
            for (int i = 0; i < entityList.Length; i++)
            {
                var entity = entityList[i];
                //EntityData<IEntityData> entityData = EntityData<IEntityData>.GetEntityData(entity.Idx);
                EntityData<IEntityData> entityData = new EntityData<IEntityData>(entity.Idx);

                CoreSystem.Logger.Log(Channel.Entity,
    $"Destroying entity({entity.Name})");

                #region Attributes
                Array.ForEach(entity.Attributes, (other) =>
                {
                    if (other == null)
                    {
                        CoreSystem.Logger.LogWarning(Channel.Presentation,
                            string.Format(c_AttributeEmptyWarning, entity.Name));
                        return;
                    }

                    Type t = other.GetType();

                    if (m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                    {
                        for (int j = 0; j < processors.Count; j++)
                        {
                            IAttributeProcessor processor = processors[j];

                            processor.OnDestroy(other, entityData);

                            ((IDisposable)processor).Dispose();
                        }
                    }
                });
                #endregion

                #region Entity

                if (m_ObjectEntities.ContainsKey(entityData.Idx))
                {
                    if (m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
                    {
                        for (int j = 0; j < entityProcessor.Count; j++)
                        {
                            IEntityDataProcessor processor = entityProcessor[j];

                            processor.OnDestroy(entityData);
                        }
                    }

                    OnEntityDestroy?.Invoke(entityData);
                }

                #endregion

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
            }

            OnEntityCreated = null;
            OnEntityDestroy = null;

            m_ObjectEntities.Clear();
            m_AttributeProcessors.Clear();
            m_EntityProcessors.Clear();

            m_ProxySystem.OnDataObjectDestroy -= M_ProxySystem_OnDataObjectDestroyAsync;

            m_ProxySystem.OnDataObjectProxyCreated -= M_ProxySystem_OnDataObjectProxyCreated;
            m_ProxySystem.OnDataObjectProxyRemoved -= M_ProxySystem_OnDataObjectProxyRemoved;
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

            m_ProxySystem.OnDataObjectProxyCreated += M_ProxySystem_OnDataObjectProxyCreated;
            m_ProxySystem.OnDataObjectProxyRemoved += M_ProxySystem_OnDataObjectProxyRemoved;
        }
        private void Bind(Events.EventSystem other)
        {
            m_EventSystem = other;
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

            return base.OnStartPresentation();
        }

        private void M_ProxySystem_OnDataObjectProxyCreated(ProxyTransform obj, RecycleableMonobehaviour monoObj)
        {
            if (!m_EntityGameObjects.TryGetValue(obj.m_Hash, out Hash entityHash) ||
                !(m_ObjectEntities[entityHash] is IEntity entity)) return;

            monoObj.m_Entity = Entity<IEntity>.GetEntity(entity.Idx);
            ProcessEntityOnProxyCreated(this, entity, monoObj);
        }
        private void M_ProxySystem_OnDataObjectProxyRemoved(ProxyTransform obj, RecycleableMonobehaviour monoObj)
        {
            if (!m_EntityGameObjects.TryGetValue(obj.m_Hash, out Hash entityHash) ||
                !(m_ObjectEntities[entityHash] is IEntity entity)) return;

            ProcessEntityOnProxyRemoved(this, entity, monoObj);
            monoObj.m_Entity = Entity<IEntity>.Empty;
        }

        private void M_ProxySystem_OnDataObjectDestroyAsync(ProxyTransform obj)
        {
            if (!m_EntityGameObjects.TryGetValue(obj.m_Hash, out Hash entityHash) ||
                !m_ObjectEntities.ContainsKey(entityHash)) return;

            ProcessEntityOnDestroy(this, m_ObjectEntities[entityHash]);

            m_EntityGameObjects.Remove(obj.m_Hash);
            m_ObjectEntities.Remove(entityHash);
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

        public Entity<IEntity> CreateEntity(in string name, in float3 position)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);
            InternalEntityValidation(name, position, out EntityBase temp);

            ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, quaternion.identity, 1, true);
            return InternalCreateEntity(in temp, in obj);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(in Hash hash, in float3 position)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);
            InternalEntityValidation(in hash, in position, out EntityBase temp);

            ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, quaternion.identity, 1, true);
            return InternalCreateEntity(in temp, in obj);
        }
        public Entity<IEntity> CreateEntity(in string name, in float3 position, in quaternion rotation, in float3 localSize, in bool enableCull)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);
            InternalEntityValidation(in name, in position, out EntityBase temp);

            ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, in rotation, in localSize, in enableCull);
            return InternalCreateEntity(in temp, in obj);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="localSize"></param>
        /// <param name="enableCull"></param>
        /// <returns></returns>
        public Entity<IEntity> CreateEntity(in Hash hash, in float3 position, in quaternion rotation, in float3 localSize, in bool enableCull)
        {
            CoreSystem.Logger.ThreadBlock(nameof(CreateEntity), ThreadInfo.Unity);
            InternalEntityValidation(in hash, in position, out EntityBase temp);

            ProxyTransform obj = InternalCreateProxy(in temp, temp.Prefab, in position, in rotation, in localSize, in enableCull);
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
            entity = (EntityBase)original;
            return true;
        }
        #endregion
        private ProxyTransform InternalCreateProxy(in EntityBase from,
            in PrefabReference prefab, in float3 pos, in quaternion rot, in float3 scale, in bool enableCull)
        {
            if (!prefab.IsValid())
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Presentation,
                    $"{from.Name} has an invalid prefab. This is not allowed.");
            }

            return m_ProxySystem.CreateNewPrefab(in prefab, in pos, in rot, in scale, from.m_EnableCull, from.Center, from.Size);
        }
        private Entity<IEntity> InternalCreateEntity(in EntityBase entityBase, in ProxyTransform obj)
        {
            EntityBase entity = (EntityBase)entityBase.Clone();

            entity.transform = obj;
            entity.m_IsCreated = true;

            m_ObjectEntities.Add(entity.Idx, entity);

            m_EntityGameObjects.Add(obj.m_Hash, entity.Idx);

            ProcessEntityOnCreated(this, entity);
            return Entity<IEntity>.GetEntity(entity.Idx);
        }
        #endregion

        #region Create EntityDataBase

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"><seealso cref="IEntityData.Hash"/> 값</param>
        /// <returns></returns>
        public EntityData<IEntityData> CreateObject(Hash hash)
        {
            InternalEntityDataValidation(hash, out EntityDataBase original);
            return InternalCreateObject(original);
        }
        public EntityData<IEntityData> CreateObject(string name)
        {
            InternalEntityDataValidation(name, out EntityDataBase original);
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
            entityData = (EntityDataBase)original;
            return true;
        }
        #endregion

        private EntityData<IEntityData> InternalCreateObject(EntityDataBase obj)
        {
            EntityDataBase objClone = (EntityDataBase)obj.Clone();
            objClone.m_IsCreated = true;

            IEntityData clone = (IEntityData)objClone;

            m_ObjectEntities.Add(clone.Idx, clone);

            ProcessEntityOnCreated(this, clone);
            return EntityData<IEntityData>.GetEntityData(clone.Idx);
        }

        #endregion

        /// <summary>
        /// 해당 엔티티를 즉시 파괴합니다.
        /// </summary>
        /// <remarks>
        /// 씬이 전환되는 경우, 해당 씬에서 생성된 <see cref="EntityBase"/>는 자동으로 파괴되므로 호출하지 마세요. 단, <see cref="EntityDataBase"/>(<seealso cref="DataGameObject"/>가 없는 엔티티)는 씬이 전환되어도 자동으로 파괴되지 않습니다.
        /// </remarks>
        /// <param name="hash"><seealso cref="IEntityData.Idx"/> 값</param>
        public void DestroyEntity(Entity<IEntity> entity) => InternalDestroyEntity(entity.Idx);
        /// <inheritdoc cref="DestroyEntity(Entity{IEntity})"/>
        public void DestroyEntity(EntityData<IEntityData> entity) => InternalDestroyEntity(entity.Idx);
        internal void InternalDestroyEntity(in Hash hash)
        {
            if (!m_ObjectEntities.ContainsKey(hash)) throw new Exception();

            ProcessEntityOnDestroy(this, m_ObjectEntities[hash]);

            if (!CoreSystem.s_BlockCreateInstance && m_ObjectEntities[hash] is IEntity entity)
            {
                ProxyTransform obj = entity.transform;
                Hash index = obj.m_Hash;
                obj.Destroy();
                m_EntityGameObjects.Remove(index);
            }

            ((IDisposable)m_ObjectEntities[hash]).Dispose();
            m_ObjectEntities.Remove(hash);
        }

#line default

        #region Processor
        private static void ProcessEntityOnCreated(EntitySystem system, IEntityData entity)
        {
            CoreSystem.Logger.Log(Channel.Entity,
                $"Create entity({entity.Name})");

            EntityData<IEntityData> entityData = EntityData<IEntityData>.GetEntityData(entity.Idx);

            #region Attributes
            Array.ForEach(entity.Attributes, (other) =>
            {
                if (other == null)
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation,
                        string.Format(c_AttributeEmptyWarning, entity.Name));
                    return;
                }

                Type t = other.GetType();

                if (!TypeHelper.TypeOf<AttributeBase>.Type.Equals(t.BaseType))
                {
                    if (system.m_AttributeProcessors.TryGetValue(t.BaseType, out List<IAttributeProcessor> groupProcessors))
                    {
                        for (int j = 0; j < groupProcessors.Count; j++)
                        {
                            IAttributeProcessor processor = groupProcessors[j];

                            processor.OnCreated(other, entityData);
                        }
                        CoreSystem.Logger.Log(Channel.Entity, $"Processed OnCreated at entity({entity.Name}), {t.Name}");
                    }
                }

                if (system.m_AttributeProcessors.TryGetValue(t, out List<IAttributeProcessor> processors))
                {
                    for (int j = 0; j < processors.Count; j++)
                    {
                        IAttributeProcessor processor = processors[j];

                        processor.OnCreated(other, entityData);
                    }
                    CoreSystem.Logger.Log(Channel.Entity, $"Processed OnCreated at entity({entity.Name}), {t.Name}");
                }
            });
            #endregion

            #region Entity
            if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            {
                for (int i = 0; i < entityProcessor.Count; i++)
                {
                    IEntityDataProcessor processor = entityProcessor[i];

                    processor.OnCreated(entityData);
                }
            }
            #endregion

            system.OnEntityCreated?.Invoke(entityData);
        }
        private static void ProcessEntityOnPresentation(EntitySystem system, IEntityData entity)
        {
            EntityData<IEntityData> entityData = EntityData<IEntityData>.GetEntityData(entity.Idx);

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

            EntityData<IEntityData> entityData = EntityData<IEntityData>.GetEntityData(entity.Idx);

            #region Attributes
            Array.ForEach(entity.Attributes, (other) =>
            {
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

                        processor.OnDestroy(other, entityData);
                    }
                }

                other.Dispose();
            });
            #endregion

            #region Entity
            if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            {
                for (int i = 0; i < entityProcessor.Count; i++)
                {
                    IEntityDataProcessor processor = entityProcessor[i];

                    processor.OnDestroy(entityData);
                }
            }
            #endregion

            system.OnEntityDestroy?.Invoke(entityData);
        }

        private static void ProcessEntityOnProxyCreated(EntitySystem system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            CoreSystem.Logger.Log(Channel.Entity,
                $"Processing OnProxyCreated at {entity.Name}");

            Entity<IEntity> entityData = Entity<IEntity>.GetEntity(entity.Idx);

            //#region Entity
            //if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            //{
            //    for (int i = 0; i < entityProcessor.Count; i++)
            //    {
            //        IEntityDataProcessor processor = entityProcessor[i];

            //    }
            //}
            //#endregion

            #region Attributes
            Array.ForEach(entity.Attributes, (other) =>
            {
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
                        if (processors[j] is IAttributeOnProxyCreated onProxyCreated)
                        {
                            onProxyCreated.OnProxyCreated(other, entityData, monoObj);
                        }
                    }
                }
            });
            #endregion
        }
        private static void ProcessEntityOnProxyRemoved(EntitySystem system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            CoreSystem.Logger.Log(Channel.Entity,
                $"Processing OnProxyRemoved at {entity.Name}");

            Entity<IEntity> entityData = Entity<IEntity>.GetEntity(entity.Idx);

            //#region Entity
            //if (system.m_EntityProcessors.TryGetValue(entity.GetType(), out List<IEntityDataProcessor> entityProcessor))
            //{
            //    for (int i = 0; i < entityProcessor.Count; i++)
            //    {
            //        IEntityDataProcessor processor = entityProcessor[i];

            //    }
            //}
            //#endregion

            #region Attributes
            Array.ForEach(entity.Attributes, (other) =>
            {
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
                            onProxyRemoved.OnProxyRemoved(other, entityData, monoObj);
                        }
                    }
                }
            });
            #endregion
        }

        #endregion

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
        public Query GetQuery(EntityData<IEntityData> entity) => Query.Dequeue(this, entity);

        #region Raycast
        public Raycaster Raycast(Entity<IEntity> from, Ray ray)
        {
            NativeArray<Hash> keys = new NativeArray<Hash>(PresentationSystem<EntitySystem>.System.m_EntityGameObjects.Values.ToArray(), Allocator.TempJob);

            NativeList<RaycastHitInfo> hits = new NativeList<RaycastHitInfo>(64, Allocator.Persistent);

            RaycastJob job = new RaycastJob(from, keys, ray, hits);
            return new Raycaster(job.Schedule(keys.Length, 64), hits);
        }

        public class Raycaster : IDisposable
        {
            private readonly JobHandle m_Job;
            private readonly NativeList<RaycastHitInfo> m_Hits;

            public bool JobCompleted => m_Job.IsCompleted;
            public bool Hit
            {
                get
                {
                    m_Job.Complete();
                    if (m_Hits.Length > 0) return true;
                    return false;
                }
            }
            public RaycastHitInfo Target
            {
                get
                {
                    m_Job.Complete();

                    RaycastHitInfo temp = default;
                    float dis = float.MaxValue;
                    for (int i = 0; i < m_Hits.Length; i++)
                    {
                        if (m_Hits[i].Distance < dis)
                        {
                            dis = m_Hits[i].Distance;
                            temp = m_Hits[i];
                        }
                    }

                    return temp;
                }
            }
            public RaycastHitInfo[] Targets
            {
                get
                {
                    m_Job.Complete();

                    return m_Hits.ToArray();
                }
            }

            internal Raycaster(JobHandle job, NativeList<RaycastHitInfo> hits)
            {
                m_Job = job;
                m_Hits = hits;
            }
            ~Raycaster()
            {
                Dispose();
            }

            public void Dispose()
            {
                m_Hits.Dispose();
            }
        }
        public struct RaycastHitInfo
        {
            private readonly Entity<IEntity> m_Entity;
            private readonly float m_Distance;
            private readonly float3 m_Point;

            public Entity<IEntity> Entity => m_Entity;
            public float Distance => m_Distance;
            public float3 Point => m_Point;

            internal RaycastHitInfo(Entity<IEntity> entity, float dis, float3 point)
            {
                m_Entity = entity;
                m_Distance = dis;
                m_Point = point;
            }
        }
        private struct RaycastJob : IJobParallelFor
        {
            [ReadOnly] private readonly Entity<IEntity> m_From;
            [ReadOnly] private readonly Ray m_Ray;

            [DeallocateOnJobCompletion]
            private readonly NativeArray<Hash> m_Keys;

            private readonly NativeList<RaycastHitInfo>.ParallelWriter m_Hits;

            public RaycastJob(Entity<IEntity> from, NativeArray<Hash> keys, Ray ray, NativeList<RaycastHitInfo> hits)
            {
                m_From = from;
                m_Keys = keys;
                m_Ray = ray;

                m_Hits = hits.AsParallelWriter();
            }

            public void Execute(int index)
            {
                if (m_Keys[index].Equals(m_From.Idx)) return;

                Hash key = m_Keys[index];
                Entity<IEntity> target = Entity<IEntity>.GetEntity(key);

                var targetAABB = target.transform.aabb;
                if (targetAABB.Intersect(m_Ray, out float dis, out float3 point))
                {
                    m_Hits.AddNoResize(new RaycastHitInfo(target, dis, point));
                }
            }
        }
        #endregion
    }
}
