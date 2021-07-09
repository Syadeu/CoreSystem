using Syadeu;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Mono.Creature;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Syadeu.Mono.Creature
{
    [PreferBinarySerialization][CustomStaticSetting("Syadeu/Creature")]
    public sealed class CreatureSettings : StaticSettingEntity<CreatureSettings>
    {
//#if UNITY_EDITOR
        [SerializeField] private string m_DepTypeName;
        [SerializeField] private string m_DepSingleToneName = "Instance";
        [SerializeField] private string m_DepArrName;
        [SerializeField] private string m_DepArrElementTypeName;
        [SerializeField] private string m_DepDisplayName;

        //#endif
        private Type m_TargetType;
        private MemberInfo m_TargetSingleTone;
        private MemberInfo m_TargetArray;
        private Type m_TargetArrayElementType;
        private FieldInfo[] m_TargetArrayElementFields;

        private static string DepDisplayName => Instance.m_DepDisplayName;

        [Serializable]
        public class PrivateSet : IComparable<PrivateSet>
        {
            public int m_DataIdx;
            public int m_PrefabIdx = -1;

            public ValuePairContainer m_Values;

            public string Name => m_Values.Contains(DepDisplayName) ? m_Values.GetValue(DepDisplayName).ToString() : "NOTFOUND";

            public int CompareTo(PrivateSet other)
            {
                if (other == null) return 1;
                else if (m_DataIdx > other.m_DataIdx) return 1;
                else if (m_DataIdx == other.m_DataIdx) return 0;
                else return -1;
            }
            public PrefabList.ObjectSetting GetPrefabSetting() => PrefabList.Instance.ObjectSettings[m_PrefabIdx];
        }

        [Space]
        [SerializeField] private List<PrivateSet> m_PrivateSets = new List<PrivateSet>();

        public float m_DontSpawnEnemyWithIn = 10;
        public float m_IgnoreDistanceOfTurn = 50;
        public float m_SkipMoveAniDistance = 30;

        public List<PrivateSet> PrivateSets => m_PrivateSets;

        //private void OnEnable()
        //{
        //    ValidateData();
        //}
#if UNITY_EDITOR
        //private void OnValidate()
        //{
        //    ValidateData();
        //}
#endif
        private void ValidateData()
        {
            if (string.IsNullOrEmpty(m_DepTypeName) || string.IsNullOrEmpty(m_DepSingleToneName) || 
                string.IsNullOrEmpty(m_DepArrName)) return;
            m_TargetType = GetTargetClassTypes().Where((other) => other.Name.Equals(m_DepTypeName)).First();

            if (m_TargetType == null) return;
            m_TargetSingleTone = GetTargetSingleTones(m_TargetType).Where((other) => other.Name.Equals(m_DepSingleToneName)).First();
            m_TargetArray = GetTargetArrays(m_TargetType).Where((other) => other.Name.Equals(m_DepArrName)).First();

            if (m_TargetArray == null) return;
            m_TargetArrayElementType = GetArrayElementType(m_TargetArray);
            m_TargetArrayElementFields = m_TargetArrayElementType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            IList targetArr = CastTargetArray();

            if (targetArr == null) return;
            for (int i = 0; i < m_PrivateSets.Count; i++)
            {
                m_PrivateSets[i].m_Values = CastArrayElementFields(targetArr[m_PrivateSets[i].m_DataIdx]);
            }
        }

        public bool HasPrivateSet(int idx) => GetPrivateSet(idx) != null;
        public PrivateSet GetPrivateSet(int idx)
        {
            for (int i = 0; i < m_PrivateSets.Count; i++)
            {
                if (m_PrivateSets[i].m_DataIdx == idx) return m_PrivateSets[i];
            }
            return null;
        }

        #region Reflections

        public object CastTargetInstance()
        {
            if (m_TargetSingleTone is FieldInfo field)
            {
                return field.GetValue(null);
            }
            else if (m_TargetSingleTone is PropertyInfo property)
            {
                return property.GetGetMethod().Invoke(null, null);
            }
            else throw new Exception();
        }
        public T CastTargetInstance<T>() => (T)CastTargetInstance();
        public IList CastTargetArray()
        {
            object ins = CastTargetInstance();

            if (m_TargetArray is FieldInfo field)
            {
                return (IList)field.GetValue(ins);
            }
            else if (m_TargetArray is PropertyInfo property)
            {
                return (IList)property.GetGetMethod().Invoke(ins, null);
            }
            else throw new Exception();
        }
        public ValuePairContainer CastArrayElementFields(object element)
        {
            ValuePair[] values = new ValuePair[m_TargetArrayElementFields.Length];
            for (int i = 0; i < m_TargetArrayElementFields.Length; i++)
            {
                values[i] = ValuePair.New(m_TargetArrayElementFields[i].Name, m_TargetArrayElementFields[i].GetValue(element));
            }
            return new ValuePairContainer(values);
        }

        public static Type[] GetTargetClassTypes()
        {
            const string AssemblyCSharp = "Assembly-CSharp";

            Type[] types;
            try
            {
                types = Assembly.Load(AssemblyCSharp)
                .GetTypes()
                .Where(other => other.GetCustomAttribute<CreatureDataAttribute>() != null)
                .ToArray();
            }
            catch (FileNotFoundException)
            {
                types = new Type[0];
            }
            catch (Exception)
            {
                types = new Type[0];
            }

            return types;
        }
        public static MemberInfo[] GetTargetSingleTones(Type t)
        {
            List<MemberInfo> candidates = new List<MemberInfo>();
            if (t.BaseType != null)
            {
                candidates.AddRange(t.BaseType
                    .GetMembers()
                    .Where
                    (
                        (other) =>
                        {
                            if (other is FieldInfo field)
                            {
                                return field.IsStatic && field.FieldType.Equals(t);
                            }
                            else if (other is PropertyInfo property)
                            {
                                return property.GetGetMethod().IsStatic && property.PropertyType.Equals(t);
                            }
                            return false;
                        }
                    ));
            }

            candidates.AddRange(t
                .GetMembers()
                .Where
                (
                    (other) =>
                    {
                        if (other is FieldInfo field)
                        {
                            return field.IsStatic && field.FieldType.Equals(t);
                        }
                        else if (other is PropertyInfo property)
                        {
                            return property.GetGetMethod().IsStatic && property.PropertyType.Equals(t);
                        }
                        return false;
                    }
                ));
            return candidates.ToArray();
        }
        public static MemberInfo[] GetTargetArrays(Type t)
        {
            List<MemberInfo> candidates = new List<MemberInfo>();
            if (t.BaseType != null)
            {
                candidates.AddRange(t.BaseType
                    .GetMembers()
                    .Where((other) =>
                    {
                        if (other is FieldInfo field && field.FieldType.GetInterfaces().Contains(TypeHelper.TypeOf<IList>.Type))
                        {
                            return true;
                        }
                        return false;
                    }));
            }
            candidates.AddRange(t.GetMembers()
                .Where((other) =>
                {
                    if (other is FieldInfo field && field.FieldType.GetInterfaces().Contains(TypeHelper.TypeOf<IList>.Type))
                    {
                        return true;
                    }
                    return false;
                }));
            return candidates.ToArray();
        }
        public static Type GetArrayElementType(MemberInfo t)
        {
            if (!(t is FieldInfo field))
            {
                return null;
            }

            if (field.FieldType.IsArray)
            {
                return field.FieldType.GetElementType();
            }
            else if (field.FieldType.GenericTypeArguments.Length > 0)
            {
                return field.FieldType.GenericTypeArguments[0];
            }

            return null;
        }

        #endregion
    }
}