using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Collections
{
    public class SQLiteMigrationData : StaticSettingEntity<SQLiteMigrationData>
    {
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
