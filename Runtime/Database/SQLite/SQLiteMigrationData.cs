using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Database
{
    public class SQLiteMigrationData : StaticSettingEntity<SQLiteMigrationData>
    {
#if UNITY_EDITOR
        [MenuItem("Syadeu/SQLite/Create Migration Data", priority = 201)]
        public static void EditSettings()
        {
            Selection.activeObject = Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
#endif

        public List<TextAsset> m_Migrations = new List<TextAsset>();

        public Dictionary<string, string> MigrationData { get; } = new Dictionary<string, string>();

        public void StartSetting()
        {
            for (int i = 0; i < m_Migrations.Count; i++)
            {
                MigrationData.Add(m_Migrations[i].name, m_Migrations[i].text);
            }
        }
    }
}
