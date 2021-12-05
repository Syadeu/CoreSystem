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


using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Syadeu.Presentation
{
    internal sealed class EntityProcessorModule : PresentationSystemModule<EntitySystem>
    {
        private static Unity.Profiling.ProfilerMarker
            m_ProcessEntityOnCreateMarker = new Unity.Profiling.ProfilerMarker($"{nameof(EntityProcessorModule)}.{nameof(ProcessEntityOnCreated)}"),
            m_ProcessEntityOnDestoryMarker = new Unity.Profiling.ProfilerMarker($"{nameof(EntityProcessorModule)}.{nameof(ProcessEntityOnDestroy)}");

        private const string 
            c_AttributeWarning = "Attribute({0}) on entity({1}) has invaild value. {2}. Request Ignored.",
            c_AttributeEmptyWarning = "Entity({0}) has empty attribute. This is not allowed. Request Ignored.";

        /// <summary>
        /// 엔티티가 생성될때 실행되는 이벤트 delegate 입니다.
        /// </summary>
        /// <remarks>
        /// 모든 프로세서가 동작한 후, 맨 마지막에 실행됩니다.
        /// </remarks>
        public event Action<IEntityData> OnEntityCreated;
        /// <summary>
        /// 엔티티가 파괴될때 실행되는 이벤트 delegate 입니다.
        /// </summary>
        /// <remarks>
        /// 모든 프로세서가 동작한 후, 맨 마지막에 실행됩니다.
        /// </remarks>
        public event Action<IEntityData> OnEntityDestroy;

        private readonly Dictionary<Type, List<IAttributeProcessor>> 
            m_AttributeProcessors = new Dictionary<Type, List<IAttributeProcessor>>();
        private readonly Dictionary<Type, List<IEntityDataProcessor>> 
            m_EntityProcessors = new Dictionary<Type, List<IEntityDataProcessor>>();

        private Components.EntityComponentSystem m_ComponentSystem;

        #region Presentation Methods

        protected override void OnInitializeAsync()
        {


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
                baseProcessor.m_EntitySystem = System;

                processor.OnInitializeAsync();
            }
        }
        static bool ProcessorPredicate(Type other) => !other.IsAbstract && !other.IsInterface && TypeHelper.TypeOf<IProcessor>.Type.IsAssignableFrom(other);

        private void Bind(Components.EntityComponentSystem other)
        {
            m_ComponentSystem = other;
        }

        protected override void OnShutDown()
        {
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
        protected override void OnDispose()
        {
            m_AttributeProcessors.Clear();
            m_EntityProcessors.Clear();

            OnEntityCreated = null;
            OnEntityDestroy = null;

            m_ComponentSystem = null;
        }

        protected override void OnStartPresentation()
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
        }

        #endregion

        public static void ProcessEntityOnCreated(EntityProcessorModule system, IEntityData entity)
        {
            const string c_CreateStartMsg = "Create entity({0})";

            m_ProcessEntityOnCreateMarker.Begin();

            CoreSystem.Logger.Log(Channel.Entity,
                string.Format(c_CreateStartMsg, entity.Name));

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

            system.OnEntityCreated?.Invoke(entity);

            m_ProcessEntityOnCreateMarker.End();
        }
        public static void ProcessEntityOnPresentation(EntityProcessorModule system, IEntityData entity)
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
        public static void ProcessEntityOnDestroy(EntityProcessorModule system, IEntityData entity, InstanceID insID)
        {
            const string c_DestroyStartMsg = "Destroying entity({0})";

            m_ProcessEntityOnDestoryMarker.Begin();

            CoreSystem.Logger.Log(Channel.Entity,
                string.Format(c_DestroyStartMsg, entity.Name));

            EntityData<IEntityData> entityData = EntityData<IEntityData>.GetEntityWithoutCheck(entity.Idx);

            #region Attributes
            for (int i = 0; i < entity.Attributes.Length; i++)
            {
                IAttribute other = entity.Attributes[i];
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


                system.m_ComponentSystem
                    .RemoveNotifiedComponents(
                        other, insID
#if DEBUG_MODE
                        //, system.Debug_RemoveComponent
#endif
                    );
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

            system.OnEntityDestroy?.Invoke(entity);

            //for (int i = 0; i < entity.Attributes.Length; i++)
            //{
            //    IAttribute other = entity.Attributes[i];

            //    //other.Dispose();
            //    system.GetModule<EntityRecycleModule>().InsertReservedObject(entity.Attributes[i]);
            //}

            m_ProcessEntityOnDestoryMarker.End();
        }

        public static void ProcessEntityOnProxyCreated(EntityProcessorModule system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            const string c_StartMsg = "Processing OnProxyCreated at {0}";
            const string c_FastDeletionMsg = "Fast deletion at {0}. From ProcessEntityOnProxyCreated";

            CoreSystem.Logger.Log(Channel.Entity,
                string.Format(c_StartMsg, entity.Name));

            if (system.System.IsDestroyed(entity.Idx) || system.System.IsMarkedAsDestroyed(entity.Idx))
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_FastDeletionMsg, entity.Name));
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
        public static void ProcessEntityOnProxyRemoved(EntityProcessorModule system, IEntity entity, RecycleableMonobehaviour monoObj)
        {
            const string c_StartMsg = "Processing OnProxyRemoved at {0}";
            const string c_FastDeletionMsg = "Fast deletion at {0}. From ProcessEntityOnProxyRemoved";

            CoreSystem.Logger.Log(Channel.Entity,
                string.Format(c_StartMsg, entity.Name));

            if (system.System.IsDestroyed(entity.Idx) || system.System.IsMarkedAsDestroyed(entity.Idx))
            {
                CoreSystem.Logger.LogWarning(Channel.Entity,
                    string.Format(c_FastDeletionMsg, entity.Name));
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
    }
}
