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
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Proxy;
using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Unity.Collections;

namespace Syadeu.Presentation.Entities
{
    [BurstCompatible, StructLayout(LayoutKind.Sequential)]
    /// <summary><inheritdoc cref="IEntity"/></summary>
    /// <remarks>
    /// 사용자가 <see cref="ObjectBase"/>를 좀 더 안전하게 접근할 수 있도록 랩핑하는 struct 입니다.<br/>
    /// 이 struct 는 이미 생성된 엔티티만 담습니다. Raw 데이터 접근은 허용하지 않습니다.<br/>
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public struct Entity<T> : IEntityDataID, IValidation, IEquatable<Entity<T>>, IEquatable<InstanceID> 
        where T : class, IObject
    {
        private const string c_Invalid = "Invalid";

        public static Entity<T> Empty => new Entity<T>(InstanceID.Empty, 0, null);

        public static Entity<T> GetEntity(in InstanceID idx) => EntityDataHelper.GetEntity<T>(idx);
        public static Entity<T> GetEntityWithoutCheck(in InstanceID id) => EntityDataHelper.GetEntityWithoutCheck<T>(id);

        /// <inheritdoc cref="IEntityData.Idx"/>
        private readonly InstanceID m_Idx;
        private readonly int m_HashCode;
        private FixedString128Bytes m_Name;

        IObject IEntityDataID.Target => Target;
        public T Target
        {
            get
            {
                //const string c_WarningAccessWillDestroy = "Accessing entity({0}) that will be destroy in the next frame.";
#if DEBUG_MODE
                if (IsEmpty())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An empty entity reference trying to access transform.");
                    return null;
                }
#endif
                EntitySystem system = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;

                if (!system.m_ObjectEntities.TryGetValue(m_Idx, out var value))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An destroyed entity reference trying to access.");
                    return null;
                }
                
                if (!(value is T t))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Entity validation error. This entity is not an {TypeHelper.TypeOf<T>.ToString()} but {TypeHelper.ToString(value?.GetType())}.");
                    return null;
                }

                //if (!CoreSystem.BlockCreateInstance &&
                //    system.IsMarkedAsDestroyed(m_Idx))
                //{
                //    CoreSystem.Logger.LogWarning(Channel.Entity,
                //        string.Format(c_WarningAccessWillDestroy, RawName));
                //}

                return t;
            }
        }
        public bool HasTarget
        {
            get
            {
                if (IsEmpty()) return false;

                EntitySystem system = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;
                if (system.IsDestroyed(m_Idx)) return false;

                return true;
            }
        }

        public FixedString128Bytes RawName => m_Name;
        /// <inheritdoc cref="IObject.Name"/>
        public string Name => m_Idx.IsEmpty() ? c_Invalid : m_Name.ConvertToString();
        /// <inheritdoc cref="IObject.Hash"/>
        public Hash Hash
        {
            get
            {
#if DEBUG_MODE
                if (Target == null)
                {
                    return Hash.Empty;
                }
#endif
                return Target.Hash;
            }
        }
        /// <inheritdoc cref="IObject.Idx"/>
        public InstanceID Idx => m_Idx;
        public Type Type => m_Idx.IsEmpty() ? null : Target.GetType();

#pragma warning disable IDE1006 // Naming Styles
        /// <inheritdoc cref="EntityBase.transform"/>
        public ProxyTransform transform
        {
            get
            {
#if DEBUG_MODE
                if (IsEmpty())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An empty entity reference trying to access transform.");

                    return ProxyTransform.Null;
                }
#endif
                //EntitySystem entitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System;
                //EntityTransformModule transformModule = entitySystem.GetModule<EntityTransformModule>();
                //if (transformModule.HasTransform(Idx))
                //{
                //    return transformModule.GetTransform(Idx);
                //}
                ref EntityTransformStatic transformStatic = ref EntityTransformStatic.GetValue().Data;
                if (transformStatic.HasTransform(Idx))
                {
                    return transformStatic.GetTransform(Idx);
                }

                CoreSystem.Logger.Log(Channel.Entity,
                    $"This entity({RawName}) doesn\'t have any transform. " +
                    $"If you want to access transform, create it before access.");

                return ProxyTransform.Null;
            }
        }
        public bool hasProxy
        {
            get
            {
#if DEBUG_MODE
                if (IsEmpty())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An empty entity reference trying to access.");
                    return false;
                }
                else if (!IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An invalid entity reference trying to access.");
                    return false;
                }
#endif
                ProxyTransform tr = transform;
                return tr.hasProxy && !tr.hasProxyQueued;
            }
        }
        public RecycleableMonobehaviour proxy
        {
            get
            {
#if DEBUG_MODE
                if (IsEmpty() || Target == null)
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "An empty entity reference trying to access proxy.");

                    return null;
                }
#endif
                return transform.proxy;
            }
        }

#pragma warning restore IDE1006 // Naming Styles

        internal Entity(InstanceID id, int hashCode, string name)
        {
            m_Idx = id;
            if (string.IsNullOrEmpty(name))
            {
                m_Name = default(FixedString128Bytes);
            }
            else m_Name = name;

            m_HashCode = hashCode;
        }
        internal Entity(InstanceID id, int hashCode, FixedString128Bytes name)
        {
            m_Idx = id;
            m_Name = name;

            m_HashCode = hashCode;
        }

        public bool IsEmpty() => Equals(Empty);
        public bool IsValid()
        {
            if (IsEmpty() || !HasTarget) return false;

            return true;
        }

        public bool Equals(Entity<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(InstanceID other) => m_Idx.Equals(other);
        public bool Equals(IEntityDataID other) => m_Idx.Equals(other.Idx);

        public override int GetHashCode()
        {
#if DEBUG_MODE
            if (IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "An empty entity reference trying to access transform.");
                return 0;
            }
#endif
            return m_HashCode;
        }

        public static implicit operator T(Entity<T> a) => a.Target;
        public static implicit operator Entity<T>(InstanceID a) => GetEntity(a);
        public static implicit operator Entity<T>(T a)
        {
            if (a == null)
            {
                return Empty;
            }
            return GetEntity(a.Idx);
        }

        public static implicit operator Entity<IObject>(Entity<T> a)
        {
            return new Entity<IObject>(a.Idx, a.m_HashCode, a.m_Name);
        }
    }
}
