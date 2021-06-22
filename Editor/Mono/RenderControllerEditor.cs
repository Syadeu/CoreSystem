using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syadeu.Mono;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SyadeuEditor
{
    [Obsolete][CustomEditor(typeof(RenderController))]
    public class RenderControllerEditor : Editor
    {
        private RenderController m_Render;

        private bool m_ShowOriginalContents = false;

        private bool m_OpenAdditionalRender = true;
        private Behaviour[] m_ChildScriptList;
        private bool[] m_SelectedChildList;

        private RecycleableMonobehaviour m_RecycleMono;

        private void OnEnable()
        {
            m_Render = target as RenderController;
            m_RecycleMono = m_Render.GetComponent<RecycleableMonobehaviour>();

            if (Application.isPlaying) return;

            m_ChildScriptList = m_Render.GetComponentsInChildren<Behaviour>();
            m_SelectedChildList = new bool[m_ChildScriptList.Length];
            for (int i = 0; i < m_ChildScriptList.Length; i++)
            {
                if (m_ChildScriptList[i] == target) continue;
                if (m_Render.AdditionalRenders.Contains(m_ChildScriptList[i]))
                {
                    m_SelectedChildList[i] = true;
                }
            }

            EditorUtils.SortComponentOrder(m_Render, 1, false);
        }
        private void OnValidate()
        {
            m_RecycleMono = m_Render.GetComponent<RecycleableMonobehaviour>();

            if (Application.isPlaying) return;

            m_ChildScriptList = m_Render.GetComponentsInChildren<Behaviour>();
            m_SelectedChildList = new bool[m_ChildScriptList.Length];
            for (int i = 0; i < m_ChildScriptList.Length; i++)
            {
                if (m_ChildScriptList[i] == target) continue;
                if (m_Render.AdditionalRenders.Contains(m_ChildScriptList[i]))
                {
                    m_SelectedChildList[i] = true;
                }
            }
        }

        public static void DrawStatus(RenderController render, bool center)
        {
            string txt = DrawStatus(render);
            EditorUtils.StringRich($"상태: {txt}", center);
        }
        public static string DrawStatus(RenderController render)
        {
            StringColor visibleColor;
            string visibleText;
            if (!Application.isPlaying)
            {
                visibleColor = StringColor.maroon;
                return $"<color={visibleColor}>실행 중이 아님</color>";
            }
            if (render.IsInvisible)
            {
                visibleColor = StringColor.maroon;
                visibleText = "Invisible";
            }
            else
            {
                if (render.IsForcedOff)
                {
                    visibleColor = StringColor.maroon;
                    visibleText = "(Overrided) Invisible";
                }
                else
                {
                    visibleColor = StringColor.teal;
                    visibleText = "Visible";
                }
            }
            return $"<color={visibleColor}>{visibleText}</color>";
        }
        private void DrawRecycleMono()
        {
            StringColor activeColor;
            string activeText;

            if (!Application.isPlaying)
            {
                activeColor = StringColor.maroon;
                EditorUtils.StringRich($"상태: <color={activeColor}>실행 중이 아님</color>", true);
                return;
            }

            if (!m_RecycleMono.Activated)
            {
                activeColor = StringColor.maroon;
                activeText = "비활성화";
            }
            else
            {
                activeColor = StringColor.teal;
                activeText = "활성화";
            }
            EditorUtils.StringRich($"상태: <color={activeColor}>{activeText}</color>", true);
            if (m_RecycleMono.WaitForDeletion)
            {
                EditorUtils.StringRich("삭제 대기 중", StringColor.maroon, true);
            }
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Render Controller");
            EditorUtils.SectorLine();

            EditorUtils.StringRich(m_Render.IsStandalone ? "Standalone" : "Managed", true);
            DrawStatus(m_Render, true);
            if (Application.isPlaying)
            {
                Vector3 screenPoint = m_Render.GetScreenPoint();
                EditorUtils.StringRich(screenPoint.ToString(), true);
            }

            EditorUtils.SectorLine();
            if (m_RecycleMono != null)
            {
                EditorUtils.StringHeader($"{m_RecycleMono.GetType().Name}", 15);
                DrawRecycleMono();
                EditorUtils.SectorLine();
            }

            if (Application.isPlaying) return;

            EditorUtils.StringHeader("Settings", 15);
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel += 1;
            Setting();
            EditorGUI.indentLevel -= 1;
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorGUILayout.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private void Setting()
        {
            var camProperty = serializedObject.FindProperty("m_Camera");
            var offsetProperty = serializedObject.FindProperty("m_Offset");

            if (m_Render.IsStandalone)
            {
                if (camProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("카메라가 설정되지 않았습니다", MessageType.Error);
                }
            }
            m_Render.IsStandalone = EditorGUILayout.ToggleLeft("단독 컨트롤러", m_Render.IsStandalone);
            if (m_Render.IsStandalone)
            {
                EditorGUI.indentLevel += 1;

                EditorGUILayout.PropertyField(camProperty, new GUIContent("> 타겟 카메라: "));
                EditorGUILayout.PropertyField(offsetProperty, new GUIContent("> 오프셋: "));

                EditorGUI.indentLevel -= 1;
                EditorGUILayout.Space();
            }

            EditorGUILayout.HelpBox(
                "RenderController 컴포넌트의 RenderOff(), RenderOn() 메소드를 실행했을때 " +
                "같이 영향을 받을 컴포넌트를 연결할 수 있습니다.\n" +
                "예를 들어, 선택된 컴포넌트는 RenderController에서 RenderOff() 메소드가 실행되었을때 " +
                "선택된 컴포넌트의 enabled 는 false가 됩니다.", MessageType.Info);
            EditorGUI.indentLevel += 1;
            m_OpenAdditionalRender = EditorUtils.Foldout(m_OpenAdditionalRender, "차일드 컴포넌트 연결");
            EditorGUI.indentLevel -= 1;
            if (m_OpenAdditionalRender)
            {
                EditorGUI.indentLevel += 1;

                for (int i = 0; i < m_ChildScriptList.Length; i++)
                {
                    if (m_ChildScriptList[i] == target ||
                        m_ChildScriptList[i] == m_RecycleMono) continue;
                    EditorGUI.BeginChangeCheck();
                    m_SelectedChildList[i] = EditorGUILayout.ToggleLeft($"{m_ChildScriptList[i].name} : {m_ChildScriptList[i].GetType()}", m_SelectedChildList[i]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (m_SelectedChildList[i])
                        {
                            m_Render.AdditionalRenders.Add(m_ChildScriptList[i]);
                        }
                        else
                        {
                            m_Render.AdditionalRenders.Remove(m_ChildScriptList[i]);
                        }
                    }
                }

                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.Space();

            var onVisible = serializedObject.FindProperty("OnVisible");
            var onInvisible = serializedObject.FindProperty("OnInvisible");

            EditorGUILayout.PropertyField(onVisible, new GUIContent("화면에 표시될때 한번만 실행할 함수: OnVisible"));
            EditorGUILayout.PropertyField(onInvisible, new GUIContent("화면에서 제거될때 한번만 실행할 함수: OnInvisible"));

        }

        
    }
}
