using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Presentation.Entities
{
    /// <summary>
    /// <see cref="EntitySystem"/>에서 엔티티 구조의 제일 하단 abstract 입니다.
    /// </summary>
    /// <remarks>
    /// 이 abstract 를 상속받음으로서 새로운 엔티티 계층 구조를 작성할 수 있습니다.
    /// </remarks>
    public abstract class EntityDataBase : ObjectBase, IObject
    {
        [JsonIgnore] internal bool m_IsCreated = false;
        [JsonIgnore] internal List<AttributeBase> m_Attributes;

        Hash IObject.Idx => Idx;
        List<AttributeBase> IObject.Attributes => m_Attributes;
        [JsonProperty(Order = -10, PropertyName = "Attributes")] [UnityEngine.HideInInspector] public List<Hash> Attributes { get; set; }

        [JsonIgnore] public bool isCreated => m_IsCreated;

        AttributeBase IObject.GetAttribute(Type t)
        {
            IObject entity = this;
            return entity.Attributes.FindFor((other) => other.GetType().Equals(t));
        }
        AttributeBase[] IObject.GetAttributes(Type t)
        {
            IObject entity = this;
            return entity.Attributes.Where((other) => other.GetType().Equals(t)).ToArray();
        }
        T IObject.GetAttribute<T>() => (T)((IObject)this).GetAttribute(TypeHelper.TypeOf<T>.Type);
        T[] IObject.GetAttributes<T>() => ((IObject)this).GetAttributes(TypeHelper.TypeOf<T>.Type).Select((other) => (T)other).ToArray();

        public T GetAttribute<T>() where T : AttributeBase
        {
            T output = null;
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                for (int i = 0; i < Attributes.Count; i++)
                {
                    AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(Attributes[i]);
                    if (att == null) continue;

                    if (att is T t)
                    {
                        output = t;
                        break;
                    }
                }
            }
            else
#endif
            {
                IObject entity = this;
                output = entity.GetAttribute<T>();
            }
            return output;
        }

        public abstract bool IsValid();
        public override ObjectBase Copy()
        {
            EntityDataBase entity = (EntityDataBase)base.Copy();
            if (Attributes == null) Attributes = new List<Hash>();
            entity.Attributes = new List<Hash>(Attributes);

            return entity;
        }
        public override sealed object Clone()
        {
            const string c_AttributeWarning = "This object({0}) has an invalid attribute({1}) at {2}. This is not allowed.";
            EntityDataBase entity = (EntityDataBase)Copy();

            entity.m_Attributes = new List<AttributeBase>();
            for (int i = 0; i < Attributes.Count; i++)
            {
                AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(Attributes[i]);
                if (att == null)
                {
                    CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_AttributeWarning, Name, Attributes[i], i));
                    continue;
                }

                AttributeBase clone = (AttributeBase)att.Clone();
                clone.Parent = entity;
                entity.m_Attributes.Add(clone);
            }

            return entity;
        }
        public override sealed string ToString() => Name;
    }
}
