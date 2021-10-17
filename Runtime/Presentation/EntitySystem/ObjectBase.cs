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
    [RequireDerived]
    public abstract class ObjectBase : IObject, IDisposable, IEquatable<ObjectBase>
    {
        const string c_NameBase = "New {0}";

        [JsonIgnore] internal int m_HashCode;

        /// <summary>
        /// 이 오브젝트의 이름입니다.
        /// </summary>
        [JsonProperty(Order = -20, PropertyName = "Name")] public string Name { get; set; }
        /// <summary>
        /// 이 오브젝트의 오리지널 해쉬입니다.
        /// </summary>
        [JsonProperty(Order = -19, PropertyName = "Hash")] [ReflectionSealedView] public Hash Hash { get; set; }
        /// <summary>
        /// 이 오브젝트의 인스턴스 해쉬입니다.
        /// </summary>
        [JsonIgnore] public InstanceID Idx { get; private set; } = InstanceID.Empty;

        [JsonIgnore] private bool m_Reserved = true;

        [JsonIgnore] public bool Reserved => m_Reserved;

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
            entity.Idx = Hash.NewHash();

            entity.m_Reserved = false;

            return entity;
        }
        public virtual object Clone() => Copy();

        void IDisposable.Dispose() { }
        /// <summary>
        /// 인스턴스가 정말 파괴될 때 실행됩니다.
        /// </summary>
        internal virtual void InternalOnDestroy()
        {
            
        }
        /// <summary>
        /// Pool 에서 꺼내져 재사용될때 실행됩니다.
        /// </summary>
        internal virtual void InternalReset()
        {
            Idx = Hash.NewHash();
            m_Reserved = false;
        }
        /// <summary>
        /// Pool 로 돌아갈 때 실행됩니다.
        /// </summary>
        internal virtual void InternalReserve()
        {
            OnReserve();
            m_Reserved = true;
        }

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

        public override sealed int GetHashCode()
        {
            return m_HashCode;
        }

        public bool Equals(IObject other) => Hash.Equals(other.Hash);
        public bool Equals(ObjectBase other) => Hash.Equals(other.Hash);
    }
}
