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

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Syadeu.Collections;
using Syadeu.Internal;
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="EntitySystem"/>의 모든 객체들이 참조하는 가장 기본 abstract 입니다.
    /// </summary>
    /// <remarks>
    /// 객체 Entity 는 <seealso cref="Entities.EntityDataBase"/>, <seealso cref="Entities.EntityBase"/>, <seealso cref="Entities.FXEntity"/>, <seealso cref="Entities.ObjectEntity"/>, <seealso cref="Entities.UIObjectEntity"/> 를 참조하세요. <br/><br/>
    /// 
    /// 행동 Entity 는 <seealso cref="Actions.TriggerAction"/>, <seealso cref="Actions.TriggerPredicateAction"/>, <seealso cref="Actions.InstanceAction"/> 을 참조하세요. <br/><br/>
    /// 
    /// 데이터 Entity 는 <seealso cref="Data.DataObjectBase"/>, <seealso cref="Data.ConstantData"/> 를 참조하세요.
    /// </remarks>
    [RequireDerived]
    public abstract class ObjectBase : IObject, IDisposable, IEquatable<ObjectBase>
    {
        const string c_NameBase = "New {0}";

        /// <summary>
        /// 이 오브젝트의 이름입니다.
        /// </summary>
        [SerializeField]
        [JsonProperty(Order = -1000, PropertyName = "Name")]
        public string Name;
        /// <summary>
        /// 이 오브젝트의 오리지널 해쉬입니다.
        /// </summary>
        [JsonProperty(Order = -999, PropertyName = "Hash")]
        [SerializeField]
        [ReflectionSealedView]
        public Hash Hash;
        /// <summary>
        /// 이 오브젝트의 인스턴스 해쉬입니다.
        /// </summary>
        [JsonIgnore] public InstanceID Idx { get; private set; } = InstanceID.Empty;

        [JsonIgnore] internal protected bool m_Reserved = true;

        [JsonIgnore] public bool Reserved => m_Reserved;
        [JsonIgnore] string IObject.Name => Name;
        [JsonIgnore] Hash IObject.Hash => Hash;

        public ObjectBase()
        {
            Name = string.Format(c_NameBase, GetType().Name);
            Hash = Hash.NewHash();
        }
        ~ObjectBase()
        {
            CoreSystem.Logger.Log(Channel.GC, $"Disposing entity object({Name})");
            //Dispose();
        }

        /// <summary>
        /// <see cref="EntitySystem"/>에서 이 오브젝트 인스턴스를 생성하기 위해 호출하는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// 엔티티 선언내에 class와 같은 맴버를 포함하고 있다면 반드시 이 메소드를 override 하여 해당 객체를 복사하여야합니다.
        /// </remarks>
        /// <returns></returns>
        protected virtual ObjectBase Copy()
        {
            ObjectBase entity = (ObjectBase)MemberwiseClone();
            entity.Name = string.Copy(Name);
            entity.Idx = new InstanceID(Hash.NewHash());

            entity.m_Reserved = false;
            entity.OnCreated();

            return entity;
        }
        public virtual object Clone() => Copy();

        void IDisposable.Dispose() { }
        /// <summary>
        /// 인스턴스가 정말 파괴될 때 실행됩니다.
        /// </summary>
        internal virtual void InternalOnDestroy()
        {
            OnDestroy();
        }
        /// <summary>
        /// Pool 에서 꺼내져 재사용될때 실행됩니다.
        /// </summary>
        internal virtual void InternalInitialize()
        {
            Idx = new InstanceID(Hash.NewHash());
            m_Reserved = false;

            OnInitialize();
        }
        /// <summary>
        /// Pool 로 돌아갈 때 실행됩니다.
        /// </summary>
        internal virtual void InternalOnReserve()
        {
            OnReserve();
            m_Reserved = true;
        }

        /// <summary>
        /// 객체가 처음 생성될떄 실행됩니다.
        /// </summary>
        protected virtual void OnCreated() { }
        /// <summary>
        /// Pool 에서 꺼내져 재사용될때 실행됩니다.
        /// </summary>
        protected virtual void OnInitialize() { }
        /// <summary>
        /// 인스턴스가 정말 파괴될 때 실행됩니다.
        /// </summary>
        /// <remarks>
        /// <see cref="ObjectBase"/> 를 상속받는 모든 오브젝트들은 재사용됩니다. 
        /// 이 메소드는 시스템(게임)이 완전히 종료되거나, 메모리 효율을 위해 파괴될 때 실행됩니다.
        /// </remarks>
        protected virtual void OnDestroy() { }
        /// <summary>
        /// 인스턴스가 Pool 로 돌아갈 때 실행됩니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="EntityExtensionMethods.Destroy(IEntityDataID)"/> 등과 같은 Destroy 콜이 들어왔을 때 
        /// 실행됩니다. 인스턴스가 메모리에서 완전히 제거될 때에는 <seealso cref="OnDestroy"/> 를 실행합니다.
        /// </remarks>
        protected virtual void OnReserve() { }

        public override sealed bool Equals(object obj)
        {
            if (obj is ObjectBase target && target.Hash.Equals(Hash)) return true;

            return false;
        }
        public override sealed int GetHashCode()
        {
            ulong hash = Idx.Hash;
            return unchecked((int)hash);
        }

        public bool Equals(IObject other) => Hash.Equals(other.Hash);
        public bool Equals(ObjectBase other) => Hash.Equals(other.Hash);

        #region Internal

#if UNITY_EDITOR
        internal string GetScriptPath()
        {
            return InternalScriptPath();
        }
        private static string InternalScriptPath([System.Runtime.CompilerServices.CallerFilePath] string filePath = "")
        {
            return filePath;
        }
#endif

        #endregion
    }
}
