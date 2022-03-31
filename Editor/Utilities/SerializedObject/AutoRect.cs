using UnityEngine;

namespace SyadeuEditor.Utilities
{
    public struct AutoRect
    {
        private readonly Rect m_Original;
        private Rect m_Current;

        public AutoRect(Rect rect)
        {
            m_Original = rect;
            m_Current = rect;
        }

        public void Indent(float pixel)
        {
            m_Current.x += pixel;
            m_Current.width -= pixel;
        }
        public Rect Pop()
        {
            Rect temp = m_Current;
            temp.height = PropertyDrawerHelper.GetPropertyHeight(1);

            m_Current.height -= temp.height;
            m_Current.y += temp.height;

            return temp;
        }
    }
}
