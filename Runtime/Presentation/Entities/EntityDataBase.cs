using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
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
        /// <summary><inheritdoc cref="isCreated"/></summary>
        [JsonIgnore] internal bool m_IsCreated = false;
        /// <summary><inheritdoc cref="IEntityData.Attributes"/></summary>
        [JsonIgnore] internal Dictionary<Type, AttributeBase[]> m_AttributesHashMap;
        // TODO : 이거 임시, 나중에 최적화시 지울 것
        [JsonIgnore] internal AttributeBase[] m_Attributes;

        Hash IEntityData.Idx => Idx;
        AttributeBase[] IEntityData.Attributes => m_Attributes;

        /// <summary><inheritdoc cref="m_Attributes"/></summary>
        [JsonProperty(Order = -10, PropertyName = "Attributes")] private List<Hash> m_AttributeList = new List<Hash>();

        [JsonIgnore, UnityEngine.HideInInspector] public List<Hash> Attributes => m_AttributeList;
        [JsonIgnore] private HashSet<Hash> AttritbutesHashSet { get; } = new HashSet<Hash>();
        [JsonIgnore] public bool isCreated => m_IsCreated;

        bool IEntityData.HasAttribute(Hash hash) => AttritbutesHashSet.Contains(hash);
        // TODO : 현재 가장 상위 타입만 체크하여 반환하므로 나중에 상속받은 타입도 고려해서 받아오게 수정할 것
        AttributeBase IEntityData.GetAttribute(Type t)
        {
            if (!m_AttributesHashMap.TryGetValue(t, out var list)) return null;
            return list[0];
        }
        AttributeBase[] IEntityData.GetAttributes(Type t)
        {
            if (!m_AttributesHashMap.TryGetValue(t, out var list)) return null;
            return list;
        }
        T IEntityData.GetAttribute<T>()
        {
            AttributeBase att = ((IEntityData)this).GetAttribute(TypeHelper.TypeOf<T>.Type);
            return att == null ? null : (T)att;
        }
        T[] IEntityData.GetAttributes<T>()
        {
            AttributeBase[] atts = ((IEntityData)this).GetAttributes(TypeHelper.TypeOf<T>.Type);
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
                for (int i = 0; i < m_AttributeList.Count; i++)
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
            if (Disposed || !m_IsCreated || PresentationSystem<GameObjectProxySystem>.System.Disposed) return false;

            return true;
        }
        protected override ObjectBase Copy()
        {
            EntityDataBase entity = (EntityDataBase)base.Copy();
            if (m_AttributeList == null) m_AttributeList = new List<Hash>();
            entity.m_AttributeList = new List<Hash>(m_AttributeList);

            return entity;
        }
        public override sealed object Clone()
        {
            const string c_AttributeWarning = "This object({0}) has an invalid attribute({1}) at {2}. This is not allowed.";
            EntityDataBase entity = (EntityDataBase)Copy();

            Dictionary<Type, List<AttributeBase>> tempHashMap = new Dictionary<Type, List<AttributeBase>>();

            entity.m_Attributes = new AttributeBase[m_AttributeList.Count];
            for (int i = 0; i < m_AttributeList.Count; i++)
            {
                AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(m_AttributeList[i]);
                if (att == null)
                {
                    CoreSystem.Logger.LogError(Channel.Entity, string.Format(c_AttributeWarning, Name, m_AttributeList[i], i));
                    continue;
                }

                AttributeBase clone = (AttributeBase)att.Clone();
                clone.Parent = new EntityData<IEntityData>(entity.Idx);

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
