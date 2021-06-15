using Syadeu;
using SyadeuEditor.Tree;
#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#endif

using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CoreSystem))]
    public sealed class CoreSystemEditor : EditorEntity<CoreSystem>
    {
        //private bool m_OpenBackgroundRoutines = false;

        private void OnEnable()
        {
            m_RoutinesView = new VerticalTreeView(Asset);
            m_RoutinesView.MakeCustomSearchFilter((e, str) =>
            {
                str = str.ToLower();
                if (e is RoutineTreeElement routineElement)
                {
                    if (routineElement.Routine.ObjectName.ToLower().Contains(str)) return true;
                }
                else if (e.Name.ToLower().Contains(str))
                {
                    return true;
                }
                return false;
            });
            if (Application.isPlaying)
            {
                CoreSystem.Instance.OnRoutineChanged += Instance_OnRoutineChanged;
            }
        }
        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                CoreSystem.Instance.OnRoutineChanged -= Instance_OnRoutineChanged;
            }
        }

        private void Instance_OnRoutineChanged()
        {
            if (m_Routines.Count != CoreSystem.Instance.GetCustomBackgroundUpdateCount() + CoreSystem.Instance.GetCustomUpdateCount())
            {
                ValidateRoutineView();
            }
        }

        VerticalTreeView m_RoutinesView;
        List<CoreRoutine> m_Routines = new List<CoreRoutine>();
        private class RoutineTreeElement : VerticalTreeElement
        {
            private CoreRoutine m_Routine;
            public CoreRoutine Routine => m_Routine;

            public RoutineTreeElement(VerticalTreeViewEntity tree, CoreRoutine routine) : base(tree)
            {
                m_Routine = routine;
                m_Name = routine.ObjectName.Split('+')[1];
            }

            public override object Data => m_Routine;

            public override void OnGUI()
            {
                //throw new System.NotImplementedException();
            }

            protected override void OnRemove()
            {
                //throw new System.NotImplementedException();
            }
        }
        private void ValidateRoutineView()
        {
            IReadOnlyList<CoreRoutine> backgroundRoutines = CoreSystem.Instance.GetCustomBackgroundUpdates();
            IReadOnlyList<CoreRoutine> foregroundRoutines = CoreSystem.Instance.GetCustomUpdates();

            m_Routines.Clear();
            m_Routines.AddRange(backgroundRoutines);
            m_Routines.AddRange(foregroundRoutines);

            m_RoutinesView
                .SetupElements(m_Routines, (other) =>
                {
                    CoreRoutine routine = (CoreRoutine)other;
                    VerticalFolderTreeElement folder; RoutineTreeElement element;
                    if (routine.IsEditor)
                    {
                        folder = m_RoutinesView.GetOrCreateFolder("Editor Routine");
                        element = new RoutineTreeElement(m_RoutinesView, routine);
                    }
                    else if (routine.IsBackground)
                    {
                        folder = m_RoutinesView.GetOrCreateFolder("Background Routine");
                        element = new RoutineTreeElement(m_RoutinesView, routine);
                    }
                    else
                    {
                        folder = m_RoutinesView.GetOrCreateFolder("Foreground Routine");
                        element = new RoutineTreeElement(m_RoutinesView, routine);
                    }
                    string objPath = routine.ObjectName.Split('+')[0];
                    var topFolder = m_RoutinesView.GetOrCreateFolder(objPath);
                    element.SetParent(topFolder);

                    topFolder.SetParent(folder);
                    return element;
                });
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("CoreSystem");
            EditorUtils.SectorLine();

            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                Runtime();
#if CORESYSTEM_FMOD
                EditorUtils.SectorLine();
                FMOD();
#endif
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
            m_RoutinesView.OnGUI();

            //m_OpenBackgroundRoutines = EditorGUILayout.Foldout(m_OpenBackgroundRoutines, "Open Background Routines");
            //if (m_OpenBackgroundRoutines)
            //{
            //    IReadOnlyList<CoreRoutine> backgroundRoutines = CoreSystem.Instance.GetCustomBackgroundUpdates();
            //    EditorGUI.indentLevel += 1;
            //    for (int i = 0; i < backgroundRoutines.Count; i++)
            //    {
            //        EditorGUILayout.LabelField(backgroundRoutines[i].ObjectName);
            //    }
            //    EditorGUI.indentLevel -= 1;
            //}

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

#if CORESYSTEM_FMOD
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
#endif
    }
}
