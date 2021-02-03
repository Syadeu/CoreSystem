using System.IO;

using Syadeu.Database;
using Syadeu.Extentions.EditorUtils;

using UnityEngine;
using UnityEditor;

namespace SyadeuEditor
{
    public class SQLiteWindow : EditorWindow
    {
        #region Initialize

        static SQLiteWindow window;
        [MenuItem("Syadeu/SQLite/SQLite Window", priority = 200)]
        public static void Initialize()
        {
            if (window == null)
            {
                window = CreateInstance<SQLiteWindow>();
                window.titleContent = new GUIContent("SQLite Viewer");
                window.minSize = new Vector2(1200, 600);
            }

            window.ShowUtility();
        }

        private string[] m_ToolbarNames = new string[]
        {
        "Viewer",
        "Analyzer"
        };
        private int m_SelectedToolbar = 0;

        private static bool m_DatabaseLoaded = false;
        private static string m_DatabasePath = "";
        private static SQLiteDatabase m_Database;

        private void OnEnable()
        {
            m_DatabasePath = Application.streamingAssetsPath;
            if (!Directory.Exists(m_DatabasePath)) Directory.CreateDirectory(m_DatabasePath);
        }

        #endregion

        private int m_SeletedTable = 0;
        private Rect m_TableListRect = new Rect(10, 110, 200, 470);
        private Rect m_TableInfoRect = new Rect(225, 110, 960, 470);
        private Vector2 m_TableListScroll = Vector2.zero;
        private Vector2 m_TableInfoScroll = Vector2.zero;
        private string[] m_TableNames;

        private void OnGUI()
        {
            EditorUtils.StringHeader("SQLite Window");
            EditorUtils.SectorLine();

            #region 데이터 경로 지정 및 닫기
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("데이터 경로: ", m_DatabasePath);
            EditorGUI.EndDisabledGroup();
            if (!m_DatabaseLoaded)
            {
                if (GUILayout.Button("경로 지정"))
                {
                    string temp = EditorUtility.OpenFolderPanel("경로 지정", m_DatabasePath, "");
                    if (!string.IsNullOrEmpty(temp)) m_DatabasePath = temp;
                }
            }
            else
            {
                if (GUILayout.Button("데이터 닫기"))
                {
                    m_DatabaseLoaded = false;
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
            #endregion

            #region 데이터파일 열기
            if (!m_DatabaseLoaded)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("데이터 열기"))
                {
                    if (!m_DatabasePath.Contains(".db"))
                    {
                        string temp = EditorUtility.OpenFilePanel("데이터 열기", m_DatabasePath, "db");
                        if (!string.IsNullOrEmpty(temp)) m_DatabasePath = temp;
                        else return;
                    }

                    string[] split = m_DatabasePath.Split('/');
                    string fileName = split[split.Length - 1].Replace(".db", "");
                    string filePath = m_DatabasePath.Replace(split[split.Length - 1], "");
                    $"{filePath} : {fileName}".ToLog();
                    m_Database = SQLiteDatabase.Initialize(filePath, fileName, true);
                    m_DatabaseLoaded = true;
                    return;
                }
                if (GUILayout.Button("새로 생성하기"))
                {
                    m_Database = SQLiteDatabase.Initialize(m_DatabasePath, "database", true);
                    m_DatabasePath += "/database.db";
                    m_DatabaseLoaded = true;
                }
                EditorGUILayout.EndHorizontal();
            }

            #endregion

            if (!m_DatabaseLoaded)
            {
                EditorUtils.SectorLine();
                return;
            }
            m_SelectedToolbar = GUILayout.Toolbar(m_SelectedToolbar, m_ToolbarNames);
            EditorUtils.SectorLine();

            if (m_Database.Tables.Count == 0)
            {
                EditorGUILayout.LabelField("테이블 정보가 없습니다");
                return;
            }

            BeginWindows();
            switch (m_SelectedToolbar)
            {
                case 0:
                    GUILayout.Window(1, m_TableListRect, TableListWindow, "", "Box");
                    GUILayout.Window(2, m_TableInfoRect, TableInfoWindow, "", "Box");
                    break;
                case 1:
                    break;
                default:
                    break;
            }
            EndWindows();
        }

        private void TableListWindow(int unusedWindowID)
        {
            m_TableListScroll = GUILayout.BeginScrollView(m_TableListScroll, false, false, GUILayout.Width(m_TableListRect.width), GUILayout.Height(m_TableListRect.height));

            EditorGUI.BeginChangeCheck();
            m_SeletedTable = GUILayout.SelectionGrid(m_SeletedTable, m_TableNames, 1);
            if (EditorGUI.EndChangeCheck())
            {
                //ReloadTableContents();
            }

            GUILayout.EndScrollView();
        }
        private void TableInfoWindow(int unusedWindowID)
        {
            m_TableInfoScroll = GUILayout.BeginScrollView(m_TableInfoScroll, false, false, GUILayout.Width(m_TableInfoRect.width), GUILayout.Height(m_TableInfoRect.height));

            EditorUtils.StringHeader(m_TableNames[m_SeletedTable]);


            GUILayout.EndScrollView();
        }
    }
}
