using Syadeu.Collections;
using Syadeu.Presentation;
using SyadeuEditor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class EntityDataWindow : EntityWindowMenuItem
    {
        public override int Order => 0;
        public override string Name => "Entity";

        public override void OnIntialize(EntityWindow window)
        {
            TreeViewState = new TreeViewState();
            EntityListTreeView = new EntityListTreeView(window, TreeViewState);
            EntityListTreeView.OnSelect += t => Selected = t;
        }

        #region List GUI

        private EntityListTreeView EntityListTreeView;
        private TreeViewState TreeViewState;
        private ObjectBase m_Selected;

        public string SearchString
        {
            get => EntityListTreeView.searchString;
            set => EntityListTreeView.searchString = value;
        }
        public ObjectBase Selected
        {
            get => m_Selected;
            set
            {
                m_Selected = value;
                if (value != null)
                {
                    SelectedDrawer = ObjectBaseDrawer.GetDrawer(value);
                }
                else SelectedDrawer = null;
            }
        }
        public ObjectBaseDrawer SelectedDrawer { get; private set; }

        public void Select(IFixedReference reference)
        {
            EntityListTreeView.SetSelection(reference);
        }
        public void Select(ObjectBase entityObj)
        {
            EntityListTreeView.SetSelection(entityObj);
        }
        public void Add(ObjectBase drawer)
        {
            EntityListTreeView.AddItem(drawer);
            EntityListTreeView.Reload();
        }
        public void Remove(ObjectBase drawer)
        {
            if (Selected != null && Selected.Equals(drawer))
            {
                Selected = null;
            }

            EntityListTreeView.RemoveItem(drawer);
            EntityListTreeView.Reload();
        }
        public void Reload()
        {
            EntityListTreeView.Reload();
        }

        public override void OnListGUI(Rect pos)
        {
            EntityListTreeView.OnGUI(pos);
        }

        #endregion

        Vector2 m_Scroll;

        public override void OnViewGUI(Rect pos)
        {
            using (new GUILayout.AreaScope(pos))
            {
                Draw(pos);
            }
        }
        private void Draw(Rect pos)
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll, true, true,
                GUILayout.MaxWidth(pos.width), GUILayout.MaxHeight(pos.height)))
            {
                m_Scroll = scroll.scrollPosition;

                #region TestRect Controller

                //m_MainWindow.m_CopyrightRect = EditorGUILayout.RectField("copyright", m_MainWindow.m_CopyrightRect);
                //m_MainWindow.HeaderPos = EditorGUILayout.RectField("headerPos", m_MainWindow. HeaderPos);
                //m_MainWindow.HeaderLinePos = EditorGUILayout.RectField("HeaderLinePos", m_MainWindow.HeaderLinePos);
                //m_MainWindow.EntityListPos = EditorGUILayout.RectField("entitylistPos", m_MainWindow. EntityListPos);

                //m_MainWindow.ViewPos = EditorGUILayout.RectField("ViewPos", m_MainWindow.ViewPos);
                //EditorGUILayout.Space();

                #endregion

                using (new EditorUtilities.BoxBlock(ColorPalettes.PastelDreams.Yellow, GUILayout.Width(pos.width - 15)))
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    if (Selected != null)
                    {
                        SelectedDrawer.OnGUI();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("select object");
                    }

                    if (change.changed) Window.IsDirty = true;
                }
            }
        }
    }
}
