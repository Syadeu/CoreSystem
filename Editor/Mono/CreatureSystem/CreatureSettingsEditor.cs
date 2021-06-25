using UnityEditor;
using Syadeu.Mono.Creature;
using SyadeuEditor.Tree;

namespace SyadeuEditor
{
    [CustomEditor(typeof(CreatureSettings))]
    public sealed class CreatureSettingsEditor : EditorEntity<CreatureSettings>
    {
        private VerticalTreeView m_TreeView;

        private bool m_ShowOriginalContents = false;

        private void OnEnable()
        {
            OnValidate();
        }
        private void OnValidate()
        {
            m_TreeView = new VerticalTreeView(Asset, serializedObject);
            m_TreeView.SetupElements(Asset.PrivateSets, (other) =>
            {
                CreatureSettings.PrivateSet item = (CreatureSettings.PrivateSet)other;

                return new TreeSetElement(m_TreeView, item);
            });
        }
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Creature Settings");
            EditorUtils.SectorLine();

            m_TreeView.OnGUI();

            EditorGUILayout.Space();
            m_ShowOriginalContents = EditorUtils.Foldout(m_ShowOriginalContents, "Original Contents");
            if (m_ShowOriginalContents) base.OnInspectorGUI();
        }

        private class TreeSetElement : VerticalTreeElement<CreatureSettings.PrivateSet>
        {
            public TreeSetElement(VerticalTreeView tree, CreatureSettings.PrivateSet set) : base(tree, set)
            {
                m_Name = set.Name;
            }
            public override void OnGUI()
            {
                Target.m_PrefabIdx = PrefabListEditor.DrawPrefabSelector(Target.m_PrefabIdx);
                Target.m_Values.DrawValueContainer();
            }
        }
    }
}
