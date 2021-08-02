using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Presentation.Entities
{
    /// <summary><inheritdoc cref="IEntityData"/></summary>
    /// <remarks>
    /// 이 <see langword="abstract"/> 를 상속받음으로서 새로운 엔티티 계층 구조를 작성할 수 있습니다.
    /// <br/><br/>
    /// class 맴버 선언을 그리 추천하고 싶지 않지만, 필요에 의해 선언이 내부에 되었다면,<br/>
    /// 해당 값을 복사하여 인스턴스를 만들기 위해 <see cref="ObjectBase.Copy"/>을 override 하여 해당 값을 복사하여야합니다.
    /// <br/>
    /// </remarks>
    public abstract class EntityDataBase : ObjectBase, IEntityData
    {
        /// <summary><inheritdoc cref="isCreated"/></summary>
        [JsonIgnore] internal bool m_IsCreated = false;
        /// <summary><inheritdoc cref="IEntityData.Attributes"/></summary>
        [JsonIgnore] internal List<AttributeBase> m_Attributes;

        Hash IEntityData.Idx => Idx;
        List<AttributeBase> IEntityData.Attributes => m_Attributes;
        /// <summary><inheritdoc cref="m_Attributes"/></summary>
        [JsonProperty(Order = -10, PropertyName = "Attributes")] [UnityEngine.HideInInspector] public List<Hash> Attributes { get; set; }

        [JsonIgnore] public bool isCreated => m_IsCreated;

        AttributeBase IEntityData.GetAttribute(Type t)
        {
            IEntityData entity = this;
            return entity.Attributes.FindFor((other) => other.GetType().Equals(t));
        }
        AttributeBase[] IEntityData.GetAttributes(Type t)
        {
            IEntityData entity = this;
            return entity.Attributes.Where((other) => other.GetType().Equals(t)).ToArray();
        }
        T IEntityData.GetAttribute<T>() => (T)((IEntityData)this).GetAttribute(TypeHelper.TypeOf<T>.Type);
        T[] IEntityData.GetAttributes<T>() => ((IEntityData)this).GetAttributes(TypeHelper.TypeOf<T>.Type).Select((other) => (T)other).ToArray();

        /// <inheritdoc cref="IEntityData.GetAttribute{T}"/>
        /// <remarks>
        /// 에디터용입니다. 런타임에서는 사용을 자제해주세요.
        /// </remarks>
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
                IEntityData entity = this;
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
                clone.Parent = EntityData<IEntityData>.GetEntityData(entity.Idx);
                entity.m_Attributes.Add(clone);
            }

            return entity;
        }
        public override sealed string ToString() => Name;
    }
}
