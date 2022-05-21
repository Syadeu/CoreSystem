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
using Syadeu.Collections.LowLevel;
using Syadeu.Collections.Threading;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using TypeInfo = Syadeu.Collections.TypeInfo;

namespace Syadeu.Presentation.Components
{
    /// <summary>
    /// <see cref="EntityData{T}"/> 컴포넌트 시스템입니다.
    /// </summary>
    /// <remarks>
    /// 컴포넌트는 Dispose 패턴을 따르며, <seealso cref="IDisposable"/> 를 컴포넌트가 상속받고 있으면
    /// 체크하여 해당 컴포넌트가 제거될 시 수행됩니다. <seealso cref="INotifyComponent{TComponent}"/> 를 통해
    /// 상속받는 <seealso cref="ObjectBase"/> 가 파괴될 시 같이 파괴되도록 수행할 수 있습니다.<br/>
    /// <br/>
    /// 사용자는 직접 이 시스템을 통하여 컴포넌트 관련 작업을 수행하는 것이 아닌, <seealso cref="EntityData{T}.AddComponent{TComponent}(in TComponent)"/>
    /// 등과 같은 간접 메소드를 통해 작업을 수행할 수 있습니다. 자세한 기능은 <seealso cref="EntityData{T}"/> 를 참조하세요.
    /// </remarks>
    unsafe internal sealed class EntityComponentSystem : PresentationSystemEntity<EntityComponentSystem>,
        INotifySystemModule<EntityNotifiedComponentModule>,
        INotifySystemModule<EntityComponentProcessorModule>
#if DEBUG_MODE
        , INotifySystemModule<EntityComponentDebugModule>
#endif
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;
        public override bool EnableAfterTransformPresentation => true;

        private NativeArray<ComponentBuffer> m_ComponentArrayBuffer;
        private NativeArray<EntityComponentBuffer> m_ECBBuffer;
        private UnsafeRingQueue<int> m_RequestedECBComponentIndices;

        /// <summary>
        /// Key => <see cref="ComponentBuffer.TypeInfo"/>.GetHashCode()<br/>
        /// Value => <see cref="ComponentBuffer.m_ComponentBuffer"/> index
        /// </summary>
        private UnsafeMultiHashMap<int, int> m_ComponentHashMap;

        private AtomicOperator m_ComponentAccessOp;

        private static Unity.Profiling.ProfilerMarker
            s_AddComponentMarker = new Unity.Profiling.ProfilerMarker("EntityComponentSystem.set_AddComponent"),
            s_RemoveComponentMarker = new Unity.Profiling.ProfilerMarker("EntityComponentSystem.set_RemoveComponent"),
            s_RemoveNotifiedComponentMarker = new Unity.Profiling.ProfilerMarker("EntityComponentSystem.set_RemoveNotifiedComponent"),
            s_GetComponentMarker = new Unity.Profiling.ProfilerMarker("EntityComponentSystem.get_GetComponent"),
            s_GetComponentReadOnlyMarker = new Unity.Profiling.ProfilerMarker("EntityComponentSystem.get_GetComponentReadOnly"),
            s_GetComponentPointerMarker = new Unity.Profiling.ProfilerMarker("EntityComponentSystem.get_GetComponentPointer");

        private ActionWrapper m_CompleteAllDisposedComponents;

        /// <summary>
        /// 해당 타입의 컴포넌트가 <see cref="InstanceID"/> 에 추가되었을 때 발생하는 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 단일 컴포넌트에 대한 이벤트는 여기에 적합하지 않습니다. <seealso cref="ComponentProcessor{TComponent}"/> 를 참조하세요.
        /// </remarks>
        public event Action<InstanceID, Type> OnComponentAdded;
        /// <summary>
        /// 해당 타입의 컴포넌트가 <see cref="InstanceID"/> 에서 제거되었을 때 발생하는 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 단일 컴포넌트에 대한 이벤트는 여기에 적합하지 않습니다. <seealso cref="ComponentProcessor{TComponent}"/> 를 참조하세요.
        /// </remarks>
        public event Action<InstanceID, Type> OnComponentRemove;

        public int BufferLength => m_ComponentArrayBuffer.Length;

        private EntitySystem m_EntitySystem;
        private SceneSystem m_SceneSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            // Type Hashing 을 위해 랜덤 생성자를 만듭니다.
            // 왜인지 모르겠지만 간혈적으로 Type.GetHashCode() 가 0 을 반환하여, 직접 값을 만듭니다.
            //m_Random = new Unity.Mathematics.Random();
            //m_Random.InitState();
            EntityComponentExtensionMethods.s_ComponentSystem = this;
            m_ComponentAccessOp = new AtomicOperator();

            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);

            // 버퍼를 생성하기 위해 미리 모든 컴포넌트 타입들의 정보를 가져옵니다.
            Type[] types = TypeHelper.GetTypes(CollectTypes<IEntityComponent>);

            // Hashing 을 위해 최소 버퍼 사이즈인 512 값보다 낮으면 강제로 버퍼 사이즈를 1024 로 맞춥니다.
            int length;
            if (types.Length < 512)
            {
                length = 1024;
            }
            else length = types.Length * 2;

            m_ComponentHashMap = new UnsafeMultiHashMap<int, int>(length, AllocatorManager.Persistent);
            ref UntypedUnsafeHashMap hashMap =
                ref UnsafeUtility.As<UnsafeMultiHashMap<int, int>, UntypedUnsafeHashMap>(ref m_ComponentHashMap);
            UntypedUnsafeHashMap* hashMapPointer = (UntypedUnsafeHashMap*)UnsafeUtility.AddressOf(ref hashMap);

            m_ECBBuffer = new NativeArray<EntityComponentBuffer>(types.Length, Allocator.Persistent);
            EntityComponentBuffer* ecbPointer = (EntityComponentBuffer*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_ECBBuffer);

            m_RequestedECBComponentIndices = new UnsafeRingQueue<int>(64, AllocatorManager.Persistent);
            UnsafeRingQueue<int>* requestECBPointer = (UnsafeRingQueue<int>*)UnsafeUtility.AddressOf(ref m_RequestedECBComponentIndices);

            m_ComponentArrayBuffer = new NativeArray<ComponentBuffer>(length, Allocator.Persistent);
            ComponentBuffer* readPtr = ((ComponentBuffer*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_ComponentArrayBuffer));
            for (int i = 0; i < types.Length; i++)
            {
                ComponentBuffer buffer = BuildComponentBuffer(ecbPointer + i, types[i], length, out int idx);
                m_ComponentArrayBuffer[idx] = buffer;

                ref ComponentType data = ref ComponentType.GetValue(types[i]).Data;
                data.m_ComponentECBRequester = requestECBPointer;

                data.m_ComponentBuffer = readPtr + idx;
                data.m_ComponentIndex = idx;
            }

            ComponentDisposer.Initialize(
                (ComponentBuffer*)m_ComponentArrayBuffer.GetUnsafePtr(),
                hashMapPointer);

            m_CompleteAllDisposedComponents = ActionWrapper.GetWrapper();
            m_CompleteAllDisposedComponents.SetProfiler($"{nameof(EntityComponentSystem)}.{nameof(CompleteAllDisposedComponents)}");
            m_CompleteAllDisposedComponents.SetAction(CompleteAllDisposedComponents);

            PresentationManager.Instance.PreUpdate += m_CompleteAllDisposedComponents.Invoke;

            return base.OnInitialize();
        }
        private ComponentBuffer BuildComponentBuffer(EntityComponentBuffer* ecb, Type componentType, in int totalLength, out int idx)
        {
            TypeInfo typeInfo = componentType.ToTypeInfo();
            int hashCode = typeInfo.GetHashCode();
            idx = math.abs(hashCode) % totalLength;

            if (TypeHelper.IsZeroSizeStruct(componentType))
            {
                CoreSystem.Logger.LogError(LogChannel.Component,
                    $"Zero sized wrapper struct({TypeHelper.ToString(componentType)}) is in component list.");
            }
            
            if (/*!UnsafeUtility.IsBlittable(componentType) ||*/ !UnsafeUtility.IsUnmanaged(componentType))
            {
                CoreSystem.Logger.LogError(LogChannel.Component,
                    $"managed struct({TypeHelper.ToString(componentType)}) is in component list.");

                return default(ComponentBuffer);
            }

            ComponentBuffer temp = new ComponentBuffer();            
            // 새로운 버퍼를 생성하고, heap 에 메모리를 할당합니다.
            temp.Initialize(ecb, typeInfo);

            return temp;
        }

        protected override void OnDispose()
        {
            PresentationManager.Instance.PreUpdate -= m_CompleteAllDisposedComponents.Invoke;
            m_CompleteAllDisposedComponents.Reserve();
            m_CompleteAllDisposedComponents = null;

            m_SceneSystem.OnSceneChanged -= CompleteAllDisposedComponents;

            for (int i = 0; i < m_ComponentArrayBuffer.Length; i++)
            {
                if (!m_ComponentArrayBuffer[i].IsCreated) continue;

                m_ComponentArrayBuffer[i].Dispose();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                UnsafeAtomicSafety safety
                    = CLSTypedDictionary<UnsafeAtomicSafety>.GetValue(m_ComponentArrayBuffer[i].TypeInfo.Type);

                if (!safety.Disposed)
                {
                    if (!CoreSystem.BlockCreateInstance)
                    {
                        CoreSystem.Logger.LogError(LogChannel.Component,
                            $"Component({TypeHelper.ToString(m_ComponentArrayBuffer[i].TypeInfo.Type)}) buffer has not safely disposed.");
                    }

                    safety.Dispose();
                }
#endif

                ComponentType.GetValue(m_ComponentArrayBuffer[i].TypeInfo.Type).Data.m_ComponentBuffer = null;
            }
            m_ComponentArrayBuffer.Dispose();
            m_ECBBuffer.Dispose();
            m_RequestedECBComponentIndices.Dispose();

            m_ComponentHashMap.Dispose();

            EntityComponentExtensionMethods.s_ComponentSystem = null;
            m_EntitySystem = null;
            m_SceneSystem = null;
        }

        #region Binds

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
        }
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
            m_SceneSystem.OnSceneChanged += CompleteAllDisposedComponents;
        }

        #endregion

        private void CompleteAllDisposedComponents()
        {
            //if (m_DisposedComponents.Count > 0)
            //{
            //    // Parallel Job 이 수행되고 있는 중에 컴포넌트가 제거되면 안되므로
            //    // 먼저 모든 Job 을 완료합니다.
            //    IJobParallelForEntitiesExtensions.CompleteAllJobs();

            //    int count = m_DisposedComponents.Count;
            //    for (int i = 0; i < count; i++)
            //    {
            //        m_DisposedComponents.Dequeue().Dispose();
            //    }
            //}
        }

        protected override PresentationResult AfterTransformPresentation()
        {
            ComponentBuffer* componentBuffer = (ComponentBuffer*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_ComponentArrayBuffer);

            int requestedEcbCount = m_RequestedECBComponentIndices.Length;
            for (int i = 0; i < requestedEcbCount; i++)
            {
                int componentIdx = m_RequestedECBComponentIndices.Dequeue();
                ref ComponentBuffer buffer = ref UnsafeUtility.ArrayElementAsRef<ComponentBuffer>(componentBuffer, componentIdx);
                if (buffer.m_ECB.Value.IsWriting)
                {
                    m_RequestedECBComponentIndices.Enqueue(componentIdx);
                    continue;
                }

                HandleECB(ref buffer);
            }

            return base.AfterTransformPresentation();

            void HandleECB(ref ComponentBuffer buffer)
            {
                if (!buffer.m_ECB.Value.TryReadAdded(out var rdr))
                {
                    $"1 {rdr.Count()} :: {rdr.ForEachCount} :: {rdr.RemainingItemCount}".ToLog();

                    buffer.m_ECB.Value.Dispose();
                    return;
                }

                int count = rdr.BeginForEachIndex(0);
                for (int i = 0; i < count; i+=2)
                {
                    EntityComponentBuffer.BinaryType type = (EntityComponentBuffer.BinaryType)rdr.Read<int>();
                    InstanceID entity = rdr.Read<InstanceID>();

                    if ((type & EntityComponentBuffer.BinaryType.Add) == EntityComponentBuffer.BinaryType.Add)
                    {
                        byte* p = rdr.ReadUnsafePtr(buffer.TypeInfo.Size);

                        AddComponent(in entity, buffer.TypeInfo, p);
                        i++;
                    }
                    else if ((type & EntityComponentBuffer.BinaryType.Remove) == EntityComponentBuffer.BinaryType.Remove)
                    {
                        RemoveComponent(in entity, buffer.TypeInfo);
                    }
                }
                rdr.EndForEachIndex();

                buffer.m_ECB.Value.Dispose();
            }
        }

        #endregion

        #region Hashing

        //public int CreateHashCode() => m_Random.NextInt(int.MinValue, int.MaxValue);

        private int GetComponentIndex<TComponent>()
        {
            TypeInfo typeInfo = TypeStatic<TComponent>.TypeInfo;
            int hashCode = typeInfo.GetHashCode();
            int idx = math.abs(hashCode) % m_ComponentArrayBuffer.Length;

#if DEBUG_MODE
            if (idx == 0)
            {
                CoreSystem.Logger.LogError(LogChannel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return idx;
        }
        public int GetComponentIndex(Type t)
        {
            TypeInfo typeInfo = t.ToTypeInfo();
            int hashCode = typeInfo.GetHashCode();
            int idx = math.abs(hashCode) % m_ComponentArrayBuffer.Length;

#if DEBUG_MODE
            if (idx == 0)
            {
                CoreSystem.Logger.LogError(LogChannel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({t.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return idx;
        }
        public int GetComponentIndex(TypeInfo typeInfo)
        {
            int hashCode = typeInfo.GetHashCode();
            int idx = math.abs(hashCode) % m_ComponentArrayBuffer.Length;

#if DEBUG_MODE
            if (idx == 0)
            {
                CoreSystem.Logger.LogError(LogChannel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({typeInfo.Type.Name}) infomation at initializing stage.");

                throw new InvalidOperationException($"Component buffer error. See Error Log.");
            }
#endif
            return idx;
        }
        public int GetEntityIndex(in InstanceID entity)
        {
            int idx = math.abs(entity.GetHashCode()) % ComponentBuffer.c_InitialCount;
            return idx;
        }
        public int2 GetIndex(in Type t, in InstanceID entity)
        {
            int
                cIdx = GetComponentIndex(t),
                eIdx = GetEntityIndex(entity);

            return new int2(cIdx, eIdx);
        }
        public int2 GetIndex(in TypeInfo t, in InstanceID entity)
        {
            int
                cIdx = GetComponentIndex(t),
                eIdx = GetEntityIndex(entity);

            return new int2(cIdx, eIdx);
        }
        public int2 GetIndex<TComponent>(in InstanceID entity)
        {
            int
                cIdx = GetComponentIndex<TComponent>(),
                eIdx = GetEntityIndex(entity);

            return new int2(cIdx, eIdx);
        }

        #endregion

        #region Component Methods

        /// <summary>
        /// DEBUG_MODE 중에 해당 컴포넌트가 제대로 사용될 수 있는지 확인합니다.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <param name="result"></param>
        public void ComponentBufferSafetyCheck<TComponent>(out bool result)
            where TComponent : unmanaged, IEntityComponent
        {
#if DEBUG_MODE
            if (GetComponentIndex<TComponent>() == 0)
            {
                CoreSystem.Logger.LogError(LogChannel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.ToString()}) infomation at initializing stage.");

                result = false;
                return;
            }
#endif
            result = true;
        }
        public ref ComponentBuffer GetComponentBuffer<TComponent>()
        {
            int idx = GetComponentIndex<TComponent>();

            return ref UnsafeUtility.ArrayElementAsRef<ComponentBuffer>(m_ComponentArrayBuffer.GetUnsafeReadOnlyPtr(), idx);
        }
        public ComponentBuffer* GetComponentBufferPointer<TComponent>()
        {
            int idx = GetComponentIndex<TComponent>();

            return ((ComponentBuffer*)m_ComponentArrayBuffer.GetUnsafeReadOnlyPtr()) + idx;
        }
        public IntPtr GetComponentBufferPointerIntPtr<TComponent>()
        {
            int idx = GetComponentIndex<TComponent>();

            return (IntPtr)((ComponentBuffer*)m_ComponentArrayBuffer.GetUnsafeReadOnlyPtr() + idx);
        }

        public void AddComponent(in InstanceID entity, in Type type)
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), Syadeu.Internal.ThreadInfo.Unity);

            using (s_AddComponentMarker.Auto())
            {
                int2 index = GetIndex(in type, in entity);
                Collections.Buffer.NativeReference<ComponentBuffer> buffer
                    = new Collections.Buffer.NativeReference<ComponentBuffer>(m_ComponentArrayBuffer, index.x);
#if DEBUG_MODE
                if (!buffer.Value.IsCreated)
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Component buffer error. " +
                        $"Didn\'t collected this component({TypeHelper.ToString(type)}) infomation at initializing stage.");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }
#endif
                if (!buffer.Value.Find(in entity, ref index.y) &&
                    !buffer.Value.FindEmpty(ref index.y))
                {
                    buffer.Value.Increment();

                    if (!buffer.Value.FindEmpty(ref index.y))
                    {
                        CoreSystem.Logger.LogError(LogChannel.Component,
                            $"Component buffer error. " +
                            $"Component({TypeHelper.ToString(type)}) Hash has been conflected twice. Maybe need to increase default buffer size?");

                        throw new InvalidOperationException($"Component buffer error. See Error Log.");
                    }
                }

                buffer.Value.SetElementAt(index.y, in entity);
                m_ComponentHashMap.Add(buffer.Value.TypeInfo.GetHashCode(), index.y);

                OnComponentAdded?.Invoke(entity, type);
                GetModule<EntityComponentProcessorModule>()
                    .ProcessOnCreated(entity, type, m_ComponentArrayBuffer[index.x].ElementAt(index.y));

                CoreSystem.Logger.Log(LogChannel.Component,
                    $"Component {TypeHelper.ToString(type)} set at entity({entity.Hash}), index {index}");
            }
        }
        public void AddComponent(in InstanceID entity, in TypeInfo type) => AddComponent(in entity, type.Type);
        public void AddComponent(in InstanceID entity, in TypeInfo type, byte* data)
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), Syadeu.Internal.ThreadInfo.Unity);

            using (s_AddComponentMarker.Auto())
            {
                int2 index = GetIndex(in type, in entity);
                Collections.Buffer.NativeReference<ComponentBuffer> buffer
                    = new Collections.Buffer.NativeReference<ComponentBuffer>(m_ComponentArrayBuffer, index.x);
#if DEBUG_MODE
                if (!buffer.Value.IsCreated)
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Component buffer error. " +
                        $"Didn\'t collected this component({type.Type.Name}) infomation at initializing stage.");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }
#endif
                if (!buffer.Value.Find(in entity, ref index.y) &&
                    !buffer.Value.FindEmpty(ref index.y))
                {
                    ComponentBuffer boxed = m_ComponentArrayBuffer[index.x];
                    boxed.Increment();
                    m_ComponentArrayBuffer[index.x] = boxed;

                    if (!buffer.Value.FindEmpty(ref index.y))
                    {
                        CoreSystem.Logger.LogError(LogChannel.Component,
                            $"Component buffer error. " +
                            $"Component({type.Type.Name}) Hash has been conflected twice. Maybe need to increase default buffer size?");

                        throw new InvalidOperationException($"Component buffer error. See Error Log.");
                    }
                }

                m_ComponentArrayBuffer[index.x].SetElementAt(in index.y, in entity, data);
                m_ComponentHashMap.Add(m_ComponentArrayBuffer[index.x].TypeInfo.GetHashCode(), index.y);

                OnComponentAdded?.Invoke(entity, type.Type);
                GetModule<EntityComponentProcessorModule>()
                    .ProcessOnCreated(entity, type.Type, m_ComponentArrayBuffer[index.x].ElementAt(index.y));

                CoreSystem.Logger.Log(LogChannel.Component,
                    $"Component {type.Type.Name} set at entity({entity.Hash}), index {index}");
            }
        }
        public ref TComponent AddComponent<TComponent>(in InstanceID entity) where TComponent : unmanaged, IEntityComponent
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), Syadeu.Internal.ThreadInfo.Unity);

            using (s_AddComponentMarker.Auto())
            {
                int2 index = GetIndex<TComponent>(entity);
                Collections.Buffer.NativeReference<ComponentBuffer> buffer
                    = new Collections.Buffer.NativeReference<ComponentBuffer>(m_ComponentArrayBuffer, index.x);
#if DEBUG_MODE
                if (!buffer.Value.IsCreated)
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Component buffer error. " +
                        $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }
#endif
                if (!buffer.Value.Find(in entity, ref index.y) &&
                    !buffer.Value.FindEmpty(ref index.y))
                {
                    buffer.Value.Increment();
                    
                    if (!buffer.Value.FindEmpty(ref index.y))
                    {
                        CoreSystem.Logger.LogError(LogChannel.Component,
                            $"Component buffer error. " +
                            $"Component({TypeHelper.TypeOf<TComponent>.Name}) Hash has been conflected twice. Maybe need to increase default buffer size?");

                        throw new InvalidOperationException($"Component buffer error. See Error Log.");
                    }
                }

                buffer.Value.SetElementAt(index.y, entity);
                m_ComponentHashMap.Add(buffer.Value.TypeInfo.GetHashCode(), index.y);

                OnComponentAdded?.Invoke(entity, TypeHelper.TypeOf<TComponent>.Type);
                GetModule<EntityComponentProcessorModule>()
                    .ProcessOnCreated(entity, TypeHelper.TypeOf<TComponent>.Type, m_ComponentArrayBuffer[index.x].ElementAt(index.y));

                CoreSystem.Logger.Log(LogChannel.Component,
                    $"Component {TypeHelper.TypeOf<TComponent>.Name} set at entity({entity.Hash}), index {index}");

                return ref *buffer.Value.ElementAtPointer<TComponent>(in index.y);
            }
        }
        public void AddNotifiedComponents(IObject obj, Action<InstanceID, Type> onAdd = null)
        {
            GetModule<EntityNotifiedComponentModule>().TryAddComponent(obj, onAdd);
        }
        public void RemoveComponent<TComponent>(in InstanceID entity)
            where TComponent : unmanaged, IEntityComponent
        {
            using (s_RemoveComponentMarker.Auto())
            {
                int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
                if (!m_ComponentArrayBuffer[index.x].IsCreated)
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Component buffer error. " +
                        $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                    return;
                }
#endif
                OnComponentRemove?.Invoke(entity, TypeHelper.TypeOf<TComponent>.Type);
                GetModule<EntityComponentProcessorModule>()
                    .ProcessOnDestroy(entity, TypeHelper.TypeOf<TComponent>.Type, m_ComponentArrayBuffer[index.x].ElementAt(index.y));

                ComponentDisposer.Dispose(index, entity);
            }
        }
        public void RemoveComponent(in InstanceID entity, Type componentType)
        {
            using (s_RemoveComponentMarker.Auto())
            {
                int2 index = GetIndex(componentType, entity);
#if DEBUG_MODE
                if (!m_ComponentArrayBuffer[index.x].IsCreated)
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Component buffer error. " +
                        $"Didn\'t collected this component({TypeHelper.ToString(componentType)}) infomation at initializing stage.");

                    return;
                }
#endif
                OnComponentRemove?.Invoke(entity, componentType);
                GetModule<EntityComponentProcessorModule>()
                    .ProcessOnDestroy(entity, componentType, m_ComponentArrayBuffer[index.x].ElementAt(index.y));

                ComponentDisposer.Dispose(index, entity);
            }
        }
        public void RemoveComponent(in InstanceID entity, in TypeInfo componentType)
        {
            using (s_RemoveComponentMarker.Auto())
            {
                int2 index = GetIndex(componentType, entity);
#if DEBUG_MODE
                if (!m_ComponentArrayBuffer[index.x].IsCreated)
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Component buffer error. " +
                        $"Didn\'t collected this component({componentType.Type.Name}) information at initializing stage.");

                    return;
                }
#endif
                OnComponentRemove?.Invoke(entity, componentType.Type);
                GetModule<EntityComponentProcessorModule>()
                    .ProcessOnDestroy(entity, componentType.Type, m_ComponentArrayBuffer[index.x].ElementAt(index.y));

                ComponentDisposer.Dispose(index, entity);
            }
        }
        public void RemoveNotifiedComponents(IObject obj, Action<InstanceID, Type> onRemove = null)
        {
            using (s_RemoveNotifiedComponentMarker.Auto())
            {
                GetModule<EntityNotifiedComponentModule>().TryRemoveComponent(obj, onRemove);
            }
        }
        public bool HasComponent<TComponent>(in InstanceID entity) 
            where TComponent : unmanaged, IEntityComponent
        {
            int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(LogChannel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                return false;
            }
#endif
            if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
            {
                return false;
            }

            //m_ComponentArrayBuffer[index.x].ElementAt<TComponent>(index.y, out _, out TComponent* p);

            //if (((TComponent*)m_ComponentArrayBuffer[index.x].m_ComponentBuffer)[index.y] is IValidation validation &&
            //    !validation.IsValid())
            //{
            //    return false;
            //}

            return true;
        }
        public bool HasComponent(InstanceID entity, Type componentType)
        {
            int2 index = GetIndex(componentType, entity);
#if DEBUG_MODE
            if (!m_ComponentArrayBuffer[index.x].IsCreated)
            {
                CoreSystem.Logger.LogError(LogChannel.Component,
                    $"Component buffer error. " +
                    $"Didn\'t collected this component({componentType.Name}) infomation at initializing stage.");

                return false;
            }
#endif
            if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
            {
                return false;
            }

            return true;
        }
        public ref TComponent GetComponent<TComponent>(InstanceID entity) 
            where TComponent : unmanaged, IEntityComponent
        {
            using (s_GetComponentMarker.Auto())
            {
                int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
                if (!m_ComponentArrayBuffer[index.x].IsCreated)
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Component buffer error. " +
                        $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }
#endif
                if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Entity({entity.Hash}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }

                IJobParallelForEntitiesExtensions.CompleteAllJobs();

                m_ComponentArrayBuffer[index.x].ElementAt<TComponent>(index.y, out _, out var p);
                return ref p.Value;
            }
        }
        public TComponent GetComponentReadOnly<TComponent>(in InstanceID entity)
            where TComponent : unmanaged, IEntityComponent
        {
            using (s_GetComponentReadOnlyMarker.Auto())
            {
                int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
                if (!m_ComponentArrayBuffer[index.x].IsCreated)
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Component buffer error. " +
                        $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }
#endif
                if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Entity({entity.Hash}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }

                m_ComponentArrayBuffer[index.x].ElementAt<TComponent>(index.y, out _, out var p);

                return p.Value;
            }
        }
        public TComponent* GetComponentPointer<TComponent>(in InstanceID entity) 
            where TComponent : unmanaged, IEntityComponent
        {
            using (s_GetComponentPointerMarker.Auto())
            {
                int2 index = GetIndex<TComponent>(entity);
#if DEBUG_MODE
                if (!m_ComponentArrayBuffer[index.x].IsCreated)
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Component buffer error. " +
                        $"Didn\'t collected this component({TypeHelper.TypeOf<TComponent>.Name}) infomation at initializing stage.");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }
#endif
                if (!m_ComponentArrayBuffer[index.x].Find(entity, ref index.y))
                {
                    CoreSystem.Logger.LogError(LogChannel.Component,
                        $"Entity({entity.Hash}) doesn\'t have component({TypeHelper.TypeOf<TComponent>.Name})");

                    throw new InvalidOperationException($"Component buffer error. See Error Log.");
                }

                s_GetComponentPointerMarker.End();
                m_ComponentArrayBuffer[index.x].ElementAt<TComponent>(index.y, out _, out var p);

                return p.Ptr;
            }
        }

        #endregion

        #region Utils

        internal static bool CollectTypes<T>(Type t)
        {
            if (t.IsAbstract || t.IsInterface) return false;

            if (TypeHelper.TypeOf<T>.Type.IsAssignableFrom(t)) return true;

            return false;
        }
        internal static bool IsComponentType(Type t)
        {
            if (!TypeHelper.TypeOf<IEntityComponent>.Type.IsAssignableFrom(t))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Inner Classes

        /// <summary>
        /// Frame 단위로 Presentation 이 진행되기 떄문에, 해당 프레임에서 제거된 프레임일지어도
        /// 같은 프레임내에서 접근하는 Data Access Call 을 허용하기 위해 다음 프레임에 제거되도록 합니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="m_DisposedComponents"/> queue 에 저장되어 다음 Player Loop 의 
        /// <seealso cref="PresentationManager.PreUpdate"/> 에서 Dipose 됩니다.
        /// </remarks>
        private struct ComponentDisposer
        {
            [NativeDisableUnsafePtrRestriction] private static ComponentBuffer* s_Buffer;
            /// <summary>
            /// <see cref="EntityComponentSystem.m_ComponentHashMap"/>
            /// </summary>
            [NativeDisableUnsafePtrRestriction] private static UntypedUnsafeHashMap* s_HashMap;

            public static void Initialize(ComponentBuffer* buffer, UntypedUnsafeHashMap* hashMap)
            {
                s_Buffer = buffer;
                s_HashMap = hashMap;
            }
            public static void Dispose(int2 index, InstanceID entity)
            {
                if (!s_Buffer[index.x].Find(entity, ref index.y))
                {
                    $"couldn\'t find component({s_Buffer[index.x].TypeInfo.Type.Name}) target in entity({entity.Hash}, {entity.GetObject().Name}) : index{index}".ToLogError();
                    return;
                }
                IJobParallelForEntitiesExtensions.CompleteAllJobs();

                ref UnsafeMultiHashMap<int, int> hashMap
                    = ref UnsafeUtility.As<UntypedUnsafeHashMap, UnsafeMultiHashMap<int, int>>(ref *s_HashMap);

                hashMap.Remove(s_Buffer[index.x].TypeInfo.GetHashCode(), index.y);

                s_Buffer[index.x].RemoveAt(index.y);
            }
        }

        #endregion
    }

    public delegate void EntityComponentDelegate<TEntity, TComponent>(in TEntity entity, in TComponent component) where TComponent : unmanaged, IEntityComponent;
}
