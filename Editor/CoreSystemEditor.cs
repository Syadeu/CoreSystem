using Syadeu;
using Syadeu.Extensions.Logs;
using Syadeu.FMOD;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CoreSystem))]
    public sealed class CoreSystemEditor : Editor
    {
        SceneGUIBox sceneGUI = new SceneGUIBox();

        private void OnSceneGUI()
        {
            CoreSystem ins = target as CoreSystem;

            //sceneGUI = new SceneGUIBox();
            sceneGUI.Label("CoreSystem Test Label");
            sceneGUI.Label("CoreSystem Test Label3");
            //sceneGUI.BeginHorizontal();
            sceneGUI.Label("test1");
            sceneGUI.Button("test2", () => { "test2 pushed".ToLog(); });
            //sceneGUI.EndHorizontal();
            sceneGUI.Label("test3");
            sceneGUI.Label("test4");
            sceneGUI.Button("test5 longongongongong", () => { "test5 pushed".ToLog(); });
            //sceneGUI.Button("test6", () => { "test6 pushed".ToLog(); });
            
            sceneGUI.Label("end");
            
            sceneGUI.Draw(ins.transform.position);
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("CoreSystem");
            EditorUtils.SectorLine();

            EditorGUILayout.Space();

            {
                sceneGUI.m_BorderOffset = EditorGUILayout.Vector2Field("Border Offset", sceneGUI.m_BorderOffset);
                sceneGUI.m_TextSpace = EditorGUILayout.FloatField("Text Space", sceneGUI.m_TextSpace);
            }

            if (Application.isPlaying)
            {
                Runtime();
                EditorUtils.SectorLine();
                FMOD();
            }
            else EditorUtils.StringRich("이 시스템은 실행 중에만 정보를 표시합니다", 12, StringColor.maroon, true);
        }

        bool m_OpenManagerList = false;
        bool m_OpenInsManagerList = false;
        bool m_OpenDataManagerList = false;
        void Runtime()
        {
            EditorUtils.StringHeader("Generals", 15);
            EditorGUI.indentLevel += 1;

            #region Manager
            m_OpenManagerList = EditorUtils.Foldout(m_OpenManagerList, $"현재 생성된 파괴불가 매니저: {CoreSystem.GetStaticManagers().Count}개");
            if (m_OpenManagerList)
            {
                EditorGUI.indentLevel += 1;
                
                for (int i = 0; i < CoreSystem.GetStaticManagers().Count; i++)
                {
                    if (CoreSystem.GetStaticManagers()[i].HideInHierarchy)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.LabelField($"> {CoreSystem.GetStaticManagers()[i].GetType().Name}", new GUIStyle("TextField"));
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        if (EditorUtils.Button($"> {CoreSystem.GetStaticManagers()[i].GetType().Name}", "TextField", 1))
                        {
                            EditorGUIUtility.PingObject(CoreSystem.GetStaticManagers()[i].gameObject);
                        }
                    }
                }
                
                EditorGUI.indentLevel -= 1;
            }

            IReadOnlyList<IStaticMonoManager> _insMgrs = CoreSystem.GetInstanceManagers();
            m_OpenInsManagerList = EditorUtils.Foldout(m_OpenInsManagerList, $"현재 생성된 인스턴스 매니저: {_insMgrs.Count}개");
            if (m_OpenInsManagerList)
            {
                EditorGUI.indentLevel += 1;

                for (int i = 0; i < _insMgrs.Count; i++)
                {
                    if (_insMgrs[i].HideInHierarchy)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.LabelField($"> {_insMgrs[i].GetType().Name}", new GUIStyle("TextField"));
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        if (EditorUtils.Button($"> {_insMgrs[i].GetType().Name}", "TextField", 1))
                        {
                            EditorGUIUtility.PingObject(_insMgrs[i].gameObject);
                        }
                    }
                }

                EditorGUI.indentLevel -= 1;
            }

            IReadOnlyList<IStaticDataManager> _dataMgrs = CoreSystem.GetDataManagers();
            EditorUtils.ShowSimpleListLabel(ref m_OpenDataManagerList, 
                $"현재 생성된 데이터 매니저: {_dataMgrs.Count}개",
                _dataMgrs, new GUIStyle("TextField"), true);
            #endregion

            EditorGUI.indentLevel -= 1;
            EditorUtils.SectorLine();

            EditorUtils.StringHeader("Routines", 15);
            EditorGUI.indentLevel += 1;

            #region Routine
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.IntField("백그라운드 루틴", CoreSystem.Instance.GetCustomBackgroundUpdateCount());
            EditorGUILayout.IntField("유니티 루틴", CoreSystem.Instance.GetCustomUpdateCount());
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel -= 1;
            #endregion

            EditorGUILayout.Space();

            EditorUtils.StringHeader("Jobs", 15);
            EditorGUI.indentLevel += 1;

            #region Job
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField($"생성된 백그라운드 워커", CoreSystem.Instance.GetBackgroundJobWorkerCount());
            EditorGUILayout.IntField($"가동중인 백그라운드 워커", CoreSystem.Instance.GetCurrentRunningBackgroundWorkerCount());
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("등록된 잡 갯수");
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Background", CoreSystem.Instance.GetBackgroundJobCount());
            EditorGUILayout.IntField("Foreground", CoreSystem.Instance.GetForegroundJobCount());
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            #endregion

            EditorGUI.indentLevel -= 1;
        }

        Dictionary<string, int> m_FMODPlaylist = new Dictionary<string, int>();
        bool m_OpenFMODSoundList = false;
        void FMOD()
        {
            EditorUtils.StringHeader("FMOD", 15);
            EditorGUI.indentLevel += 1;

            if (FMODSystem.Initialized == false)
            {
                EditorGUILayout.LabelField("현재 표시할 정보가 없음");
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);

            int activatedCount = 0;
            for (int i = 0; i < FMODSound.Instances.Count; i++)
            {
                if (FMODSound.Instances[i].Activated)
                {
                    activatedCount += 1;
                }
            }

            EditorGUILayout.IntField("현재 인스턴스 갯수: ", FMODSound.Instances.Count);
            EditorGUILayout.IntField("현재 사용중인 갯수: ", activatedCount);

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            m_OpenFMODSoundList = EditorGUILayout.Foldout(m_OpenFMODSoundList, m_OpenFMODSoundList ? "리스트 닫기" : "리스트 열기");
            if (m_OpenFMODSoundList)
            {
                EditorGUI.indentLevel += 1;

                m_FMODPlaylist.Clear();
                List<string> currentPlaylistNames = new List<string>();
                IReadOnlyList<FMODSound> currentPlaylist = FMODSystem.GetPlayList();

                for (int i = 0; i < currentPlaylist.Count; i++)
                {
                    currentPlaylistNames.Add(currentPlaylist[i].SoundGUID.EventPath);
                    if (!m_FMODPlaylist.ContainsKey(currentPlaylist[i].SoundGUID.EventPath))
                    {
                        m_FMODPlaylist.Add(currentPlaylist[i].SoundGUID.EventPath, 1);
                    }
                    else
                    {
                        m_FMODPlaylist[currentPlaylist[i].SoundGUID.EventPath] += 1;
                    }
                }

                var list = m_FMODPlaylist.Keys.ToArray();
                for (int i = 0; i < list.Length; i++)
                {
                    EditorGUILayout.LabelField($"> {list[i]}: {m_FMODPlaylist[list[i]]}개 재생 중");
                }

                EditorGUI.indentLevel -= 1;
            }

            EditorGUI.indentLevel -= 1;
        }
    }
}
