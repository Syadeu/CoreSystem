using Syadeu.Database;
using Syadeu.Presentation.Map;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation.Map
{
    public sealed class MapObject
    {
        const string c_EditorOnly = "EditorOnly";

        private MapData m_Parent;
        private GameObject m_GameObject;
        private MapDataEntity.Object m_Data;

        public MapData Parent => m_Parent;
        public GameObject GameObject => m_GameObject;
        public MapDataEntity.Object Data => m_Data;

        public MapObject(MapData parent, Transform folder, MapDataEntity.Object obj)
        {
            m_Parent = parent;
            m_Data = obj;

            if (m_Data.m_Object.IsValid())
            {
                PrefabReference prefab = m_Data.m_Object.GetObject().Prefab;

                if (prefab.Equals(PrefabReference.None))
                {
                    m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    m_GameObject.transform.SetParent(folder);
                }
                else if (prefab.IsValid())
                {
                    GameObject temp = (GameObject)prefab.GetObjectSetting().m_RefPrefab.editorAsset;
                    m_GameObject = (GameObject)PrefabUtility.InstantiatePrefab(temp, folder);
                }
                else
                {
                    m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    m_GameObject.transform.SetParent(folder);
                }
            }
            else
            {
                m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m_GameObject.transform.SetParent(folder);
            }

            m_GameObject.tag = c_EditorOnly;
            m_GameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

            Transform tr = m_GameObject.transform;

            tr.position = m_Data.m_Translation;
            tr.rotation = m_Data.m_Rotation;
            tr.localScale = m_Data.m_Scale;
        }

        public PrefabReference Prefab
        {
            get => m_Data.m_Object.GetObject().Prefab;
            set
            {
                if (m_Data.m_Object.GetObject().Prefab.Equals(value)) return;

                m_Data.m_Object.GetObject().Prefab = value;
                Transform folder = m_GameObject.transform.parent;
                UnityEngine.Object.DestroyImmediate(m_GameObject);

                if (value.IsValid())
                {
                    GameObject temp = (GameObject)value.GetObjectSetting().m_RefPrefab.editorAsset;
                    m_GameObject = (GameObject)PrefabUtility.InstantiatePrefab(temp, folder);
                }
                else
                {
                    m_GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    m_GameObject.transform.SetParent(folder);
                }

                m_GameObject.tag = c_EditorOnly;
                m_GameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                Transform tr = m_GameObject.transform;

                tr.position = m_Data.m_Translation;
                tr.rotation = m_Data.m_Rotation;
                tr.localScale = m_Data.m_Scale;
            }
        }
        public Vector3 Position
        {
            get => m_Data.m_Translation;
            set
            {
                m_Data.m_Translation = value;
                m_GameObject.transform.position = value;

                EntityDataList.Instance.SaveData(Parent.MapDataEntityBase);
            }
        }
        public Quaternion Rotation
        {
            get => m_Data.m_Rotation;
            set
            {
                m_Data.m_Rotation = value;
                m_GameObject.transform.rotation = value;

                EntityDataList.Instance.SaveData(Parent.MapDataEntityBase);
            }
        }
        public Vector3 Scale
        {
            get => m_Data.m_Scale;
            set
            {
                m_Data.m_Scale = value;
                m_GameObject.transform.localScale = value;

                EntityDataList.Instance.SaveData(Parent.MapDataEntityBase);
            }
        }

        public void Destroy(bool withoutSave = false)
        {
            m_Parent.Destroy(this, withoutSave);

            m_GameObject = null;
            m_Data = null;
            m_Parent = null;
        }
    }
}
