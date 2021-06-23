using Syadeu;
using SyadeuEditor.Tree;
#if CORESYSTEM_FMOD
using Syadeu.FMOD;
#endif

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            m_ManagerView = new VerticalTreeView(Asset);

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
                CoreSystem.Instance.OnManagerChanged += ValidateManagerView;
                CoreSystem.Instance.OnRoutineChanged += Instance_OnRoutineChanged;

                ValidateManagerView();
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                CoreSystem.Instance.OnManagerChanged -= ValidateManagerView;
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

        #region ManagerView

        VerticalTreeView m_ManagerView;
        List<IStaticManager> m_Managers = new List<IStaticManager>();
        private class ManagerTreeElement : VerticalTreeElement
        {
            private IStaticManager m_Manager;
            public IStaticManager Manager => m_Manager;

            private string m_Description = null;

            public ManagerTreeElement(VerticalTreeViewEntity tree, IStaticManager manager) : base(tree)
            {
                m_Manager = manager;
                string[] split = manager.ToString().Split('.');
                m_Name = split[split.Length - 1].Trim(')');

                var desc = manager.GetType().GetCustomAttribute<StaticManagerDescriptionAttribute>();
                if (desc != null)
                {
                    m_Description = desc.m_Description;
                }
            }
            public override void OnGUI()
            {
                if (string.IsNullOrEmpty(m_Description)) return;

                EditorGUILayout.LabelField(m_Description);
            }
        }
        private void ValidateManagerView()
        {
            m_Managers.Clear();
            m_Managers.AddRange(CoreSystem.GetStaticManagers());
            m_Managers.AddRange(CoreSystem.GetDataManagers());
            m_Managers.AddRange(CoreSystem.GetInstanceManagers());
            m_ManagerView
                .SetupElements(m_Managers, (other) =>
                {
                    VerticalFolderTreeElement folder; ManagerTreeElement element;
                    if (other is StaticManagerEntity staticMgr)
                    {
                        folder = m_ManagerView.GetOrCreateFolder("Static Manager");
                    }
                    else if (other is IStaticMonoManager monoMgr)
                    {
                        folder = m_ManagerView.GetOrCreateFolder("Mono Manager");
                    }
                    else
                    {
                        folder = m_ManagerView.GetOrCreateFolder("Data Manager");
                    }
                    element = new ManagerTreeElement(m_ManagerView, other as IStaticManager);
                    element.SetParent(folder);

                    return element;
                });
        }

        #endregion

        #region Routine View

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

            public override void OnGUI()
            {
                //throw new System.NotImplementedException();
            }
        }
        private void ValidateRoutineView()
        {
            m_Routines.Clear();
            m_Routines.AddRange(CoreSystem.Instance.GetCustomBackgroundUpdates());
            m_Routines.AddRange(CoreSystem.Instance.GetCustomUpdates());

            m_RoutinesView
                .SetupElements(m_Routines, (other) =>
                {
                    CoreRoutine routine = (CoreRoutine)other;
                    VerticalFolderTreeElement folder; RoutineTreeElement element;
                    if (routine.IsEditor)
                    {
                        folder = m_RoutinesView.GetOrCreateFolder("Editor Routine");
                    }
                    else if (routine.IsBackground)
                    {
                        folder = m_RoutinesView.GetOrCreateFolder("Background Routine");
                    }
                    else
                    {
                        folder = m_RoutinesView.GetOrCreateFolder("Foreground Routine");
                    }
                    element = new RoutineTreeElement(m_RoutinesView, routine);

                    string objPath = routine.ObjectName.Split('+')[0];
                    var topFolder = m_RoutinesView.GetOrCreateFolder(objPath);
                    element.SetParent(topFolder);

                    topFolder.SetParent(folder);
                    return element;
                });
        }

        #endregion

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
            else
            {
                EditorGUILayout.BeginVertical("Box");
                EditorUtils.StringRich("이 시스템은 실행 중에만 정보를 표시합니다", 12, true);
                EditorGUILayout.EndVertical();
            }
        }

        bool m_OpenManagerList = false;
        bool m_OpenInsManagerList = false;
        bool m_OpenDataManagerList = false;
        void Runtime()
        {
            EditorUtils.StringHeader("Generals", 15);
            EditorGUI.indentLevel += 1;

            #region Manager
            m_ManagerView.OnGUI();

            //m_OpenManagerList = EditorUtils.Foldout(m_OpenManagerList, $"현재 생성된 파괴불가 매니저: {CoreSystem.GetStaticManagers().Count}개");
            //if (m_OpenManagerList)
            //{
            //    EditorGUI.indentLevel += 1;
                
            //    for (int i = 0; i < CoreSystem.GetStaticManagers().Count; i++)
            //    {
            //        IStaticMonoManager mgr = CoreSystem.GetStaticManagers()[i] as IStaticMonoManager;
            //        if (mgr.HideInHierarchy)
            //        {
            //            EditorGUI.BeginDisabledGroup(true);
            //            EditorGUILayout.LabelField($"> {CoreSystem.GetStaticManagers()[i].GetType().Name}", new GUIStyle("TextField"));
            //            EditorGUI.EndDisabledGroup();
            //        }
            //        else
            //        {
            //            if (EditorUtils.Button($"> {CoreSystem.GetStaticManagers()[i].GetType().Name}", "TextField", 1))
            //            {
            //                EditorGUIUtility.PingObject(mgr.gameObject);
            //            }
            //        }
            //    }
                
            //    EditorGUI.indentLevel -= 1;
            //}

            //IReadOnlyList<IStaticManager> _insMgrs = CoreSystem.GetInstanceManagers();
            //m_OpenInsManagerList = EditorUtils.Foldout(m_OpenInsManagerList, $"현재 생성된 인스턴스 매니저: {_insMgrs.Count}개");
            //if (m_OpenInsManagerList)
            //{
            //    EditorGUI.indentLevel += 1;

            //    for (int i = 0; i < _insMgrs.Count; i++)
            //    {
            //        IStaticMonoManager mgr = _insMgrs[i] as IStaticMonoManager;
            //        if (mgr.HideInHierarchy)
            //        {
            //            EditorGUI.BeginDisabledGroup(true);
            //            EditorGUILayout.LabelField($"> {_insMgrs[i].GetType().Name}", new GUIStyle("TextField"));
            //            EditorGUI.EndDisabledGroup();
            //        }
            //        else
            //        {
            //            if (EditorUtils.Button($"> {_insMgrs[i].GetType().Name}", "TextField", 1))
            //            {
            //                EditorGUIUtility.PingObject(mgr.gameObject);
            //            }
            //        }
            //    }

            //    EditorGUI.indentLevel -= 1;
            //}

            //IReadOnlyList<IStaticManager> _dataMgrs = CoreSystem.GetDataManagers();
            //EditorUtils.ShowSimpleListLabel(ref m_OpenDataManagerList, 
            //    $"현재 생성된 데이터 매니저: {_dataMgrs.Count}개",
            //    _dataMgrs, new GUIStyle("TextField"), true);
            #endregion

            EditorGUI.indentLevel -= 1;
            EditorUtils.SectorLine();

            EditorUtils.StringHeader("Routines", 15);
            EditorGUI.indentLevel += 1;

            #region Routine

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

            #endregion

            EditorGUI.indentLevel -= 1;
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
