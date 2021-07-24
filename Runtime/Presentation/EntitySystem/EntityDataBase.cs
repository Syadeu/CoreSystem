﻿using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Presentation
{
    public abstract class EntityDataBase : ObjectBase, IObject, ICloneable
    {
        [JsonIgnore] internal List<AttributeBase> m_Attributes;

        Hash IObject.Idx => Hash;
        List<AttributeBase> IObject.Attributes => m_Attributes;
        [JsonProperty(Order = -8, PropertyName = "Attributes")] [UnityEngine.HideInInspector] public List<Hash> Attributes { get; set; }

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
            EntityDataBase entity = (EntityDataBase)Copy();

            entity.m_Attributes = new List<AttributeBase>();
            for (int i = 0; i < Attributes.Count; i++)
            {
                AttributeBase att = (AttributeBase)EntityDataList.Instance.GetObject(Attributes[i]);
                if (att == null)
                {
                    CoreSystem.Logger.LogError(Channel.Entity, $"This object has an invalid attribute({Attributes[i]}) at {i}. This is not allowed.");
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
    //public sealed class TestMapProcessor : 
}