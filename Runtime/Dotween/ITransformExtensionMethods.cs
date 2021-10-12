using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Syadeu.Presentation.Dotween
{
    public static class ITransformExtensionMethods
    {
        private sealed class ITransformDOTweenHelper
        {
            private static int s_Index = 0;

            private readonly int m_Index;
            private ITransform m_Transform;
            private Tweener m_Tweener;

            public ITransformDOTweenHelper()
            {
                m_Index = s_Index;
                s_Index += 1;
            }
            public void DOPosition(ITransform tr, float3 endValue, float duration)
            {
                m_Transform = tr;
                m_Tweener = DOTween.To<float3, float3, Float3Options>(
                      DOTweenPlugins.s_Float3Plugin, GetPosition, SetPosition, endValue, duration);

                int hash = tr.GetHashCode();
                if (!s_Tweens.TryGetValue(hash, out var list))
                {
                    list = new List<Tweener>();
                    s_Tweens.Add(hash, list);
                }
                list.Add(m_Tweener);

                m_Tweener.onKill += Reserve;
            }

            public float3 GetPosition() => m_Transform.position;
            public void SetPosition(float3 position) => m_Transform.position = position;

            public void Reserve()
            {
                int hash = m_Transform.GetHashCode();
                s_Tweens[hash].Remove(m_Tweener);

                m_Transform = null;
                m_Tweener = null;

                PoolContainer<ITransformDOTweenHelper>.Enqueue(this);
            }

            public override int GetHashCode() => m_Index;
        }
        static ITransformExtensionMethods()
        {
            PoolContainer<ITransformDOTweenHelper>.Initialize(Factory, 16);
        }
        private static ITransformDOTweenHelper Factory()
        {
            return new ITransformDOTweenHelper();
        }
        private static readonly Dictionary<int, List<Tweener>> s_Tweens = new Dictionary<int, List<Tweener>>();

        public static void DOMove(this ITransform tr, float3 position, float time)
        {
            ITransformDOTweenHelper temp = PoolContainer<ITransformDOTweenHelper>.Dequeue();
            temp.DOPosition(tr, position, time);
        }
        public static void DOKill(this ITransform tr, bool complete = false)
        {
            if (s_Tweens.TryGetValue(tr.GetHashCode(), out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    list[i].Kill(complete);
                }
            }
        }
    }
}
