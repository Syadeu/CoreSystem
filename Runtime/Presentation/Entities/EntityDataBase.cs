using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

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
        ///// <summary><inheritdoc cref="isCreated"/></summary>
        //[JsonIgnore] internal bool m_IsCreated = false;
        /// <summary><inheritdoc cref="IEntityData.Attributes"/></summary>
        [JsonIgnore] internal Dictionary<Type, AttributeBase[]> m_AttributesHashMap;
        // TODO : 이거 임시, 나중에 최적화시 지울 것
        [JsonIgnore] internal AttributeBase[] m_Attributes;

        InstanceID IObject.Idx => Idx;
        IAttribute[] IEntityData.Attributes => m_Attributes;

        /// <summary><inheritdoc cref="m_Attributes"/></summary>
        [JsonProperty(Order = -10, PropertyName = "Attributes")] private Reference<AttributeBase>[] m_AttributeList = Array.Empty<Reference<AttributeBase>>();

        [JsonIgnore, UnityEngine.HideInInspector] public Reference<AttributeBase>[] Attributes => m_AttributeList;
        [JsonIgnore] private HashSet<Hash> AttritbutesHashSet { get; } = new HashSet<Hash>();
        //[JsonIgnore] public bool isCreated => m_IsCreated;

        public bool HasAttribute(Hash hash)
        {
            if (Reserved)
            {
                for (int i = 0; i < m_AttributeList.Length; i++)
                {
                    if (m_AttributeList[i].Hash.Equals(hash)) return true;
                }
                return false;
            }

            return AttritbutesHashSet.Contains(hash);
        }
        public bool HasAttribute(Type attributeType)
        {
            if (Reserved)
            {
                for (int i = 0; i < m_AttributeList.Length; i++)
                {
                    if (attributeType.IsAssignableFrom(m_AttributeList[i].GetObject().GetType())) return true;
                }
                return false;
            }

            if (m_AttributesHashMap.ContainsKey(attributeType)) return true;
            foreach (var item in m_AttributesHashMap)
            {
                if (attributeType.IsAssignableFrom(item.Key))
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasAttribute<T>() where T : class, IAttribute => HasAttribute(TypeHelper.TypeOf<T>.Type);

        IAttribute IEntityData.GetAttribute(Type t)
        {
            if (Reserved)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This object({Name}) is not an instance. {nameof(IEntityData.GetAttribute)} is not allowed.");
                return null;
            }

            if (m_AttributesHashMap.TryGetValue(t, out var list)) return list[0];

            for (int i = 0; i < m_Attributes.Length; i++)
            {
                if (t.IsAssignableFrom(m_Attributes[i].GetType()))
                {
                    return m_Attributes[i];
                }
            }
            return null;
        }
        IAttribute[] IEntityData.GetAttributes(Type t)
        {
            if (Reserved)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This object({Name}) is not an instance. {nameof(IEntityData.GetAttributes)} is not allowed.");
                return null;
            }

            if (m_AttributesHashMap.TryGetValue(t, out var list)) return list;

            foreach (var item in m_AttributesHashMap)
            {
                if (t.IsAssignableFrom(item.Key))
                {
                    return item.Value;
                }
            }

            return null;
        }
        T IEntityData.GetAttribute<T>()
        {
            if (Reserved)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This object({Name}) is not an instance. {nameof(IEntityData.GetAttribute)} is not allowed.");
                return null;
            }

            IAttribute att = ((IEntityData)this).GetAttribute(TypeHelper.TypeOf<T>.Type);
            return att == null ? null : (T)att;
        }
        T[] IEntityData.GetAttributes<T>()
        {
            if (Reserved)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This object({Name}) is not an instance. {nameof(IEntityData.GetAttributes)} is not allowed.");
                return null;
            }

            IAttribute[] atts = ((IEntityData)this).GetAttributes(TypeHelper.TypeOf<T>.Type);
            if (atts == null) return null;

            return atts.Select((other) => (T)other).ToArray();
        }
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
                for (int i = 0; i < m_AttributeList.Length; i++)
                {
                    AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(m_AttributeList[i]);
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

        public virtual bool IsValid()
        {
            if (Reserved || PresentationSystem<DefaultPresentationGroup, GameObjectProxySystem>.System.Disposed) return false;

            return true;
        }
        protected override ObjectBase Copy()
        {
            EntityDataBase entity = (EntityDataBase)base.Copy();

            Reference<AttributeBase>[] copy = new Reference<AttributeBase>[m_AttributeList.Length];
            Array.Copy(m_AttributeList, copy, m_AttributeList.Length);
            entity.m_AttributeList = copy;

            return entity;
        }
        public override sealed object Clone()
        {
            const string c_AttributeWarning = "This object({0}) has an invalid attribute({1}) at {2}. This is not allowed.";
            EntityDataBase entity = (EntityDataBase)Copy();

            Dictionary<Type, List<AttributeBase>> tempHashMap = new Dictionary<Type, List<AttributeBase>>();

            entity.m_Attributes = new AttributeBase[m_AttributeList.Length];
            for (int i = 0; i < m_AttributeList.Length; i++)
            {
                AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(m_AttributeList[i]);
                if (att == null)
                {
                    CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_AttributeWarning, Name, m_AttributeList[i].Hash, i));
                    continue;
                }

                AttributeBase clone = (AttributeBase)att.Clone();
                clone.ParentEntity = entity;

                entity.m_Attributes[i] = clone;

                Type attType = clone.GetType();
                if (!tempHashMap.TryGetValue(attType, out List<AttributeBase> list))
                {
                    list = new List<AttributeBase>();
                    tempHashMap.Add(attType, list);
                }
                list.Add(clone);
                AttritbutesHashSet.Add(m_AttributeList[i]);
            }

            entity.m_AttributesHashMap = new Dictionary<Type, AttributeBase[]>();
            foreach (var item in tempHashMap)
            {
                entity.m_AttributesHashMap.Add(item.Key, item.Value.ToArray());
            }

            return entity;
        }
        internal override void InternalReserve()
        {
            base.InternalReserve();

            for (int i = 0; i < m_Attributes.Length; i++)
            {
                m_Attributes[i].InternalReserve();
            }
        }
        internal override void InternalInitialize()
        {
            base.InternalInitialize();

            for (int i = 0; i < m_Attributes.Length; i++)
            {
                m_Attributes[i].InternalInitialize();
            }
        }
        public override sealed string ToString() => Name;

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<EntityDataBase>>();
            AotHelper.EnsureList<Reference<EntityDataBase>>();
            AotHelper.EnsureType<EntityData<EntityDataBase>>();
            AotHelper.EnsureList<EntityData<EntityDataBase>>();
            AotHelper.EnsureList<EntityDataBase>();
        }
    }
}
