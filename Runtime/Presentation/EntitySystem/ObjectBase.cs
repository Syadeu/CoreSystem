using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Syadeu.Database;
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
    public abstract class ObjectBase : IObject, IDisposable
    {
        const string c_NameBase = "New {0}";

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
        [JsonIgnore] public Hash Idx { get; private set; } = Hash.Empty;

        [JsonIgnore] public bool Disposed { get; private set; } = false;

        public ObjectBase()
        {
            Name = string.Format(c_NameBase, GetType().Name);
            Hash = Hash.NewHash();
        }
        ~ObjectBase()
        {
            CoreSystem.Logger.Log(Channel.GC, $"Disposing entity object({Name})");
            Dispose();
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

            return entity;
        }
        public virtual object Clone() => Copy();

        public void Dispose()
        {
            if (Disposed) return;
            OnDispose();
            Disposed = true;
        }
        /// <summary>
        /// 이 인스턴스 객체가 메모리에서 제거될때 실행됩니다.
        /// </summary>
        protected virtual void OnDispose() { }
    }

    public interface IObject : ICloneable
    {
        /// <summary>
        /// 이 오브젝트 엔티티의 이름입니다.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 이 오브젝트 엔티티의 오리지널 해쉬입니다.
        /// <see cref="EntityDataList"/>
        /// </summary>
        Hash Hash { get; }
        /// <summary>
        /// 이 오브젝트 엔티티의 인스턴스 고유 해쉬입니다.
        /// </summary>
        [JsonIgnore] Hash Idx { get; }
    }
}
