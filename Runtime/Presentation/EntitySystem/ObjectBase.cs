using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Syadeu.Database;
using Syadeu.Internal;
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="EntitySystem"/>의 모든 객체들이 참조하는 가장 기본 abstract 입니다.
    /// </summary>
    public abstract class ObjectBase : ICloneable, IDisposable
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
        [JsonIgnore] public Hash Idx { get; private set; }

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

        public virtual ObjectBase Copy()
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
        protected virtual void OnDispose() { }
    }
}
