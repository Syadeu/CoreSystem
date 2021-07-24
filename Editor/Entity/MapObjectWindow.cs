using Syadeu.Internal;
using Syadeu.Presentation;

namespace SyadeuEditor
{
    public sealed class MapObjectWindow : EditorWindowEntity<MapObjectWindow>
    {
        protected override string DisplayName => "Map Object Window";

        private Reference<MapDataEntity> m_MapData;

        private void OnGUI()
        {
            ReflectionHelperEditor.DrawReferenceSelector("test", (hash) =>
            {
                m_MapData = new Reference<MapDataEntity>(hash);
            }, m_MapData, TypeHelper.TypeOf<MapDataEntity>.Type);
        }
    }
}
