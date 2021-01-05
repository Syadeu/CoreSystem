using System;
using System.Collections.Generic;
using System.IO;

using Syadeu.FMOD;

using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(FMODLocalizedAudioTable))]
    public class LocalizedAudioTableEditor : Editor
    {
        private GUIStyle headerStyle;

        private void OnEnable()
        {
            headerStyle = new GUIStyle
            {
                richText = true
            };
            AudioTableDataCheck();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField($"<size=20>FMOD Localized Audio Table Data</size>", headerStyle);
            EditorGUILayout.Space();
            EditorGUILayout.ObjectField(label: "Data File: ", FMODLocalizedAudioTable.Instance, typeof(FMODLocalizedAudioTable), false);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load Audio Table"))
            {
                if (FMODLocalizedAudioTable.AudioTableDatas.Length > 0)
                {
                    if (EditorUtility.DisplayDialog("Are you sure reset current audio table datas?",
                    "This will remove and override your current audio table datas", "Confirm", "Cancel"))
                    {
                        AudioTableDatas.Clear();
                        InitializeWithAudioTable();
                        AudioTableDataCheck();
                    }
                }
                else
                {
                    InitializeWithAudioTable();
                    AudioTableDataCheck();
                }
            }
            if (GUILayout.Button("Create Audio Table"))
            {
                if (AudioTableDatas.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("Are you sure reset current audio table datas?",
                    "This will remove and override your current audio table datas", "Confirm"))
                    {
                        AudioTableDatas.Clear();
                        InitializeWithFolder();
                        AudioTableDataCheck();
                    }
                }
                else
                {
                    InitializeWithFolder();
                    AudioTableDataCheck();
                }
            }
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(AudioTablePath) &&
                GUILayout.Button("Save"))
            {
                string path = SaveTableData(DataToString());
                ReadAudioTableData(path);
            }
            if (GUILayout.Button("Refresh"))
            {
                AudioTableDataCheck();
            }
            if (GUILayout.Button("Build"))
            {
                Build();
            }
            EditorGUILayout.LabelField("_______________________________________________________________________");

            DrawAudioTables();

            EditorGUILayout.LabelField("_______________________________________________________________________");

            base.OnInspectorGUI();
        }

        private string AudioTablePath = null;
        private List<AudioTableData> AudioTableDatas = new List<AudioTableData>();
        void ReadAudioTableData(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                do
                {
                    string[] split = reader.ReadLine().Split(',');
                    string name = split[0];
                    if (!string.IsNullOrEmpty(name))
                    {
                        var data = new AudioTableData
                        {
                            Index = AudioTableDatas.Count,
                            Name = name,
                            Path = split[1]
                        };
                        AudioTableDatas.Add(data);
                    }
                } while (!reader.EndOfStream);
            }
        }
        void InitializeWithAudioTable()
        {
            AudioTablePath = EditorUtility.OpenFilePanel("Target Audio Table", "", "txt");
            if (AudioTablePath.Length == 0) return;

            ReadAudioTableData(AudioTablePath);
        }
        void InitializeWithFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Target Audio Folder", "", "");
            if (path.Length == 0) return;

            string audioTable = null;

            // Get all subdirectories
            string[] subdirectoryEntries = Directory.GetDirectories(path);

            // Loop through them to see if they have any other subdirectories
            foreach (string subdirectory in subdirectoryEntries)
                LoadSubDirs(path, subdirectory, ref audioTable);

            string savePath = SaveTableData(audioTable, true);
            ReadAudioTableData(savePath);
        }
        string SaveTableData(string tableData, bool needPath = false)
        {
            string path;
            if (needPath)
            {
                path = EditorUtility.SaveFilePanel("Save Audio Table", AudioTablePath, "keys", "txt");
            }
            else
            {
                path = AudioTablePath;
            }

            if (path.Length != 0) File.WriteAllText(path, tableData);
            return path;
        }
        private void LoadSubDirs(string root, string dir, ref string audioTable)
        {
            string[] subdirectoryEntries = Directory.GetDirectories(dir);
            if (subdirectoryEntries.Length > 0)
            {
                foreach (string subdirectory in subdirectoryEntries)
                {
                    LoadSubDirs(root, subdirectory, ref audioTable);
                }
            }
            else
            {
                string[] audios = Directory.GetFiles(dir);
                for (int i = 0; i < audios.Length; i++)
                {
                    audios[i] = ParseAudioTableString(root, audios[i]);
                }

                if (string.IsNullOrEmpty(audioTable))
                {
                    audioTable += audios[0];
                    for (int i = 1; i < audios.Length; i++)
                    {
                        audioTable += $"\n{audios[i]}";
                    }
                }
                else
                {
                    foreach (var audio in audios)
                    {
                        audioTable += $"\n{audio}";
                    }
                }
            }
        }
        string ParseAudioTableString(string root, string path)
        {
            if (!string.IsNullOrEmpty(root)) path = path.Replace(root, "");
            path = path.Replace("\\", "/");
            path = path.Remove(0, 1);

            string[] split = path.Split('/');
            string name = split[split.Length - 1].Replace(".wav", "");

            return $"{name},{path}";
        }
        string DataToString()
        {
            string tableData = null;
            tableData += $"{AudioTableDatas[0].Name},{AudioTableDatas[0].Path}";
            for (int i = 1; i < AudioTableDatas.Count; i++)
            {
                tableData += $"\n{AudioTableDatas[i].Name},{AudioTableDatas[i].Path}";
            }
            return tableData;
        }

        void Build()
        {
            if (Folders.Count < 1) return;

            var roots = Folders.FindAll((match) => { return match.parent == null; });

            FMODLocalizedAudioTable.AudioTableDatas = new AudioTableRoot[roots.Count];
            for (int i = 0; i < roots.Count; i++)
            {
                var groups = new List<AudioTableGroup>();
                BuildGroup(roots[i], ref groups);
                FMODLocalizedAudioTable.AudioTableDatas[i] = new AudioTableRoot
                {
                    m_Guid = Guid.NewGuid().ToString(),

                    m_Name = roots[i].name,
                    m_Index = roots[i].index,

                    AudioTableGroups = groups.ToArray()
                };
            }
            EditorUtility.SetDirty(FMODLocalizedAudioTable.Instance);
        }
        void BuildGroup(FolderTable root, ref List<AudioTableGroup> groups)
        {
            if (root.childs.Count > 0)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    BuildGroup(root.childs[i], ref groups);
                }
            }
            else
            {
                if (root.datas.Count > 0)
                {
                    AudioTable[] audioTables = new AudioTable[root.datas.Count];
                    for (int i = 0; i < audioTables.Length; i++)
                    {
                        audioTables[i] = new AudioTable
                        {
                            m_Guid = Guid.NewGuid().ToString(),

                            m_Index = root.datas[i].Index,
                            m_Name = root.datas[i].Name
                        };
                    }

                    var tempGroup = new AudioTableGroup
                    {
                        m_Guid = Guid.NewGuid().ToString(),

                        AudioTables = audioTables,
                        m_Index = root.index,
                        m_Name = root.name
                    };
                    groups.Add(tempGroup);
                }
            }
        }
        private int count = 0;
        private readonly List<FolderTable> Folders = new List<FolderTable>();
        private readonly List<bool> showFolders = new List<bool>();

        class AudioTableData
        {
            public int Index;
            public string Name;
            public string Path;
        }
        class FolderTable
        {
            public int index;
            public string name;

            public FolderTable parent;
            public List<FolderTable> childs = new List<FolderTable>();
            public List<AudioTableData> datas = new List<AudioTableData>();
        }
        void AudioTableDataCheck()
        {
            Folders.Clear();
            showFolders.Clear();
            count = AudioTableDatas.Count;
            foreach (var data in AudioTableDatas)
            {
                if (!CreateParents(data))
                {
                    continue;
                }
            }
            foreach (var folder in Folders)
            {
                if (folder.parent != null)
                {
                    folder.parent.childs.Add(folder);
                }
            }
        }
        readonly char[] separator = new char[] { '/' };
        bool CreateParents(AudioTableData data)
        {
            string[] splitFolders = data.Path.Split(separator, 99, StringSplitOptions.RemoveEmptyEntries);
            if (splitFolders.Length < 1) return false;

            FolderTable[] folders = new FolderTable[splitFolders.Length - 1];
            for (int i = 0; i < folders.Length; i++)
            {
                FolderTable exist;
                if (i == 0)
                {
                    exist = Folders.Find((match) =>
                    {
                        return match.name == splitFolders[i] && match.parent == null;
                    });
                }
                else
                {
                    exist = Folders.Find((match) =>
                    {
                        return match.name == splitFolders[i] && match.parent == folders[i - 1];
                    });
                }

                if (exist == null)
                {
                    var tempFolder = new FolderTable
                    {
                        index = count,
                        name = splitFolders[i]
                    };
                    Folders.Add(tempFolder);
                    count += 1;
                    showFolders.Add(false);

                    folders[i] = tempFolder;
                    if (i > 0) folders[i].parent = folders[i - 1];
                }
                else folders[i] = exist;
            }

            folders[folders.Length - 1].datas.Add(data);
            return true;
        }
        void DrawAudioTables()
        {
            if (Folders.Count < 1) return;

            var roots = Folders.FindAll((match) => { return match.parent == null; });

            EditorGUI.indentLevel += 1;
            foreach (var r in roots)
            {
                DrawChildTable(r);
            }
            EditorGUI.indentLevel -= 1;
        }
        void DrawChildTable(FolderTable folder)
        {
            showFolders[folder.index - AudioTableDatas.Count] = EditorGUILayout.Foldout(showFolders[folder.index - AudioTableDatas.Count], $"{folder.name}");
            if (showFolders[folder.index - AudioTableDatas.Count])
            {
                EditorGUI.indentLevel += 1;
                if (folder.childs.Count > 0)
                {
                    foreach (var subFolder in folder.childs)
                    {
                        DrawChildTable(subFolder);
                    }
                }

                if (folder.datas.Count > 0)
                {
                    foreach (var data in folder.datas)
                    {
                        DrawData(data);
                    }
                }
                EditorGUI.indentLevel -= 1;
            }
        }

        void DrawData(AudioTableData data)
        {
            data.Name = EditorGUILayout.TextField($"> Index: {data.Index} ", data.Name);
        }
    }
}
