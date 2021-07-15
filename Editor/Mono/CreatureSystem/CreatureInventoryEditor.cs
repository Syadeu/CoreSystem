using Syadeu.Database;
using UnityEditor;
using UnityEngine;
using Syadeu.Mono;
using Syadeu;

#if UNITY_ADDRESSABLES
#endif

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureInventory))]
    public sealed class CreatureInventoryEditor : EditorEntity<CreatureInventory>
    {
        Texture2D m_DefaultTex;
        private void OnEnable()
        {
            InvokeMethod("ValidateData");

            m_DefaultTex = new Texture2D(100, 100);
            for (int i = 0; i < m_DefaultTex.width; i++)
            {
                for (int j = 0; j < m_DefaultTex.height; j++)
                {
                    m_DefaultTex.SetPixel(i, j, Color.gray);
                }
            }
            m_DefaultTex.Apply();
        }
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Add Item"))
            {
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < ItemDataList.Instance.m_Items.Count; i++)
                {
                    Item item = ItemDataList.Instance.m_Items[i];
                    menu.AddItem(new GUIContent(item.Name), false, 
                        () => CreateItemInstance(item));
                }

                Rect rect = GUILayoutUtility.GetLastRect();
                rect.position = Event.current.mousePosition;
                menu.DropDown(rect);
            }

            EditorGUILayout.BeginVertical(EditorUtils.Box);
            EditorUtils.StringHeader("Inventory", 18);
            EditorUtils.Line();
            EditorGUI.indentLevel += 1;
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < Asset.Inventory.Count; i++)
            {
                if (i != 0 && i % 5 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                Texture itemTex;
                if (Asset.Inventory[i].Data != null &&
                    Asset.Inventory[i].Data.m_ImagePath != null &&
                    Asset.Inventory[i].Data.m_ImagePath.editorAsset is Texture tex)
                {
                    //if (Asset.Inventory[i].Data.m_ImagePath.editorAsset != null &&
                    //    Asset.Inventory[i].Data.m_ImagePath.editorAsset is Sprite sprite)
                    //{
                    //    itemTex = sprite.texture;
                    //}
                    //else
                    //{
                    //    EditorGUILayout.LabelField(Asset.Inventory[i].Data.m_ImagePath.editorAsset.GetType().Name);
                    //}
                    //itemTex = Asset.Inventory[i].Data.m_ImagePath.editorAsset.GetType().Name;
                    itemTex = tex;
                }
                else itemTex = m_DefaultTex;

                //EditorGUILayout.TextField("Name: ", Asset.Inventory[i].ToString());
                //if (Asset.Inventory[i].Data != null &&
                //    Asset.Inventory[i].Data.m_ImagePath != null &&
                //    Asset.Inventory[i].Data.m_ImagePath.editorAsset != null)
                //{
                //    EditorGUILayout.LabelField(Asset.Inventory[i].Data.m_ImagePath.editorAsset.GetType().Name);
                //}


                //Sprite sprite = (Sprite)Asset.Inventory[i].Data.m_ImagePath.editorAsset;
                //EditorGUI.DrawTextureTransparent(EditorGUILayout.GetControlRect(false), sprite.texture);


                Rect rect = EditorGUILayout.GetControlRect(false, GUILayout.Height(75), GUILayout.Width(75), GUILayout.MaxHeight(75), GUILayout.MaxWidth(75));
                rect = EditorGUI.IndentedRect(rect);
                rect.width = 75;

                if (EditorGUI.DropdownButton(rect, new GUIContent(itemTex), FocusType.Passive, EditorUtils.Box))
                {
                    "Clicked".ToLog();
                }
                //EditorGUI.DrawTextureTransparent(rect, , ScaleMode.StretchToFill, 2);

                rect.y += rect.height * .3f;
                rect.x -= rect.width * .2f;
                rect.width += 15;
                EditorGUI.LabelField(rect, EditorUtils.String(Asset.Inventory[i].ToString()), EditorUtils.CenterStyle);
                
                //if (Asset.Inventory.Count > i + 1) EditorUtils.Line();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private void CreateItemInstance(Item item)
        {
            Asset.Insert(CreatureInventory.Type.Inventory, item.CreateInstance());
        }

    }
}
