using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Presentation
{
    public abstract class EntityBase : IEntity, ICloneable
    {
        [JsonIgnore] internal Hash m_GameObjectHash;
        [JsonIgnore] internal Hash m_TransformHash;
        [JsonIgnore] internal List<AttributeBase> m_Attributes;

        public string Name { get; set; } = "New Entity";
        [JsonProperty(Order = -10, PropertyName = "Hash")] [ReflectionSealedView] public Hash Hash { get; set; }
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
                AttributeBase att = (AttributeBase)EntityDataList.Instance.GetAttribute(Attributes[i]).Clone();
                entity.m_Attributes.Add(att);
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
