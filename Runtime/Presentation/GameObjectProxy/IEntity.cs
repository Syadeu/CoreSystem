using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Presentation
{
    public interface IEntity : IValidation
    {
        [JsonProperty(Order = -30, PropertyName = "Name")] string Name { get; }

        [JsonIgnore] DataGameObject gameObject { get; }
        [JsonIgnore] DataTransform transform { get; }

        [JsonIgnore] List<AttributeBase> Attributes { get; }

        AttributeBase GetAttribute(Type t);
        AttributeBase[] GetAttributes(Type t);
        T GetAttribute<T>() where T : AttributeBase;
        T[] GetAttributes<T>() where T : AttributeBase;
    }
    public abstract class EntityBase : IEntity, ICloneable
    {
        [JsonIgnore] internal Hash m_GameObjectHash;
        [JsonIgnore] internal Hash m_TransformHash;
        [JsonIgnore] internal List<AttributeBase> m_Attributes;

        public string Name { get; set; } = "New Entity";
        [JsonProperty(Order = -10, PropertyName = "Hash")] public Hash Hash { get; set; }
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
    public abstract class AttributeBase : IAttribute, ICloneable
    {
        public string Name { get; set; } = "New Attribute";
        public Hash Hash { get; set; }

        public virtual object Clone()
        {
            AttributeBase att = (AttributeBase)MemberwiseClone();
            att.Name = string.Copy(Name);

            return att;
        }

        public override string ToString() => Name;
    }

    [Serializable]
    public sealed class CreatureEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "HP")] public float m_HP;
        [JsonProperty(Order = 1, PropertyName = "Values")] public ValuePairContainer m_Values = new ValuePairContainer();

        public override object Clone()
        {
            CreatureEntity entity = (CreatureEntity)base.Clone();
            entity.m_Values = (ValuePairContainer)m_Values.Clone();

            return entity;
        }
    }

    internal interface IAttributeProcessor
    {
        Type TargetAttribute { get; }
        void OnCreated(AttributeBase attribute, DataGameObject dataObj);
        void OnPresentation(AttributeBase attribute, DataGameObject dataObj);
        void OnDead(AttributeBase attribute, DataGameObject dataObj);
        void OnDestory(AttributeBase attribute, DataGameObject dataObj);
    }
    public abstract class AttributeProcessor : IAttributeProcessor
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, DataGameObject dataObj) => OnCreated(attribute, dataObj);
        void IAttributeProcessor.OnPresentation(AttributeBase attribute, DataGameObject dataObj) => OnPresentation(attribute, dataObj);
        void IAttributeProcessor.OnDead(AttributeBase attribute, DataGameObject dataObj) => OnDead(attribute, dataObj);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, DataGameObject dataObj) => OnDestory(attribute, dataObj);

        protected abstract Type TargetAttribute { get; }
        protected abstract void OnCreated(AttributeBase attribute, DataGameObject dataObj);
        protected virtual void OnPresentation(AttributeBase attribute, DataGameObject dataObj) { }
        protected virtual void OnDead(AttributeBase attribute, DataGameObject dataObj) { }
        protected abstract void OnDestory(AttributeBase attribute, DataGameObject dataObj);
    }
    public abstract class AttributeProcessor<T> : IAttributeProcessor where T : AttributeBase
    {
        Type IAttributeProcessor.TargetAttribute => TargetAttribute;
        void IAttributeProcessor.OnCreated(AttributeBase attribute, DataGameObject dataObj) => OnCreated((T)attribute, dataObj);
        void IAttributeProcessor.OnPresentation(AttributeBase attribute, DataGameObject dataObj) => OnPresentation((T)attribute, dataObj);
        void IAttributeProcessor.OnDead(AttributeBase attribute, DataGameObject dataObj) => OnDead((T)attribute, dataObj);
        void IAttributeProcessor.OnDestory(AttributeBase attribute, DataGameObject dataObj) => OnDestory((T)attribute, dataObj);

        private Type TargetAttribute => TypeHelper.TypeOf<T>.Type;
        protected abstract void OnCreated(T attribute, DataGameObject dataObj);
        protected virtual void OnPresentation(T attribute, DataGameObject dataObj) { }
        protected virtual void OnDead(T attribute, DataGameObject dataObj) { }
        protected abstract void OnDestory(T attribute, DataGameObject dataObj);
    }
}
