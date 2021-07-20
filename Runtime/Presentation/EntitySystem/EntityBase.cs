using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Presentation
{
    /// <summary><inheritdoc cref="IEntity"/></summary>
    /// <remarks>
    /// 이 클래스를 상속받음으로서 새로운 오브젝트를 선언할 수 있습니다.<br/>
    /// 선언된 클래스는 <seealso cref="EntityDataList"/>에 자동으로 타입이 등록되어 추가할 수 있게 됩니다.
    /// </remarks>
    public abstract class EntityBase : ObjectBase, IEntity, ICloneable
    {
        [JsonIgnore] internal Hash m_GameObjectHash;
        [JsonIgnore] internal Hash m_TransformHash;
        [JsonIgnore] internal List<AttributeBase> m_Attributes;

        [JsonProperty(Order = -9, PropertyName = "PrefabIdx")] public PrefabReference PrefabIdx { get; set; }
        [JsonProperty(Order = -8, PropertyName = "Attributes")] public List<Hash> Attributes { get; set; }
        List<AttributeBase> IEntity.Attributes => m_Attributes;

        [JsonIgnore] public DataGameObject gameObject => PresentationSystem<GameObjectProxySystem>.System.GetDataGameObject(m_GameObjectHash);
        [JsonIgnore] public DataTransform transform => PresentationSystem<GameObjectProxySystem>.System.GetDataTransform(m_TransformHash);

        public bool IsValid()
        {
            if (m_GameObjectHash.Equals(Hash.Empty) || m_TransformHash.Equals(Hash.Empty)) return false;

            return true;
        }
        public virtual object Clone()
        {
            EntityBase entity = (EntityBase)MemberwiseClone();
            entity.Name = string.Copy(Name);
            entity.Attributes = new List<Hash>(Attributes);

            entity.m_Attributes = new List<AttributeBase>();
            for (int i = 0; i < Attributes.Count; i++)
            {
                AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(Attributes[i]);
                if (att == null)
                {
                    CoreSystem.Logger.LogError(Channel.Entity, $"This Entity has an invalid attribute({Attributes[i]}) at {i}. This is not allowed.");
                    continue;
                }

                AttributeBase clone = (AttributeBase)att.Clone();
                clone.Parent = entity;
                entity.m_Attributes.Add(clone);
            }

            return entity;
        }

        AttributeBase IEntity.GetAttribute(Type t)
        {
            IEntity entity = this;
            return entity.Attributes.FindFor((other) => other.GetType().Equals(t));
        }
        AttributeBase[] IEntity.GetAttributes(Type t)
        {
            IEntity entity = this;
            return entity.Attributes.Where((other) => other.GetType().Equals(t)).ToArray();
        }
        T IEntity.GetAttribute<T>() => (T)((IEntity)this).GetAttribute(TypeHelper.TypeOf<T>.Type);
        T[] IEntity.GetAttributes<T>() => ((IEntity)this).GetAttributes(TypeHelper.TypeOf<T>.Type).Select((other) => (T)other).ToArray();

        public override string ToString() => Name;
    }
}
