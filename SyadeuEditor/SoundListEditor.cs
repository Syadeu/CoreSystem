using System;
using System.Collections.Generic;
using System.Linq;

using Syadeu.FMOD;

using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(SoundList))]
    public class SoundListEditor : Editor
    {
        private SoundList SoundList { get; set; }
        private GUIStyle headerStyle;

        private bool enableDuplicate = false;
        private bool[] showSounds;
        private bool[] showAudio;
        private List<bool[]> showAudioClipsList;
        private bool[] showChangeIndex;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            SoundList = target as SoundList;

            headerStyle = new GUIStyle
            {
                richText = true
            };

            showSounds = new bool[SoundList.fSounds.Count];
            showAudio = new bool[SoundList.fSounds.Count];
            showAudioClipsList = new List<bool[]>();
            for (int i = 0; i < SoundList.fSounds.Count; i++)
            {
                showAudioClipsList.Add(new bool[SoundList.fSounds[i].eventClips.Count]);
            }
            showChangeIndex = new bool[SoundList.fSounds.Count];
        }

        public override void OnInspectorGUI()
        {
            if (Application.isPlaying || SoundList == null) return;
            EditorGUILayout.LabelField(string.Format("<size=20>List Configuation</size> :: {0}", SoundList.listName), headerStyle);
            EditorGUILayout.LabelField("_______________________________________________________________________");
            EditorGUILayout.Space();

            if (FMODSettings.Instance.m_SoundLists.Contains(SoundList))
            {
                if (GUILayout.Button("사운드 리스트에서 제거"))
                {
                    FMODSettings.Instance.m_SoundLists.Remove(SoundList);
                    EditorUtility.SetDirty(FMODSettings.Instance);
                }
            }
            else
            {
                if (GUILayout.Button("사운드 리스트에 추가"))
                {
                    FMODSettings.Instance.m_SoundLists.Add(SoundList);
                    EditorUtility.SetDirty(FMODSettings.Instance);
                }
            }

            #region 초기 세팅
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            SoundList.listIndex = EditorGUILayout.IntField(label: "글로벌 인덱스 : ", SoundList.listIndex);
            SoundList.listName = EditorGUILayout.TextField(label: "리스트 이름 :", SoundList.listName);
            
            GUILayout.BeginHorizontal();
            enableDuplicate = EditorGUILayout.ToggleLeft("Enable Duplicate", enableDuplicate);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (SoundList.fSounds.Count > 0)
            {
                EditorGUILayout.LabelField(label: "사운드 갯수 : " + SoundList.fSounds.Count);
            }
            if (GUILayout.Button("추가"))
            {
                SoundList.fSounds.Add(new SoundList.FInput());
            }
            if (SoundList.fSounds.Count > 0)
            {
                if (GUILayout.Button("제거"))
                {
                    SoundList.fSounds.RemoveAt(SoundList.fSounds.Count - 1);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(SoundList);

                showSounds = new bool[SoundList.fSounds.Count];
                showAudio = new bool[SoundList.fSounds.Count];
                showAudioClipsList.Clear();
                for (int i = 0; i < SoundList.fSounds.Count; i++)
                {
                    showAudioClipsList.Add(new bool[SoundList.fSounds[i].eventClips.Count]);
                }
                showChangeIndex = new bool[SoundList.fSounds.Count];

                Repaint();
            }

            if (SoundList.fSounds == null && SoundList.fSounds.Count == 0) return;

            #endregion

            if (showSounds == null) return;

            if (showSounds.Length != SoundList.fSounds.Count)
            {
                showSounds = new bool[SoundList.fSounds.Count];
                showAudio = new bool[SoundList.fSounds.Count];
                showAudioClipsList.Clear();
                for (int i = 0; i < SoundList.fSounds.Count; i++)
                {
                    showAudioClipsList.Add(new bool[SoundList.fSounds[i].eventClips.Count]);
                }
                showChangeIndex = new bool[SoundList.fSounds.Count];
                Repaint();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel += 1;

            int totalCount;
            totalCount = SoundList.fSounds.Count;

            for (int i = 0; i < totalCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                showSounds[i] = EditorGUILayout.ToggleLeft(label: string.Format("<size=13>{0} : {1}</size>", SoundList.fSounds[i].index, SoundList.fSounds[i].name), showSounds[i], headerStyle);

                if (enableDuplicate)
                {
                    if (GUILayout.Button("Duplicate"))
                    {
                        SoundList.FInput dup = (SoundList.FInput)SoundList.fSounds[i].Clone();

                        SoundList.fSounds.Add(dup);

                        showSounds = new bool[SoundList.fSounds.Count];
                        showAudio = new bool[SoundList.fSounds.Count];
                        showAudioClipsList.Clear();
                        for (int _i = 0; _i < SoundList.fSounds.Count; _i++)
                        {
                            showAudioClipsList.Add(new bool[SoundList.fSounds[_i].eventClips.Count]);
                        }
                        showChangeIndex = new bool[SoundList.fSounds.Count];
                        Repaint();
                        return;
                    }
                }
                if (GUILayout.Button("제거"))
                {
                    SoundList.fSounds.Remove(SoundList.fSounds[i]);

                    showSounds = new bool[SoundList.fSounds.Count];
                    showAudio = new bool[SoundList.fSounds.Count];
                    showAudioClipsList.Clear();
                    for (int _i = 0; _i < SoundList.fSounds.Count; _i++)
                    {
                        showAudioClipsList.Add(new bool[SoundList.fSounds[_i].eventClips.Count]);
                    }
                    showChangeIndex = new bool[SoundList.fSounds.Count];

                    Repaint();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel += 1;
                if (showSounds[i])
                {
                    EditorGUILayout.BeginHorizontal();
                    int tempIndex;
                    if (SoundList.fSounds[i] == null)
                    {
                        tempIndex = 0;
                    }
                    else tempIndex = SoundList.fSounds[i].index;
                    EditorGUILayout.LabelField(label: "로컬 인덱스 : " + tempIndex.ToString());

                    #region 인덱스 변경관련

                    //if (showChangeIndex[i] == null) return;
                    showChangeIndex[i] = EditorGUILayout.ToggleLeft(label: "인덱스 변경", showChangeIndex[i]);
                    EditorGUILayout.EndHorizontal();
                    if (showChangeIndex[i])
                    {
                        EditorGUILayout.Space();
                        SoundList.fSounds[i].index = EditorGUILayout.IntField(label: "인덱스 : ", SoundList.fSounds[i].index);

                        EditorGUILayout.LabelField("______________________________________________________________");
                        EditorGUI.indentLevel -= 1;
                        continue;
                    }

                    #endregion

                    if (SoundList.fSounds[i].index == 0)
                    {
                        EditorGUILayout.LabelField("로컬 인덱스를 먼저 추가해주세요");
                        EditorGUILayout.LabelField("______________________________________________________________");
                        EditorGUI.indentLevel -= 1;
                        continue;
                    }
                    else if (SoundList.fSounds[i].index == 999999)
                    {
                        EditorGUILayout.LabelField("입력된 값이 잘못되었습니다");
                        EditorGUILayout.LabelField("______________________________________________________________");
                        EditorGUI.indentLevel -= 1;
                        continue;
                    }

                    EditorGUILayout.LabelField("______________________________________________________________");
                    SoundList.fSounds[i].name = EditorGUILayout.TextField(label: "사운드 이름 :", SoundList.fSounds[i].name);

                    //EditorGUILayout.LabelField("______________________________________________________________");

                    EditorGUILayout.LabelField("______________________________________________________________");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("사운드 갯수 : " + SoundList.fSounds[i].eventClips.Count);
                    if (GUILayout.Button("추가"))
                    {
                        if (SoundList.fSounds[i].eventClips.Count < 1)
                        {
                            SoundList.fSounds[i].eventClips.Add(new SoundList.EventClips());
                            showAudioClipsList.Clear();
                            for (int _i = 0; _i < SoundList.fSounds.Count; _i++)
                            {
                                showAudioClipsList.Add(new bool[SoundList.fSounds[i].eventClips.Count]);
                            }
                        }
                    }
                    if (GUILayout.Button("제거"))
                    {
                        SoundList.fSounds[i].eventClips.RemoveAt(SoundList.fSounds[i].eventClips.Count - 1);
                        showAudioClipsList.Clear();
                        for (int _i = 0; _i < SoundList.fSounds.Count; _i++)
                        {
                            showAudioClipsList.Add(new bool[SoundList.fSounds[i].eventClips.Count]);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    showAudio[i] = EditorGUILayout.ToggleLeft(label: "Event Clip", showAudio[i]);
                    if (showAudio[i] && SoundList.fSounds[i].eventClips.Count != 0)
                    {
                        EditorGUI.indentLevel += 1;
                        EventClips(SoundList.fSounds[i], i);
                        EditorGUI.indentLevel -= 1;
                    }
                    EditorGUILayout.LabelField("______________________________________________________________");
                }
                else
                {
                    showChangeIndex[i] = false;
                }
                EditorGUI.indentLevel -= 1;
            }
            EditorGUI.indentLevel -= 1;

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(SoundList);
                Repaint();
            }

            EditorGUILayout.Space();
            if (!CheckDataLegit(out string warnning))
            {
                EditorGUILayout.HelpBox(warnning, MessageType.Error, true);
            }

            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        void EventClips(SoundList.FInput sound, int count)
        {
            if (showAudioClipsList.Count != SoundList.fSounds.Count)
            {
                showAudioClipsList = new List<bool[]>();
                for (int i = 0; i < SoundList.fSounds.Count; i++)
                {
                    showAudioClipsList.Add(new bool[SoundList.fSounds[i].eventClips.Count]);
                }
            }

            #region FMOD Event generals

            var ev = serializedObject.FindProperty($"fSounds.Array.data[{count}].eventClips.Array.data[{0}].Event");
            if (ev != null) EditorGUILayout.PropertyField(ev, new GUIContent("Event"));
            
            FMODUnity.EditorEventRef editorEvent = FMODUnity.EventManager.EventFromPath(sound.eventClips[0].Event);

            if (editorEvent != null)
            {
                {
                    EditorGUI.BeginDisabledGroup(editorEvent == null || !editorEvent.Is3D);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Override Attenuation");
                    EditorGUI.BeginChangeCheck();
                    sound.eventClips[0].OverrideAttenuation = EditorGUILayout.Toggle(sound.eventClips[0].OverrideAttenuation, GUILayout.Width(20));
                    if (EditorGUI.EndChangeCheck() ||
                        (sound.eventClips[0].OverrideMinDistance == -1 && sound.eventClips[0].OverrideMaxDistance == -1) // never been initialiased
                        )
                    {
                        sound.eventClips[0].OverrideMinDistance = editorEvent.MinDistance;
                        sound.eventClips[0].OverrideMaxDistance = editorEvent.MaxDistance;
                    }
                    EditorGUI.BeginDisabledGroup(sound.eventClips[0].OverrideAttenuation);
                    EditorGUIUtility.labelWidth = 30;
                    sound.eventClips[0].OverrideMinDistance = EditorGUILayout.FloatField("Min", sound.eventClips[0].OverrideMinDistance);
                    sound.eventClips[0].OverrideMinDistance = Mathf.Clamp(sound.eventClips[0].OverrideMinDistance, 0, sound.eventClips[0].OverrideMaxDistance);
                    sound.eventClips[0].OverrideMaxDistance = EditorGUILayout.FloatField("Max", sound.eventClips[0].OverrideMaxDistance);
                    sound.eventClips[0].OverrideMaxDistance = Mathf.Max(sound.eventClips[0].OverrideMinDistance, sound.eventClips[0].OverrideMaxDistance);
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();
                }

                //sound.eventClips[i].Params = EditorGUILayout.Foldout(sound.eventClips[i].Params, "Initial Parameter Values");
                var eventRef = FMODUnity.EventManager.EventFromPath(sound.eventClips[0].Event);
                if (eventRef != null)
                {
                    foreach (var paramRef in eventRef.Parameters)
                    {
                        bool set;
                        float value;
                        bool matchingSet, matchingValue;
                        CheckParameter(sound.eventClips[0], paramRef.Name, out set, out matchingSet, out value, out matchingValue);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel(paramRef.Name);
                        EditorGUI.showMixedValue = !matchingSet;
                        EditorGUI.BeginChangeCheck();
                        bool newSet = EditorGUILayout.Toggle(set, GUILayout.Width(20));
                        EditorGUI.showMixedValue = false;

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(serializedObject.isEditingMultipleObjects ? serializedObject.targetObjects : new UnityEngine.Object[] { serializedObject.targetObject }, "Inspector");
                            if (newSet)
                            {
                                AddParameterValue(paramRef.Name, paramRef.Default);
                            }
                            else
                            {
                                DeleteParameterValue(paramRef.Name);
                            }
                            set = newSet;
                        }

                        EditorGUI.BeginDisabledGroup(!newSet);
                        if (set)
                        {
                            EditorGUI.showMixedValue = !matchingValue;
                            EditorGUI.BeginChangeCheck();
                            value = EditorGUILayout.Slider(value, paramRef.Min, paramRef.Max);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObjects(serializedObject.isEditingMultipleObjects ? serializedObject.targetObjects : new UnityEngine.Object[] { serializedObject.targetObject }, "Inspector");
                                SetParameterValue(paramRef.Name, value);
                            }
                            EditorGUI.showMixedValue = false;
                        }
                        else
                        {
                            EditorGUI.showMixedValue = !matchingValue;
                            EditorGUILayout.Slider(paramRef.Default, paramRef.Min, paramRef.Max);
                            EditorGUI.showMixedValue = false;
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();
                    }
                }

                //fadeout.isExpanded = EditorGUILayout.Foldout(fadeout.isExpanded, "Advanced Controls");
                sound.eventClips[0].Preload = EditorGUILayout.ToggleLeft("Preload Sample Data", sound.eventClips[0].Preload);
                sound.eventClips[0].AllowFadeout = EditorGUILayout.ToggleLeft("Allow Fadeout When Stopping", sound.eventClips[0].AllowFadeout);
                sound.eventClips[0].TriggerOnce = EditorGUILayout.ToggleLeft("Trigger Once", sound.eventClips[0].TriggerOnce);
            }

            #endregion

            EditorGUILayout.Space();

            sound.eventClips[0].type = EditorGUILayout.IntField(label: "종류 :", sound.eventClips[0].type);
        }

        //[InitializeOnLoadMethod]
        bool CheckDataLegit(out string warnning)
        {
            warnning = null;

            List<int> temp = new List<int>();
            for (int i = 0; i < SoundList.fSounds.Count; i++)
            {
                if (temp.Contains(SoundList.fSounds[i].index))
                {
                    //if (EditorApplication.isPlaying)
                    //{
                    //    EditorApplication.isPlaying = false;
                    //    //throw new System.Exception("동일한 인덱스 번호를 가진 사운드는 존재할 수 없습니다.");
                    //}
                    warnning += $"겹치는 인덱스 번호({SoundList.fSounds[i].index})가 존재합니다.\n" +
                    "동일한 인덱스 번호를 가진 사운드는 존재할 수 없습니다.\n";
                }
                else temp.Add(SoundList.fSounds[i].index);
            }

            for (int i = 0; i < SoundList.fSounds.Count; i++)
            {
                if (SoundList.fSounds[i].eventClips == null ||
                    SoundList.fSounds[i].eventClips.Count == 0) continue;

                if (FMODUnity.EventManager.EventFromPath(SoundList.fSounds[i].eventClips[0].Event) == null)
                {
                    warnning += $"{SoundList.fSounds[i].index}: 이벤트가 존재하지않음\n";
                }
            }

            if (string.IsNullOrEmpty(warnning)) return true;
            return false;
        }

        void CheckParameter(SoundList.EventClips eventClips, string name, out bool set, out bool matchingSet, out float value, out bool matchingValue)
        {
            value = 0;
            set = false;
            if (serializedObject.isEditingMultipleObjects)
            {
                bool first = true;
                matchingValue = true;
                matchingSet = true;
                foreach (var obj in serializedObject.targetObjects)
                {
                    //var emitter = obj as StudioEventEmitter;
                    var emitter = eventClips;
                    var param = emitter.Params != null ? emitter.Params.FirstOrDefault((x) => x.Name == name) : null;
                    if (first)
                    {
                        set = param != null;
                        value = set ? param.Value : 0;
                        first = false;
                    }
                    else
                    {
                        if (set)
                        {
                            if (param == null)
                            {
                                matchingSet = false;
                                matchingValue = false;
                                return;
                            }
                            else
                            {
                                if (param.Value != value)
                                {
                                    matchingValue = false;
                                }
                            }
                        }
                        else
                        {
                            if (param != null)
                            {
                                matchingSet = false;
                            }
                        }
                    }
                }
            }
            else
            {
                matchingSet = matchingValue = true;

                //var emitter = serializedObject.targetObject as StudioEventEmitter;
                var emitter = eventClips;
                var param = emitter.Params != null ? emitter.Params.FirstOrDefault((x) => x.Name == name) : null;
                if (param != null)
                {
                    set = true;
                    value = param.Value;
                }
            }
        }

        void SetParameterValue(string name, float value)
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                foreach (var obj in serializedObject.targetObjects)
                {
                    SetParameterValue(obj, name, value);
                }
            }
            else
            {
                SetParameterValue(serializedObject.targetObject, name, value);
            }
        }

        void SetParameterValue(UnityEngine.Object obj, string name, float value)
        {
            var emitter = obj as FMODUnity.StudioEventEmitter;
            var param = emitter.Params != null ? emitter.Params.FirstOrDefault((x) => x.Name == name) : null;
            if (param != null)
            {
                param.Value = value;
            }
        }

        void AddParameterValue(string name, float value)
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                foreach (var obj in serializedObject.targetObjects)
                {
                    AddParameterValue(obj, name, value);
                }
            }
            else
            {
                AddParameterValue(serializedObject.targetObject, name, value);
            }
        }

        void AddParameterValue(UnityEngine.Object obj, string name, float value)
        {
            var emitter = obj as FMODUnity.StudioEventEmitter;
            var param = emitter.Params != null ? emitter.Params.FirstOrDefault((x) => x.Name == name) : null;
            if (param == null)
            {
                int end = emitter.Params.Length;
                Array.Resize(ref emitter.Params, end + 1);
                emitter.Params[end] = new FMODUnity.ParamRef();
                emitter.Params[end].Name = name;
                emitter.Params[end].Value = value;
            }
        }

        void DeleteParameterValue(string name)
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                foreach (var obj in serializedObject.targetObjects)
                {
                    DeleteParameterValue(obj, name);
                }
            }
            else
            {
                DeleteParameterValue(serializedObject.targetObject, name);
            }
        }

        void DeleteParameterValue(UnityEngine.Object obj, string name)
        {
            var emitter = obj as FMODUnity.StudioEventEmitter;
            int found = -1;
            for (int i = 0; i < emitter.Params.Length; i++)
            {
                if (emitter.Params[i].Name == name)
                {
                    found = i;
                }
            }
            if (found >= 0)
            {
                int end = emitter.Params.Length - 1;
                emitter.Params[found] = emitter.Params[end];
                Array.Resize(ref emitter.Params, end);
            }
        }
    }
}
