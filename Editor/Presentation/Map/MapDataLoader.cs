using Syadeu;
using Syadeu.Collections;
using Syadeu.Mono;
using Syadeu.Presentation;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SyadeuEditor.Presentation.Map
{
    public sealed class MapDataLoader : IDisposable
    {
        private Transform m_FolderInstance;
        public Transform Folder
        {
            get
            {
                if (m_FolderInstance == null)
                {
                    m_FolderInstance = new GameObject("MapData Preview").transform;
                }
                return m_FolderInstance;
            }
        }

        private Vector2 m_Scroll;

        private readonly List<Reference<MapDataEntityBase>> m_LoadedMapDataReference = new List<Reference<MapDataEntityBase>>();
        private readonly List<MapData> m_LoadedMapData = new List<MapData>();

        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();
        
        private MapData m_EditingMapData = null;

        private MapData m_SelectedMapData = null;
        private MapDataEntityBase.Object[] m_SelectedObjects = null;
        private MapDataEntityBase.RawObject[] m_SelectedRawObjects = null;
        private GameObject[] m_SelectedGameObjects = null;

        private bool m_WasEditedMapDataSelector = false;

        public GameObject[] SelectedGameObjects => m_SelectedGameObjects;

        public MapDataLoader()
        {
            if (!EntityDataList.IsLoaded) EntityDataList.Instance.LoadData();
        }

        private MapDataEntityBase.Object GetMapDataObject(GameObject target, out MapData mapData)
        {
            mapData = null;
            if (target == null) return null;

            for (int i = 0; i < m_LoadedMapData.Count; i++)
            {
                if (m_LoadedMapData[i][target] != null &&
                    m_LoadedMapData[i][target] is MapDataEntityBase.Object temp)
                {
                    mapData = m_LoadedMapData[i];
                    return temp;
                }
            }
            return null;
        }
        private MapDataEntityBase.RawObject GetRawMapDataObject(GameObject target, out MapData mapData)
        {
            mapData = null;
            if (target == null) return null;

            for (int i = 0; i < m_LoadedMapData.Count; i++)
            {
                if (m_LoadedMapData[i][target] != null &&
                    m_LoadedMapData[i][target] is MapDataEntityBase.RawObject temp)
                {
                    mapData = m_LoadedMapData[i];
                    return temp;
                }
            }
            return null;
        }

        public void SelectObject(GameObject obj)
        {
            m_SelectedObjects = null;
            m_SelectedRawObjects = null;
            m_SelectedGameObjects = null;

            if (obj == null)
            {
                return;
            }

            var entityObj = GetMapDataObject(obj, out m_SelectedMapData);
            if (entityObj != null)
            {
                m_SelectedObjects = new MapDataEntityBase.Object[1];
                m_SelectedObjects[0] = entityObj;
                m_SelectedMapData.SetDirty();

                m_SelectedGameObjects = new GameObject[] { obj };
                Selection.activeGameObject = m_SelectedGameObjects[0];
                return;
            }

            var rawObj = GetRawMapDataObject(obj, out m_SelectedMapData);
            if (rawObj != null)
            {
                m_SelectedRawObjects = new MapDataEntityBase.RawObject[1];
                m_SelectedRawObjects[0] = rawObj;
                m_SelectedMapData.SetDirty();

                m_SelectedGameObjects = new GameObject[] { obj };
                Selection.activeGameObject = m_SelectedGameObjects[0];
                return;
            }

            m_SelectedGameObjects = null;
            Selection.activeGameObject = null;
        }
        public void SelectObjects(GameObject[] obj)
        {
            m_SelectedObjects = null;
            m_SelectedRawObjects = null;
            m_SelectedGameObjects = null;

            if (obj == null)
            {
                return;
            }

            List<GameObject> gameObjects = new List<GameObject>();
            List<MapDataEntityBase.Object> entityObjs = new List<MapDataEntityBase.Object>();
            List<MapDataEntityBase.RawObject> rawObjs = new List<MapDataEntityBase.RawObject>();
            for (int i = 0; i < obj.Length; i++)
            {
                var entityObj = GetMapDataObject(obj[i], out m_SelectedMapData);
                if (entityObj != null)
                {
                    gameObjects.Add(obj[i]);
                    entityObjs.Add(entityObj);

                    m_SelectedMapData.SetDirty();
                    continue;
                }

                var rawObj = GetRawMapDataObject(obj[i], out m_SelectedMapData);
                if (rawObj != null)
                {
                    gameObjects.Add(obj[i]);
                    rawObjs.Add(rawObj);

                    m_SelectedMapData.SetDirty();
                }
            }

            m_SelectedObjects = entityObjs.ToArray();
            m_SelectedRawObjects = rawObjs.ToArray();

            //m_SelectedObjects = new MapDataEntityBase.Object[obj.Length];
            //for (int i = 0; i < obj.Length; i++)
            //{
            //    m_SelectedObjects[i] = GetMapDataObject(obj[i], out m_SelectedMapData);
            //    if (m_SelectedMapData != null)
            //    {
            //        m_SelectedMapData.SetDirty();
            //    }
            //}

            m_SelectedGameObjects = gameObjects.Count == 0 ? null : gameObjects.ToArray();
        }

        public void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = scroll.scrollPosition;

                using (new EditorUtilities.BoxBlock(Color.gray))
                {
                    EditorGUI.BeginDisabledGroup(m_LoadedMapDataReference.Count == 0);
                    if (GUILayout.Button("Save"))
                    {
                        for (int i = 0; i < m_LoadedMapData.Count; i++)
                        {
                            if (!m_LoadedMapData[i].IsDirty) continue;

                            EntityDataList.Instance.SaveData(m_LoadedMapData[i].Entity);
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    for (int i = 0; i < m_LoadedMapDataReference.Count; i++)
                    {
                        int index = i;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            DrawMapDataSelector(m_LoadedMapDataReference[index], (other) =>
                            {
                                if (m_EditingMapData != null && m_EditingMapData.Equals(m_LoadedMapData[index]))
                                {
                                    m_EditingMapData = null;
                                }

                                if (other.IsEmpty() || !other.IsValid())
                                {
                                    m_LoadedMapDataReference.RemoveAt(index);

                                    if (m_LoadedMapData[index] != null)
                                    {
                                        m_LoadedMapData[index].Dispose();
                                        m_LoadedMapData.RemoveAt(index);
                                    }
                                }
                                else
                                {
                                    m_LoadedMapDataReference[index] = other;

                                    if (m_LoadedMapData[index] != null)
                                    {
                                        m_LoadedMapData[index].Dispose();
                                    }
                                    m_LoadedMapData[index] = new MapData(Folder, other);
                                }

                                m_WasEditedMapDataSelector = true;
                            });

                            bool isEditing = false;
                            if (m_EditingMapData != null && m_EditingMapData.Equals(m_LoadedMapData[index]))
                            {
                                isEditing = true;
                            }

                            EditorGUI.BeginChangeCheck();
                            isEditing = GUILayout.Toggle(isEditing, "E", EditorStyleUtilities.MiniButton, GUILayout.Width(20));
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (isEditing) m_EditingMapData = m_LoadedMapData[index];
                                else
                                {
                                    if (m_EditingMapData != null && m_EditingMapData.Equals(m_LoadedMapData[index]))
                                    {
                                        m_EditingMapData = null;
                                    }
                                }
                            }

                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                if (m_EditingMapData != null && m_EditingMapData.Equals(m_LoadedMapData[index]))
                                {
                                    m_EditingMapData = null;
                                }

                                m_LoadedMapDataReference.RemoveAt(index);

                                if (m_LoadedMapData[index] != null)
                                {
                                    m_LoadedMapData[index].Dispose();
                                    m_LoadedMapData.RemoveAt(index);
                                }

                                if (m_LoadedMapData.Count == 0)
                                {
                                    SelectObjects(null);

                                    UnityEngine.Object.DestroyImmediate(m_FolderInstance.gameObject);
                                    m_FolderInstance = null;
                                }

                                i--;
                                continue;
                            }
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawMapDataSelector(Reference<MapDataEntityBase>.Empty, (other) =>
                        {
                            if (other.IsEmpty() || !other.IsValid())
                            {
                                "not valid".ToLog();
                                return;
                            }

                            if (m_LoadedMapDataReference.Contains(other))
                            {
                                "cannot load that already loaded".ToLog();
                                return;
                            }

                            m_LoadedMapDataReference.Add(other);
                            m_LoadedMapData.Add(new MapData(Folder, other));

                            m_WasEditedMapDataSelector = true;
                        });

                        EditorGUI.BeginDisabledGroup(true);
                        GUILayout.Toggle(false, "E", EditorStyleUtilities.MiniButton, GUILayout.Width(20));
                        GUILayout.Button("-", GUILayout.Width(20));
                        EditorGUI.EndDisabledGroup();
                    }

                    if (m_WasEditedMapDataSelector)
                    {
                        m_CreatedObjects.Clear();
                        for (int i = 0; i < m_LoadedMapData.Count; i++)
                        {
                            m_CreatedObjects.AddRange(m_LoadedMapData[i].CreatedObjects);
                        }

                        m_WasEditedMapDataSelector = false;
                    }
                }

                EditorUtilities.Line();

                using (new EditorUtilities.BoxBlock(Color.gray))
                {
                    EditorUtilities.StringRich("Lightmapping", 13);
                    LightingSettings lightSettings = Lightmapping.GetLightingSettingsForScene(EditorSceneManager.GetActiveScene());
                    EditorGUILayout.ObjectField("Light Settings", lightSettings, typeof(LightingSettings), false);

                    //if (GUILayout.Button("Bake"))
                    //{
                    //    m_FolderInstance.gameObject.hideFlags = HideFlags.None;

                    //    if (Lightmapping.isRunning)
                    //    {
                    //        Lightmapping.ForceStop();
                    //        Lightmapping.Cancel();
                    //    }

                    //    if (m_LoadedMapData.Count != 0)
                    //    {
                    //        Lightmapping.ClearLightingDataAsset();

                    //        Lightmapping.lightingSettings = lightSettings;
                    //        Lightmapping.BakeAsync();
                    //    }
                    //    else
                    //    {
                    //        "no loaded map data found".ToLog();
                    //    }
                    //}
                }
            }
        }

        public void OnSceneGUI()
        {
            if (m_LoadedMapData.Count == 0) return;

            #region Mouse Event

            int mouseControlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (Event.current.GetTypeForControl(mouseControlID))
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        //if (m_SelectedObject == null)
                        //{
                        //    GUIUtility.hotControl = mouseControlID;

                        //    GameObject obj = HandleUtility.PickGameObject(Event.current.mousePosition, true, null, null);

                        //    m_SelectedObject = GetMapDataObject(obj, out m_SelectedMapData);
                        //    m_SelectedGameObject = m_SelectedObject == null ? null : obj;

                        //    //if (m_SelectedObject != null) $"{m_SelectedObject.name}".ToLog();

                        //    Event.current.Use();
                        //}
                    }
                    else if (Event.current.button == 1)
                    {
                        //if (m_SelectedObject != null)
                        //{
                        //    GUIUtility.hotControl = mouseControlID;

                        //    GenericMenu menu = new GenericMenu();
                        //    menu.AddDisabledItem(new GUIContent(m_SelectedObject.m_Object.GetObject().Name));
                        //    menu.AddSeparator(string.Empty);

                        //    menu.AddItem(new GUIContent("Open in Window"), false, () =>
                        //    {
                        //        EntityWindow.Instance.Select(m_SelectedObject.m_Object);
                        //    });

                        //    menu.ShowAsContext();

                        //    Event.current.Use();
                        //}
                    }
                    else if (Event.current.button == 2)
                    {
                        if (m_EditingMapData == null)
                        {
                            "no editting map data".ToLog();
                            return;
                        }

                        GUIUtility.hotControl = mouseControlID;

                        m_SelectedMapData = null;
                        m_SelectedObjects = null;
                        m_SelectedGameObjects = null;

                        #region Draw Object Creation PopupWindow

                        Rect rect = GUILayoutUtility.GetLastRect();
                        rect.position = Event.current.mousePosition;
                        Vector3 pos = EditorSceneUtils.GetMouseScreenPos();

                        var list = EntityDataList.Instance.m_Objects.Where((other) => other.Value is EntityBase).Select((other) => (EntityBase)other.Value).ToArray();

                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Raw"), false, () =>
                        {
                            PopupWindow.Show(rect, SelectorPopup<int, PrefabList.ObjectSetting>.GetWindow
                            (
                                list: PrefabList.Instance.ObjectSettings,
                                setter: (prefabIdx) =>
                                {
                                    if (prefabIdx < 0)
                                    {
                                        return;
                                    }

                                    PrefabReference<GameObject> refobj = new PrefabReference<GameObject>(prefabIdx);
                                    GameObject asset = (GameObject)refobj.GetEditorAsset();
                                    var renderers = asset.GetComponentsInChildren<Renderer>();
                                    Bounds bounds = renderers[0].bounds;
                                    for (int i = 1; i < renderers.Length; i++)
                                    {
                                        bounds.Encapsulate(renderers[i].bounds);
                                    }

                                    MapDataEntityBase.RawObject obj = new MapDataEntityBase.RawObject
                                    {
                                        m_Object = refobj,
                                        m_Translation = pos,
                                        m_Rotation = quaternion.identity,
                                        m_Scale = 1,

                                        m_Center = bounds.center,
                                        m_Size = bounds.size
                                    };

                                    //m_SelectedGameObject = m_EditingMapData.Add(obj);
                                    //m_SelectedObject = GetMapDataObject(m_SelectedGameObject, out m_SelectedMapData);
                                    SelectObject(m_EditingMapData.Add(obj));
                                    m_WasEditedMapDataSelector = true;
                                    //Repaint();
                                },
                                getter: (objSet) =>
                                {
                                    for (int i = 0; i < PrefabList.Instance.ObjectSettings.Count; i++)
                                    {
                                        if (objSet.Equals(PrefabList.Instance.ObjectSettings[i])) return i;
                                    }
                                    return -1;
                                },
                                noneValue: -2
                                ));
                        });
                        menu.AddItem(new GUIContent("Entity"), false, () =>
                        {
                            PopupWindow.Show(rect, SelectorPopup<Hash, EntityBase>.GetWindow
                            (
                                list: list,
                                setter: (hash) =>
                                {
                                    if (hash.IsEmpty())
                                    {
                                        return;
                                    }

                                    Reference<EntityBase> refobj = new Reference<EntityBase>(hash);
                                    MapDataEntityBase.Object obj = new MapDataEntityBase.Object
                                    {
                                        m_Object = refobj,
                                        m_Translation = pos,
                                        m_Rotation = quaternion.identity,
                                        m_Scale = 1
                                    };

                                    //m_SelectedGameObject = m_EditingMapData.Add(obj);
                                    //m_SelectedObject = GetMapDataObject(m_SelectedGameObject, out m_SelectedMapData);
                                    SelectObject(m_EditingMapData.Add(obj));
                                    m_WasEditedMapDataSelector = true;
                                    //Repaint();
                                },
                                getter: (other) => other.Hash,
                                noneValue: Hash.Empty
                                ));
                        });
                        menu.ShowAsContext();

                        #endregion

                        //Repaint();

                        Event.current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    //GUIUtility.hotControl = 0;
                    //if (Event.current.button == 0)
                    //{

                    //}

                    //Event.current.Use();
                    break;
            }

            #endregion

            for (int i = 0; i < m_SelectedGameObjects?.Length; i++)
            {
                var entityObj = GetMapDataObject(m_SelectedGameObjects[i], out _);
                if (entityObj != null)
                {
                    DrawSelectedObject(entityObj, m_SelectedGameObjects[i]);
                    continue;
                }

                var rawObj = GetRawMapDataObject(m_SelectedGameObjects[i], out _);
                if (rawObj != null)
                {
                    DrawSelectedRawObject(rawObj, m_SelectedGameObjects[i]);
                }
            }
        }
        public void Dispose()
        {
            for (int i = 0; i < m_LoadedMapData.Count; i++)
            {
                m_LoadedMapData[i].Dispose();
            }

            m_LoadedMapDataReference.Clear();
            m_LoadedMapData.Clear();

            UnityEngine.Object.DestroyImmediate(Folder.gameObject);
        }

        private void DrawSelectedObject(MapDataEntityBase.Object obj, GameObject proxy)
        {
            const float width = 180;

            EntityBase objData = obj.m_Object.GetObject();
            if (objData == null)
            {
                Handles.BeginGUI();
                Rect tempRect = new Rect(HandleUtility.WorldToGUIPoint(obj.m_Translation), new Vector2(width, 60));
                GUI.BeginGroup(tempRect, "INVALID", EditorStyleUtilities.Box);

                if (GUI.Button(GUILayoutUtility.GetRect(width, 20, GUILayout.ExpandWidth(false)), "Remove"))
                {
                    m_SelectedMapData.Remove(proxy);

                    m_SelectedGameObjects = null;
                    m_SelectedMapData = null;
                    m_SelectedObjects = null;
                }

                GUI.EndGroup();
                Handles.EndGUI();
                return;
            }

            AABB selectAabb = obj.aabb;

            #region Scene GUI Overlays

            Vector3 worldPos = selectAabb.center; worldPos.y = selectAabb.max.y;
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);

            if (guiPos.x + width > Screen.width) guiPos.x = Screen.width - width;
            else
            {
                guiPos.x += 50;
            }
            Rect rect = new Rect(guiPos, new Vector2(width, 60));

            Handles.BeginGUI();
            string objName = $"{(objData != null ? $"{objData.Name}" : "None")}";
            GUI.BeginGroup(rect, objName, EditorStyleUtilities.Box);

            #region Proxy Copy

            obj.m_Translation = proxy.transform.position;
            obj.m_Rotation = proxy.transform.rotation;
            obj.m_Scale = proxy.transform.localScale;

            obj.m_Static = proxy.isStatic;

            #endregion

            if (GUI.Button(GUILayoutUtility.GetRect(width, 20, GUILayout.ExpandWidth(false)), "Remove"))
            {
                if (EditorUtility.DisplayDialog($"Remove ({objName})", "Are you sure?", "Remove", "Cancel"))
                {
                    m_SelectedMapData.Remove(proxy);
                    
                    m_SelectedGameObjects = null;
                    m_SelectedMapData = null;
                    m_SelectedObjects = null;

                    //Repaint();
                }
            }
            GUI.EndGroup();
            Handles.EndGUI();

            #endregion

            if (m_SelectedObjects == null) return;

            Handles.color = Color.red;
            Handles.DrawWireCube(selectAabb.center, selectAabb.size);
        }
        private void DrawSelectedRawObject(MapDataEntityBase.RawObject obj, GameObject proxy)
        {
            const float width = 180;
            PrefabList.ObjectSetting prefabSetting = obj.m_Object.GetObjectSetting();
            if (prefabSetting == null)
            {
                Handles.BeginGUI();
                Rect tempRect = new Rect(HandleUtility.WorldToGUIPoint(obj.m_Translation), new Vector2(width, 60));
                GUI.BeginGroup(tempRect, "INVALID", EditorStyleUtilities.Box);

                if (GUI.Button(GUILayoutUtility.GetRect(width, 20, GUILayout.ExpandWidth(false)), "Remove"))
                {
                    m_SelectedMapData.Remove(proxy);

                    m_SelectedGameObjects = null;
                    m_SelectedMapData = null;
                    m_SelectedRawObjects = null;
                }

                GUI.EndGroup();
                Handles.EndGUI();
                return;
            }

            AABB selectAabb = obj.aabb;

            #region Scene GUI Overlays

            Vector3 worldPos = selectAabb.center; worldPos.y = selectAabb.max.y;
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);

            if (guiPos.x + width > Screen.width) guiPos.x = Screen.width - width;
            else
            {
                guiPos.x += 50;
            }
            Rect rect = new Rect(guiPos, new Vector2(width, 60));

            Handles.BeginGUI();
            string objName = $"{(prefabSetting.Name != null ? $"{prefabSetting.Name}" : "None")}";
            GUI.BeginGroup(rect, objName, EditorStyleUtilities.Box);

            #region Proxy Copy

            obj.m_Translation = proxy.transform.position;
            obj.m_Rotation = proxy.transform.rotation;
            obj.m_Scale = proxy.transform.localScale;

            obj.m_Static = proxy.isStatic;

            #endregion

            if (GUI.Button(GUILayoutUtility.GetRect(width, 20, GUILayout.ExpandWidth(false)), "Remove"))
            {
                if (EditorUtility.DisplayDialog($"Remove ({objName})", "Are you sure?", "Remove", "Cancel"))
                {
                    m_SelectedMapData.Remove(proxy);

                    m_SelectedGameObjects = null;
                    m_SelectedMapData = null;
                    m_SelectedObjects = null;

                    //Repaint();
                }
            }
            GUI.EndGroup();
            Handles.EndGUI();

            #endregion

            if (m_SelectedObjects == null) return;

            Handles.color = Color.red;
            Handles.DrawWireCube(selectAabb.center, selectAabb.size);
        }

        private static void DrawMapDataSelector(IFixedReference current, Action<Reference<MapDataEntityBase>> setter)
        {
            ReferenceDrawer.DrawReferenceSelector("Map data: ", (hash) =>
            {
                if (hash.Equals(Hash.Empty))
                {
                    setter.Invoke(Reference<MapDataEntityBase>.Empty);
                    return;
                }

                Reference<MapDataEntityBase> reference = new Reference<MapDataEntityBase>(hash);
                setter.Invoke(reference);
            }, current, TypeHelper.TypeOf<MapDataEntityBase>.Type);
        }

        public sealed class MapData : IEquatable<MapData>, IDisposable
        {
            const string c_EditorOnly = "EditorOnly";

            private readonly Transform m_Folder = null;
            private readonly MapDataEntityBase m_MapData;
            private readonly Dictionary<MapDataEntityBase.Object, GameObject> m_MapDataObjects = new Dictionary<MapDataEntityBase.Object, GameObject>();
            private readonly Dictionary<MapDataEntityBase.RawObject, GameObject> m_MapDataRawObjects = new Dictionary<MapDataEntityBase.RawObject, GameObject>();

            private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();

            private readonly Dictionary<GameObject, MapDataEntityBase.Object> m_Dictionary = new Dictionary<GameObject, MapDataEntityBase.Object>();
            private readonly Dictionary<GameObject, MapDataEntityBase.RawObject> m_RawDictionary = new Dictionary<GameObject, MapDataEntityBase.RawObject>();

            public bool IsDirty { get; private set; } = false;
            public IReadOnlyList<GameObject> CreatedObjects => m_CreatedObjects;
            public MapDataEntityBase Entity => m_MapData;

            public object this[GameObject i]
            {
                get
                {
                    if (m_Dictionary.TryGetValue(i, out MapDataEntityBase.Object obj)) return obj;
                    else if (m_RawDictionary.TryGetValue(i, out var data)) return data;

                    return null;
                }
            }
            public GameObject this[MapDataEntityBase.Object i]
            {
                get
                {
                    if (m_MapDataObjects.TryGetValue(i, out var data)) return data;
                    return null;
                }
            }
            public GameObject this[MapDataEntityBase.RawObject i]
            {
                get
                {
                    if (m_MapDataRawObjects.TryGetValue(i, out var data)) return data;
                    return null;
                }
            }

            public MapData(Transform parent, Reference<MapDataEntityBase> reference)
            {
                m_MapData = reference.GetObject();

                m_Folder = new GameObject(m_MapData.Name).transform;
                m_Folder.SetParent(parent);

                var dataList = m_MapData.m_Objects.ToList();
                for (int i = 0; i < dataList.Count; i++)
                {
                    GameObject obj = InstantiateObject(m_Folder, dataList[i]);

                    m_CreatedObjects.Add(obj);
                    m_Dictionary.Add(obj, dataList[i]);
                    m_MapDataObjects.Add(dataList[i], obj);
                }

                var rawDataList = m_MapData.m_RawObjects.ToList();
                for (int i = 0; i < rawDataList.Count; i++)
                {
                    GameObject obj = InstantiateObject(m_Folder, rawDataList[i]);

                    m_CreatedObjects.Add(obj);
                    m_RawDictionary.Add(obj, rawDataList[i]);
                    m_MapDataRawObjects.Add(rawDataList[i], obj);
                }
            }

            #region Add & Remove

            public GameObject Add(MapDataEntityBase.Object target)
            {
                GameObject obj = InstantiateObject(m_Folder, target);

                m_MapDataObjects.Add(target, obj);

                m_CreatedObjects.Add(obj);
                m_Dictionary.Add(obj, target);

                m_MapData.m_Objects = m_MapDataObjects.Keys.ToArray();
                IsDirty = true;

                return obj;
            }
            public GameObject Add(MapDataEntityBase.RawObject target)
            {
                GameObject obj = InstantiateObject(m_Folder, target);

                m_MapDataRawObjects.Add(target, obj);

                m_CreatedObjects.Add(obj);
                m_RawDictionary.Add(obj, target);

                m_MapData.m_RawObjects = m_MapDataRawObjects.Keys.ToArray();
                IsDirty = true;

                return obj;
            }
            public void Remove(MapDataEntityBase.Object target)
            {
                GameObject obj = this[target];
                if (obj == null) return;

                Remove(obj);
            }
            public void Remove(GameObject target)
            {
                if (m_Dictionary.TryGetValue(target, out MapDataEntityBase.Object data))
                {
                    m_MapDataObjects.Remove(data);

                    m_CreatedObjects.Remove(target);
                    m_Dictionary.Remove(target);

                    UnityEngine.Object.DestroyImmediate(target);
                    m_MapData.m_Objects = m_MapDataObjects.Keys.ToArray();
                }
                else if (m_RawDictionary.TryGetValue(target, out var rawData))
                {
                    m_MapDataRawObjects.Remove(rawData);

                    m_CreatedObjects.Remove(target);
                    m_RawDictionary.Remove(target);

                    UnityEngine.Object.DestroyImmediate(target);
                    m_MapData.m_RawObjects = m_MapDataRawObjects.Keys.ToArray();
                }
                
                IsDirty = true;
            }

            #endregion

            public void SetDirty()
            {
                IsDirty = true;
            }

            private static GameObject InstantiateObject(Transform parent, MapDataEntityBase.RawObject target)
            {
                GameObject obj;
                if (target.m_Object.IsNone() ||
                    !target.m_Object.IsValid())
                {
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.SetParent(parent);
                }
                else
                {
                    var temp = target.m_Object.GetEditorAsset();
                    if (!(temp is GameObject gameObj))
                    {
                        obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        obj.transform.SetParent(parent);
                    }
                    else
                    {
                        obj = (GameObject)PrefabUtility.InstantiatePrefab(gameObj, parent);
                    }
                    //
                }

                //obj.tag = c_EditorOnly;
                //obj.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                Transform tr = obj.transform;

                tr.position = target.m_Translation;
                tr.rotation = target.m_Rotation;
                tr.localScale = target.m_Scale;

                //obj.isStatic = target.m_Static;
                if (target.m_Static)
                {
                    SetStaticRecursive(obj);
                }

                return obj;
            }
            private static GameObject InstantiateObject(Transform parent, MapDataEntityBase.Object target)
            {
                GameObject obj;
                if (!target.m_Object.IsValid() || 
                    target.m_Object.GetObject().Prefab.IsNone() ||
                    !target.m_Object.GetObject().Prefab.IsValid())
                {
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.SetParent(parent);
                }
                else
                {
                    var temp = target.m_Object.GetObject().Prefab.GetEditorAsset();
                    if (!(temp is GameObject gameObj))
                    {
                        obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        obj.transform.SetParent(parent);
                    }
                    else
                    {
                        obj = (GameObject)PrefabUtility.InstantiatePrefab(gameObj, parent);
                    }
                    //
                }

                //obj.tag = c_EditorOnly;
                //obj.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

                Transform tr = obj.transform;

                tr.position = target.m_Translation;
                tr.rotation = target.m_Rotation;
                tr.localScale = target.m_Scale;

                //obj.isStatic = target.m_Static;
                if (target.m_Static)
                {
                    SetStaticRecursive(obj);
                }

                return obj;
            }
            private static void SetStaticRecursive(GameObject obj)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    if (obj.transform.GetChild(i).childCount > 0)
                    {
                        SetStaticRecursive(obj.transform.GetChild(i).gameObject);
                    }

                    obj.transform.GetChild(i).gameObject.isStatic = true;
                }

                obj.isStatic = true;
            }

            public void Dispose()
            {
                if (IsDirty)
                {
                    EntityDataList.Instance.SaveData(m_MapData);
                }
                
                UnityEngine.Object.DestroyImmediate(m_Folder.gameObject);
            }

            public bool Equals(MapData other) => m_MapData.Equals(other.m_MapData);
        }
    }
}
