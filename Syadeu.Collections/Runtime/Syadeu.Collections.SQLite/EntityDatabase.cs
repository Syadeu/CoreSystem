// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Mono.Data.Sqlite;
using System.IO;
using Unity.Collections;

namespace Syadeu.Collections.SQLite
{
    public struct EntityDatabase
    {
        private FixedString512Bytes m_Path;

        public EntityDatabase(string path)
        {
            m_Path = path;
            if (!Directory.Exists(Path.GetFullPath(path))) Directory.CreateDirectory(Path.GetFullPath(path));
        }
        public void Read()
        {
            string path = m_Path.ConvertToString();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            SqliteCommandBuilder builder = new SqliteCommandBuilder();
            
            using (var con = new SqliteConnection(path))
            using (var adapter = new SqliteDataAdapter(con.CreateCommand()))
            {
                con.Open();

                //adapter.co
            }
        }
    }
}
