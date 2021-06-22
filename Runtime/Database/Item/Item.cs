using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Syadeu.Mono;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Syadeu.Database
{
    [Serializable] [JsonConverter(typeof(ItemValueJsonConverter))]
    public abstract class ItemValue
    {
        public string m_Name;

        public virtual object GetValue() => throw new NotImplementedException();
        //public abstract void SetValue(object value);
    }
    [Serializable]
    public sealed class ITemValueNull : ItemValue
    {
        public override object GetValue() => null;
        //public override void SetValue(object value)
        //{
        //    throw new NotImplementedException();
        //}
    }
    [Serializable]
    public abstract class ItemValue<T> : ItemValue where T : IConvertible
    {
        ///// <summary>
        ///// <see cref="ItemValueType"/>
        ///// </summary>
        //public int m_Type;
        public T m_Value;

        public override object GetValue()
        {
            //switch ((ItemValueType)m_Type)
            //{
            //    case ItemValueType.String:
            //        return m_Value;
            //    case ItemValueType.Boolean:
            //        return Convert.ChangeType(m_Value, typeof(bool));
            //    case ItemValueType.Float:
            //        return Convert.ChangeType(m_Value, typeof(float));
            //    case ItemValueType.Integer:
            //        return Convert.ChangeType(m_Value, typeof(int));
            //    default:
            //        return null;
            //}
            return m_Value;
        }
        //public override void SetValue(object value)
        //{
        //    m_Value = (T)value;
        //}
    }

    [Serializable] public sealed class SerializableItemIntValue : ItemValue<int> { }
    [Serializable] public sealed class SerializableItemFloatValue : ItemValue<float> { }
    [Serializable] public sealed class SerializableItemStringValue : ItemValue<string> { }
    [Serializable] public sealed class SerializableItemBoolValue : ItemValue<bool> { }
    //internal enum ItemValueType
    //{
    //    Null,

    //    String,
    //    Boolean,
    //    Float,
    //    Integer
    //}

    public class BaseSpecifiedConcreteClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(ItemValue).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null; // pretend TableSortRuleConvert is not specified (thus avoiding a stack overflow)
            return base.ResolveContractConverter(objectType);
        }
    }
    public class ItemValueJsonConverter : JsonConverter
    {
        static readonly JsonSerializerSettings SpecifiedSubclassConversion 
            = new JsonSerializerSettings() { ContractResolver = new BaseSpecifiedConcreteClassConverter() };

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
            => objectType == typeof(ItemValue);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //JToken t = JToken.FromObject(value);

            //if (t.Type != JTokenType.Object)
            //{
            //    t.WriteTo(writer);
            //}
            //else
            //{
            //    JObject o = (JObject)t;
            //    //IList<string> propertyNames = o.Properties().Select(p => p.Name).ToList();

            //    //o.AddFirst(new JProperty("Keys", new JArray(propertyNames)));

            //    o.WriteTo(writer);
            //}
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
            JObject jo = JObject.Load(reader);
            if (!jo.TryGetValue("m_Value", out JToken value))
            {
                return JsonConvert.DeserializeObject<ITemValueNull>(jo.ToString(), SpecifiedSubclassConversion);
            }

            Type t = value.GetType();
            if (t.Equals(typeof(bool)))
            {
                return JsonConvert.DeserializeObject<SerializableItemBoolValue>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else if (t.Equals(typeof(float)))
            {
                return JsonConvert.DeserializeObject<SerializableItemFloatValue>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else if (t.Equals(typeof(int)))
            {
                return JsonConvert.DeserializeObject<SerializableItemIntValue>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else
            {
                return JsonConvert.DeserializeObject<SerializableItemStringValue>(jo.ToString(), SpecifiedSubclassConversion);
            }

            //switch (jo["m_Value"].Value<int>())
            //{
            //    case 1:
            //        return JsonConvert.DeserializeObject<DerivedType1>(jo.ToString(), SpecifiedSubclassConversion);
            //    case 2:
            //        return JsonConvert.DeserializeObject<DerivedType2>(jo.ToString(), SpecifiedSubclassConversion);
            //    default:
            //        throw new Exception();
            //}
            //throw new NotImplementedException();
        }

        //public override bool CanRead
        //{
        //    get { return false; }
        //}

        
    }

    [Serializable]
    public sealed class Item
    {
        public string m_Name;
        public string m_Guid;

        [Tooltip("GUID")]
        /// <summary>
        /// <see cref="ItemType"/>
        /// </summary>
        public string[] m_ItemTypes;
        [Tooltip("GUID")]
        /// <summary>
        /// <see cref="ItemEffectType"/>
        /// </summary>
        public string[] m_ItemEffectTypes;

        [SerializeReference] public ItemValue[] m_Values;

        [NonSerialized] private ItemProxy m_Proxy = null;

        [NonSerialized] private List<ItemInstance> m_Instances = new List<ItemInstance>();
        [NonSerialized] public Action m_OnEquip;
        [NonSerialized] public Action m_OnUse;

        public Item()
        {
            m_Name = "NewItem";
            m_Guid = Guid.NewGuid().ToString();
        }

        public ItemProxy GetProxy()
        {
            if (m_Proxy == null)
            {
                m_Proxy = new ItemProxy(this);
            }
            return m_Proxy;
        }

        private int GetValueIdx(string name)
        {
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Values[i].m_Name.Equals(name))
                {
                    return i;
                }
            }
            throw new Exception();
        }
        public object GetValue(string name) => m_Values[GetValueIdx(name)].GetValue();
        public void SetValue(string name, object value)
        {
            int other = GetValueIdx(name);

            if (value == null)
            {
                m_Values[other] = new ITemValueNull();
            }
            else if (value is bool boolVal)
            {
                //ItemValue<bool> temp = new ItemValue<bool>();
                SerializableItemBoolValue temp = new SerializableItemBoolValue
                {
                    m_Name = name,
                    m_Value = boolVal
                };
                m_Values[other] = temp;
            }
            else if (value is float floatVal)
            {
                //ItemValue<float> temp = new ItemValue<float>();
                SerializableItemFloatValue temp = new SerializableItemFloatValue
                {
                    m_Name = name,
                    m_Value = floatVal
                };
                m_Values[other] = temp;
            }
            else if (value is int intVal)
            {
                //ItemValue<int> temp = new ItemValue<int>();
                SerializableItemIntValue temp = new SerializableItemIntValue
                {
                    m_Name = name,
                    m_Value = intVal
                };
                m_Values[other] = temp;
            }
            else
            {
                //ItemValue<string> temp = new ItemValue<string>();
                SerializableItemStringValue temp = new SerializableItemStringValue
                {
                    m_Name = name,
                    m_Value = value.ToString()
                };
                m_Values[other] = temp;
            }
            //m_Values[other] = 

            //if (value == null) other.m_Value = null;
            //else other.m_Value = value.ToString();
            //m_Values[other]..(value);

            //ItemDataList.SetValueType(other);
        }

        public ItemInstance CreateInstance()
        {
            ItemInstance instance = new ItemInstance(this);

            return instance;
        }
        public ItemInstance GetInstance(Guid guid)
        {
            for (int i = 0; i < m_Instances.Count; i++)
            {
                if (m_Instances[i].Guid.Equals(guid))
                {
                    return m_Instances[i];
                }
            }

            throw new Exception();
        }
    }
    public sealed class ItemProxy : LuaProxyEntity<Item>
    {
        public ItemProxy(Item item) : base(item) { }

        public string Name => Target.m_Name;

        private ItemTypeProxy[] m_ItemTypes = null;
        public ItemTypeProxy[] ItemTypes
        {
            get
            {
                if (m_ItemTypes == null)
                {
                    m_ItemTypes = new ItemTypeProxy[Target.m_ItemTypes.Length];
                    for (int i = 0; i < m_ItemTypes.Length; i++)
                    {
                        m_ItemTypes[i] = ItemDataList.Instance.GetItemType(Target.m_ItemTypes[i]).GetProxy();
                    }
                }
                
                return m_ItemTypes;
            }
        }

        public Action OnEquip { get => Target.m_OnEquip; set => Target.m_OnEquip = value; }
        public Action OnUse { get => Target.m_OnUse; set => Target.m_OnUse = value; }

        public object GetValue(string name) => Target.GetValue(name);
        public void SetValue(string name, object value) => Target.SetValue(name, value);
    }
    public sealed class ItemInstance
    {
        private readonly Guid m_Guid;

        private readonly Item m_Data;
        private readonly ItemType[] m_ItemTypes;
        private readonly ItemEffectType[] m_ItemEffectTypes;

        public Guid Guid => m_Guid;

        internal ItemInstance(Item item)
        {
            m_Guid = Guid.NewGuid();

            m_Data = item;

            m_ItemTypes = new ItemType[item.m_ItemTypes.Length];
            for (int i = 0; i < m_ItemTypes.Length; i++)
            {
                m_ItemTypes[i] = ItemDataList.Instance.GetItemType(item.m_ItemTypes[i]);
            }

            m_ItemEffectTypes = new ItemEffectType[item.m_ItemEffectTypes.Length];
            for (int i = 0; i < m_ItemEffectTypes.Length; i++)
            {
                m_ItemEffectTypes[i] = ItemDataList.Instance.GetItemEffectType(item.m_ItemEffectTypes[i]);
            }
        }
    }

    [Serializable]
    public sealed class ItemType
    {
        public string m_Name;
        public string m_Guid;

        [Space]
        public ItemValue[] m_Values;

        [NonSerialized] private ItemTypeProxy m_Proxy = null;

        public ItemType()
        {
            m_Name = "NewItemType";
            m_Guid = Guid.NewGuid().ToString();
        }

        public ItemTypeProxy GetProxy()
        {
            if (m_Proxy == null)
            {
                m_Proxy = new ItemTypeProxy(this);
            }
            return m_Proxy;
        }
    }
    public sealed class ItemTypeProxy : LuaProxyEntity<ItemType>
    {
        public ItemTypeProxy(ItemType itemType) : base(itemType) { }
    }

    [Serializable]
    public sealed class ItemEffectType
    {
        public string m_Name;
        public string m_Guid;

        [Space]
        public ItemValue[] m_Values;

        [NonSerialized] private ItemEffectTypeProxy m_Proxy = null;

        public ItemEffectType()
        {
            m_Name = "NewItemEffectType";
            m_Guid = Guid.NewGuid().ToString();
        }

        public ItemEffectTypeProxy GetProxy()
        {
            if (m_Proxy == null) m_Proxy = new ItemEffectTypeProxy(this);
            return m_Proxy;
        }
    }
    public sealed class ItemEffectTypeProxy : LuaProxyEntity<ItemEffectType>
    {
        public ItemEffectTypeProxy(ItemEffectType itemEffectType) : base(itemEffectType) { }
    }
}
