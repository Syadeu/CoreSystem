using Newtonsoft.Json;
using Syadeu.Collections;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    internal sealed class SerializeScriptableObject<T> : ScriptableObject
    {
        public static SerializeScriptableObject<T> Deserialize(in string json)
        {
            SerializeScriptableObject<T> temp = CreateInstance<SerializeScriptableObject<T>>();
            temp.m_Object = JsonConvert.DeserializeObject<T>(json);

            return temp;
        }
        public static SerializeScriptableObject<T> Deserialize(in T obj)
        {
            SerializeScriptableObject<T> temp = CreateInstance<SerializeScriptableObject<T>>();
            temp.m_Object = obj;

            return temp;
        }

        [SerializeField] private T m_Object;

        public T Object => m_Object;
    }

    public sealed class SerializedObject<T> : IDisposable
    {
        private SerializeScriptableObject<T> m_Object;
        private SerializedObject m_SerializedObject;
        //private SerializedObjectEditor<T> m_Editor;

        public SerializedProperty SerializedProperty
        {
            get
            {
                return m_SerializedObject.GetIterator();
            }
        }

        internal SerializedObject(SerializeScriptableObject<T> obj, SerializedObject serializedObject)
        {
            m_Object = obj;
            m_SerializedObject = serializedObject;
        }
        public SerializedObject(string json)
        {
            m_Object = SerializeScriptableObject<T>.Deserialize(json);
            m_SerializedObject = new SerializedObject(m_Object);
        }
        public SerializedObject(T obj)
        {
            m_Object = SerializeScriptableObject<T>.Deserialize(obj);
            m_SerializedObject = new SerializedObject(m_Object);
        }
        
        public void Dispose()
        {
            if (m_Object == null) throw new Exception("already disposed.");

            UnityEngine.Object.DestroyImmediate(m_Object);
            
            m_Object = null;
            m_SerializedObject = null;
        }

        public SerializedObjectEditor<T> GetEditor()
        {
            var iter = TypeHelper.GetTypesIter((t) => TypeHelper.TypeOf<SerializedObjectEditor<T>>.Type.IsAssignableFrom(t));
            if (iter.Any())
            {
                return (SerializedObjectEditor<T>)Editor.CreateEditor(m_Object, iter.First());
            }

            return null;
        }

        #region Property Utils

        public SerializedProperty FindPropertyRelative(string path)
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
                SerializeScriptableObject<T> temp = (SerializeScriptableObject<T>)base.target;
                return temp.Object;
            }
        }
        protected new SerializedObject<T> serializedObject
        {
            get
            {
                if (m_SerializedObject == null)
                {
                    m_SerializedObject = new SerializedObject<T>((SerializeScriptableObject<T>)base.target, base.serializedObject);
                }

                return m_SerializedObject;
            }
        }

        //private void OnEnable()
        //{
        //    serializedObject
        //}

        //public override sealed void OnInspectorGUI()
        //{
        //    base.OnInspectorGUI();
        //}
        //protected virtual void OnGUI() { }
    }

    public abstract class PropertyDrawer<T> : PropertyDrawer
    {
        public override sealed void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //base.OnGUI(position, property, label);

            AutoRect rect = new AutoRect(EditorGUI.IndentedRect(position));
            BeforePropertyGUI(ref rect, property, label);

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                OnPropertyGUI(ref rect, property, label);
            }
        }

        protected virtual void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label) { }
        protected virtual void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label) { }
    }
}
