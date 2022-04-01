using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    internal sealed class SerializeScriptableObject : ScriptableObject
    {
        public static SerializeScriptableObject Deserialize<T>(in string json)
        {
            SerializeScriptableObject temp = CreateInstance<SerializeScriptableObject>();
            temp.m_Object = JsonConvert.DeserializeObject<T>(json);

            return temp;
        }
        public static SerializeScriptableObject Deserialize<T>(in T obj)
        {
            SerializeScriptableObject temp = CreateInstance<SerializeScriptableObject>();
            temp.m_Object = obj;

            return temp;
        }

        [SerializeReference] private object m_Object;

        public object Object => m_Object;
    }

    public sealed class SerializedObject<T> : IDisposable
    {
        private static SerializedObject<T> s_Shared = new SerializedObject<T>();
        public static SerializedObject<T> GetSharedObject(T obj)
        {
            if (s_Shared.m_Object != null)
            {
                UnityEngine.Object.DestroyImmediate(s_Shared.m_Object);
            }

            s_Shared.m_Object = SerializeScriptableObject.Deserialize(obj);
            s_Shared.m_SerializedObject = new SerializedObject(s_Shared.m_Object);

            return s_Shared;
        }
        public static float GetPropertyHeight(T obj)
        {
            SerializedObject<T> temp = GetSharedObject(obj);
            return temp.PropertyHeight;
        }

        private SerializeScriptableObject m_Object;
        private SerializedObject m_SerializedObject;
        private AnimFloat m_PropertyHeight;

        public SerializedProperty SerializedProperty
        {
            get
            {
                return m_SerializedObject.FindProperty("m_Object");
            }
        }
        public float PropertyHeight
        {
            get
            {
                float target = EditorGUI.GetPropertyHeight(this, true) + 20;

                if (m_PropertyHeight == null)
                {
                    m_PropertyHeight = new AnimFloat(target);
                }

                //if (SerializedProperty.isExpanded)
                {
                    m_PropertyHeight.target = target;
                }
                //else
                //{
                //    m_PropertyHeight.target = EditorGUI.GetPropertyHeight(this, false);
                //}

                return m_PropertyHeight.value;
            }
        }

        private SerializedObject() { }
        internal SerializedObject(SerializeScriptableObject obj, SerializedObject serializedObject)
        {
            m_Object = obj;
            m_SerializedObject = serializedObject;
        }
        public SerializedObject(string json)
        {
            m_Object = SerializeScriptableObject.Deserialize(json);
            m_SerializedObject = new SerializedObject(m_Object);
        }
        public SerializedObject(T obj)
        {
            m_Object = SerializeScriptableObject.Deserialize(obj);
            m_SerializedObject = new SerializedObject(m_Object);
        }
        
        public void Dispose()
        {
            if (m_Object == null) throw new Exception("already disposed.");

            UnityEngine.Object.DestroyImmediate(m_Object);
            
            m_Object = null;
            m_SerializedObject = null;
        }

        public void GetCachedEditor(ref Editor editor)
        {
            var iter = TypeHelper.GetTypesIter((t) => !t.IsAbstract && !t.IsInterface && TypeHelper.TypeOf<SerializedObjectEditor<T>>.Type.IsAssignableFrom(t));
            if (iter.Any())
            {
                Editor.CreateCachedEditor(m_Object, iter.First(), ref editor);
                return;
            }

            Editor.CreateCachedEditor(m_Object, TypeHelper.TypeOf<DefaultSerializedObjectEditor>.Type, ref editor);
        }
        public Editor GetEditor()
        {
            var iter = TypeHelper.GetTypesIter((t) => !t.IsAbstract && !t.IsInterface && TypeHelper.TypeOf<SerializedObjectEditor<T>>.Type.IsAssignableFrom(t));
            if (iter.Any())
            {
                return (SerializedObjectEditor<T>)Editor.CreateEditor(m_Object, iter.First());
            }
            
            return Editor.CreateEditor(m_Object, TypeHelper.TypeOf<DefaultSerializedObjectEditor>.Type);
        }

        #region Property Utils

        public SerializedProperty FindProperty(string path)
        {
            return SerializedProperty.FindPropertyRelative(path);
        }

        public void ApplyModifiedProperties() => m_SerializedObject.ApplyModifiedProperties();

        #endregion

        public static implicit operator SerializedObject(SerializedObject<T> t) => t.m_SerializedObject;
        public static implicit operator SerializedProperty(SerializedObject<T> t) => t.SerializedProperty;
    }
    public abstract class SerializedObjectEditor<T> : EditorEntity
    {
        private SerializedObject<T> m_SerializedObject = null;

        protected new T target
        {
            get
            {
                SerializeScriptableObject temp = (SerializeScriptableObject)base.target;
                return (T)temp.Object;
            }
        }
        protected Type type => target.GetType();
        protected new SerializedObject<T> serializedObject
        {
            get
            {
                if (m_SerializedObject == null)
                {
                    m_SerializedObject = new SerializedObject<T>((SerializeScriptableObject)base.target, base.serializedObject);
                }

                return m_SerializedObject;
            }
        }
    }

    public abstract class PropertyDrawer<T> : PropertyDrawer
    {
        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //base.OnGUI(position, property, label);

            AutoRect rect = new AutoRect(position);
            BeforePropertyGUI(ref rect, property, label);

            ReflectionSealedViewAttribute sealedViewAttribute
                = fieldInfo.GetCustomAttribute<ReflectionSealedViewAttribute>();

            using (new EditorGUI.DisabledGroupScope(sealedViewAttribute != null))
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                OnPropertyGUI(ref rect, property, label);
            }
        }

        protected virtual void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label) { }
        protected virtual void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label) { }

        protected void SaveCache<TObject>(SerializedProperty property, string name, TObject obj)
        {
            Hash hash = Hash.NewHash(property.propertyPath);
            string cacheName = hash.ToString() + name;

            EditorPrefs.SetString(cacheName, JsonConvert.SerializeObject(obj));
        }
        protected TObject LoadCache<TObject>(SerializedProperty property, string name) => LoadCache(property, name, default(TObject));
        protected TObject LoadCache<TObject>(SerializedProperty property, string name, TObject defaultValue)
        {
            Hash hash = Hash.NewHash(property.propertyPath);
            string cacheName = hash.ToString() + name;

            string json = EditorPrefs.GetString(cacheName, JsonConvert.SerializeObject(defaultValue));
            return JsonConvert.DeserializeObject<TObject>(json);
        }
    }
}
