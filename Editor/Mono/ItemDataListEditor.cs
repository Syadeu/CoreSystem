using System;
using System.IO;
using Syadeu.Database;

using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    [CustomEditor(typeof(ItemDataList))]
    public sealed class ItemDataListEditor : EditorEntity<ItemDataList>
    {
        private bool m_ShowOriginalContents = false;

        private static string Path => $"{Application.dataPath}/{ItemDataList.c_ItemDataPath}";
        public static string TypePath => $"{Path}/ItemTypes";
        public static string EffectPath => $"{Path}/ItemEffects";

        private void OnEnable()
        {
            if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);
            if (!Directory.Exists(TypePath)) Directory.CreateDirectory(TypePath);
            if (!Directory.Exists(EffectPath)) Directory.CreateDirectory(EffectPath);


        }

        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Item Datas");
            EditorUtils.SectorLine();

            if (GUILayout.Button("Clear"))
            {
                Asset.m_Items = new Item[0];
                Asset.m_ItemTypes = new ItemType[0];
                Asset.m_ItemEffectTypes = new ItemEffectType[0];
                EditorUtils.SetDirty(target);
            }
            if (GUILayout.Button("Load"))
            {
                Asset.LoadDatas();
                EditorUtils.SetDirty(target);
            }
            if (GUILayout.Button("Save"))
            {
                Asset.SaveDatas();
                EditorUtils.SetDirty(target);
            }

            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }
    }
}
