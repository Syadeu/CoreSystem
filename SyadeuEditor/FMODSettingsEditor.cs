using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Syadeu.FMOD
{
    [CustomEditor(typeof(FMODSettings))]
    public sealed class FMODSettingsEditor : Editor
    {
        private GUIStyle headerStyle;

        public FMODRoom[] tempRooms;
        public static bool[] enableSoundRoomVisualize;

        bool showGeneralSetting;
        bool showSoundlist;
        bool showSoundroom;

        private void OnEnable()
        {
            headerStyle = new GUIStyle
            {
                richText = true
            };

            Reset();
        }
        void Reset()
        {
            enableSoundRoomVisualize = new bool[FMODSettings.SoundRooms.Count];

            if (FMODRoom.roomFolder != null)
            {
                DestroyImmediate(FMODRoom.roomFolder.gameObject);
                FMODRoom.roomFolder = null;
            }

            FMODRoom.insRooms.Clear();

            tempRooms = new FMODRoom[FMODSettings.SoundRooms.Count];
        }

        public override void OnInspectorGUI()
        {
            if (Application.isPlaying)
            {
                if (FMODRoom.roomFolder != null)
                {
                    DestroyImmediate(FMODRoom.roomFolder.gameObject);
                }

                return;
            }

            EditorGUILayout.LabelField($"<size=20>FMOD System Configuation</size>", headerStyle);
            EditorGUILayout.Space();
            EditorGUILayout.ObjectField(label: "Setting File: ", FMODSettings.Instance, typeof(FMODSettings), false);
            EditorGUILayout.LabelField("_______________________________________________________________________");
            EditorGUILayout.Space();
            EditorGUI.indentLevel += 1;

            EditorGUI.BeginChangeCheck();

            GeneralSettings();
            EditorGUILayout.LabelField("_______________________________________________________________________");
            VisualizeSoundList();
            EditorGUILayout.LabelField("_______________________________________________________________________");
            VisualizeSoundRooms();
            EditorGUILayout.LabelField("_______________________________________________________________________");

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(FMODSettings.Instance);
            }

            EditorGUI.indentLevel -= 1;
            base.OnInspectorGUI();
        }

        void GeneralSettings()
        {
            EditorGUILayout.LabelField("<size=15>General Settings</size>", headerStyle);

            showGeneralSetting = EditorGUILayout.ToggleLeft(label: showGeneralSetting ? "Hide" : "Show", showGeneralSetting);
            if (!showGeneralSetting) return;

            EditorGUI.indentLevel += 1;

            FMODSettings.Instance.m_DisplayLogs = EditorGUILayout.ToggleLeft(label: "Display logs", FMODSettings.Instance.m_DisplayLogs);

            EditorGUI.indentLevel -= 1;
        }
        void VisualizeSoundList()
        {
            EditorGUILayout.LabelField("<size=15>Sound Lists</size>", headerStyle);

            showSoundlist = EditorGUILayout.ToggleLeft(label: showSoundlist ? "Hide" : "Show", showSoundlist);
            if (!showSoundlist) return;

            EditorGUI.indentLevel += 1;

            for (int i = 0; i < FMODSettings.SoundLists.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{FMODSettings.SoundLists[i].listIndex}: {FMODSettings.SoundLists[i].listName}");
                if (GUILayout.Button("Remove"))
                {
                    FMODSettings.SoundLists.RemoveAt(i);
                    i--;
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel += 1;

                EditorGUILayout.ObjectField("Asset: ", FMODSettings.SoundLists[i], typeof(SoundList), false);
                EditorGUILayout.LabelField($"Contains: {FMODSettings.SoundLists[i].fSounds.Count}");

                if (CheckSoundlistError(FMODSettings.SoundLists[i], out string warnning))
                {
                    EditorGUILayout.LabelField("Error Found");
                    EditorGUILayout.HelpBox(warnning, MessageType.Error);
                }

                EditorGUI.indentLevel -= 1;
            }

            EditorGUI.indentLevel -= 1;
        }

        bool CheckSoundlistError(SoundList soundList, out string warnning)
        {
            warnning = null;

            List<int> temp = new List<int>();
            for (int i = 0; i < soundList.fSounds.Count; i++)
            {
                if (temp.Contains(soundList.fSounds[i].index))
                {
                    StringBuilder(ref warnning, $"겹치는 인덱스 번호({soundList.fSounds[i].index})가 존재합니다.\n" +
                    "동일한 인덱스 번호를 가진 사운드는 존재할 수 없습니다.");
                }
                else temp.Add(soundList.fSounds[i].index);
            }

            for (int i = 0; i < soundList.fSounds.Count; i++)
            {
                if (FMODUnity.EventManager.EventFromPath(soundList.fSounds[i].eventClips[0].Event) == null)
                {
                    StringBuilder(ref warnning, $"{soundList.fSounds[i].index}: 이벤트가 존재하지않음");
                }
            }

            if (string.IsNullOrEmpty(warnning)) return false;
            return true;
        }

        void StringBuilder(ref string value, string add)
        {
            if (string.IsNullOrEmpty(value))
            {
                value += add;
            }
            else
            {
                value += $"\n{add}";
            }
        }

        void VisualizeSoundRooms()
        {
            EditorGUILayout.LabelField("<size=15>Room Settings</size>", headerStyle);

            showSoundroom = EditorGUILayout.ToggleLeft(showSoundroom ? "Hide" : "Show", showSoundroom);
            if (!showSoundroom)
            {
                if (tempRooms != null)
                {
                    for (int i = 0; i < tempRooms.Length; i++)
                    {
                        if (tempRooms[i] == null) continue;
                        tempRooms[i].drawBounds = false;
                    }
                }

                return;
            }

            EditorGUI.indentLevel += 1;

            if (GUILayout.Button("Add"))
            {
                FMODSettings.SoundRooms.Add(SoundRoom.Null);
                Reset();
                Repaint();
                return;
            }

            for (int i = 0; i < FMODSettings.SoundRooms.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Room {i}");
                if (GUILayout.Button("Remove"))
                {
                    FMODSettings.SoundRooms.RemoveAt(i);
                    i--;
                    Reset();
                    Repaint();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel += 1;

                EditorGUILayout.BeginVertical();
                enableSoundRoomVisualize[i] = EditorGUILayout.ToggleLeft("Edit", enableSoundRoomVisualize[i]);

                if (tempRooms[i] == null)
                {
                    tempRooms[i] = FMODRoom.Set(FMODSettings.SoundRooms[i]);
                }
                tempRooms[i].drawBounds = enableSoundRoomVisualize[i];

                if (enableSoundRoomVisualize[i])
                {
                    for (int a = 0; a < enableSoundRoomVisualize.Length; a++)
                    {
                        if (a != i)
                        {
                            enableSoundRoomVisualize[a] = false;
                        }
                    }

                    tempRooms[i].transform.position = EditorGUILayout.Vector3Field("Position: ", tempRooms[i].transform.position);
                    tempRooms[i].backgroundType = EditorGUILayout.IntField(label: "Background Type: ", tempRooms[i].backgroundType);
                    tempRooms[i].directOcclusion = EditorGUILayout.Slider("Direct: ", tempRooms[i].directOcclusion, 0, 1);

                    Vector3[] vertices = new Vector3[6];
                    for (int b = 0; b < vertices.Length; b++)
                    {
                        vertices[b] = tempRooms[i].m_Vertices[b].transform.localPosition;
                    }

                    FMODSettings.SoundRooms[i] =
                        new SoundRoom(tempRooms[i].backgroundType, tempRooms[i].bounds, tempRooms[i].directOcclusion, tempRooms[i].transform.position, vertices);
                }
                else
                {
                    EditorGUILayout.LabelField($"Background Type: {FMODSettings.SoundRooms[i].BackgroundType}");
                    EditorGUILayout.LabelField($"Direct: {FMODSettings.SoundRooms[i].Direct}");
                }
                EditorGUILayout.EndVertical();

                EditorGUI.indentLevel -= 1;
            }

            EditorGUI.indentLevel -= 1;
        }

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawBounds(FMODRoom room, GizmoType gizmoType)
        {
            if (!room.drawBounds) return;

            room.bounds = new Bounds
            {
                center = room.transform.position,
                size = Vector3.Scale(room.transform.lossyScale, room.transform.localScale)
            };
            room.bounds.Encapsulate(room.m_Vertices[0].transform.position);
            room.bounds.Encapsulate(room.m_Vertices[1].transform.position);
            room.bounds.Encapsulate(room.m_Vertices[2].transform.position);
            room.bounds.Encapsulate(room.m_Vertices[3].transform.position);
            room.bounds.Encapsulate(room.m_Vertices[4].transform.position);
            room.bounds.Encapsulate(room.m_Vertices[5].transform.position);

            Vector3 center = room.bounds.center;
            // bottom
            room.m_Vertices[0].transform.position = new Vector3(center.x, room.m_Vertices[0].transform.position.y, center.z);
            // left
            room.m_Vertices[1].transform.position = new Vector3(room.m_Vertices[1].transform.position.x, center.y, center.z);
            // right
            room.m_Vertices[2].transform.position = new Vector3(room.m_Vertices[2].transform.position.x, center.y, center.z);
            //forward
            room.m_Vertices[3].transform.position = new Vector3(center.x, center.y, room.m_Vertices[3].transform.position.z);
            // backward
            room.m_Vertices[4].transform.position = new Vector3(center.x, center.y, room.m_Vertices[4].transform.position.z);
            // upward
            room.m_Vertices[5].transform.position = new Vector3(center.x, room.m_Vertices[5].transform.position.y, center.z);


            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawCube(room.bounds.center, room.bounds.size);
            Gizmos.color = new Color(0, 0.7f, 0, 0.7f);
            Gizmos.DrawWireCube(room.bounds.center, room.bounds.size);
        }

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawEditCenterCube(FMODRoom room, GizmoType gizmoType)
        {
            if (!room.drawBounds) return;

            Gizmos.color = new Color(0, 0.3f, 0.5f, 0.7f);
            Gizmos.DrawCube(room.bounds.center, Vector3.one * 0.1f);
        }

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Selected | GizmoType.Pickable)]
        static void DrawEditCenterCube(FMODRoomVertice vertice, GizmoType gizmoType)
        {
            if (vertice.room == null || !vertice.room.drawBounds) return;

            Gizmos.color = new Color(0.9f, 0, 0.1f, 0.8f);
            Gizmos.DrawCube(vertice.transform.position, Vector3.one * 0.05f);
        }
    }
}
