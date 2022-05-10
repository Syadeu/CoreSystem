// Copyright 2022 Seung Ha Kim
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

using Syadeu.Collections;
using UnityEngine.UIElements;

namespace Syadeu.Presentation.Render.UI
{
#if CORESYSTEM_DOTWEEN
    using DG.Tweening;
    using DG.Tweening.Core;
    using DG.Tweening.Plugins;
    using DG.Tweening.Plugins.Options;

    public static class UIElementAnimation
    {
        static FloatPlugin FloatPlugin = new FloatPlugin();

        public static void SetHeight(VisualElement element, float value)
        {
            element.style.height = value;
        }

        private sealed class UIElementAnimationDOTweenHelper
        {
            private static int s_Index = 0;

            private readonly int m_Index;
            private VisualElement m_VisualElement;
            private Tweener m_Tweener;

            public UIElementAnimationDOTweenHelper()
            {
                m_Index = s_Index;
                s_Index += 1;
            }
            public TweenerCore<float, float, FloatOptions> DOFade(VisualElement tr, float endValue, float duration)
            {
                int hash = tr.GetHashCode();

                m_VisualElement = tr;

                var temp = DOTween.To(FloatPlugin, GetOpacity, SetOpacity, endValue, duration);
                temp.SetId(hash);

                m_Tweener = temp;

                m_Tweener.onKill += Reserve;
                return temp.SetUpdate(true);
            }
            public TweenerCore<float, float, FloatOptions> DOHeight(VisualElement tr, float endValue, float duration)
            {
                int hash = tr.GetHashCode();

                m_VisualElement = tr;

                var temp = DOTween.To(FloatPlugin, GetHeight, SetHeight, endValue, duration);
                temp.SetId(hash);

                m_Tweener = temp;

                m_Tweener.onKill += Reserve;
                return temp.SetUpdate(true);
            }

            public float GetOpacity() => m_VisualElement.resolvedStyle.opacity;
            public void SetOpacity(float value) => m_VisualElement.style.opacity = value;
            public float GetHeight() => m_VisualElement.resolvedStyle.height;
            public void SetHeight(float value) => m_VisualElement.style.height = value;

            public void Reserve()
            {
                m_VisualElement = null;
                m_Tweener = null;

                PoolContainer<UIElementAnimationDOTweenHelper>.Enqueue(this);
            }

            public override int GetHashCode() => m_Index;
        }
        static UIElementAnimation()
        {
            PoolContainer<UIElementAnimationDOTweenHelper>.Initialize(Factory, 16);
        }
        private static UIElementAnimationDOTweenHelper Factory()
        {
            return new UIElementAnimationDOTweenHelper();
        }

        public static int DOKill(this VisualElement t, bool complete = false)
        {
            return DOTween.Kill(t.GetHashCode(), complete);
        }
        public static TweenerCore<float, float, FloatOptions> DOFade(this VisualElement t, float to, float duration)
        {            
            var temp = PoolContainer<UIElementAnimationDOTweenHelper>.Dequeue();
            return temp.DOFade(t, to, duration);
        }

        public static TweenerCore<float, float, FloatOptions> DOHeight(this VisualElement t, float to, float duration)
        {            
            var temp = PoolContainer<UIElementAnimationDOTweenHelper>.Dequeue();
            return temp.DOHeight(t, to, duration);
        }
    }
#endif
}
