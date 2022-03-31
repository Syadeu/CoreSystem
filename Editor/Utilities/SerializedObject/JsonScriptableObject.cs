using Newtonsoft.Json;
using System;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    internal sealed class JsonScriptableObject<T> : ScriptableObject
    {
        public static JsonScriptableObject<T> Deserialize(in string json)
        {
            JsonScriptableObject<T> temp = CreateInstance<JsonScriptableObject<T>>();
            temp.m_Object = JsonConvert.DeserializeObject<T>(json);

            return temp;
        }

        [SerializeField] private T m_Object;
    }

    public sealed class JsonObject<T> : IDisposable
    {
        private JsonScriptableObject<T> m_Object;
        private SerializedObject m_SerializedObject;

        public SerializedObject SerializedObject => m_SerializedObject;
        public SerializedProperty SerializedProperty
        {
            get
            {
                return SerializedObject.GetIterator();
            }
        }

        public JsonObject(string json)
        {
            m_Object = JsonScriptableObject<T>.Deserialize(json);
            m_SerializedObject = new SerializedObject(m_Object);
        }
        public void Dispose()
        {
            if (m_Object == null) throw new Exception("already disposed.");

            UnityEngine.Object.DestroyImmediate(m_Object);

            m_Object = null;
            m_SerializedObject = null;
        }

        public static implicit operator SerializedObject(JsonObject<T> t) => t.SerializedObject;
    }
}
