using System.IO;

using Syadeu.Database;
using Syadeu.Extensions.Logs;

using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;

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

        private double m_TotalMemory;

        private void OnEnable()
        {
            m_DatabasePath = Application.streamingAssetsPath;
            if (!Directory.Exists(m_DatabasePath)) Directory.CreateDirectory(m_DatabasePath);
        }

        #endregion

        private int m_SeletedTable = 0;
        private Rect m_TableListRect = new Rect(10, 110, 200, 470);
        private Rect m_TableRightRect = new Rect(225, 110, 960, 470);
        private Vector2 m_TableListScroll = Vector2.zero;
        private Vector2 m_TableInfoScroll = Vector2.zero;
        private Vector2 m_TableAnalyzerScroll = Vector2.zero;
        private string[] m_TableNames;

        private void OnGUI()
        {
            EditorUtils.StringHeader("SQLite Window", StringColor.grey, true);
            EditorUtils.SectorLine();

            #region 데이터 경로 지정 및 닫기
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Path: ", m_DatabasePath);
            EditorGUI.EndDisabledGroup();
            if (!m_DatabaseLoaded)
            {
                if (GUILayout.Button("Set database path"))
                {
                    string temp = EditorUtility.OpenFolderPanel("Set database path", m_DatabasePath, "");
                    if (!string.IsNullOrEmpty(temp)) m_DatabasePath = temp;
                }
            }
            else
            {
                if (GUILayout.Button("Close"))
                {
                    m_DatabaseLoaded = false;
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
            #endregion

            #region Datafile Open
            if (!m_DatabaseLoaded)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open"))
                {
                    if (!m_DatabasePath.Contains(".db"))
                    {
                        string temp = EditorUtility.OpenFilePanel("Open", m_DatabasePath, "db");
                        if (!string.IsNullOrEmpty(temp)) m_DatabasePath = temp;
                        else return;
                    }

                    string[] split = m_DatabasePath.Split('/');
                    string fileName = split[split.Length - 1].Replace(".db", "");
                    string filePath = m_DatabasePath.Replace(split[split.Length - 1], "");
                    //$"{filePath} : {fileName}".ToLog();
                    m_Database = SQLiteDatabase.Initialize(filePath, fileName, true);

                    m_TotalMemory = Marshal.SizeOf(m_Database);
                    m_TableNames = new string[m_Database.Tables.Count];
                    for (int i = 0; i < m_TableNames.Length; i++)
                    {
                        m_TableNames[i] = m_Database.Tables[i].Name;
                        m_TotalMemory += Marshal.SizeOf(m_Database.Tables[i]);
                    }

                    m_TotalMemory /= 1e+6;

                    m_DatabaseLoaded = true;
                    return;
                }
                if (GUILayout.Button("Create New"))
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
                EditorGUILayout.LabelField("No Table infos");
                return;
            }

            BeginWindows();
            switch (m_SelectedToolbar)
            {
                case 0:
                    GUILayout.Window(1, m_TableListRect, TableListWindow, "", "Box");
                    GUILayout.Window(2, m_TableRightRect, TableInfoWindow, "", "Box");
                    break;
                case 1:
                    GUILayout.Window(1, m_TableListRect, TableListWindow, "", "Box");
                    GUILayout.Window(2, m_TableRightRect, TableAnalyzerWindow, "", "Box");
                    break;
                default:
                    break;
            }
            EndWindows();
        }

        private void TableListWindow(int unusedWindowID)
        {
            if (m_TableNames.Length == 0) return;
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
            if (m_TableNames.Length == 0) return;
            m_TableInfoScroll = GUILayout.BeginScrollView(m_TableInfoScroll, false, false, GUILayout.Width(m_TableRightRect.width), GUILayout.Height(m_TableRightRect.height));

            if (m_TableNames.Length < m_SeletedTable) m_SeletedTable = 0;

            EditorUtils.StringHeader(m_TableNames[m_SeletedTable]);
            EditorUtils.SectorLine();

            EditorUtils.StringRich("작업중", true);

            GUILayout.EndScrollView();
        }
        private void TableAnalyzerWindow(int unusedWindowID)
        {
            if (m_TableNames.Length == 0) return;

            EditorUtils.StringRich("Global Infomation", 20, StringColor.grey);
            EditorUtils.SectorLine();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("File size: ", $"{new FileInfo(m_DatabasePath).Length / 1e+6} Mb");
            EditorGUILayout.TextField("Require Minimum Memory: ", $"{m_TotalMemory} Mb");
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            string databaseVersion = m_Database.GetVersion();
            string versionInfo;
            StringColor versionInfoColor;
            if (string.IsNullOrEmpty(databaseVersion))
            {
                versionInfo = "No Version Data Found";
                versionInfoColor = StringColor.maroon;
            }
            else
            {
                if (databaseVersion.Equals(Application.version))
                {
                    versionInfo = "Normal";
                    versionInfoColor = StringColor.teal;
                }
                else
                {
                    versionInfo = "Verion Not Match";
                    versionInfoColor = StringColor.maroon;
                }
            }
            EditorUtils.StringRich(versionInfo, versionInfoColor, true);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Application Version: ", $"{Application.version}");
            EditorGUILayout.TextField("Database Version: ", 
                string.IsNullOrEmpty(databaseVersion) ? "No Version Data Found" : $"{databaseVersion}");
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            
            EditorUtils.SectorLine();

            EditorUtils.StringHeader($"{m_TableNames[m_SeletedTable]} :: <size=13>Analyzer</size>");
            EditorUtils.SectorLine();

            m_TableAnalyzerScroll = GUILayout.BeginScrollView(m_TableAnalyzerScroll, false, false, GUILayout.Width(m_TableRightRect.width), GUILayout.Height(m_TableRightRect.height * .5f));

            SQLiteTable selectedTable = m_Database.Tables[m_SeletedTable];
            EditorGUILayout.BeginHorizontal();
            EditorUtils.StringRich($"Name: ", StringColor.grey);
            for (int i = 0; i < selectedTable.Columns.Count; i++)
            {
                EditorUtils.StringRich($"{selectedTable.Columns[i].Name}", StringColor.grey);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorUtils.StringRich($"Type: ", StringColor.grey);
            for (int i = 0; i < selectedTable.Columns.Count; i++)
            {
                EditorUtils.StringRich($"{selectedTable.Columns[i].Type.Name}", StringColor.grey);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }
    }
}
