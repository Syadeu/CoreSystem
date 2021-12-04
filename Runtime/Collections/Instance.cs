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

using Syadeu.Collections;
using System;

namespace Syadeu.Collections
{
    /// <summary>
    /// Contains only instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Instance<T> : IInstance<T>, IEquatable<Instance<T>>
        where T : class, IObject
    {
        public static readonly Instance<T> Empty = new Instance<T>(InstanceID.Empty);

        private readonly InstanceID m_Idx;

        public InstanceID Idx => m_Idx;

        //public Instance(Hash idx)
        //{
        //    m_Idx = idx;
        //}
        public Instance(InstanceID id)
        {
            m_Idx = id;
        }
        public Instance(IEntityDataID entity)
        {
            m_Idx = entity.Idx;
        }
        public Instance(IObject obj)
        {
            if (obj.Idx.IsEmpty())
            {
                UnityEngine.Debug.LogError(
                    $"Object({obj.Name}) is not an instance.");
                m_Idx = InstanceID.Empty;
                return;
            }
            if (!(obj is T))
            {
                UnityEngine.Debug.LogError(
                    $"Object({obj.Name}) is not a {TypeHelper.TypeOf<T>.Name}.");
                m_Idx = InstanceID.Empty;
                return;
            }

            m_Idx = obj.Idx;
        }

        public bool IsEmpty() => Equals(Empty);
        public bool Equals(Instance<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(IInstance other) => m_Idx.Equals(other.Idx);
    }

    public struct Instance : IInstance, IEquatable<Instance>
    {
        public static readonly Instance Empty = new Instance(InstanceID.Empty);

        private readonly InstanceID m_Idx;

        public InstanceID Idx => m_Idx;

        public Instance(InstanceID idx)
        {
            m_Idx = idx;
        }
        public Instance(IObject obj)
        {
            if (obj.Idx.IsEmpty())
            {
                UnityEngine.Debug.LogError(
                    $"Object({obj.Name}) is not an instance.");
                m_Idx = InstanceID.Empty;
                return;
            }

            m_Idx = obj.Idx;
        }

        public bool IsEmpty() => Equals(Empty);
        public bool Equals(Instance other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(IInstance other) => m_Idx.Equals(other.Idx);
    }
}
