using Syadeu.Database;
using UnityEditor;
using UnityEngine;
using Syadeu.Mono;

#if UNITY_ADDRESSABLES
#endif

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureInventory))]
    public sealed class CreatureInventoryEditor : EditorEntity<CreatureInventory>
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Add Item"))
            {
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < ItemDataList.Instance.m_Items.Count; i++)
                {
                    Item item = ItemDataList.Instance.m_Items[i];
                    menu.AddItem(new GUIContent(item.m_Name), false, 
                        () => CreateItemInstance(item));
                }

                Rect rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;
                menu.DropDown(rect);
            }

            EditorGUILayout.BeginVertical(EditorUtils.Box);
            EditorUtils.StringHeader("Inventory", 15);
            EditorUtils.Line();
            EditorGUI.indentLevel += 1;
            for (int i = 0; i < Asset.Inventory.Count; i++)
            {
                EditorGUILayout.TextField("Name: ", Asset.Inventory[i].ToString());

                if (Asset.Inventory.Count > i + 1) EditorUtils.Line();
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private void CreateItemInstance(Item item)
        {
            Asset.Insert(item.CreateInstance());
        }
    }
}
